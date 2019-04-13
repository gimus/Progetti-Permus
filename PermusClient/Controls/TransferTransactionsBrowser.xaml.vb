Imports Permus

Public Class TransferTransactionsBrowser
    Inherits commandableControl
    Dim _mode As itemsBrowserModeEnum = 0

    Public Enum itemsBrowserModeEnum
        FromUserToMe = 0
        ToUserFromMe = 1
        FromMe = 2
        ToMe = 3

    End Enum

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Property Mode As itemsBrowserModeEnum
        Get
            Return _mode
        End Get
        Set(value As itemsBrowserModeEnum)
            _mode = value
        End Set
    End Property

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
                aggiornaTitolo()
                loadAppropriateItems()
        End Select
    End Sub

    Protected Sub aggiornaTitolo()
        Dim sbj As Subject = C.subjects.getElementById(P1)
        Dim tbj As Subject = C.currentUser
        Select Case Mode
            Case itemsBrowserModeEnum.FromUserToMe
                Me.tbTitle.Text = String.Format("Transfers Transactions from {0} to {1}", sbj.name, tbj.name)
            Case itemsBrowserModeEnum.ToUserFromMe
                Me.tbTitle.Text = String.Format("Transfers Transactions from {1} to {0}", sbj.name, tbj.name)
            Case itemsBrowserModeEnum.FromMe
                Me.tbTitle.Text = String.Format("Transfers Transactions from {0}", tbj.name)
            Case itemsBrowserModeEnum.ToMe
                Me.tbTitle.Text = String.Format("Transfers Transactions received by {0}", tbj.name)

        End Select

    End Sub

    Protected Sub loadAppropriateItems()

        Dim l As List(Of TransferTransaction) = Nothing
        Select Case _mode
            Case itemsBrowserModeEnum.FromUserToMe
                l = C.userTransferTransactions.Values.Where(Function(x) x.fromSubject = P1).ToList
            Case itemsBrowserModeEnum.ToUserFromMe
                l = C.userTransferTransactions.Values.Where(Function(x) x.toSubject = P1).ToList
            Case itemsBrowserModeEnum.FromMe
                l = C.userTransferTransactions.Values.Where(Function(x) x.fromSubject = C.currentUser.id).ToList
            Case itemsBrowserModeEnum.ToMe
                l = C.userTransferTransactions.Values.Where(Function(x) x.toSubject = C.currentUser.id).ToList
        End Select

        'Dim visualizzaElementiNonCompensati As Boolean = btnTransferElementRed.IsChecked
        'Dim visualizzaElementiParzialmenteCompensati As Boolean = btnTransferElementYellow.IsChecked
        'Dim visualizzaElementiCompensati As Boolean = btnTransferElementGreen.IsChecked
        'Dim visualizzaCompensazioni As Boolean = btnTransferElementBlue.IsChecked


        'Dim l1 As New List(Of TransferItemInfo)

        'For Each i As TransferItemInfo In l
        '    If i.isCompensation Then
        '        If visualizzaCompensazioni Then
        '            l1.Add(i)
        '        End If
        '    Else
        '        If i.compensation.percent = 0 And visualizzaElementiNonCompensati Then
        '            l1.Add(i)
        '        End If

        '        If i.compensation.percent >= 100 And visualizzaElementiCompensati Then
        '            l1.Add(i)
        '        End If

        '        If i.compensation.percent > 0 And i.compensation.percent < 100 And visualizzaElementiParzialmenteCompensati Then
        '            l1.Add(i)
        '        End If
        '    End If
        'Next
        Dim l1 As New List(Of TransferTransaction)
        l1.AddRange(l.OrderByDescending(Function(x) x.timestamp))

        Me.LB.ItemsSource = l1
        If l.Count > 0 Then
            LB.SelectedIndex = 0
        Else
            Detail.TransferTransaction = Nothing
        End If
    End Sub

    Private Sub LB_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles LB.SelectionChanged
        e.Handled = True
        If e.AddedItems.Count > 0 Then
            Detail.TransferTransaction = e.AddedItems(0)
        End If
    End Sub

    Private Sub BtnTransferElement_Click(sender As Object, e As RoutedEventArgs)
        loadAppropriateItems()
    End Sub

End Class
