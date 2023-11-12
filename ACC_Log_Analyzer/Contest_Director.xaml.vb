Imports NmeaParser
Imports Microsoft.Maps.MapControl.WPF
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Public Class Contest_Director
    Implements INotifyPropertyChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
    Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Property Competition As ACC.Competition
        Get
            Return DataContext.Competition
        End Get
        Set(value As ACC.Competition)
            DataContext.Competition = value
        End Set
    End Property
    Public Property Model() As OxyPlot.PlotModel
        Get
            Try
                Return DataContext.Model
            Catch ex As Exception
                Return Nothing
            End Try

        End Get
        Set(value As OxyPlot.PlotModel)
            DataContext.Model = value
        End Set
    End Property

    Public Property DC As MainViewModel
        Get
            Return DataContext
        End Get
        Set(value As MainViewModel)
            DataContext = value
        End Set
    End Property



    Private Sub OpenContestFile(sender As Object, e As RoutedEventArgs)


        Try

            Reset()

            Competition.LoadCompetitionXML()

            DataGrid_Teams.Items.Refresh()
            DC.Competition.UpdateFlightsinTeam()
        Catch
        End Try




        ''Reset()

        'Dim resetflag = Competition.LoadCompetitionXML()
        'Dim CompCache = Competition
        'Dim TeamsCache = Teams
        'If resetflag Then
        '    DataContext = New MainViewModel
        '    'If Teams IsNot Nothing Then Teams.Clear()
        'End If

        'DC.Competition = CompCache
        ''Teams = Competition.Teams


        'DataGrid_Teams.Items.Refresh()
    End Sub

    Private Sub Contest_Director_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Try
            If My.Settings IsNot Nothing Then
                Reset()
                If My.Settings.DefaultCompetitionFile <> "" Then
                    Competition.LoadCompetitionXML(My.Settings.DefaultCompetitionFile)
                End If
                DataGrid_Teams.Items.Refresh()
                DC.Competition.UpdateFlightsinTeam()
            End If

        Catch
        End Try


    End Sub

    Private Sub SaveContestFile(Optional sender As Object = Nothing, Optional e As RoutedEventArgs = Nothing)

        If sender IsNot Nothing Then
            If sender.Tag Is Nothing Then
                Competition.SaveCompetitionXML()
                Return
            End If

        End If
        Competition.SaveCompetitionXML(My.Settings.DefaultCompetitionFile)

    End Sub

    Private Sub Reset()

        DataContext = New MainViewModel
        If Teams IsNot Nothing Then Teams.Clear()


    End Sub

    Private Sub NewContest(sender As Object, e As RoutedEventArgs)

        Competition.Teams = New List(Of ACC.Team)
        Competition.Rounds = New List(Of ACC.Competition.Round)
        Competition.SaveName = Nothing
        checkbox_applicationopen.IsChecked = True
    End Sub

    Private Sub OpenTeamsFile(sender As Object, e As RoutedEventArgs)
        LoadTeamsXML()
    End Sub

    Private Sub SaveTeamsFile(sender As Object, e As RoutedEventArgs)
        SaveTeamsXML()
    End Sub



    Private Property datagrid_rounds_refresh_running As Boolean = False
    Private Async Function datagrid_rounds_refresh(sender As Object, e As Object) As Task


        Dim cc = 0
        While datagrid_rounds_refresh_running And cc < 10000
            Await Task.Delay(100)
            cc += 1
        End While


        datagrid_rounds_refresh_running = True
        RefreshDataGrids()
        DC.Competition.UpdateFlightsinTeam()
        Debug.WriteLine("refresh")

        datagrid_rounds_refresh_running = False

    End Function



    Private Sub DataGrid_Flights_refresh(sender As Object, e As RoutedEventArgs)
        RefreshDataGrids()
        Dim R As ACC.Competition.Round = sender.DataContext
        If R Is Nothing Then Return
        R.UpdateScores()
        DC.Competition.UpdateFlightsinTeam()
    End Sub

    Private Async Sub DataGrid_Flights_SelectedCellsChanged(sender As Object, e As SelectedCellsChangedEventArgs)
        Try
            Dim F As ACC.Flight

            Select Case TypeName(sender)
                Case "Button"
                    F = sender.DataContext
                Case Else
                    F = sender.SelectedItem

            End Select

            myMap.Children.Clear()
            Model = New OxyPlot.PlotModel
            'Model.Series.Clear()
            'Model.Legends.Clear()

            If F Is Nothing Then Return

            'If F.EvaluatingGGAs Then Return

            While F.EvaluatingGGAs
                Await Task.Delay(10)
            End While

            If F.GGAs Is Nothing Then Return
            If F.GGAs.Count = 0 Then Return




            Dim plot_altitude_setup As New List(Of OxyPlot.DataPoint)
            Dim plot_altitude_climb As New List(Of OxyPlot.DataPoint)
            Dim plot_altitude_cruise As New List(Of OxyPlot.DataPoint)
            Dim plot_altitude_landing As New List(Of OxyPlot.DataPoint)
            Dim plot_velocity_all As New List(Of OxyPlot.DataPoint)
            Dim plot_velocity_vtg As New List(Of OxyPlot.DataPoint)
            'Dim plot_positionunsafe As New List(Of OxyPlot.DataPoint)

            Dim Series_positionunsafe As New OxyPlot.Series.ScatterSeries With {.Title = "Unsave Indication HDOP > 2m", .MarkerFill = OxyPlot.OxyColors.Red, .MarkerType = OxyPlot.MarkerType.Diamond}


            Dim Locs_setup = New LocationCollection()
            Dim Locs_climb = New LocationCollection()
            Dim Locs_cruise = New LocationCollection()
            Dim Locs_landing = New LocationCollection()
            Dim PL_setup As New MapPolyline With {.Stroke = Brushes.CornflowerBlue, .StrokeThickness = 2, .Opacity = 0.4}
            Dim PL_climb As New MapPolyline With {.Stroke = Brushes.Blue, .StrokeThickness = 2, .Opacity = 1}
            Dim PL_cruise As New MapPolyline With {.Stroke = Brushes.YellowGreen, .StrokeThickness = 2, .Opacity = 1}
            Dim PL_landing As New MapPolyline With {.Stroke = Brushes.LightSteelBlue, .StrokeThickness = 2, .Opacity = 1}

            For n = Math.Max(F.IndexTakeoff - 300, 0) To F.IndexTakeoff - 1
                plot_altitude_setup.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).Altitude - F.GGAs(F.IndexTakeoff).Altitude))
                Locs_setup.Add(New Location With {.Latitude = F.GGAs(n).Latitude, .Longitude = F.GGAs(n).Longitude, .Altitude = F.GGAs(n).Altitude})
                If n > 0 Then
                    Dim deltatime = F.GGAs(n).FixTime.TotalSeconds - F.GGAs(n - 1).FixTime.TotalSeconds
                    Dim distpoints = Geolocation.GeoCalculator.GetDistance(F.GGAs(n - 1).Latitude, F.GGAs(n - 1).Longitude, F.GGAs(n).Latitude, F.GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    Dim currentspeed = distpoints / deltatime

                    plot_velocity_all.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed * 3.6))

                    currentspeed = GetSmoothedSpeed(F, n, 10)

                    plot_velocity_vtg.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed))
                    If F.GGAs(n).UnsafeFix Then
                        Series_positionunsafe.Points.Add(New OxyPlot.Series.ScatterPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, -10))
                        'plot_positionunsafe.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).UnsafeFix))
                    End If
                End If

            Next


            For n = F.IndexTakeoff To F.Index60s
                plot_altitude_climb.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).Altitude - F.GGAs(F.IndexTakeoff).Altitude))
                Locs_climb.Add(New Location With {.Latitude = F.GGAs(n).Latitude, .Longitude = F.GGAs(n).Longitude, .Altitude = F.GGAs(n).Altitude})
                If n > 0 Then
                    Dim deltatime = F.GGAs(n).FixTime.TotalSeconds - F.GGAs(n - 1).FixTime.TotalSeconds
                    Dim distpoints = Geolocation.GeoCalculator.GetDistance(F.GGAs(n - 1).Latitude, F.GGAs(n - 1).Longitude, F.GGAs(n).Latitude, F.GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    Dim currentspeed = distpoints / deltatime

                    plot_velocity_all.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed * 3.6))
                    currentspeed = F.GGAs(n).VTG_overground

                    currentspeed = GetSmoothedSpeed(F, n, 10)

                    plot_velocity_vtg.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed))
                    If F.GGAs(n).UnsafeFix Then
                        Series_positionunsafe.Points.Add(New OxyPlot.Series.ScatterPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, -10))
                        'plot_positionunsafe.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).UnsafeFix))
                    End If
                End If

            Next

            myMap.Center = New Location With {.Latitude = F.GGAs(F.Index60s).Latitude, .Longitude = F.GGAs(F.Index60s).Longitude, .Altitude = F.GGAs(F.Index60s).Altitude}
            myMap.ZoomLevel = 16

            For n = F.Index60s + 1 To F.Index180s
                If n > F.GGAs.Count - 1 Then Exit For
                plot_altitude_cruise.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).Altitude - F.GGAs(F.IndexTakeoff).Altitude))
                Locs_cruise.Add(New Location With {.Latitude = F.GGAs(n).Latitude, .Longitude = F.GGAs(n).Longitude, .Altitude = F.GGAs(n).Altitude})
                If n > 0 Then
                    Dim deltatime = F.GGAs(n).FixTime.TotalSeconds - F.GGAs(n - 1).FixTime.TotalSeconds
                    Dim distpoints = Geolocation.GeoCalculator.GetDistance(F.GGAs(n - 1).Latitude, F.GGAs(n - 1).Longitude, F.GGAs(n).Latitude, F.GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    Dim currentspeed = distpoints / deltatime



                    plot_velocity_all.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed * 3.6))

                    currentspeed = F.GGAs(n).VTG_overground

                    currentspeed = GetSmoothedSpeed(F, n, 10)

                    plot_velocity_vtg.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed))
                    If F.GGAs(n).UnsafeFix Then
                        Series_positionunsafe.Points.Add(New OxyPlot.Series.ScatterPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, -10))
                        'plot_positionunsafe.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).UnsafeFix))
                    End If
                End If
            Next

            For n = F.Index180s + 1 To Math.Min(F.GGAs.Count - 2, F.Index180s + 1200)
                plot_altitude_landing.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).Altitude - F.GGAs(F.IndexTakeoff).Altitude))
                Locs_landing.Add(New Location With {.Latitude = F.GGAs(n).Latitude, .Longitude = F.GGAs(n).Longitude, .Altitude = F.GGAs(n).Altitude})
                If n > 0 Then
                    Dim deltatime = F.GGAs(n).FixTime.TotalSeconds - F.GGAs(n - 1).FixTime.TotalSeconds
                    Dim distpoints = Geolocation.GeoCalculator.GetDistance(F.GGAs(n - 1).Latitude, F.GGAs(n - 1).Longitude, F.GGAs(n).Latitude, F.GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    Dim currentspeed = distpoints / deltatime

                    plot_velocity_all.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed * 3.6))
                    currentspeed = F.GGAs(n).VTG_overground

                    'If n > 3 And n < F.GGAs.Count - 3 Then
                    '    currentspeed = (F.GGAs(n - 2).VTG_overground + F.GGAs(n - 1).VTG_overground + F.GGAs(n).VTG_overground + F.GGAs(n + 1).VTG_overground + F.GGAs(n + 2).VTG_overground) / 5
                    'End If

                    currentspeed = GetSmoothedSpeed(F, n, 10)

                    plot_velocity_vtg.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, currentspeed))
                    If F.GGAs(n).UnsafeFix Then
                        Series_positionunsafe.Points.Add(New OxyPlot.Series.ScatterPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, -10))
                        'plot_positionunsafe.Add(New OxyPlot.DataPoint(F.GGAs(n).FixTime.TotalSeconds - F.GGAs(F.IndexTakeoff).FixTime.TotalSeconds, F.GGAs(n).UnsafeFix))
                    End If
                End If
            Next



            Model.Legends.Add(New OxyPlot.Legends.Legend) '= "Legend"
            Model.Legends.Last.LegendPosition = OxyPlot.Legends.LegendPosition.RightTop
            Model.Legends.Last.LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside
            Model.Axes.Add(New OxyPlot.Axes.LinearAxis With {.MajorGridlineStyle = OxyPlot.LineStyle.Dot, .Title = "Altitude [m] or Speed [km/h]", .Position = OxyPlot.Axes.AxisPosition.Left})
            'Model.Axes.Add(New OxyPlot.Axes.LinearAxis With {.MajorGridlineStyle = OxyPlot.LineStyle.Dot, .Title = "Altitude [m] or Speed [km/h]", .Position = OxyPlot.Axes.AxisPosition.Right})
            Model.Axes.Add(New OxyPlot.Axes.LinearAxis With {.MajorGridlineStyle = OxyPlot.LineStyle.Dot, .Title = "Time [s]", .Position = OxyPlot.Axes.AxisPosition.Bottom})


            Dim Series_setup As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_altitude_setup, .Title = "Alt. Setup [m]", .Color = OxyPlot.OxyColors.CornflowerBlue}
            Model.Series.Add(Series_setup)

            Dim Series_altitude As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_altitude_climb, .Title = "Alt. Climb [m]", .Color = OxyPlot.OxyColors.Blue}
            Model.Series.Add(Series_altitude)

            Dim Series_speed As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_altitude_cruise, .Title = "Alt. Cruise [m]", .Color = OxyPlot.OxyColors.YellowGreen}
            Model.Series.Add(Series_speed)

            Dim Series_landing As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_altitude_landing, .Title = "Alt. Landing [m]", .Color = OxyPlot.OxyColors.LightSteelBlue}
            Model.Series.Add(Series_landing)

            Dim Series_velocity As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_velocity_all, .Title = "Speed calculated [km/h]", .Color = OxyPlot.OxyColors.BlueViolet}
            'Model.Series.Add(Series_velocity)

            Dim Series_velocity_vtg As New OxyPlot.Series.LineSeries With {.ItemsSource = plot_velocity_vtg, .Title = "Speed [km/h]", .Color = OxyPlot.OxyColors.DarkGray}
            Model.Series.Add(Series_velocity_vtg)

            Model.Series.Add(Series_positionunsafe)

            'If Series_positionunsafe.ActualPoints.Count > 0 Then

            'End If

            DC.NotifyPropertyChanged(NameOf(Model))

            PL_climb.Locations = Locs_climb
            PL_cruise.Locations = Locs_cruise
            PL_landing.Locations = Locs_landing
            PL_setup.Locations = Locs_setup

            myMap.Children.Add(PL_climb)
            myMap.Children.Add(PL_cruise)
            myMap.Children.Add(PL_landing)
            myMap.Children.Add(PL_setup)

            myMap.Mode = New AerialMode()



        Catch ex As Exception
            Debug.WriteLine(ex.Message)
        End Try


    End Sub


    Private Sub DeleteSelectedRound(sender As Object, e As RoutedEventArgs)
        Competition.Rounds.Remove(datagrid_rounds.SelectedItem)

        RefreshDataGrids()
    End Sub

    Private Sub NewRoundEmpty(sender As Object, e As RoutedEventArgs)
        Competition.Rounds.Add(New ACC.Competition.Round With {.Number = Competition.Rounds.Count + 1})
        RefreshDataGrids()
    End Sub

    Private Sub NewRoundTeams(sender As Object, e As RoutedEventArgs)

        Dim NR As New ACC.Competition.Round With {.Number = Competition.Rounds.Count + 1}
        Dim cc = 0
        For Each T In Teams
            cc += 1
            NR.Flights.Add(New ACC.Flight With {.TeamNumber = T.Number, .Number = cc})
        Next

        Competition.Rounds.Add(NR)
        RefreshDataGrids()
    End Sub

    Private Sub RefreshDataGrids()
        Try
            datagrid_rounds.Items.Refresh()

        Catch ex As Exception

        End Try

        Try
            DataGrid_Flights.Items.Refresh()
        Catch ex As Exception

        End Try
    End Sub

    Private Sub NewRoundShuffle(sender As Object, e As RoutedEventArgs)
        If Teams.Count = 0 Then Return
        Dim A(Teams.Count - 1) As Integer

        For n = 0 To A.Length - 1
            A(n) = Teams(n).Number
        Next

        RandomizeArray(A)

        Dim NR As New ACC.Competition.Round With {.Number = Competition.Rounds.Count + 1}
        Dim cc = 0
        For n = 0 To A.Length - 1
            cc += 1
            NR.Flights.Add(New ACC.Flight With {.TeamNumber = A(n), .Number = cc})
        Next
        Competition.Rounds.Add(NR)
        RefreshDataGrids()
    End Sub

    Private Sub NewRoundScoreDescend(sender As Object, e As RoutedEventArgs)
        For Each R In DC.Competition.Rounds
            R.UpdateScores()
        Next
        DC.Competition.UpdateFlightsinTeam()

        Dim cacheTeams = Teams.OrderBy(Function(x) x.OverallScore).ToList
        cacheTeams.Reverse()

        Dim NR As New ACC.Competition.Round With {.Number = Competition.Rounds.Count + 1}
        Dim cc = 0
        For Each T In cacheTeams
            cc += 1
            NR.Flights.Add(New ACC.Flight With {.TeamNumber = T.Number, .Number = cc})
        Next

        Competition.Rounds.Add(NR)
        RefreshDataGrids()

        Teams.OrderBy(Function(x) x.Number).ToList

    End Sub

    Private Sub NewRoundScoreAscend(sender As Object, e As RoutedEventArgs)
        For Each R In DC.Competition.Rounds
            R.UpdateScores()
        Next
        DC.Competition.UpdateFlightsinTeam()

        Dim cacheTeams = Teams.OrderBy(Function(x) x.OverallScore).ToList

        Dim NR As New ACC.Competition.Round With {.Number = Competition.Rounds.Count + 1}
        Dim cc = 0
        For Each T In cacheTeams
            cc += 1
            NR.Flights.Add(New ACC.Flight With {.TeamNumber = T.Number, .Number = cc})
        Next

        Competition.Rounds.Add(NR)
        RefreshDataGrids()

        Teams.OrderBy(Function(x) x.Number).ToList

    End Sub

    Private Async Sub AddNMEAtoFlight(sender As Object, e As RoutedEventArgs)
        Dim F As ACC.Flight = sender.DataContext
        Await F.ImportNMEA()
        DataGrid_Flights_SelectedCellsChanged(sender, Nothing)
        SaveContestFile()
    End Sub

    Private Sub RemoveNMEAfromFlight(sender As Object, e As RoutedEventArgs)
        Dim F As ACC.Flight = sender.DataContext
        F.RemoveNMEA()
        F.NMEA_Filename = ""
        F.ClearGGAs()
        DataGrid_Flights_SelectedCellsChanged(sender, Nothing)
    End Sub

    Private Sub ChangeTab(sender As Object, e As RoutedEventArgs)
        tabcontrol.SelectedIndex = CInt(sender.Tag)
    End Sub

    'Private Sub Constest_Director_PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Handles Me.PropertyChanged
    '    datagrid_rounds_refresh(Nothing, Nothing)
    '    Competition.SaveCompetitionXML()
    'End Sub

    Private Sub CheckTool(sender As Object, e As RoutedEventArgs)

        Dim Teswindow As New MainWindow
        Teswindow.Show()

    End Sub

    Private Sub tabcontrol_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles tabcontrol.SelectionChanged
        'datagrid_rounds_refresh(Nothing, Nothing)
        'Competition.SaveCompetitionXML()
    End Sub

    Private Sub Addnewflighttoround(sender As Object, e As RoutedEventArgs)
        On Error Resume Next
        CType(DataGrid_Flights.ItemsSource, List(Of ACC.Flight)).Add(New ACC.Flight With {.Number = DataGrid_Flights.Items.Count + 1})
        datagrid_rounds_refresh(Nothing, Nothing)
    End Sub

    Private Sub Removeselectedflightfromround(sender As Object, e As RoutedEventArgs)
        On Error Resume Next
        CType(DataGrid_Flights.ItemsSource, List(Of ACC.Flight)).Remove(DataGrid_Flights.SelectedItem)
        datagrid_rounds_refresh(Nothing, Nothing)
    End Sub

    Private Sub DeleteSelectedPenalty(sender As Object, e As RoutedEventArgs)
        On Error Resume Next
        CType(datagridpenalties.ItemsSource, List(Of ACC.Penalty)).Remove(CType(datagridpenalties.SelectedItem, ACC.Penalty))
        ThisCompetition.NotifyPropertyChanged(NameOf(ThisCompetition.Penalties))
        datagrid_rounds_refresh(Nothing, Nothing)

        datagridpenalties.Items.Refresh()

    End Sub




    'Private Sub oxygraph_PreviewMouseMove(sender As Object, e As MouseEventArgs) Handles oxygraph.PreviewMouseMove

    '    If e.LeftButton Then
    '        Debug.WriteLine(Now)
    '        e.Handled = False
    '    End If

    'End Sub

    'Private Sub oxygraph_PreviewMouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles oxygraph.PreviewMouseLeftButtonUp
    '    Debug.WriteLine("end")


    '    Dim pp As New Pushpin
    '    pp.Location = New Location With {}

    '    myMap.Children.Add(pp)

    'End Sub


End Class
