Imports System.Collections.ObjectModel
Imports Permus

Public Class InboundTransfersBrowser
    Inherits commandableControl
    Protected pendingTransfers As PendingTransfers
    Dim ocpt As ObservableCollection(Of TransferTransaction)
    Dim autoaccetta As Boolean = My.Settings.DEBUG_autoaccetta

    Public Sub New()
        InitializeComponent()
        ocpt = New ObservableCollection(Of TransferTransaction)
        Me.LB.ItemsSource = ocpt
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
            Case "TICK"
                If C.userHasIncomingPendingTransfers Or Me.LB.Items.Count > 0 Then
                    CheckPendingTransfers()
                Else
                    Debug.Print("no pending transfers ....")
                End If
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
            Dim tt As TransferTransaction = e.AddedItems(0)
            Dim ttp As TransferTransactionPackage = C.api.transferTransaction(C.prepareSignedCommand(C.currentUser.x509Certificate, "<transferId>" & tt.transferId & "</transferId>")).Result
            If ttp IsNot Nothing Then
                ttp.correlateCompensations(C)
            End If
            PendingTransferDetail.transferTransactionPackage = ttp
        End If
    End Sub

    Protected Sub CheckPendingTransfers()
        pendingTransfers = C.api.pendigTransfers(C.prepareSignedCommand(C.currentUser.x509Certificate)).Result
        Dim toRemove As New List(Of TransferTransaction)
        For Each tt As TransferTransaction In ocpt
            If pendingTransfers.ContainsKey(tt.transferId) Then
                pendingTransfers(tt.transferId).tagged = True
            Else
                toRemove.Add(tt)
            End If
        Next

        For Each tt As TransferTransaction In toRemove
            ocpt.Remove(tt)
        Next

        For Each pti As PendingTransferInfo In pendingTransfers.Values.Where(Function(x) x.tagged = False)
            Dim ttp As TransferTransactionPackage = C.api.transferTransaction(C.prepareSignedCommand(C.currentUser.x509Certificate, "<transferId>" & pti.id & "</transferId>")).Result
            If ttp IsNot Nothing Then
                ttp.transaction.loadSubjects(C)

                If autoaccetta Then
                    C.acceptTransaction(ttp)
                Else
                    ocpt.Add(ttp.transaction)
                End If

            End If
        Next

        If LB.SelectedIndex = -1 Then
            If ocpt.Count > 0 Then
                LB.SelectedIndex = 0
            Else
                PendingTransferDetail.transferTransactionPackage = Nothing
            End If
        End If
    End Sub


End Class
