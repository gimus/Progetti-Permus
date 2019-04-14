Imports System.Security.Cryptography.X509Certificates
Imports Permus

Public Class BlockMasterBlockChain
    Inherits BlockChain
    Protected Friend pendingTransferTransactions As New Dictionary(Of String, TransferTransactionPackage)

    Public Shared M As BlockMasterBlockChain
    Public da As BlockMasterDataAdapter

    Public Sub New(localDBConnectionString As String)
        M = Me
        da = New BlockMasterDataAdapter(localDBConnectionString)
        readCurrentStatusFromDB()
    End Sub

    Protected Sub readCurrentStatusFromDB()
        updateSystemInfo()

        ' se la blockchain è vuota inizializziamola creando il primo blocco che registriamo nel nostro database 
        If _systemInfo.currentBlockSerial = 0 Then

            Dim firstBlock As New Block
            With firstBlock
                .version = _systemInfo.blockChainVersion
                .timestamp = utility.getUTC()
                .serial = 1
                .prev_vers = {0}
            End With

            saveBlockToDB(firstBlock)

            Dim cer As X509Certificate2

            cer = utility.getCertificateFromStoreBySubjectName(My.Settings.CER_Authority, StoreName.My, StoreLocation.LocalMachine)
            If cer IsNot Nothing Then
                enrollCertificate(cer)
            End If

            cer = utility.getCertificateFromStoreBySubjectName(My.Settings.CER_SubAuthority, StoreName.My, StoreLocation.LocalMachine)
            If cer IsNot Nothing Then
                enrollCertificate(cer)
            End If

        End If

        parseBlockChain(1)

        currentBlock = readBlockFromDB(_systemInfo.currentBlockSerial)

        updateSystemInfo()
    End Sub

    Protected Sub updateSystemInfo()
        _systemInfo = da.getSystemInfo()
        _systemInfo.blockMasterReady = blockChainValid
    End Sub

    Public Overloads ReadOnly Property systemInfo(Optional userId As String = "", Optional token As String = "") As SystemInfo
        Get
            updateSystemInfo()
            Dim si As SystemInfo = _systemInfo.Clone()

            With si
                If currentBlock IsNot Nothing Then
                    .currentTransactionSerial = currentBlock.transactions.Count
                End If

                If userId <> "" And token <> "" Then
                    si.requesterInfo = getUserInfo(userId, True)
                    si.requesterInfo.isOnline = True
                End If

            End With

            Return si
        End Get
    End Property

    Public Function getUserInfo(userId As String, Optional updatePing As Boolean = False) As UserInfo
        Dim ui As New UserInfo
        Dim subj As Subject = subjects.getElementById(userId)
        With ui
            If subj IsNot Nothing Then
                .userId = userId
                .coinBalance = subj.coinBalance.balance
                .userIncomingPendingTransfersCount = UserIncomingPendingTransfersCount(userId)
                .userOutgoingPendingTransfersCount = UserOutgoingPendingTransfersCount(userId)
                .isOnline = Now().Subtract(subj.lastPing).TotalSeconds <= 10
                If updatePing Then
                    subj.lastPing = Now()
                End If
            End If
        End With
        Return ui
    End Function

    Protected Function readBlockFromDB(serial As Long) As Block
        Return da.getBlock(serial)
    End Function

    Protected Function saveBlockToDB(b As Block) As Boolean
        Return da.saveBlock(b)
    End Function

    Protected Overrides Function obtainBlock(serial As Long, Optional dummyAndIninfluentParameter As Boolean = False, Optional fireNotifyEvents As Boolean = True) As Block
        Return readBlockFromDB(serial)
    End Function

    Protected Sub addTransactionToCurrentBlock(tr As Transaction)
        ' aggiungiamo la transazione al blocco corrente
        currentBlock.addTransaction(tr)
        ' ne assorbiamo i dati pubblici rilevanti
        parseTransaction(tr, currentBlock)

        ' se abbiamo raggiunto il numero massimo di transazioni previste per il blocco, lo salviamo e poi ne creiamo uno nuovo
        If currentBlock.transactions.Count >= _systemInfo.maxTransactionsPerBlock Then
            ' salviamo nel DB il blocco corrente
            saveBlockToDB(currentBlock)
            Dim oldBlock As Block = currentBlock
            ' creiamo un nuovo Blocco
            currentBlock = createNextBlock(oldBlock)

        End If

        ' salviamolo immediatamente
        saveBlockToDB(currentBlock)
        updateSystemInfo()
    End Sub

    Protected Function createNextBlock(previousBlock As Block) As Block
        Dim b As New Block
        With b
            .version = _systemInfo.blockChainVersion
            .timestamp = utility.getUTC()
            If previousBlock IsNot Nothing Then
                .serial = previousBlock.serial + 1
                .prev_vers = previousBlock.computeHash()
            Else
                .serial = 1
                .prev_vers = {0}
            End If
        End With
        Return b
    End Function

    Public Overrides Sub onSubjectAddedOrUpdated(subject As Subject)
        Dim dt As DataTable = da.getSubjectProfiles("subjectId", subject.id)
        If dt.Rows.Count = 0 Then
            dt = da.GestioneProfilo(subject.id, "CREA", subject.name, subject.email)
        End If
        If dt.Rows.Count > 0 Then
            subject.profile = Factory.leggiProtectedSubjectProfile(dt.Rows(0))
            If subject.profile.hasTelegram Then
                App.H.AddSubject(subject)
            End If
        End If

    End Sub

    Public Function enrollCertificate(cer As X509Certificate2) As Block

        ' check if certificate was already present
        If _subjects.certificateExists(cer.Thumbprint) Then
            Throw New Exception("Certificate already enrolled!")
        End If

        ' check if certificate was issued by a valid Certification Authority
        If Not utility.isIssuerCertificateTrusted(cer, Me.systemInfo.certificationAuthorities) Then
            Throw New Exception("Certificate not trusted!")
        End If
        ' check if certificate subject is valid format
        Dim subject As Subject = Subject.fromX509Certificate(cer)

        If Not subject.isValid Then
            Throw New Exception("Certificate Subject not valid!")
        End If

        ' save identity in the chain

        Dim tr As New SubjectIdentityTransaction
        tr.subjectCertificate = cer
        tr.subject = tr.subjectCertificate.Subject
        addTransactionToCurrentBlock(tr)

        ' mantain a copy in memory

        _subjects.addCertificate(cer)

        Return currentBlock
    End Function

    Public Function manageTransferTransaction(type As String, requester As Subject, ttp As TransferTransactionPackage) As TransferTransactionPackage
        Select Case type
            Case "TransferTransactionInit"
                ttp.transaction.loadSubjects(Me)


                If TypeOf ttp.transaction Is CoinCreation Then
                    check(ttp.transaction.sFrom.isAuthority, "Only BlockChain Authorities can create coins")
                Else
                    If ttp.transaction.isSale Then
                        check(ttp.transaction.sTo.coinBalance.balance >= ttp.transaction.coinAmount, String.Format("{0}: not enough coins to pay", ttp.transaction.sTo.name))
                    Else
                        check(ttp.transaction.sFrom.coinBalance.balance >= ttp.transaction.coinAmount, String.Format("{0}: not enough coins to transfer", ttp.transaction.sFrom.name))
                    End If

                    If ttp.transaction.isPrivate Then
                        Dim pt As PrivateCoSignedTransferTransaction = ttp.transaction
                        ' verifichiamo che non si tratti di un tentativo di fare una transizione nuova con un blocco già usato in una transazione aggiunta in precedenza
                        check(Not da.existsPrivateBlock(pt.transferObject.description), String.Format("invalid private block!"))

                        ' verifichiamo che non si tratti di un tentativo di fare una transizione nuova con un blocco già usato in una transazione pending
                        For Each tx As TransferTransactionPackage In Me.pendingTransferTransactions.Values.Where(Function(x) x.transaction.isPending And x.transaction.isPrivate)
                            Dim ptx As PrivateCoSignedTransferTransaction = tx.transaction
                            check(ptx.transferObject.description <> pt.transferObject.description, String.Format("invalid private block!"))
                        Next

                    End If
                End If

                check(ttp.transaction.transferId = "", "Transfer transaction already initialized")
                check(ttp.transaction.fromSubject <> "", "Missing SubjectFrom")
                check(ttp.transaction.toSubject <> "", "Missing SubjectTo")
                check(_subjects.ContainsKey(ttp.transaction.fromSubject), "Bad SubjectFrom")
                check(_subjects.ContainsKey(ttp.transaction.toSubject), "Bad SubjectTo")
                check(ttp.transaction.toSubject <> ttp.transaction.fromSubject, "Same Subjects")
                check(ttp.transaction.fromSubject = requester.id, "Requester is not subjectFrom")

                ttp.transaction.transferId = Guid.NewGuid.ToString

                ttp.transaction.updateState()
                Me.pendingTransferTransactions.Add(ttp.transaction.transferId, ttp)

                ' approfittiamo per fare un po' di pulizia
                executeHouseKeeping()


            'Case "TransferTransactionInfo"
            '    Dim transferId As String = utility.getChildElementValue(command, "transferId")
            '    ttp = pendingTransferTransactions.Values.Where(Function(x) (x.transaction.fromSubject = requester.id Or x.transaction.toSubject = requester.id) And (x.transaction.isPending Or x.transaction.transferId = transferId)).FirstOrDefault


            Case "TransferTransactionPropose"
                Dim oldttp = pendingTransferTransactions(ttp.transaction.transferId)

                ttp.transaction.loadSubjects(Me)

                check(oldttp IsNot Nothing, "Transfer transaction missing")
                check(oldttp.transaction.matches(ttp.transaction), "Transfer transactions mismatch")
                check(ttp.transaction.fromSubject = requester.id, "Requester is not subjectFrom")
                ' check(ttp.transaction.transferObjectDescription <> "", "Transfer Description Empty")

                ttp.transaction.signatureFrom.obtainCertificateFromSubject(ttp.transaction.sFrom)
                check(ttp.transaction.verifySignature(ttp.transaction.signatureFrom), "Bad SignatureFrom")

                Me.pendingTransferTransactions(ttp.transaction.transferId) = ttp

                ttp.transaction.updateState()

                If Not ttp.transaction.requireAcceptance Then
                    addTransactionToCurrentBlock(ttp.transaction)
                End If

                App.H.notifySubjectsTransferTransaction(ttp)

            Case "TransferTransactionAccept"
                Dim oldttp = pendingTransferTransactions(ttp.transaction.transferId)

                ttp.transaction.loadSubjects(Me)

                check(oldttp IsNot Nothing, "Transfer transaction missing")
                check(oldttp.transaction.matches(ttp.transaction), "Transfer transactions mismatch")
                check(oldttp.transaction.requireAcceptance, "Transaction doesn't require acceptance")

                check(ttp.transaction.toSubject = requester.id, "Requester is not subjectTo")

                Dim cst As CoSignedTransferTransaction = ttp.transaction

                cst.signatureFrom.obtainCertificateFromSubject(ttp.transaction.sFrom)
                check(cst.verifySignature(ttp.transaction.signatureFrom), "Bad SignatureFrom")

                cst.signatureTo.obtainCertificateFromSubject(ttp.transaction.sTo)
                check(cst.verifySignature(cst.signatureTo), "Bad SignatureTo")


                addTransactionToCurrentBlock(ttp.transaction)
                Me.pendingTransferTransactions(ttp.transaction.transferId) = ttp
                If ttp.isPrivate Then
                    da.savePrivateBlock(ttp.envelopedPrivateBlock, ttp.transaction)
                End If

                ttp.transaction.updateState()

                App.H.notifySubjectsTransferTransaction(ttp)

        End Select

        Return ttp
    End Function

    Protected Function UserOutgoingPendingTransfersCount(userId As String) As Integer
        Return pendingTransferTransactions.Values.Where(Function(x) (x.transaction.fromSubject = userId And x.transaction.isAwaitingApproval)).Count
    End Function

    Protected Friend Function UserIncomingPendingTransfersCount(userId As String) As Integer
        Return pendingTransferTransactions.Values.Where(Function(x) (x.transaction.toSubject = userId And x.transaction.isAwaitingApproval)).Count
    End Function

    Public Function UserPendingTransfers(userId As String) As PendingTransfers
        Dim pt As New PendingTransfers
        Dim pti As PendingTransferInfo
        For Each ttp As TransferTransactionPackage In pendingTransferTransactions.Values.Where(Function(x) (x.transaction.affectsSubject(userId)) And x.transaction.isPending)
            pti = New PendingTransferInfo
            pti.subjectFrom = ttp.transaction.fromSubject
            pti.subjectTo = ttp.transaction.toSubject
            pti.id = ttp.transaction.transferId
            pti.state = ttp.transaction.transferTransactionState
            pti.type = ttp.transaction.transferTransactionTitle
            pt.Add(pti.id, pti)
        Next
        Return pt
    End Function

    Public Function cancelPendingTransfer(requester As Subject, transferId As String) As PendingTransfers
        Dim ttp As TransferTransactionPackage
        ttp = pendingTransferTransactions.Values.Where(Function(x) x.transaction.affectsSubject(requester.id) And x.transaction.transferId = transferId).FirstOrDefault
        If ttp IsNot Nothing Then
            pendingTransferTransactions.Remove(ttp.transaction.transferId)
            ttp.transaction.cancel()
            App.H.notifySubjectsTransferTransaction(ttp)
            ttp = Nothing
        End If
        Return UserPendingTransfers(requester.id)
    End Function


    Public Function getPendingTransfer(requester As Subject, transferId As String) As TransferTransactionPackage
        Dim ttp As TransferTransactionPackage
        ttp = pendingTransferTransactions.Values.Where(Function(x) x.transaction.affectsSubject(requester.id) And x.transaction.transferId = transferId And x.transaction.isPending).FirstOrDefault
        check(ttp IsNot Nothing, "No current o recent transaction involves requesting user")
        Return ttp
    End Function

    Protected Sub executeHouseKeeping()
        Dim listanera As List(Of TransferTransactionPackage) = Nothing
        For Each tx As TransferTransactionPackage In Me.pendingTransferTransactions.Values.Where(Function(x) Not x.transaction.isPending)
            If Now().Subtract(tx.transaction.timestamp).TotalMinutes > 10 Then
                If listanera Is Nothing Then listanera = New List(Of TransferTransactionPackage)
                listanera.Add(tx)
            End If
        Next
        If listanera IsNot Nothing Then
            For Each tx As TransferTransactionPackage In listanera
                Me.pendingTransferTransactions.Remove(tx.transaction.transferId)
            Next
        End If
    End Sub

    Protected Shared Sub check(conditionToCheck As Boolean, errorMessage As String)
        If Not conditionToCheck Then
            Throw New Exception(errorMessage)
        End If
    End Sub

End Class
