Imports NmeaParser
Imports Microsoft.Maps.MapControl.WPF
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Xml.Serialization
Module ACC_Log_Common

    Function Prescore_60s_Altitude(_SixtySecAltitude_ As Double) As Double
        Return Math.Round(Math.Pow(_SixtySecAltitude_, 4) * -0.0000392 + Math.Pow(_SixtySecAltitude_, 3) * 0.0108 + Math.Pow(_SixtySecAltitude_, 2) * -1.156 + _SixtySecAltitude_ * 64.2 - 537, 2)
    End Function

    Public Sub WriteToXmlFile(Of T As New)(ByVal filePath As String, ByVal objectToWrite As T, ByVal Optional append As Boolean = False)
        Dim writer As IO.TextWriter = Nothing

        Try
            Dim serializer = New Xml.Serialization.XmlSerializer(GetType(T))
            writer = New IO.StreamWriter(filePath, append)
            serializer.Serialize(writer, objectToWrite)
        Catch ex As Exception
            Debug.WriteLine(ex.Message)



        End Try

        If writer IsNot Nothing Then writer.Close()

    End Sub

    Public Function ReadFromXmlFile(Of T As New)(ByVal filePath As String) As T
        Dim reader As IO.TextReader = Nothing

        Try
            Dim serializer = New Xml.Serialization.XmlSerializer(GetType(T))
            reader = New IO.StreamReader(filePath)
            Return CType(serializer.Deserialize(reader), T)
        Finally
            If reader IsNot Nothing Then reader.Close()
        End Try
    End Function

    Public Sub RandomizeArray(ByVal items() As Integer)
        Dim max_index As Integer = items.Length - 1
        Dim rnd As New Random
        For i As Integer = 0 To max_index - 1
            ' Pick an item for position i.
            Dim j As Integer = rnd.Next(i, max_index + 1)

            ' Swap them.
            Dim temp As Integer = items(i)
            items(i) = items(j)
            items(j) = temp
        Next i
    End Sub

    Public Property Teams As List(Of ACC.Team)
    Public Sub SaveTeamsXML(Optional filepath As String = Nothing)
        If filepath Is Nothing Then
            Dim dlg = New Ookii.Dialogs.Wpf.VistaSaveFileDialog()
            dlg.Filter = ".acc_team files (*.acc_team)|*.acc_team"

            If Not dlg.ShowDialog Then Return

            filepath = dlg.FileName

        End If


        WriteToXmlFile(filepath, Teams)

    End Sub
    Public Function LoadTeamsXML(Optional filepath As String = Nothing) As Boolean

        If filepath Is Nothing Then
            Dim dlg = New Ookii.Dialogs.Wpf.VistaOpenFileDialog()
            dlg.Filter = ".acc_team files (*.acc_team)|*.acc_team"

            If Not dlg.ShowDialog Then Return False
            filepath = dlg.FileName
        End If

        Dim Import = ReadFromXmlFile(Of List(Of ACC.Team))(filepath)
        'Teams.Clear
        Teams = Import

        Return True
    End Function

    Function GetSmoothedSpeed(ByRef F As ACC.Flight, n As Integer, span As Integer)
        Dim currentspeed = F.GGAs(n).VTG_overground

        If n > span And n < F.GGAs.Count - span Then

            For i = -span To span Step 1

                currentspeed += F.GGAs(n + i).VTG_overground

            Next

            currentspeed = currentspeed / (2 * span + 1)

        End If

        Return currentspeed
    End Function

    Public Property ThisCompetition As New ACC.Competition


End Module

Namespace ACC
    Public Class Team
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
            Try
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            Catch
            End Try
        End Sub

        Public Property Name As String
        Public Property Number As Short
        Public Property Country As String
        Public Property City As String
        Public Property KO As Boolean = False
        Public Property PayloadPrediction_c0 As Double = 0
        Public Property PayloadPrediction_c1 As Double = 0
        Public Property _Score_Report_ As Double = 0
        Public Property Score_Report As Double
            Get
                Return _Score_Report_
            End Get
            Set(value As Double)
                _Score_Report_ = value
                NotifyPropertyChanged(NameOf(OverallScore))
            End Set
        End Property
        Public Property _Score_Video_ As Double = 0
        Public Property Score_Video As Double
            Get
                Return _Score_Video_
            End Get
            Set(value As Double)
                _Score_Video_ = value
                NotifyPropertyChanged(NameOf(OverallScore))
            End Set
        End Property
        Public Property _Score_Drawings_ As Double = 0
        Public Property Score_Drawings As Double
            Get
                Return _Score_Drawings_
            End Get
            Set(value As Double)
                _Score_Drawings_ = value
                NotifyPropertyChanged(NameOf(OverallScore))
            End Set
        End Property
        'Public Property _Penalties_ As Double = 0
        <XmlIgnore()> Public ReadOnly Property Penalties As Double
            Get

                Return ThisCompetition.Penalties.FindAll(Function(x) x.TeamNumber = Number).Select(Function(x) Math.Abs(x.Score)).Sum
            End Get
            'Set(value As Double)
            '    _Penalties_ = value
            '    NotifyPropertyChanged(NameOf(OverallScore))
            'End Set
        End Property
        <XmlIgnore()> Public Property Flights As New List(Of ACC.Flight)
        <XmlIgnore()> Public ReadOnly Property OverallScore As Double
            Get
                Try
                    OverallScore = Math.Round(Score_Report + Score_Video + Score_Drawings + FlightsScore - Math.Abs(Penalties))
                Catch ex As Exception
                    Return -1
                End Try

                'NotifyPropertyChanged(NameOf(FlightsScore))
                'NotifyPropertyChanged(NameOf(FlightsScoreTooltip))
                'NotifyPropertyChanged(NameOf(Penalties))

            End Get

        End Property

        <XmlIgnore()> Public ReadOnly Property FlightsScore As Double
            Get

                'Return 0

                If Math.Min(ThisCompetition.Rounds.Count, Flights.Count) = 0 Then Return 0

                Dim local_Flights = Flights
                Dim RoundsDivisor = ThisCompetition.Rounds.Count

                If ThisCompetition.Rounds.Count > 6 Then
                    RoundsDivisor -= 2
                ElseIf ThisCompetition.Rounds.Count > 3 Then
                    RoundsDivisor -= 1
                End If

                If Math.Min(ThisCompetition.Rounds.Count, Flights.Count) > 6 Then


                    local_Flights = local_Flights.OrderBy(Function(x) x.TotalScore).ToList

                    local_Flights.Remove(local_Flights.First)
                    local_Flights.Remove(local_Flights.First)

                    Return local_Flights.Select(Function(x) x.TotalScore).Sum / RoundsDivisor

                ElseIf Math.Min(ThisCompetition.Rounds.Count, Flights.Count) > 3 Then
                    local_Flights = local_Flights.OrderBy(Function(x) x.TotalScore).ToList

                    local_Flights.Remove(local_Flights.First)

                    Return local_Flights.Select(Function(x) x.TotalScore).Sum / RoundsDivisor


                Else
                    Return local_Flights.Select(Function(x) x.TotalScore).Sum / RoundsDivisor

                End If


            End Get
        End Property

        <XmlIgnore()> Public ReadOnly Property FlightsScoreTooltip As String
            Get
                Dim TS = Flights.OrderBy(Function(x) x.TotalScore).Reverse.ToList.Select(Function(x) x.TotalScore).ToArray

                Dim S As String = ""
                For n = 0 To TS.Length - 1
                    If n <> 0 Then S &= vbCrLf

                    S &= TS(n).ToString

                Next

                Return S

            End Get
        End Property





    End Class

    Public Class Penalty
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
            Try
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            Catch
            End Try
        End Sub
        Public Property Reason As String
        Private Property _Time_ As DateTime = DateTime.MinValue
        Public Property Time As DateTime
            Get
                If _Time_ = DateTime.MinValue Then _Time_ = Now
                Return _Time_
            End Get
            Set(value As DateTime)
                _Time_ = value
                NotifyPropertyChanged()
            End Set
        End Property
        Public Property Score As Double
        Public Property Comment As String
        Public Property ResponsibleJuryMember As String

        Public Property _TeamNumber_ As Integer
        Public Property TeamNumber As Integer
            Get
                Return _TeamNumber_
            End Get
            Set(value As Integer)
                _TeamNumber_ = value
                NotifyPropertyChanged(NameOf(Team))
                NotifyPropertyChanged()
                'Team = ACC_Log_Common.Teams.Find(Function(x) x.Number = value)
            End Set
        End Property

        <XmlIgnore()> Public ReadOnly Property Team As Team
            Get
                Return Teams.Find(Function(x) x.Number = TeamNumber)
            End Get
        End Property

    End Class

    Public Class GGA_replacement
        Public Property Latitude As Double
        Public Property Longitude As Double
        Public Property Altitude As Double
        Public Property FixTime As TimeSpan
        Public Property VTG_overground As Double
        Public Property UnsafeFix As Boolean
    End Class


    Public Class Flight
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
            Try
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            Catch
            End Try
        End Sub

        Private UseGNS As Boolean = False

        Private Property UpdateScoresRunning As Boolean = False
        Public Async Sub UpdateScores(Optional withparent As Boolean = True)

            While UpdateScoresRunning
                Await Task.Delay(10)
            End While


            UpdateScoresRunning = True
            NotifyPropertyChanged(NameOf(Bonus_PayloadPrediction))
            NotifyPropertyChanged(NameOf(Bonus_Loading))
            NotifyPropertyChanged(NameOf(Bonus_UnLoading))
            NotifyPropertyChanged(NameOf(Bonus_Takeoff))
            NotifyPropertyChanged(NameOf(Score_Payload))
            NotifyPropertyChanged(NameOf(Score_Altitude))
            NotifyPropertyChanged(NameOf(Score_Distance))
            'NotifyPropertyChanged(NameOf(ClimbPrescore))
            NotifyPropertyChanged(NameOf(FlightScore))
            NotifyPropertyChanged(NameOf(TotalScore))
            If Parent IsNot Nothing And withparent Then
                Parent.UpdateScores()
                If Parent.Parent IsNot Nothing Then Parent.Parent.UpdateFlightsinTeam()
            End If

            UpdateScoresRunning = False

        End Sub

        <XmlIgnore()> Public ReadOnly Property TotalScore As Double
            Get
                TotalScore = FlightScore
                If Penalty_FlightInvalid Then TotalScore = 0

                'UpdateFlightsinTeam()

            End Get
        End Property


        <XmlIgnore()> Public ReadOnly Property FlightScore As Double
            Get
                Return Math.Round(((Score_Payload + Score_Distance + Score_Altitude) / 3 + Bonus_Loading + Bonus_PayloadPrediction + Bonus_UnLoading) * Bonus_Takeoff)

            End Get
        End Property
        Public Property Number As Short
        Private Property _NMEA_ As New List(Of String)
        Public Property NMEA As List(Of String)
            Get
                Return _NMEA_
            End Get
            Set(value As List(Of String))
                _NMEA_ = value
                'EvaluateGGAs()
                StartEvaluateGGAs()
                NotifyPropertyChanged()
            End Set
        End Property
        Private Property _NMEA_Filename_ As String = ""
        Public Property NMEA_Filename As String
            Get
                Return _NMEA_Filename_
            End Get
            Set(value As String)
                _NMEA_Filename_ = value
                NotifyPropertyChanged()
            End Set
        End Property

        Public Sub StartEvaluateGGAs()

            'EvaluateGGAs()

            Dim T As New Threading.Thread(AddressOf EvaluateGGAs)
            T.Start()


        End Sub

        <XmlIgnore()> Private Property _GGAs_ As List(Of GGA_replacement) = Nothing
        <XmlIgnore()> Public ReadOnly Property GGAs As List(Of GGA_replacement)
            Get
                If _GGAs_ Is Nothing And _NMEA_.Count > 0 Then
                    'GetGGAs()
                    'EvaluateGGAs()
                    StartEvaluateGGAs()
                End If
                Return _GGAs_
            End Get
        End Property


        Public Async Function GetGGAsThread() As Task

            Dim T As New Threading.Thread(AddressOf GetGGAs)
            T.Start()

            While T.IsAlive
                Await Task.Delay(200)
            End While


        End Function

        Public Sub GetGGAs()
            On Error Resume Next
            If NMEA.Count = 0 Then Return


            _GGAs_ = New List(Of GGA_replacement)

            Dim vtg As Messages.Vtg = Nothing
            Dim gga As Messages.Gga = Nothing
            Dim altitude As Double = -1000
            Dim gns As Messages.Gns = Nothing

            For Each L In NMEA


                If L.StartsWith("$GNVTG") Then
                    vtg = TryCast(Messages.NmeaMessage.Parse(L), Messages.Vtg)
                End If


                If L.StartsWith("$GNGGA") Then
                    gga = TryCast(Messages.NmeaMessage.Parse(L), Messages.Gga)
                    If gga IsNot Nothing And vtg IsNot Nothing Then _GGAs_.Add(New GGA_replacement With {.Altitude = gga.Altitude, .Latitude = gga.Latitude, .FixTime = gga.FixTime, .Longitude = gga.Longitude, .VTG_overground = vtg.SpeedKph})

                End If

                If L.StartsWith("$SMGPS2,") Then
                End If



            Next

            If _GGAs_.Count = 0 And Not UseGNS Then
                _GGAs_ = New List(Of GGA_replacement)


                'Dim vtg As Messages.Vtg = Nothing

                For Each L In NMEA


                    If L.StartsWith("$GNGNS") Then
                        gns = TryCast(Messages.NmeaMessage.Parse(L), Messages.Gns)
                    End If


                    If L.StartsWith("$GNVTG") Then
                        vtg = TryCast(Messages.NmeaMessage.Parse(L), Messages.Vtg)
                    End If


                    If L.StartsWith("$SMGPS,") Then
                        Dim S1 = L.Split("*").First
                        Dim S2 = S1.Split(",")(1)
                        Dim S3 = S2.Split(" ").First
                        altitude = Double.Parse(S3, Globalization.CultureInfo.InvariantCulture)

                        'Dim posunsafe = Not (gns.Status = Messages.Gns.NavigationalStatus.Safe)
                        Dim posunsafe As Boolean = True
                        If gns IsNot Nothing Then posunsafe = (gns.Hdop > 1)

                        'If posunsafe Then
                        '    'Debug.WriteLine("x")
                        'End If

                        If gns IsNot Nothing And vtg IsNot Nothing And altitude <> -1000 Then
                            _GGAs_.Add(New GGA_replacement With {.Altitude = altitude, .Latitude = gns.Latitude, .FixTime = gns.FixTime, .Longitude = gns.Longitude, .VTG_overground = vtg.SpeedKph, .UnsafeFix = posunsafe})
                            'Else
                            'Debug.WriteLine("x")
                        End If

                    End If

                    If L.StartsWith("$SMGPS2,") Then

                    End If



                Next
            End If


            NotifyPropertyChanged(NameOf(GGAs))

        End Sub

        Private Property _IndexShift_ As Integer = 0
        Public Property IndexShift As Integer
            Get
                Return _IndexShift_
            End Get
            Set(value As Integer)
                _IndexShift_ = value
                StartEvaluateGGAs()
                'EvaluateGGAs()
            End Set
        End Property
        Public Property _TeamNumber_ As Integer
        Public Property TeamNumber As Integer
            Get
                Return _TeamNumber_
            End Get
            Set(value As Integer)
                _TeamNumber_ = value
                NotifyPropertyChanged(NameOf(Team))
                NotifyPropertyChanged()
                'Team = ACC_Log_Common.Teams.Find(Function(x) x.Number = value)
            End Set
        End Property

        Public ReadOnly Property Team As Team
            Get
                Return ACC_Log_Common.Teams.Find(Function(x) x.Number = TeamNumber)
            End Get
        End Property

        Private Property _Payload_ As Double = 0
        Public Property Payload As Double
            Get
                Return _Payload_
            End Get
            Set(value As Double)
                _Payload_ = value
                NotifyPropertyChanged()
                UpdateScores()
            End Set
        End Property
        <XmlIgnore()> Private ReadOnly Property PenaltyPayload As Double
            Get
                If Penalty_FlightInvalid Then Return 0
                If NMEA.Count = 0 Then Return 0
                Return Payload
            End Get
        End Property
        <XmlIgnore()> Public ReadOnly Property Score_Payload
            Get
                If Penalty_FlightInvalid Or Parent Is Nothing Then Return 0
                Return Math.Round(PenaltyPayload / Parent.Flights.Select(Function(x) x.PenaltyPayload).Max * 1000)
            End Get
        End Property
        <XmlIgnore()> Public ReadOnly Property Bonus_PayloadPrediction As Double
            Get
                If Team Is Nothing Then Return 0
                If Team.PayloadPrediction_c0 = 0 And Team.PayloadPrediction_c1 = 0 Then Return 0
                If NMEA.Count = 0 Then Return 0
                Dim predPL = Team.PayloadPrediction_c0 + Team.PayloadPrediction_c1 * Parent.AirDensity
                Return Math.Round(Math.Max(50 * (1 - Math.Abs(Payload / predPL - 1)), 0))
            End Get
        End Property

        <XmlIgnore()> Private Property _Altitude60s_ As Double = 0
        <XmlIgnore()> Public Property Altitude60s As Double
            Get
                Return Math.Round(_Altitude60s_, 0)
            End Get
            Set(value As Double)
                _Altitude60s_ = value
                NotifyPropertyChanged()
                UpdateScores()
                NotifyPropertyChanged(NameOf(ClimbPrescore))
                NotifyPropertyChanged(NameOf(ClimbPrescore))
                NotifyPropertyChanged(NameOf(Score_Altitude))
            End Set
        End Property
        <XmlIgnore()> Public ReadOnly Property Score_Altitude
            Get
                If Penalty_FlightInvalid Or Parent Is Nothing Then Return 0
                Return Math.Round(ClimbPrescore / Parent.Flights.Select(Function(x) x.ClimbPrescore).Max * 1000)
            End Get
        End Property
        <XmlIgnore()> Public ReadOnly Property ClimbPrescore As Double
            Get
                If Penalty_FlightInvalid Then Return 0
                Return Math.Max(Math.Round(Math.Pow(_Altitude60s_, 4) * -0.0000392 + Math.Pow(_Altitude60s_, 3) * 0.0108 + Math.Pow(_Altitude60s_, 2) * -1.156 + _Altitude60s_ * 64.2 - 537, 0), 0)

            End Get
        End Property
        <XmlIgnore()> Private Property _Distance120s_ As Double = 0
        <XmlIgnore()> Public Property Distance120s As Double
            Get
                Return Math.Round(_Distance120s_, 0)
            End Get
            Set(value As Double)
                _Distance120s_ = value
                NotifyPropertyChanged()
            End Set
        End Property

        <XmlIgnore()> Public ReadOnly Property Score_Distance
            Get
                If Penalty_FlightInvalid Or Parent Is Nothing Then Return 0
                Return Math.Round(Distance120s / Parent.Flights.Select(Function(x) x.Distance120s).Max * 1000)
            End Get
        End Property

        Public Property IndexTakeoff As Integer = -1
        Public Property Index60s As Integer = -1
        Public Property Index180s As Integer = -1

        Public Property EvaluatingGGAs As Boolean = False

        Public Sub ClearGGAs()
            _GGAs_ = New List(Of GGA_replacement)
            NMEA = New List(Of String)
        End Sub
        Public Async Sub EvaluateGGAs()
            Try

                If EvaluatingGGAs Then
                    While EvaluatingGGAs
                        Await Task.Delay(500)
                        Return
                    End While
                    Return
                End If

                EvaluatingGGAs = True

                Try
                    Await GetGGAsThread()
                    'GetGGAs()
                Catch ex As Exception
                    Debug.Write(ex.Message)
                End Try



                Altitude60s = 0
                Distance120s = 0
                Penalty_toohigh = False
                Penalty_toolow = False

                If GGAs Is Nothing Then
                    EvaluatingGGAs = False
                    Return
                End If
                If GGAs.Count = 0 Then
                    EvaluatingGGAs = False
                    Return
                End If

                Dim deltatime As Double
                Dim distpoints As Double
                Dim currentspeed As Double
                Dim currentaltitude As Double

                Dim n = 0
                While n < GGAs.Count - 1 And n >= 0
                    n += 1
                    deltatime = GGAs(n).FixTime.TotalSeconds - GGAs(n - 1).FixTime.TotalSeconds
                    distpoints = Geolocation.GeoCalculator.GetDistance(GGAs(n - 1).Latitude, GGAs(n - 1).Longitude, GGAs(n).Latitude, GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    currentspeed = distpoints / deltatime * 3.6

                    currentspeed = GGAs(n).VTG_overground

                    currentspeed = GetSmoothedSpeed(Me, n, 10)

                    currentaltitude = GGAs(n).Altitude - GGAs(0).Altitude
                    If currentaltitude > 120 Then Penalty_toohigh = True
                    If currentaltitude > 10 Then
                        If currentspeed < 5 Then
                            EvaluationMessage = Join({EvaluationMessage, "Warning: Takeoff ground-speed might be lower than 5 km/h"}, " ")
                        End If
                        Exit While
                    End If

                End While

                While n < GGAs.Count And n > 0

                    deltatime = GGAs(n).FixTime.TotalSeconds - GGAs(n - 1).FixTime.TotalSeconds
                    distpoints = Geolocation.GeoCalculator.GetDistance(GGAs(n - 1).Latitude, GGAs(n - 1).Longitude, GGAs(n).Latitude, GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    currentspeed = distpoints / deltatime * 3.6

                    currentspeed = GGAs(n).VTG_overground

                    currentspeed = GetSmoothedSpeed(Me, n, 10)

                    'currentaltitude = GGAs(n).Altitude - GGAs(0).Altitude
                    'If currentaltitude > 120 Then Penalty_toohigh = True

                    If currentspeed < 5 Then Exit While
                    '  Debug.WriteLine(n)
                    n -= 1
                End While

                ShiftIndexTimeOffset = GGAs(n + IndexShift).FixTime.TotalSeconds - GGAs(n).FixTime.TotalSeconds
                NotifyPropertyChanged(NameOf(ShiftIndexTimeOffset))
                n += IndexShift

                IndexTakeoff = n


                Dim timesincetakeoff As Double
                Dim takeofftime As Double
                Dim takeoffaltitude As Double

                takeofftime = GGAs(n).FixTime.TotalSeconds
                takeoffaltitude = GGAs(n).Altitude

                While n < GGAs.Count - 1 And n >= 0
                    ' Debug.WriteLine(n)
                    timesincetakeoff = GGAs(n).FixTime.TotalSeconds - takeofftime
                    If timesincetakeoff > 60 Then Exit While

                    currentaltitude = GGAs(n).Altitude - GGAs(IndexTakeoff).Altitude
                    If currentaltitude > 120 Then Penalty_toohigh = True
                    n += 1

                End While

                Altitude60s = GGAs(n).Altitude - takeoffaltitude
                Index60s = n

                Dim distancesum As Double = 0

                While n < GGAs.Count And n > 0
                    ' Debug.WriteLine(n)
                    currentaltitude = GGAs(n).Altitude - GGAs(IndexTakeoff).Altitude
                    If currentaltitude > 120 Then Penalty_toohigh = True
                    If currentaltitude < 10 Then Penalty_toolow = True
                    If currentaltitude >= 10 Then
                        distancesum += Geolocation.GeoCalculator.GetDistance(GGAs(n - 1).Latitude, GGAs(n - 1).Longitude, GGAs(n).Latitude, GGAs(n).Longitude, 5, Geolocation.DistanceUnit.Meters)
                    End If

                    timesincetakeoff = GGAs(n).FixTime.TotalSeconds - takeofftime
                    If timesincetakeoff > 180 Then Exit While
                    n += 1
                End While

                Index180s = n
                Distance120s = distancesum
                EvaluatingGGAs = False
            Catch
                EvaluatingGGAs = False
            End Try

        End Sub

        <XmlIgnore()> Public Property ShiftIndexTimeOffset As Double = 0

        <XmlIgnore()> Private Property _EvaluationMessage_ As String = Nothing
        <XmlIgnore()> Public Property EvaluationMessage As String
            Get
                Return _EvaluationMessage_
            End Get
            Set(value As String)
                _EvaluationMessage_ = value
                NotifyPropertyChanged()
            End Set
        End Property
        Private Property _LoadingTime_ As Double = -1
        Public Property LoadingTime As Double
            Get
                Return _LoadingTime_
            End Get
            Set(value As Double)
                _LoadingTime_ = value
                UpdateScores()
            End Set
        End Property
        <XmlIgnore()> Public ReadOnly Property Bonus_Loading As Double
            Get
                If LoadingTime < 0 Then Return 0
                If NMEA.Count = 0 Then Return 0
                Return Math.Round(Math.Max((1 - (LoadingTime / 120)) * 60, 0))

            End Get
        End Property
        Private Property _UnLoadingTime_ As Double = -1
        Public Property UnLoadingTime As Double
            Get
                Return _UnLoadingTime_
            End Get
            Set(value As Double)
                _UnLoadingTime_ = value
                UpdateScores()
            End Set
        End Property
        <XmlIgnore()> Public ReadOnly Property Bonus_UnLoading As Double
            Get
                If UnLoadingTime < 0 Then Return 0
                If NMEA.Count = 0 Then Return 0
                Return Math.Round(Math.Max((1 - (UnLoadingTime / 120)) * 60, 0))

            End Get
        End Property
        Private Property _Takeoff40m_ As Boolean = False
        Public Property Takeoff40m As Boolean
            Get
                Return _Takeoff40m_
            End Get
            Set(value As Boolean)
                _Takeoff40m_ = value
                UpdateScores()
            End Set
        End Property
        <XmlIgnore()> Public ReadOnly Property Bonus_Takeoff As Double
            Get
                If Takeoff40m Then
                    Return 1.1
                Else
                    Return 1
                End If
            End Get
        End Property
        <XmlIgnore()> Private Property _Penalty_toolow_ As Boolean = False
        <XmlIgnore()> Public Property Penalty_toolow As Boolean
            Get
                Return _Penalty_toolow_
            End Get
            Set(value As Boolean)
                _Penalty_toolow_ = value
                NotifyPropertyChanged()
                'UpdateScores()
            End Set
        End Property
        <XmlIgnore()> Private Property _Penalty_toohigh_ As Boolean = False
        <XmlIgnore()> Public Property Penalty_toohigh As Boolean
            Get
                Return _Penalty_toohigh_
            End Get
            Set(value As Boolean)
                _Penalty_toohigh_ = value
                NotifyPropertyChanged()
                'UpdateScores()
            End Set
        End Property
        Private Property _Penalty_lostparts_ As Boolean = False
        Public Property Penalty_lostparts As Boolean
            Get
                Return _Penalty_lostparts_
            End Get
            Set(value As Boolean)
                _Penalty_lostparts_ = value
                NotifyPropertyChanged()
                UpdateScores()
            End Set
        End Property
        Private Property _Penalty_exceedrunway_ As Boolean = False
        Public Property Penalty_exceedrunway As Boolean
            Get
                Return _Penalty_exceedrunway_
            End Get
            Set(value As Boolean)
                _Penalty_exceedrunway_ = value
                NotifyPropertyChanged()
                UpdateScores()
            End Set
        End Property
        Private Property _Penalty_leftflightarea_ As Boolean = False
        Public Property Penalty_leftflightarea As Boolean
            Get
                Return _Penalty_leftflightarea_
            End Get
            Set(value As Boolean)
                _Penalty_leftflightarea_ = value
                NotifyPropertyChanged()
                UpdateScores()
            End Set
        End Property

        <XmlIgnore()> Public ReadOnly Property Penalty_FlightInvalid As Boolean
            Get
                Return Penalty_toohigh Or Penalty_lostparts Or Penalty_exceedrunway Or Penalty_leftflightarea
            End Get
        End Property
        Private Property _ParentRoundNo_ As Integer

        <XmlIgnore()> Private Property _Parent_ As Competition.Round = Nothing
        <XmlIgnore()> Public Property Parent As Competition.Round
            Get
                Return _Parent_
            End Get
            Set(value As Competition.Round)
                _Parent_ = value
                _ParentRoundNo_ = value.Number
                NotifyPropertyChanged()
            End Set
        End Property



        Public Async Function ImportNMEA() As Task

            Dim dlg = New Ookii.Dialogs.Wpf.VistaOpenFileDialog()
            dlg.Multiselect = False
            dlg.Filter = "NMEA files (*.nmea)|*.nmea"


            If dlg.ShowDialog Then

                Dim nmea_local As New List(Of String)
                Using reader As New IO.StreamReader(dlg.FileName, Text.Encoding.ASCII)
                    While Not reader.EndOfStream

                        nmea_local.Add(Await reader.ReadLineAsync)
                    End While
                End Using

                NMEA = nmea_local
                NMEA_Filename = dlg.FileName
                'EvaluateGGAs()
                'StartEvaluateGGAs()
                'NotifyPropertyChanged(NameOf(NMEA))
            End If
        End Function

        Public Sub RemoveNMEA()
            NMEA = New List(Of String)
            'NotifyPropertyChanged(NameOf(NMEA))
        End Sub
        Private Sub Flight_PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Handles Me.PropertyChanged
            If _Parent_ IsNot Nothing Then _Parent_.NotifyPropertyChanged(NameOf(Competition.Round.Flights))
        End Sub
    End Class





    Public Class Competition
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
            Try
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            Catch
            End Try
        End Sub

        Private Property _Penalties_ As New List(Of Penalty)
        Public Property Penalties As List(Of Penalty)
            Get
                Return _Penalties_
            End Get
            Set(value As List(Of Penalty))
                _Penalties_ = value
                NotifyPropertyChanged()
            End Set
        End Property
        Private Property _Rounds_ As New List(Of Round)
        Public Property Rounds As List(Of Round)
            Get
                UpdateParents()
                Return _Rounds_
            End Get
            Set(value As List(Of Round))
                _Rounds_ = value
                NotifyPropertyChanged()
                UpdateParents()
            End Set
        End Property
        Private Sub UpdateParents()
            For Each R In _Rounds_
                If R.Parent Is Nothing Then R.Parent = Me
            Next
        End Sub
        Public Property SaveName As String = ""
        Private Property _Teams_ As New List(Of Team)
        Public Property Teams As List(Of Team)
            Get
                Return ACC_Log_Common.Teams
            End Get
            Set(value As List(Of Team))
                ACC_Log_Common.Teams = value
                NotifyPropertyChanged()
            End Set
        End Property





        Public Class Round
            Implements INotifyPropertyChanged
            Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

            Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
                Try
                    RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
                Catch
                End Try
            End Sub

            <XmlIgnore()> Public Property Parent As ACC.Competition

            Private Sub Round_PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Handles Me.PropertyChanged

            End Sub

            Public Sub UpdateScores()
                For Each F In Flights
                    F.UpdateScores(False)
                Next
            End Sub

            Public Property Number As Short
            Private Property _Flights_ As New List(Of Flight)
            Public Property Flights As List(Of Flight)
                Get
                    UpdateParents()
                    Return _Flights_
                End Get
                Set(value As List(Of Flight))
                    _Flights_ = value
                    UpdateParents()
                    NotifyPropertyChanged()
                End Set
            End Property
            Private Sub UpdateParents()
                For Each F In _Flights_
                    If F.Parent Is Nothing Then F.Parent = Me
                Next
            End Sub


            Public Property AirDensity As Double = 1.2
            Private Property _Closed_ As Boolean = False
            Public Property Closed As Boolean
                Get
                    Return _Closed_
                End Get
                Set(value As Boolean)

                    _Closed_ = value
                    NotifyPropertyChanged()
                    UpdateScores()
                End Set
            End Property

        End Class

        Public Sub SaveCompetitionXML(Optional filepath As String = Nothing)
            If filepath Is Nothing Then
                Dim dlg = New Ookii.Dialogs.Wpf.VistaSaveFileDialog()
                dlg.Filter = ".acc files (*.acc)|*.acc"

                If Not dlg.ShowDialog Then Return

                filepath = dlg.FileName

            End If


            WriteToXmlFile(filepath, Me)

        End Sub

        Public Function LoadCompetitionXML(Optional filepath As String = Nothing) As Boolean

            If filepath Is Nothing Then
                Dim dlg = New Ookii.Dialogs.Wpf.VistaOpenFileDialog()
                dlg.Filter = ".acc files (*.acc)|*.acc"


                If Not dlg.ShowDialog Then
                    Return False
                End If

                filepath = dlg.FileName

                My.Settings.DefaultCompetitionFile = filepath
                My.Settings.Save()

            End If

            Dim Import = ReadFromXmlFile(Of Competition)(filepath)
            Penalties = Import.Penalties
            Rounds = Import.Rounds
            For Each R In Rounds
                For Each F In R.Flights
                    'F.EvaluateGGAs()
                    F.StartEvaluateGGAs()
                Next
            Next
            SaveName = filepath
            Teams = Import.Teams
            Return True
        End Function

        Private UpdateFlightsinTeamRunning As Boolean = False

        Public Async Sub UpdateFlightsinTeam()

            If Teams Is Nothing Then Return
            While UpdateFlightsinTeamRunning
                Task.Delay(10)
            End While
            UpdateFlightsinTeamRunning = True

            For Each T In Teams
                T.Flights = New List(Of Flight)
                For Each R In Rounds
                    Dim Entry = R.Flights.Find(Function(x) x.TeamNumber = T.Number)
                    If Entry Is Nothing Then Continue For
                    'If Entry.NMEA.Count > 0 Then T.Flights.Add(Entry)
                    T.Flights.Add(Entry)
                Next

                T.NotifyPropertyChanged(NameOf(T.Flights))
                T.NotifyPropertyChanged(NameOf(T.FlightsScore))
                T.NotifyPropertyChanged(NameOf(T.Penalties))
                T.NotifyPropertyChanged(NameOf(T.OverallScore))

            Next
            UpdateFlightsinTeamRunning = False
        End Sub

    End Class





End Namespace
