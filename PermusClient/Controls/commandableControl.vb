Imports System.Globalization
Imports Permus

Public Class commandableControl
    Inherits UserControl
    Protected WithEvents C As ClientBlockChain
    Protected mainWindow As MainWindow
    Protected cmd As String = ""
    Protected P1 As Object = ""
    Protected P2 As Object = ""
    Protected isLastCommandReiterated = False
    Public ownerPage As DetailPage


    Public Sub New()
        Try
            C = Application.C
            mainWindow = Application.mw

        Catch ex As Exception

        End Try
    End Sub

    Public Sub executeCommand(cmd As String, Optional P1 As Object = Nothing, Optional P2 As Object = Nothing)
        If cmd = Me.cmd And P1 = Me.P1 And P2 = Me.P2 Then
            isLastCommandReiterated = True
        Else
            isLastCommandReiterated = False
            Me.cmd = cmd
            Me.P1 = P1
            Me.P2 = P2
        End If
        newCommand()
    End Sub

    Public Overridable Sub newCommand()
    End Sub

    Private Sub commandableControl_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If mainWindow IsNot Nothing Then
            mainWindow.commandableControls.Add(Me)
        End If
    End Sub

    Private Sub commandableControl_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
        If mainWindow IsNot Nothing Then
            mainWindow.commandableControls.Remove(Me)
        End If
    End Sub
End Class
