Imports System.ComponentModel
Imports Permus

Public Class BlockChainEventNotificationWindow
    Dim WithEvents C As ClientBlockChain
    Protected Property MessageText As String = "----"
    Protected msg As String = "-"
    Dim nt As New Text.StringBuilder(1000)

    Public Sub New(CBC As ClientBlockChain)
        InitializeComponent()
        C = CBC
    End Sub

    Private Sub C_Notification(eventType As BlockChainNotifyEventType, eventMessage As String) Handles C.Notification
        Try
            Select Case eventType
                Case Else
                    msg = eventMessage
                    Me.Dispatcher.Invoke(AddressOf DisplayMessage, Windows.Threading.DispatcherPriority.Render)
            End Select
        Catch ex As Exception

        End Try
    End Sub

    Protected Sub DisplayMessage()
        Dim txt As String = String.Format("{0} {1}", Now().ToString("HH:mm:ss"), msg)
        nt.AppendLine(txt)
        If nt.Length > 10600 Then
            nt.Remove(0, 100)
        End If

        notification.Text = nt.ToString
        notification.ScrollToEnd()
        '        Application.mw.setStatusText(txt)
    End Sub

    Private Sub BlockChainEventNotificationWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Me.Visibility = Visibility.Hidden
        e.Cancel = True
    End Sub
End Class
