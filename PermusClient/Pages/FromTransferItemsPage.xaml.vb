Class FromTransferItemsPage
    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub FromTransactionControl_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        LB.ItemsSource = Me.DataContext
        LB.SelectedIndex = 0
    End Sub

End Class
