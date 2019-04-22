Imports System.Security.Cryptography.X509Certificates

Public Enum BlockChainNotifyEventType
    Ready = 0
    NeedsResyc = 1
    Block_Sync_Local = 10
    Block_Sync_remote = 11

    PrivateBlock_Sync_Local = 12
    PrivateBlock_Sync_remote = 13

    PrivateBlock_Missing = 13

    currentUserHasIncomingPendingTransfers = 20
    currentUserNotEnrolled = 21

    generalNotification = 90

    blockFetchedIsInvalid = 98
    BlockChainIsInvalid = 99
End Enum

Public Class BlockChain
    Protected Friend currentBlock As Block = New Block
    Protected _blockChainValid As Boolean
    Protected Friend _subjects As New Subjects
    Protected _systemInfo As New SystemInfo
    Protected isCurrentlyParsing As Boolean = False
    Protected newTransactionsCount As Long = 0
    Public partial_updates_count As Integer = 100
    Public Event LocalBlockChainUpdated()
    Public Event NewTransactionsFetched(newTransactionsCount As Long)

    Public Overridable ReadOnly Property systemInfo As SystemInfo
        Get
            Return _systemInfo
        End Get
    End Property

    Public ReadOnly Property blockChainValid As Boolean
        Get
            Return _blockChainValid
        End Get
    End Property

    Protected Function isBlockValid(blockToVerify As Block, nextBlock As Block) As Boolean
        If nextBlock.serial = blockToVerify.serial + 1 Then
            Return isBlockValid(blockToVerify, nextBlock.prev_vers)
        Else
            Return False
        End If
    End Function

    Public ReadOnly Property subjects As Subjects
        Get
            Return _subjects
        End Get
    End Property

    Protected Function isBlockValid(blockToVerify As Block, hashRecordedInNextBlockInChain As Byte()) As Boolean
        Dim hashOfBlockToVerify As Byte() = blockToVerify.computeHash()
        If utility.areEquals(hashOfBlockToVerify, hashRecordedInNextBlockInChain) Then
            Return True
        Else
            Return False
        End If
    End Function

    Protected Friend Overridable Function obtainBlock(serial As Long, Optional forceLoadFromBlockMaster As Boolean = False, Optional fireNotifyEvents As Boolean = True) As Block
        ' implementation depends on the type of derived object
        Return Nothing
    End Function

    Public Overridable Sub onSubjectAddedOrUpdated(subject As Subject)
    End Sub

    Protected Overridable Sub parseTransaction(tr As Transaction, b As Block)
        Select Case True
            Case TypeOf tr Is SubjectIdentityTransaction
                Dim sit As SubjectIdentityTransaction = tr
                If Not _subjects.certificateExists(sit.subjectCertificate.Thumbprint) Then
                    Me.newTransactionsCount += 1
                    Dim sbj As Subject = _subjects.addCertificate(sit.subjectCertificate)

                    Try
                        onSubjectAddedOrUpdated(sbj)
                    Catch ex As Exception
                    End Try

                End If

        End Select
    End Sub

    Protected Overridable Sub parseBlock(b As Block)
        Dim tr As Transaction
        If b IsNot Nothing Then
            For Each tr In b.transactions.OrderBy(Function(x) x.serial)
                parseTransaction(tr, b)
            Next
        End If
    End Sub

    Protected Sub Reset()
        _subjects.Clear()
        currentBlock = New Block
    End Sub

    Protected Overridable Function parseBlockChain(Optional blockToStart As Long = -1) As Boolean


        If blockToStart = -1 Then blockToStart = 1  ' il valore -1 serve come default nella classe CLientBlockChain derivata

        isCurrentlyParsing = True
        newTransactionsCount = 0

        If blockToStart = 1 Then
            Reset()
        End If

        Dim cb As Block
        Dim nb As Block

        _blockChainValid = True
        cb = obtainBlock(blockToStart)

        parseBlock(cb)
        Dim partial_updates_counter As Integer = 0

        For i As Integer = blockToStart + 1 To _systemInfo.currentBlockSerial

            nb = obtainBlock(i)

            If nb IsNot Nothing Then
                parseBlock(nb)
            Else
                _blockChainValid = False
            End If

            cb = nb

            ' ogni tanto informiamo l'universo che la BC ha subito modifiche
            partial_updates_counter += 1
            If partial_updates_counter > Me.partial_updates_count And (_systemInfo.currentBlockSerial - i) > Me.partial_updates_count Then
                partial_updates_counter = 0
                RaiseEvent LocalBlockChainUpdated()
            End If

        Next
        currentBlock = cb

        isCurrentlyParsing = False

        RaiseEvent LocalBlockChainUpdated()
        If newTransactionsCount > 0 Then
            RaiseEvent NewTransactionsFetched(newTransactionsCount)
        End If

        Return _blockChainValid
    End Function

End Class
