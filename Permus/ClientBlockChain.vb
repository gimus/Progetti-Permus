Imports System.Collections.ObjectModel
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Threading
Imports System.Windows.Threading
Imports Permus

Public Enum localStoreTypeEnum
    xml_plain = 0
    xml_compressed = 1
    msaccessdb = 2
End Enum

Public Class ClientBlockChain
    Inherits BlockChain
    Implements iWebApiAsyncReceiver
    Public Shared C As ClientBlockChain

    Protected da As ClientBlockChainDataAdapter
    Public api As BlockMasterWebApiClient

    Protected user As Subject
    Protected blockCache As New BlockCache(2)
    Public blockMasterSeemsOnLine As Boolean = False
    Public userHasIncomingPendingTransfers As Boolean = False

    Public userTransferTransactions As New Dictionary(Of String, TransferTransaction)
    Public localStoreType As localStoreTypeEnum = localStoreTypeEnum.msaccessdb
    Public extendedInfoToRequest As String = ""
    Public lastSnapshotBlock As Long = 1

    Dim serverPollingTimer As DispatcherTimer

    Public Event DataReceivedFromBlockMaster(requestURI As String, data As Object)
    Public Event ErrorReceivingDataFromBlockMaster(requestURI As String, ex As Exception)

    Protected currentUserEnrollmentChecked As Boolean = False
    Protected timestamp_offset As Long = Long.MinValue
    Protected blockToStartAsycBlockChainParsing As Long = 1

    Public Event BlockChainTick()
    Public Event Notification(eventType As BlockChainNotifyEventType, eventMessage As String)

    Public Sub New(BlockMasterPollingTimerIntervalSeconds As Integer, Optional localStoreType As localStoreTypeEnum = localStoreTypeEnum.xml_compressed)
        C = Me
        Me.localStoreType = localStoreType
        userTransferTransactions = New Dictionary(Of String, TransferTransaction)

        serverPollingTimer = New DispatcherTimer()
        AddHandler serverPollingTimer.Tick, AddressOf serverPollingTimer_Tick
        serverPollingTimer.Interval = New TimeSpan(0, 0, BlockMasterPollingTimerIntervalSeconds)
        serverPollingTimer.Stop()
    End Sub

    Public Function purgeLocalData() As Boolean
        currentBlock = New Block
        Return da.purgeLocalData()
    End Function

    Public Function ConnectToBlockMaster(uri As Uri) As Boolean
        _systemInfo = New SystemInfo
        serverPollingTimer.Stop()
        api = New BlockMasterWebApiClient(uri)
        Reset()

        If updateBlockChainInfo() Then
            If Me.user IsNot Nothing Then
                Select Case localStoreType
                    Case localStoreTypeEnum.msaccessdb
                        da = New ClientBlockMSAccessDbDataAdapter(Me)
                    Case localStoreTypeEnum.xml_compressed
                        da = New ClientBlockChainZippedFileSystemDataAdapter(Me)
                    Case localStoreTypeEnum.xml_plain
                        da = New ClientBlockChainFileSystemDataAdapter(Me)
                End Select


                If da.isSnapshotAvailable(Me.user.id) Then
                    Try
                        If Not da.InitBlockChainFromSnapshot(Me) Then
                            Reset()
                        End If
                    Catch ex As Exception
                        Reset()
                    End Try
                    lastSnapshotBlock = currentBlock.serial
                End If

                syncWithBlockMaster()

                serverPollingTimer.Start()

            End If

            Return True
        Else
            Return False
        End If

    End Function

    Public Property currentUser As Subject
        Get
            Return user
        End Get
        Set(value As Subject)
            If user IsNot Nothing AndAlso user.id <> value.id Then
                _systemInfo = New SystemInfo
                currentBlock = New Block()
            End If
            user = value
            currentUserEnrollmentChecked = False
            If blockMasterSeemsOnLine Then
                CheckCurrentUserEnrollment()
            End If
        End Set
    End Property

    Public Function isCurrentBlockObsolete() As Boolean

        If currentBlock Is Nothing Then
            currentBlock = New Block()
        End If
        Return currentBlock.serial < _systemInfo.currentBlockSerial Or currentBlock.transactions.Count < _systemInfo.currentTransactionSerial
    End Function

    Protected Function isBlockFetchedFromBlockMasterValid(b As Block, Optional fireNotifyEvents As Boolean = True) As Boolean
        ' il blocco caricato dal BlockMaster potrebbe essere non valido per numerosi motivi ... vediamoli uno per uno:
        If b Is Nothing Then
            If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block Empty - Could not fetch it!"))
            Return False
        End If

        If b.serial = 1 Then
            ' per il momento prendiamo per buono il primo blocco della blockchain perché non vi è modo per verificare la sua validità con un blocco precedente ...
            Return True
        Else
            ' andiamo a prenderci il blocco immediatamente precedente dalla cache in memoria oppure dal db locale
            Dim bprec As Block = blockCache.getBlock(b.serial - 1)
            If bprec Is Nothing Then
                bprec = da.getBlock(b.serial - 1)
            End If

            ' l'hash del blocco precedente deve essere quello indicato bel blocco in esame
            If Not Me.isBlockValid(bprec, b) Then
                Dim bloc_valid As Boolean = False

                ' ma esiste la possibilità che il blocco locale sia obsoleto... verifichiamolo
                Dim bprec_s As Block = api.getBlock(b.serial - 1).Result
                ' ovviamente la catena deve rimanere integra e quindi anche il blocco preso dal server deve puntare correttamente al precedente
                If utility.areEquals(bprec_s.prev_vers, bprec.prev_vers) Then
                    ' se la copia sul server è una versione aggiornata (piu transazioni) di quella nella chain locale ... lo prendiamo per buono
                    If bprec_s.transactions.Count > bprec.transactions.Count Then
                        da.saveBlock(bprec_s)
                    End If
                    ' rifacciamo il controllo 
                    bloc_valid = Me.isBlockValid(bprec_s, b)
                End If

                If Not bloc_valid Then
                    If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block # {0} fetched from BlockMaster is invalid: PREVIOUS_BLOCK_HASH_MISMATCH", b.serial))
                    Return False
                End If

            End If

            ' l'hash delle transazioni contenute nel blocco fornitoci dal BlockMaster deve essere corretto
            If Not utility.areEquals(b.trans_root, b.transactions.computeHash()) Then
                If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block # {0} fetched from BlockMaster is invalid: TRANS_ROOT_MISMATCH", b.serial))
            End If

            ' vediamo se  esiste una versione locale del blocco che abbiamo caricato dal blockMaster
            Dim bVersioneLocale As Block = da.getBlock(b.serial)

            ' se esiste dobbiamo verificare la bonta' del contenuto della nuova versione del blocco analizzando le transazioni in essi contenute
            If bVersioneLocale IsNot Nothing Then
                If Not utility.areEquals(b.trans_root, bVersioneLocale.trans_root) Then

                    ' non essendo uguali i trans_root, allora il nuovo blocco deve avere più transazioni di quello attualmente presente nella local blockchain, 
                    ' se ha le stesse o di meno allora non va certo bene

                    Dim ntLoc As Integer = bVersioneLocale.transactions.Count

                    If b.transactions.Count <= ntLoc Then
                        If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block # {0} fetched from BlockMaster is invalid: TRANSACTIONS_INVALID", b.serial))
                        Return False
                    Else
                        ' l'hash delle transazioni comuni deve essere lo stesso
                        ' ciò garantisce che oltre alle nuove transazioni non siano state modificate le transazioni precedenti
                        Dim btr() As Byte = b.transactions.computeHash(ntLoc)
                        If Not utility.areEquals(bVersioneLocale.trans_root, btr) Then
                            If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block # {0} fetched from BlockMaster is invalid: TRANSACTIONS_INVALID", b.serial))
                            Return False
                        End If

                    End If
                End If
            End If
        End If
        Return True
    End Function

    Protected Overrides Function obtainBlock(serial As Long, Optional forceLoadFromBlockMaster As Boolean = False, Optional fireNotifyEvents As Boolean = True) As Block
        Dim b As Block = Nothing

        ' proviamo ad ottenere il blocco dalla copia locale della blockchain che per definizione è sempre "trusted 
        ' a meno che non sia l'ultimo blocco localmente registrato (che potrebbe non essere aggiornato) o non vogliamo una nuova versione dal BlockMaster (es. nel resync)"

        If Not forceLoadFromBlockMaster Then
            If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.PrivateBlock_Sync_Local, String.Format("Loading block # {0} from local repository", serial))
            b = da.getBlock(serial)
            If Not da.blockExists(serial + 1) Then
                b = Nothing ' si tratta dell'ultimo blocco registrato localmente: forziamo il caricamento del blocco aggiornato dal blockmaster
            End If
        End If

        If b Is Nothing Then
            ' se il blocco non è reperibile localmente dobbiamo chiederlo al BlockMaster
            If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.PrivateBlock_Sync_remote, String.Format("Loading block # {0} from BlockMaster", serial))
            b = api.getBlock(serial).Result
            ' verifichiamo la validità del blocco ricevuto perché il blockmaster potrebbe essere stato hackerato oppure la trasmissione alterata da un "Man In The Middle" 
            If isBlockFetchedFromBlockMasterValid(b, fireNotifyEvents) Then
                ' se tutto ok, facciamo nostro il blocco includendolo nella blockchain locale
                da.saveBlock(b)
                If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.generalNotification, String.Format("Block # {0} fetched from BlockMaster saved in the local version of BlockChain!", serial))
            Else
                ' altrimenti mandiamo un allarme e ignoriamo il blocco
                If fireNotifyEvents Then RaiseEvent Notification(BlockChainNotifyEventType.blockFetchedIsInvalid, String.Format("Block # {0} fetched from BlockMaster is invalid: It will be ignored!", serial))
                b = Nothing
            End If
        End If

        If b IsNot Nothing Then
            Me.blockCache.Add(b)
        End If

        Return b
    End Function

    Public Function getBlock(serial As Long) As Block
        Return obtainBlock(serial, False, False)
    End Function

    Protected Sub serverPollingTimer_Tick(ByVal sender As Object, ByVal e As EventArgs)
        Try
            syncWithBlockMaster()
            RaiseEvent BlockChainTick()
        Catch ex As Exception
            Debug.Print(ex.Message)
        End Try
    End Sub

    Public Function syncWithBlockMaster() As Boolean

        updateBlockChainInfo()
        If blockMasterSeemsOnLine Then

            If isCurrentBlockObsolete() And Not isCurrentlyParsing Then

                RaiseEvent Notification(BlockChainNotifyEventType.NeedsResyc, "initiating BlockChain resync")

                parseBlockChainInParallelThread(currentBlock.serial)

            End If

            CheckCurrentUserEnrollment()

            If lastSnapshotBlock < (currentBlock.serial - 2) Then
                saveSnapshotAsync()
                lastSnapshotBlock = currentBlock.serial
            End If
        End If
        Return True
    End Function

    Protected Sub CheckCurrentUserEnrollment()
        If Not currentUserEnrollmentChecked And Me.subjects.Count > 0 Then
            If Me.subjects.getElementById(currentUser.id) Is Nothing Then
                RaiseEvent Notification(BlockChainNotifyEventType.currentUserNotEnrolled, currentUser.id)
            Else
                currentUserEnrollmentChecked = True
                user.isAuthority = (Me.user.id = _subjects.getAuthority.id)
            End If
        End If

    End Sub

    Public Function synced_timestamp() As Long

        If timestamp_offset = Long.MinValue Then
            Me.timestamp_offset = utility.getCurrentTimeStamp() - Me.api.getSystemTimestamp().Result
        End If

        Return utility.getCurrentTimeStamp() - Me.timestamp_offset
    End Function

    Public Function updateBlockChainInfo() As Boolean
        Try
            Dim userId As String = ""
            If user IsNot Nothing Then
                userId = user.id
            End If
            Dim si As SystemInfo
            If userId <> "" Then
                If user.token <> "" Then
                    si = api.getBlockChainInfo(userId, user.token, extendedInfoToRequest).Result
                Else
                    si = api.getCheckIn(C.prepareSignedCommand(user.x509Certificate)).Result

                    If si IsNot Nothing AndAlso si.requesterInfo IsNot Nothing Then
                        user.token = si.requesterInfo.token
                    Else
                        si = api.getBlockChainInfo(userId).Result
                    End If
                End If
            Else
                si = api.getBlockChainInfo().Result
            End If
            blockMasterSeemsOnLine = True
            processNewBlockChainInfo(si)

        Catch ex As Exception
            blockMasterSeemsOnLine = False
        End Try
        Return blockMasterSeemsOnLine
    End Function

    Protected Sub processNewBlockChainInfo(si As SystemInfo)
        If si IsNot Nothing Then
            _systemInfo = si
            If si.currentTimeStamp <> 0 Then
                Me.timestamp_offset = utility.getCurrentTimeStamp() - si.currentTimeStamp
            End If

            blockMasterSeemsOnLine = True
            userHasIncomingPendingTransfers = False
            If _systemInfo.requesterInfo IsNot Nothing Then
                If _systemInfo.requesterInfo.userId = Me.currentUser.id And _systemInfo.requesterInfo.userIncomingPendingTransfersCount > 0 Then
                    userHasIncomingPendingTransfers = True
                    RaiseEvent Notification(BlockChainNotifyEventType.currentUserHasIncomingPendingTransfers, _systemInfo.requesterInfo.userIncomingPendingTransfersCount.ToString)
                End If
            End If
        End If
    End Sub
    Public Function IsOtpTestOk(otp As String) As Boolean
        Dim req() As Byte = prepareSignedCommand(currentUser.x509Certificate, String.Format("<command><funzione>otp_test</funzione><otp>{0}</otp></command>", otp))
        Dim b As SubjectProfile = api.SubjectProfile(req).Result
        Return b IsNot Nothing
    End Function

    Public Function IsPfxTestOk() As Boolean
        Dim req() As Byte = prepareSignedCommand(currentUser.x509Certificate, String.Format("<command><funzione>pfx_test</funzione></command>"))
        Dim b As SubjectProfile = api.SubjectProfile(req).Result
        Return b IsNot Nothing
    End Function

    Public Function ModifyCurrentUserProfile(attribute As String, value As String) As Boolean
        Dim req() As Byte = prepareSignedCommand(currentUser.x509Certificate, String.Format("<command><funzione>modifica_attributo</funzione><P1>{0}</P1><P2>{1}</P2><P3></P3><P4></P4></command>", attribute, value))
        Dim b As SubjectProfile = api.SubjectProfile(req).Result
        Return b IsNot Nothing
    End Function

    Public Function EnrollNewUser(subj As Subject) As Boolean
        Dim req() As Byte = prepareRequestForCertificateEnroll(subj.x509Certificate)
        Dim b As Block = api.enrollCertificate(req).Result
        Return b IsNot Nothing
    End Function

    Protected Function prepareRequestForCertificateEnroll(Optional cer As X509Certificate2 = Nothing) As Byte()
        If cer Is Nothing Then
            cer = currentUser.x509Certificate
        End If
        If cer IsNot Nothing Then
            Dim s As String = synced_timestamp()
            Dim b() As Byte = Encoding.UTF8.GetBytes(s)
            Return computeDigitalSignatureAndGenerateP7M(b, cer)
        Else
            Return Nothing
        End If
    End Function

    Public Function prepareSignedCommand(Optional cer As X509Certificate2 = Nothing, Optional command As String = "<command></command>") As Byte()
        If cer Is Nothing Then
            cer = Me.user.x509Certificate
        End If
        Dim BlockMasterTimeStamp As String = synced_timestamp()
        Return utility.computeDigitalSignatureAndGenerateP7M(utility.getBytesFromString(command), cer, BlockMasterTimeStamp)
    End Function

    Public Function computeDigitalSignatureAndGenerateP7M(data() As Byte, cer As X509Certificate2) As Byte()
        Dim timestamp As Long = synced_timestamp()
        Return utility.computeDigitalSignatureAndGenerateP7M(data, cer, timestamp)
    End Function

    Public Sub dataReady(requestURI As String, data As Object) Implements iWebApiAsyncReceiver.dataReady
        RaiseEvent DataReceivedFromBlockMaster(requestURI, data)
    End Sub

    Public Sub exception(requestURI As String, ex As Exception) Implements iWebApiAsyncReceiver.exception
        Debug.Print(ex.Message)
        RaiseEvent ErrorReceivingDataFromBlockMaster(requestURI, ex)
    End Sub
    Protected Sub parseBlockChainInParallelThread(Optional blockToStart As Long = 1)
        Me.blockToStartAsycBlockChainParsing = blockToStart
        Dim oThread As New Thread(AddressOf Me.parseBlockChain)
        oThread.Start()
    End Sub

    Protected Overrides Function parseBlockChain(Optional blockToStart As Long = -1) As Boolean
        If Me.user IsNot Nothing Then
            If blockToStart = -1 Then blockToStart = Me.blockToStartAsycBlockChainParsing

            If blockToStart = 1 Then
                userTransferTransactions = New Dictionary(Of String, TransferTransaction)
            End If

            ' lanciamo il processo di sincronizzazione con la blockchain online, 
            If MyBase.parseBlockChain(blockToStart) Then

                ' se ci sono nuove transfer transactions le processiamo
                For Each tt As TransferTransaction In Me.newTransferTransactions
                    If tt.affectsSubject(C.currentUser.id) Then
                        parseNewUserTransferTransaction(tt)
                    End If
                Next
                newTransferTransactions.Clear()
            End If

            If Me._blockChainValid Then
                RaiseEvent Notification(BlockChainNotifyEventType.Ready, "BlockChain Ready")
            Else
                RaiseEvent Notification(BlockChainNotifyEventType.BlockChainIsInvalid, "BlockChain is invalid")
            End If

            Return True
        Else
            Return False
        End If
    End Function

    Public Sub parseNewUserTransferTransaction(t As TransferTransaction)
        If Not userTransferTransactions.ContainsKey(t.transferId) Then
            t.loadSubjects(Me)
            userTransferTransactions.Add(t.transferId, t)

            Dim pt As PrivateCoSignedTransferTransaction
            If t.isPrivate Then
                pt = t
                pt.obtainPrivateBlock(Me)
            End If

            If t.isSale Then
                t.compensation = 100
            Else
                Select Case t.transferTransactionType
                    Case TransferTransactionTypeEnum.PrivateCompensation
                        Dim pc As PrivateCompensation = t
                        If pc.privateBlock IsNot Nothing Then
                            Dim elements As List(Of TransferElement) = pc.privateBlock.transactions.getTransferElements()

                            For Each tc As TransferCompensation In pc.privateBlock.transactions.getTransferCompensations()
                                tc.elements = elements
                                Dim tc_key As String = String.Format("{0}_{1}", pc.transferObject.description, tc.serial)
                                Dim ref_key As String = tc.referencedItemId
                                If Me.userTransferTransactions.ContainsKey(ref_key) Then
                                    Dim rt As TransferTransaction = Me.userTransferTransactions(ref_key)
                                    If rt.compensations Is Nothing Then
                                        rt.compensations = New Dictionary(Of String, TransferCompensation)
                                    End If
                                    If Not rt.compensations.ContainsKey(tc_key) Then
                                        rt.compensations.Add(tc_key, tc)
                                        rt.compensation += tc.percentCompensated
                                    End If
                                End If
                            Next
                        Else
                            RaiseEvent Notification(BlockChainNotifyEventType.PrivateBlock_Missing, "Unable to obtain private block hash: " & pc.transferObject.description)
                        End If
                End Select
            End If

            If t.isPrivate Then
                pt = t
                pt.privateBlock = Nothing ' eliminiamo i riferimento al blocco per risparmiare RAM. se serve il sistema lo ricaricherà all'occorrenza
            End If

        End If

    End Sub

    Public Function acceptTransaction(ttp As TransferTransactionPackage) As Boolean
        If ttp IsNot Nothing Then
            Dim cstt As CoSignedTransferTransaction = ttp.transaction
            cstt.computeSignatureTo(Me)
            ttp = api.transferTransactionAccept(ttp, prepareSignedCommand(currentUser.x509Certificate, "<transferId>" & ttp.transaction.transferId & "</transferId>")).Result

            If ttp IsNot Nothing Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If
    End Function

    Public Function obtainPrivateBlock(blockHash As String) As Block
        Try
            Dim b As Block = da.getPrivateBlock(blockHash, user.id)
            If b Is Nothing Then
                ' scarichiamolo dal server
                Dim eb As EnvelopedBlock = api.getPrivateBlock(blockHash).Result
                If Not eb Is Nothing Then
                    b = eb.getBlock()
                    If utility.bin2hex(b.computeHash) = blockHash Then
                        da.savePrivateBlock(b, Me.user.id)
                        Return b
                    Else
                        Return Nothing
                    End If
                Else
                    Return Nothing
                End If
            Else
                Return b
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Sub savePrivateBlock(b As Block)
        da.savePrivateBlock(b, user.id)
    End Sub

    Public Sub saveSnapshotAsync()
        System.Threading.ThreadPool.QueueUserWorkItem(AddressOf doSaveSnapshot)
    End Sub
    Protected Sub doSaveSnapshot(state As Object)
        da.SaveSnapshot(Me)
    End Sub

    Public Sub browseAppDataDirectory()
        Try
            Process.Start("explorer.exe", "/root," & da.applicationDataFolder)
        Catch ex As Exception
        End Try
    End Sub

End Class
