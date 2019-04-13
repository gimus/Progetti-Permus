Imports Permus

Public Class BlocksBrowser
    Inherits commandableControl

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub ItemsBrowser_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        Dim subj As Subject = Me.DataContext
        If subj IsNot Nothing Then
            P1 = subj.id
            newCommand()
        End If
    End Sub

    Public Overrides Sub newCommand()
        Select Case cmd
            Case "LOAD"
                loadAppropriateItems()
        End Select
    End Sub

    Protected Sub loadAppropriateItems()
        Dim l As New List(Of Integer)
        For i As Integer = 1 To C.systemInfo.currentBlockSerial
            l.Add(i)
        Next
        Me.LB.ItemsSource = l
        If l.Count > 0 Then
            LB.SelectedIndex = 0
        End If
    End Sub

    Private Sub LB_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles LB.SelectionChanged
        e.Handled = True
        If e.AddedItems.Count > 0 Then
            BlockDetail.block = C.getBlock(e.AddedItems(0))
        End If
    End Sub

    Private Sub BtnTransferElement_Click(sender As Object, e As RoutedEventArgs)
        loadAppropriateItems()
    End Sub

End Class
