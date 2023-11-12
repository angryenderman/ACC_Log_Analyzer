Imports NmeaParser
Imports Microsoft.Maps.MapControl.WPF
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Class MainWindow


    Implements INotifyPropertyChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Sub New()

        ' Dieser Aufruf ist für den Designer erforderlich.
        InitializeComponent()
        mmodel = New OxyPlot.PlotModel

        mmodel.Axes.Add(New OxyPlot.Axes.LinearAxis With {.Position = OxyPlot.Axes.AxisPosition.Bottom})
        mmodel.Axes.Add(New OxyPlot.Axes.LinearAxis With {.Position = OxyPlot.Axes.AxisPosition.Left})
        mmodel.Legends.Add(New OxyPlot.Legends.Legend With {.LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside, .LegendPosition = OxyPlot.Legends.LegendPosition.TopRight})

    End Sub

    Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
        Try
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        Catch
        End Try
    End Sub

    Public Property Locs As New LocationCollection()
    Public Property nmeamessage As String = ""

    Private Async Sub xxx()
        Dim device = New NmeaFileDevice("C:\Users\Alex\Desktop\2020-05-16 SM GPS 2 Logdatei 0002.nmea", 1)
        AddHandler device.MessageReceived, AddressOf device_NmeaMessageReceived

        Await device.OpenAsync()
    End Sub

    Private Sub device_NmeaMessageReceived(ByVal sender As Object, ByVal args As NmeaMessageReceivedEventArgs)

        Dim gga As Messages.Gga = TryCast(args.Message, Messages.Gga)
        nmeamessage = args.Message.ToString
        NotifyPropertyChanged(NameOf(nmeamessage))
        If gga IsNot Nothing Then
            Application.Current.Dispatcher.Invoke(Sub()
                                                      Locs.Add(New Location With {.Latitude = gga.Latitude, .Longitude = gga.Longitude, .Altitude = gga.Altitude})
                                                      NotifyPropertyChanged(NameOf(Locs))
                                                  End Sub)
        End If
    End Sub


    Property Model() As OxyPlot.PlotModel
        Get
            Return mmodel
        End Get
        Set(value As OxyPlot.PlotModel)
            mmodel = value
            NotifyPropertyChanged()
        End Set
    End Property


    Private mmodel As New OxyPlot.PlotModel
    Private Property _SixtySecAltitude_ As Double = 0
    Public Property SixtySecAltitude As String
        Get
            Return Math.Round(_SixtySecAltitude_, 2).ToString & " m"
        End Get
        Set(value As String)
            _SixtySecAltitude_ = CDbl(value)
            NotifyPropertyChanged()
            NotifyPropertyChanged(NameOf(SixtySecPreScore))
        End Set
    End Property

    Public ReadOnly Property SixtySecPreScore As Double
        Get
            Return Math.Round(Math.Pow(_SixtySecAltitude_, 4) * -0.0000392 + Math.Pow(_SixtySecAltitude_, 3) * 0.0108 + Math.Pow(_SixtySecAltitude_, 2) * -1.156 + _SixtySecAltitude_ * 64.2 - 537, 2)
        End Get
    End Property
    Private Property _CruiseDistance_ As Double = 0
    Public Property CruiseDistance As Double
        Set(value As Double)
            _CruiseDistance_ = value
            NotifyPropertyChanged()
        End Set
        Get
            Return Math.Round(_CruiseDistance_, 2)
        End Get
    End Property


    Public Property plottest_altitude As New List(Of OxyPlot.DataPoint)
    Public Property plottest_speed As New List(Of OxyPlot.DataPoint)
    Public Property plottest_distance As New List(Of OxyPlot.DataPoint)

    Private Async Function test2(Optional filepath As String = "C:\Users\Alex\Desktop\2020-05-16 SM GPS 2 Logdatei 0002.nmea") As Task

        myMap.Children.Clear()
        Model.Series.Clear()
        plottest_altitude.Clear()
        plottest_speed.Clear()
        plottest_distance.Clear()

        Dim PL As New MapPolyline With {.Stroke = Brushes.Blue, .StrokeThickness = 1, .Opacity = 0.7}

        Locs = New LocationCollection()

        Dim reader As New IO.StreamReader(filepath, Text.Encoding.ASCII)

        Dim Timestamps As New List(Of Double)

        Dim cc = 0

        Dim GGAs As New List(Of Messages.Gga)
        Dim RMCs As New List(Of Messages.Rmc)

        Dim takeoffdetected = True
        Dim landingdetected = False

        Dim distanceintegrator As Double = 0

        While Not reader.EndOfStream

            Dim line = Await reader.ReadLineAsync
            If line.Contains("$SMGPS") Then Continue While
            Dim message As Messages.NmeaMessage

            message = Messages.NmeaMessage.Parse(line)

            Dim gga As Messages.Gga = TryCast(message, Messages.Gga)
            Dim rmc As Messages.Rmc = TryCast(message, Messages.Rmc)
            'Dim gll As Messages.Gll = TryCast(message, Messages.Gll)
            If rmc IsNot Nothing Then
                If rmc.Speed >= 5 And Not takeoffdetected And Not landingdetected Then
                    takeoffdetected = True
                End If
                If rmc.Speed < 4 And takeoffdetected Then
                    'landingdetected = True
                End If



                If takeoffdetected Then
                    RMCs.Add(rmc)
                End If
            End If

            If gga IsNot Nothing Then

                If GGAs.Count > 0 Then
                    'If landingdetected And gga.Altitude - GGAs(0).Altitude < 5 And takeoffdetected Then
                    'If gga.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds > 180 Then
                    'takeoffdetected = False
                    'End If


                    'End If
                    Dim distpoints As Double = 0
                    Dim deltatime As Double = Double.MaxValue

                    If gga.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds > 60 And _SixtySecAltitude_ = 0 Then
                        SixtySecAltitude = gga.Altitude - GGAs(0).Altitude
                    End If
                    'If gga.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds > 60 And gga.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds <= 180 Then
                    deltatime = gga.FixTime.TotalSeconds - GGAs.Last.FixTime.TotalSeconds
                        distpoints = Geolocation.GeoCalculator.GetDistance(GGAs.Last.Latitude, GGAs.Last.Longitude, gga.Latitude, gga.Longitude, 5, Geolocation.DistanceUnit.Meters)

                        distanceintegrator += distpoints
                    'End If
                    plottest_distance.Add(New OxyPlot.DataPoint(gga.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds, distpoints / deltatime * 3.6))


                End If

                If takeoffdetected Then
                    cc += 1
                    If cc = 1 Then
                        myMap.Center = New Location With {.Latitude = gga.Latitude, .Longitude = gga.Longitude, .Altitude = gga.Altitude}
                        myMap.ZoomLevel = 15
                    End If

                    Locs.Add(New Location With {.Latitude = gga.Latitude, .Longitude = gga.Longitude, .Altitude = gga.Altitude})

                    nmeamessage = line
                    NotifyPropertyChanged(NameOf(nmeamessage))

                    GGAs.Add(gga)

                    'Timestamps.Add(gga.Timestamp)
                End If




            End If



            'Await Task.Delay(0.1)

        End While

        CruiseDistance = distanceintegrator

        For Each g In GGAs
            plottest_altitude.Add(New OxyPlot.DataPoint(g.FixTime.TotalSeconds - GGAs(0).FixTime.TotalSeconds, g.Altitude - GGAs(0).Altitude))
        Next
        For Each r In RMCs
            plottest_speed.Add(New OxyPlot.DataPoint((r.FixTime.DateTime - RMCs(0).FixTime.DateTime).TotalSeconds, r.Speed * 1.852))
        Next

        'NotifyPropertyChanged(NameOf(plottest_altitude))


        'Model = New OxyPlot.PlotModel With {.Title = "Example 1"}

        Dim Series_altitude As New OxyPlot.Series.LineSeries With {.ItemsSource = plottest_altitude}
        'Series_altitude.LegendKey = "Altitude [" & GGAs(0).AltitudeUnits & "]"
        Series_altitude.Title = "Altitude"

        Model.Series.Add(Series_altitude)

        Dim Series_speed As New OxyPlot.Series.LineSeries With {.ItemsSource = plottest_speed}
        Series_speed.Title = "Speed"
        Model.Series.Add(Series_speed)

        'Dim Series_distance As New OxyPlot.Series.LineSeries With {.ItemsSource = plottest_distance}
        'Series_distance.Title = "Distance"
        'Model.Series.Add(Series_distance)


        NotifyPropertyChanged(NameOf(Model))


        PL.Locations = Locs

        Dim Altitude = Locs.Select(Function(x) x.Altitude).ToList

        'NotifyPropertyChanged(NameOf(Locs))

        'myMap.UpdateLayout()
        myMap.Children.Add(PL)
    End Function

    Private Async Sub ImportNMEAFile(sender As Object, e As RoutedEventArgs)

        Dim dlg = New Ookii.Dialogs.Wpf.VistaOpenFileDialog()
        dlg.Multiselect = False
        dlg.Filter = "NMEA files (*.nmea)|*.nmea|All files (*.*)|*.*"
        'show the dialogbox for the user to choose the folder
        'Dim folderResult As Object = dlg.ShowDialog

        'you can get the selected parth with this. Here I put it in a textbox. 

        If dlg.ShowDialog Then

            Await test2(dlg.FileName)

        End If


    End Sub
End Class
