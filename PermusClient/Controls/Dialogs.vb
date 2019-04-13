Public Class Dialogs

    Public Shared Sub warning(msg As String)
        Dim dlg As New TimedWindow(TimedWindow.TimedWindowTypeEnum.warning, "Blockchain warning", msg)
        dlg.ShowDialog()
    End Sub

    Public Shared Sub notify(msg As String)
        Dim dlg As New TimedWindow(TimedWindow.TimedWindowTypeEnum.info, "Blockchain notification", msg)
        dlg.ShowDialog()
    End Sub
End Class
