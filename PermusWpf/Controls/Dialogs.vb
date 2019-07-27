Public Class Dialogs

    Public Shared Sub warning(msg As String)
        Dim dlg As New TimedWindow(TimedWindow.TimedWindowTypeEnum.warning, "Blockchain warning", msg)
        dlg.ShowDialog()
    End Sub

    Public Shared Sub notify(msg As String, Optional owner As Window = Nothing)
        Dim dlg As New TimedWindow(TimedWindow.TimedWindowTypeEnum.info, "Blockchain notification", msg)
        If owner IsNot Nothing Then
            dlg.Owner = owner
        End If
        dlg.ShowDialog()
    End Sub
End Class
