Imports System.Security.Cryptography.X509Certificates
Imports System.Text

Public Enum TransferTransactionTypeEnum
    CoinTransfer = 0          ' il subjectFrom trasmette al subjectTo un certo ammontare di coin detratto dal subjectFrom. NON viene accettato un coinBalance negativo del subjectFrom
    CoinCreation = 1          ' il subjectFrom trasmette al subjectTo un certo ammontare di coin. viene accettato un coinBalance negativo del subjectFrom (transazione riservata solo all'utente istituzionale della comunità)
    PublicTransfer = 2        ' il subjectFrom trasmette pubblicamente al subjectTo un certo bene o servizio da compensare in futuro 
    PublicSale = 3            ' il subjectFrom trasmette pubblicamente al subjectTo un certo bene o servizio compensato immediatamente da un certo ammontare in coin detratte dal balance del subjectTo 

    PrivateTransfer = 12      ' il subjectFrom trasmette in privato al subjectTo una lista di beni o servizi da compensare in futuro 
    PrivateSale = 13          ' il subjectFrom trasmette in privato al subjectTo una lista di beni o servizi compensati immediatamente e pubblicamente da un certo ammontare in coin detratto dal balance del subjectTo 
    PrivateCompensation = 22  ' il subjectFrom trasmette in privato al subjectTo una lista di beni o servizi con i quali compensa una lista di beni o servizi ricevuti in passato
End Enum

Public MustInherit Class TransferTransaction
    Inherits Transaction
    Protected _transferTransactionType As TransferTransactionTypeEnum = TransferTransactionTypeEnum.CoinTransfer
    Protected _isSale As Boolean = False
    Protected _isPrivate As Boolean = False
    Protected _isCompensation As Boolean = False
    Protected _requireAcceptance As Boolean = False
    Protected _title As String = "?"
    Protected state As Integer = 0

    Public Property transferId As String                    ' identificativo unico del trasferimento
    Public Property fromSubject As String                   ' identificativo di chi cede il bene convenzionalmente accettato dagli utenti della chain
    Public Property toSubject As String                     ' identificativo di chi accetta il bene.  
    Public Property message As String                       ' messaggio associato all'operazione  
    Public Property coinAmount As Double = 0                ' ammontare dei coin trasferiti.  
    Public Property signatureFrom As New Signature          ' firma del soggetto che attiva la transazione
    Public Property sFrom As Subject
    Public Property sTo As Subject
    Public Property compensation As Double
    Public Property compensations As Dictionary(Of String, TransferCompensation)

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.Transfer
    End Sub

    Public Overridable Function transferNotification() As String
        Dim t As New StringBuilder(100)

        Select Case state
            Case 3
                t.AppendLine("PROPOSTA DI SCAMBIO IN ARRIVO")
                t.AppendLine("(rispondere con un OTP valido per accettare, oppure NO per rifiutare)")
                t.AppendLine("---------------------------------------")
                t.AppendLine("")
            Case 4
                t.AppendLine("OPERAZIONE DI SCAMBIO REGISTRATA")
                t.AppendLine("---------------------------------------")
                t.AppendLine("")
        End Select

        t.AppendFormat("FROM: {0}", sFrom.name)
        t.AppendLine()
        t.AppendFormat("  TO: {0}", sTo.name)
        t.AppendLine()
        t.AppendFormat("SUBJ: {0}", _title)
        t.AppendLine()
        t.AppendFormat("{0}", sAction)
        t.AppendLine()
        Return t.ToString
    End Function


    Public ReadOnly Property transferTransactionTitle As String
        Get
            Return _title
        End Get
    End Property

    Public ReadOnly Property transferTransactionType As TransferTransactionTypeEnum
        Get
            Return _transferTransactionType
        End Get
    End Property

    Public ReadOnly Property transferTransactionState As Integer
        Get
            Return state
        End Get
    End Property

    Public ReadOnly Property sMessage() As String
        Get
            If Trim(Me.message) <> "" Then
                Return String.Format(" ({0})", message)
            Else
                Return ""
            End If
        End Get
    End Property

    Public ReadOnly Property sCoins As String
        Get
            If coinAmount > 0 Then
                If coinAmount = 1 Then
                    Return "1 Coin"
                Else
                    Return String.Format("{0} Coins", Me.coinAmount.ToString("#.#"))
                End If
            Else
                Return ""
            End If
        End Get
    End Property

    Public ReadOnly Property sComp As String
        Get
            If isCompensation Then
                Return ""
            Else
                Return String.Format("{0}%", compensation)
            End If
        End Get
    End Property

    Public Overridable ReadOnly Property sAction As String
        Get
            Return ""
        End Get
    End Property

    Public Function affectsSubject(subjectId As String) As Boolean
        Return fromSubject = subjectId Or toSubject = subjectId
    End Function

    Public ReadOnly Property requireAcceptance As Boolean
        Get
            Return _requireAcceptance
        End Get
    End Property

    Public ReadOnly Property isCompensation As Boolean
        Get
            Return _isCompensation
        End Get
    End Property

    Public ReadOnly Property isSale As Boolean
        Get
            Return _isSale
        End Get
    End Property


    Public ReadOnly Property isPrivate As Boolean
        Get
            Return _isPrivate
        End Get
    End Property

    Public ReadOnly Property isAwaitingApproval As Boolean
        Get
            Return state = 2
        End Get
    End Property

    Public ReadOnly Property isPending As Boolean
        Get
            Return state < 4
        End Get
    End Property

    Protected Overridable ReadOnly Property hashBaseForSignatures As String
        Get
            Dim t As New StringBuilder(1000)
            t.Append(transferId)
            t.Append(fromSubject)
            t.Append(toSubject)
            t.Append(message)
            t.Append(coinAmount.ToString)

            Return t.ToString
        End Get
    End Property

    Protected Overrides ReadOnly Property hashBase() As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(serial.ToString)
            t.Append(utility.getUnixTime(timestamp).ToString)
            t.Append(hashBaseForSignatures)
            t.Append(signatureFrom.hashBase)
            Return t.ToString
        End Get
    End Property

    Public Overrides Function isIntegrityVerified() As Boolean
        Dim retVal As Boolean = False
        If signatureFrom.x509Certificate IsNot Nothing Then
            Dim h As String = hashBaseForSignatures
            retVal = utility.verifyDigitalSignatureSHA1(signatureFrom.x509Certificate, h, Convert.FromBase64String(signatureFrom.signature))
        End If
        Return retVal
    End Function

    Public Function verifySignature(sig As Signature) As Boolean
        Return utility.verifyDigitalSignatureSHA1(sig.x509Certificate, hashBaseForSignatures, Convert.FromBase64String(sig.signature))
    End Function

    Public Sub computeSignatureFrom(bc As ClientBlockChain)
        Dim cer As X509Certificate2 = utility.getCertificateFromStoreBySubjectName(Me.fromSubject)
        Dim ts As Long = bc.synced_timestamp
        Me.signatureFrom = computeSignature(cer, ts)
    End Sub

    Protected Function computeSignature(cer As X509Certificate2, Optional timestamp As Long = 0) As Signature
        If timestamp = 0 Then
            timestamp = utility.getCurrentTimeStamp()
        End If
        Dim sig As New Signature
        sig.x509CertificateHash = cer.GetCertHashString
        sig.signature = Convert.ToBase64String(utility.computeDigitalSignatureSHA1(cer, hashBaseForSignatures))
        sig.timestamp = timestamp
        Return sig
    End Function

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(New XAttribute("transferTransactionType", Me.transferTransactionType))
        e.Add(New XElement("transferId", Me.transferId))
        e.Add(New XElement("fromSubject", Me.fromSubject))
        e.Add(New XElement("toSubject", Me.toSubject))
        e.Add(New XElement("message", Me.message))
        e.Add(New XElement("coinAmount", Me.coinAmount.ToString))
        e.Add(signatureFrom.xml("From"))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            transferId = .Element("transferId").Value
            fromSubject = .Element("fromSubject").Value
            toSubject = .Element("toSubject").Value
            message = .Element("message").Value
            coinAmount = utility.getDoubleFromString(.Element("coinAmount").Value)
            signatureFrom.fromXml(.Element("signatureFrom"))
        End With
        updateState()
    End Sub

    Public Sub loadSubjects(C As BlockChain)
        Try
            sFrom = C.subjects.getElementById(Me.fromSubject)
            sTo = C.subjects.getElementById(Me.toSubject)
        Catch ex As Exception
            sFrom = New Subject
            sTo = New Subject
        End Try
    End Sub

    Public Overridable Sub updateState()
        If fromSubject = "" Or toSubject = "" Then state = 0
        If state = 0 And transferId <> "" Then state = 1
        If state = 1 And coinAmount <> 0 And signatureFrom.signature <> "" Then state = 3
        If state = 3 And serial > 0 And blockSerial > 0 Then state = 4
    End Sub

    Public Function matches(tt As TransferTransaction)
        Return tt.transferId = Me.transferId And tt.fromSubject = Me.fromSubject And tt.toSubject = Me.toSubject
    End Function

End Class

Public Class CoinTransfer
    Inherits TransferTransaction
    Public Sub New()
        Me._title = "Coin Transfer"
        Me._transferTransactionType = TransferTransactionTypeEnum.CoinTransfer
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("Trasferimento di {0} {1}", sCoins, sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        t.AppendFormat("<div>trasferimento di {0} coin  da {1} a {2} con il messaggio: {3}</div>", coinAmount, sFrom.name, sTo.name, message)
        Return t.ToString
    End Function

End Class

Public Class CoinCreation
    Inherits TransferTransaction
    Public Sub New()
        Me._title = "Coin Creation and Transfer"
        Me._transferTransactionType = TransferTransactionTypeEnum.CoinCreation
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("Creazione e trasferimento di {0} {1}", sCoins, sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        t.AppendFormat("<div>creazione e trasferimento di {0} coin  da {1} a {2} con il messaggio: {3}</div>", coinAmount, sFrom.name, sTo.name, message)
        Return t.ToString
    End Function


End Class

Public Class TransferObject
    Public Property id As String
    Public Property quantity As Double
    Public Property description As String
    Public Property cost As Double

    Public Sub New()
        id = "T"
        quantity = 1
        description = ""
        cost = 0
    End Sub


    Public ReadOnly Property isValid() As Boolean
        Get
            Return Trim(description) <> "" Or cost > 0
        End Get
    End Property

    Public ReadOnly Property hashBase() As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(id.ToString)
            t.Append(quantity.ToString)
            t.Append(description)
            t.Append(cost.ToString)
            Return t.ToString
        End Get
    End Property

    Public Function xml() As XElement
        Dim e As XElement = New XElement("transferObject")
        e.Add(New XElement("id", id))
        e.Add(New XElement("quantity", quantity.ToString))
        e.Add(New XElement("description", description))
        e.Add(New XElement("cost", cost.ToString))
        Return e
    End Function

    Public Sub fromXml(e As XElement)
        With e
            id = .Element("id").Value
            quantity = .Element("quantity").Value
            description = .Element("description").Value
            cost = utility.getDoubleFromString(.Element("cost").Value)
        End With
    End Sub


End Class

Public MustInherit Class CoSignedTransferTransaction
    Inherits TransferTransaction

    Public Property signatureTo As New Signature
    Public Property transferObject As New TransferObject

    Public Sub New()
        MyBase.New
        _requireAcceptance = True
    End Sub

    Protected Overrides ReadOnly Property hashBaseForSignatures As String
        Get
            Dim t As New StringBuilder(1000)
            t.Append(MyBase.hashBaseForSignatures)
            t.Append(transferObject.hashBase)
            Return t.ToString
        End Get
    End Property

    Protected Overrides ReadOnly Property hashBase() As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(MyBase.hashBase)
            t.Append(signatureTo.hashBase)
            Return t.ToString
        End Get
    End Property

    Public Sub computeSignatureTo(bc As ClientBlockChain)
        Dim cer As X509Certificate2 = utility.getCertificateFromStoreBySubjectName(Me.toSubject)
        Me.signatureTo = computeSignature(cer, bc.synced_timestamp)
    End Sub

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(signatureTo.xml("To"))
        e.Add(transferObject.xml())
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            signatureTo.fromXml(.Element("signatureTo"))
            transferObject.fromXml(.Element("transferObject"))
        End With
        updateState()
    End Sub

    Public Overrides Sub updateState()
        If fromSubject = "" Or toSubject = "" Then state = 0
        If state = 0 And transferId <> "" Then state = 1
        If state = 1 And signatureFrom.signature <> "" Then state = 2
        If state = 2 And signatureTo.signature <> "" Then state = 3
        If state = 3 And serial > 0 And blockSerial > 0 Then state = 4
    End Sub


End Class

Public Class PublicTransfer
    Inherits CoSignedTransferTransaction
    Public Sub New()
        Me._title = "Public Transfer"
        Me._transferTransactionType = TransferTransactionTypeEnum.PublicTransfer

    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("{0} {1}", Me.transferObject.description, sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        t.AppendFormat("<div>{0} da {1} a {2} con il messaggio: {3}</div>", Me.transferObject.description, sFrom.name, sTo.name, message)
        Return t.ToString
    End Function

End Class

Public Class PublicSale
    Inherits CoSignedTransferTransaction
    Public Sub New()
        Me._title = "Public Sale"
        Me._isSale = True
        Me._transferTransactionType = TransferTransactionTypeEnum.PublicSale
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("{0} in cambio di {1} {2}", Me.transferObject.description, sCoins, sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        t.AppendFormat("<div>trasferimento di {0} da {1} a {2} in cambio di {3} con il messaggio: {4}</div>", Me.transferObject.description, sFrom.name, sTo.name, sCoins, message)
        Return t.ToString
    End Function


End Class

Public MustInherit Class PrivateCoSignedTransferTransaction
    Inherits CoSignedTransferTransaction
    Public Property privateBlock As Block

    Public Sub obtainPrivateBlock(C As ClientBlockChain)
        If Me.privateBlock Is Nothing Then
            Me.privateBlock = C.obtainPrivateBlock(Me.transferObject.description)
        End If
    End Sub
End Class

Public Class PrivateTransfer
    Inherits PrivateCoSignedTransferTransaction
    Public Sub New()
        Me._title = "Private Transfer"
        _isPrivate = True
        Me._transferTransactionType = TransferTransactionTypeEnum.PrivateTransfer
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("trasferimenti privati {0}", sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        Try
            t.AppendFormat("<div>scambio privato di beni o servizi da compensare successivamente, effettuato da {0} verso {1} con il messaggio: {2}</div>", sFrom.name, sTo.name, message)
            t.AppendFormat("<div>lista dei beni o servizi traferiti: </div>")
            t.Append(Me.privateBlock.transactions.html("elements_no_coins"))

        Catch ex As Exception
            t.Append("si è verificato un errore rendendo l'elemento in HTML")
        End Try
        Return t.ToString
    End Function


    Public Overrides Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        Try
            t.AppendLine("Beni o servizi traferiti:")
            t.Append(Me.privateBlock.transactions.plainText("elements_no_coins"))

        Catch ex As Exception
            t.Append("si è verificato un errore rendendo l'elemento in TEXT")
        End Try
        Return t.ToString
    End Function

End Class

Public Class PrivateCompensation
    Inherits PrivateCoSignedTransferTransaction
    Public Sub New()
        Me._isCompensation = True
        Me._title = "Private Compensation"
        _isPrivate = True
        Me._transferTransactionType = TransferTransactionTypeEnum.PrivateCompensation
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Dim t As New StringBuilder(100)
            t.Append("Compensazione con ")
            If coinAmount > 0 Then
                If coinAmount = 1 Then
                    t.Append("1 Coin ")
                Else
                    t.AppendFormat("{0} Coins ", Me.coinAmount.ToString("#.#"))
                End If
                t.Append("e altri ")
            End If
            t.Append("trasferimenti privati")

            If Trim(Me.message) <> "" Then
                t.AppendFormat(" ({0})", message)
            End If
            Return t.ToString
        End Get
    End Property

    Public Overrides Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        Try
            t.AppendLine("elementi compensati:")
            t.Append(Me.privateBlock.transactions.plainText("compensations"))
            t.AppendLine()
            t.AppendLine("elementi utilizzati per compensare:")
            t.Append(Me.privateBlock.transactions.plainText("elements"))
            t.AppendLine()
        Catch ex As Exception
            t.Append("si è verificato un errore rendendo l'elemento in TEXT")
        End Try
        Return t.ToString
    End Function


    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        Try
            t.AppendFormat("<div>Compensazione di trasferimenti pregressi effettuata da {0} verso {1} con il messaggio: {2}</div>", sFrom.name, sTo.name, message)
            t.AppendFormat("<div>elementi compensati: </div>")
            t.Append(Me.privateBlock.transactions.html("compensations"))
            t.AppendFormat("<hr /><div>elementi utilizzati per compensare :</div>")
            t.Append(Me.privateBlock.transactions.html("elements"))

        Catch ex As Exception
            t.Append("si è verificato un errore rendendo l'elemento in HTML")
        End Try
        Return t.ToString
    End Function


End Class

Public Class PrivateSale
    Inherits PrivateCoSignedTransferTransaction
    Public Sub New()
        Me._title = "Private Sale"
        _isPrivate = True
        _isSale = True
        Me._transferTransactionType = TransferTransactionTypeEnum.PrivateSale
    End Sub

    Public Overrides ReadOnly Property sAction As String
        Get
            Return String.Format("trasferimenti privati in cambio di {0} {1}", sCoins, sMessage)
        End Get
    End Property

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(100)
        Try
            t.AppendFormat("<div>scambio privato di beni o servizi effettuato da {0} verso {1} in cambio di {2} con il messaggio: {3}</div>", sFrom.name, sTo.name, sCoins, message)
            t.AppendFormat("<div>lista dei beni o servizi trasferiti: </div>")
            t.Append(Me.privateBlock.transactions.html("elements"))
            t.AppendFormat("<div>Controvalore in Coins richiesto: {0}</div>", coinAmount)

        Catch ex As Exception
            t.Append("si è verificato un errore rendendo l'elemento in HTML")
        End Try
        Return t.ToString
    End Function


End Class
