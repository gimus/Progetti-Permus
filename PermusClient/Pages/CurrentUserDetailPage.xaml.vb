Imports Permus

Class CurrentUserDetailPage
    Inherits DetailPage
    Dim userInfo As UserInfo
    Public Sub New()
        InitializeComponent()
        DataContext = Nothing
    End Sub

    Public Overloads Property user As Subject
        Get
            Return DataContext
        End Get
        Set(value As Subject)
            DataContext = value
        End Set
    End Property

    Private Sub FromTransactionControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        Application.C.extendedInfoToRequest = ""
        TC.SelectedIndex = -1
        TC.SelectedIndex = 0
    End Sub


    Public Overrides Sub executeCommand(cmd As String)
        Select Case cmd

            Case "SHOW_INCOMING_TRANSFERS"
                tabTransfer.IsSelected = True

            Case "UPDATE"
                Dim sd As Object = Me.DataContext
                DataContext = Nothing
                DataContext = sd

            Case "f"
                tabFromHim.IsSelected = True
            Case "t"
                tabToHim.IsSelected = True
            Case "TICK"
                ITB.executeCommand("TICK")
        End Select
    End Sub

    Private Sub TC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        e.Handled = True
        If e.AddedItems.Count > 0 And user IsNot Nothing Then
            Dim ti As TabItem = e.AddedItems(0)
            Select Case ti.Name
                Case "tabFromHim"
                    ibFromHim.executeCommand("LOAD", Me.user.id)
                Case "tabToHim"
                    ibToHim.executeCommand("LOAD", Me.user.id)
            End Select
        End If

    End Sub
End Class
