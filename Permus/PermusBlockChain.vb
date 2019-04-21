Imports System.Security.Cryptography.X509Certificates
Imports Permus

'Public Enum BlockChainNotifyEventType
'    Ready = 0
'    NeedsResyc = 1
'    Block_Sync_Local = 10
'    Block_Sync_remote = 11

'    PrivateBlock_Sync_Local = 12
'    PrivateBlock_Sync_remote = 13

'    PrivateBlock_Missing = 13

'    currentUserHasIncomingPendingTransfers = 20
'    currentUserNotEnrolled = 21

'    generalNotification = 90

'    blockFetchedIsInvalid = 98
'    BlockChainIsInvalid = 99
'End Enum

Public Class PermusBlockChain
    Inherits GimusBlockChain
    Protected Friend allTransferTransactions As New List(Of String)
    Protected newTransferTransactions As New List(Of TransferTransaction)

    Protected Overrides Sub parseTransaction(tr As Transaction, b As Block)
        Select Case True

            Case TypeOf tr Is TransferTransaction
                Dim tt As TransferTransaction = tr
                If Not Me.allTransferTransactions.Contains(tt.transferId) Then
                    Me.newTransactionsCount += 1
                    Me.allTransferTransactions.Add(tt.transferId)
                    Me.newTransferTransactions.Add(tt)

                    Dim s_f As Subject = subjects.getElementById(tt.fromSubject)
                    Dim s_t As Subject = subjects.getElementById(tt.toSubject)

                    If tt.isSale Then
                        s_t.coinBalance.addSpent(tt.coinAmount)
                        s_f.coinBalance.addEarned(tt.coinAmount)
                    Else
                        s_f.coinBalance.addSpent(tt.coinAmount)
                        s_t.coinBalance.addEarned(tt.coinAmount)
                    End If
                End If
            Case Else
                MyBase.parseTransaction(tr, b)
        End Select
    End Sub

    Protected Overrides Sub Reset()
        MyBase.Reset()
        allTransferTransactions.Clear()
    End Sub

End Class
