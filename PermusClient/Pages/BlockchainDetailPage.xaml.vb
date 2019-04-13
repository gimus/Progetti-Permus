Imports Permus

Class BlockchainDetailPage
    Inherits DetailPage

    Public Sub New()
        InitializeComponent()
        DataContext = Nothing
    End Sub

    Private Sub BlockchainDetailPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        WB.Navigate(Application.C.api.baseAddress.AbsoluteUri)
    End Sub

    Public Overrides Sub executeCommand(cmd As String)
        Select Case cmd
            Case "TICK"

        End Select
    End Sub

    Private Sub TC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        e.Handled = True
        If e.AddedItems.Count > 0 Then
            Dim ti As TabItem = e.AddedItems(0)
            Select Case ti.Name
                Case "tabBlocks"
                    bb.executeCommand("LOAD")
            End Select
        End If

    End Sub

End Class
