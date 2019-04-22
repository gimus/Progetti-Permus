Imports System.Windows.Threading

Public Class TimedWindow

    Public Enum TimedWindowTypeEnum
        info = 0
        warning = 1
        fatal = 2
    End Enum
    Dim _type As TimedWindowTypeEnum = TimedWindowTypeEnum.info

    Dim tmr As DispatcherTimer
    Dim sec As Integer = 10

    Public Sub New()
        InitializeComponent()
        tmr = New DispatcherTimer()
        AddHandler tmr.Tick, AddressOf tmr_Tick
        tmr.Interval = New TimeSpan(0, 0, 1)
        tmr.Start()
        updateInterface()
    End Sub

    Public Sub New(type As TimedWindowTypeEnum, title As String, message As String)
        Me.New()
        Me.Title = title
        Me.message.Text = message
        Me.type = type
    End Sub

    Public Property type As TimedWindowTypeEnum
        Get
            Return _type
        End Get
        Set(value As TimedWindowTypeEnum)
            _type = value
            updateInterface()
        End Set
    End Property

    Protected Sub updateInterface()
        img0.Visibility = Visibility.Hidden
        img1.Visibility = Visibility.Hidden
        img2.Visibility = Visibility.Hidden
        Select Case _type
            Case TimedWindowTypeEnum.info
                img0.Visibility = Visibility.Visible
            Case TimedWindowTypeEnum.fatal
                img1.Visibility = Visibility.Visible
            Case TimedWindowTypeEnum.warning
                img2.Visibility = Visibility.Visible
        End Select
    End Sub


    Protected Sub tmr_Tick(ByVal sender As Object, ByVal e As EventArgs)
        sec -= 1
        Me.time.Text = String.Format("questa finestra si chiuderà automaticamente tra {0} secondi", sec)
        If sec < 0 Then
            Me.Close()
        End If
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs) Handles btnClose.Click
        Me.Close()
    End Sub
End Class
