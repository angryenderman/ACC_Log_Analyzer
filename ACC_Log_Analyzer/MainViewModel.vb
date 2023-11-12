Imports NmeaParser
Imports Microsoft.Maps.MapControl.WPF
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Class MainViewModel

    Implements INotifyPropertyChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(<CallerMemberName()> Optional ByVal propertyName As String = Nothing)
        Try
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        Catch
        End Try
    End Sub

    Public Property Competition As ACC.Competition
        Get
            Return ThisCompetition
        End Get
        Set(value As ACC.Competition)
            ThisCompetition = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public Property DisplayFlight As New ACC.Flight

    Private Property _Model_ As New OxyPlot.PlotModel
    Public Property Model() As OxyPlot.PlotModel
        Get
            Return _Model_
        End Get
        Set(value As OxyPlot.PlotModel)
            _Model_ = value
            NotifyPropertyChanged()
        End Set
    End Property







End Class


