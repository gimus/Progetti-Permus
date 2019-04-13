Imports Permus

Class UserDetailPage
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
        If user IsNot Nothing Then
            If user.isAuthority Then
                authorityImage.Visibility = Visibility.Visible
                userImage.Visibility = Visibility.Hidden
            Else
                authorityImage.Visibility = Visibility.Hidden
                userImage.Visibility = Visibility.Visible
            End If
            Application.C.extendedInfoToRequest = "UserInfo|" & user.id
        End If
    End Sub


    Public Overrides Sub executeCommand(cmd As String)
        Select Case cmd

            Case "UPDATE"
                Dim sd As Object = Me.DataContext
                DataContext = Nothing
                DataContext = sd

            Case "f"
                tabFromHim.IsSelected = True
            Case "t"
                tabToHim.IsSelected = True
            Case "TICK"
                Dim online As Boolean = False
                If Application.C.systemInfo.otherUserInfo IsNot Nothing Then
                    If Application.C.systemInfo.otherUserInfo.userId = user.id Then
                        userInfo = Application.C.systemInfo.otherUserInfo
                        online = userInfo.isOnline
                    Else
                        userInfo = Nothing
                    End If
                Else
                    userInfo = Nothing
                End If

                If online Then
                    bOnline.Visibility = Visibility.Visible
                Else
                    bOnline.Visibility = Visibility.Hidden
                End If
                it.executeCommand("TICK")
        End Select
    End Sub

    Private Sub TC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        e.Handled = True
        If e.AddedItems.Count > 0 Then
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
