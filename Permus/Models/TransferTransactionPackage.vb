Imports System.Security.Cryptography.X509Certificates
Imports System.Text

Public Class TransferTransactionPackage
    Inherits PermusObject
    Public privateBlock As Block
    Public Property transaction As TransferTransaction
    Public Property envelopedPrivateBlock As New EnvelopedBlock


    Public Sub New()
    End Sub

    Public Sub New(xstring As String)
        If xstring <> "" Then
            Try
                Dim doc As XDocument = XDocument.Parse(xstring)
                fromXml(doc.Root)
            Catch ex As Exception
            End Try
        End If
    End Sub

    Public Sub New(e As XElement)
        If e IsNot Nothing Then
            fromXml(e)
        End If
    End Sub


    Public Sub correlateCompensations(C As ClientBlockChain)
        ensureBlockDecrypted()
        If transaction.isPrivate And privateBlock IsNot Nothing Then
            privateBlock.transactions.correlateCompensations(C)
        End If
    End Sub

    Public Sub ensureBlockDecrypted(Optional cer As X509Certificate2 = Nothing)
        If privateBlock Is Nothing Then
            privateBlock = envelopedPrivateBlock.getBlock(cer)
        End If
    End Sub

    Public Overrides Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(2000)
        If transaction IsNot Nothing Then
            If transaction.isPrivate Then
                ensureBlockDecrypted()
                Dim pc As PrivateCoSignedTransferTransaction = transaction
                pc.privateBlock = Me.privateBlock
                t.Append(pc.plainText(type))
            Else

                t.Append(transaction.sAction)
            End If
        End If
        Return t.ToString
    End Function

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(2000)

        If transaction IsNot Nothing Then
            If transaction.isPrivate Then
                ensureBlockDecrypted()
                Dim pc As PrivateCoSignedTransferTransaction = transaction
                pc.privateBlock = Me.privateBlock
                t.Append(pc.html(type))
            Else
                t.Append(transaction.html(type))
            End If
        End If
        Return t.ToString
    End Function

    Public Overrides Function xml() As XElement
        Dim e As XElement = New XElement("TransferTransactionPackage")
        e.Add(IIf(transaction IsNot Nothing, Me.transaction.xml(), ""))
        e.Add(IIf(envelopedPrivateBlock IsNot Nothing, Me.envelopedPrivateBlock.xml(), ""))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        With e
            envelopedPrivateBlock = New EnvelopedBlock
            transaction = Permus.Transaction.createFromXml(.Element("transaction"))
            envelopedPrivateBlock.fromXml(.Element("EnvelopedBlock"))
        End With
    End Sub

    Public ReadOnly Property isPrivate As Boolean
        Get
            Return transaction.isPrivate
        End Get
    End Property



End Class

Public Class EnvelopedBlock
    Public Property envelopedBlockData As String
    Public Property blockHash As String

    Public Sub New()
    End Sub

    Public Sub New(data As String, hash As String)
        envelopedBlockData = data
        blockHash = hash
    End Sub

    Public Sub New(e As XElement)
        fromXml(e)
    End Sub

    Public Sub New(b As Block, certFrom As X509Certificate2, certTo As X509Certificate2)
        envelopeBlock(b, certFrom, certTo)
    End Sub

    Public Sub envelopeBlock(b As Block, certFrom As X509Certificate2, certTo As X509Certificate2)
        blockHash = utility.bin2hex(b.computeHash())
        Dim sb() As Byte = utility.getBytesFromString(b.xml.ToString)
        envelopedBlockData = Convert.ToBase64String(utility.encryptAndEnvelope(sb, certFrom, certTo))
    End Sub

    Public Function getBlock(Optional cer As X509Certificate2 = Nothing) As Block
        Try
            Dim ed As New EnvelopedData(Convert.FromBase64String(envelopedBlockData), cer)
            Dim b As Block = New Block(ed.envelopedContentText)
            If utility.bin2hex(b.computeHash()) = blockHash Then
                Return b
            Else
                Return Nothing
            End If

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function xml() As XElement
        Dim e As XElement = New XElement("EnvelopedBlock")
        e.Add(New XElement("envelopedBlockData", envelopedBlockData))
        e.Add(New XElement("blockHash", blockHash))
        Return e
    End Function

    Public Sub fromXml(e As XElement)
        With e
            envelopedBlockData = .Element("envelopedBlockData").Value
            blockHash = .Element("blockHash").Value
        End With
    End Sub

End Class

Public Class TransferTransactionInfo
    Inherits PermusObject

    Public Property subject As String
    Public Property id As String
    Public Property state As Integer = 0
    Public Property type As String

    Public Sub New(Optional e As XElement = Nothing)
        If e IsNot Nothing Then
            fromXml(e)
        End If
    End Sub

    Public Overrides Function xml() As XElement
        Dim e As New XElement("TransferTransactionInfo")
        e.Add(New XElement("id", id))
        e.Add(New XElement("state", state))
        e.Add(New XElement("type", type))
        e.Add(New XElement("subject", subject))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        With e
            subject = (.Element("subject").Value)
            id = (.Element("id").Value)
            state = (.Element("state").Value)
            type = (.Element("type").Value)
        End With
    End Sub

End Class

Public Class PendingTransferInfo
    Public Property subjectFrom As String
    Public Property subjectTo As String
    Public Property id As String
    Public Property state As Integer = 0
    Public Property type As String
    Public tagged As Boolean
    Public Property sFrom As Subject
    Public Property sTo As Subject

    Public Sub New(Optional e As XElement = Nothing)
        If e IsNot Nothing Then
            fromXml(e)
        End If
    End Sub

    Public Function xml() As XElement
        Dim e As New XElement("pendingTransactionInfo")
        e.Add(New XElement("subjectFrom", subjectFrom))
        e.Add(New XElement("subjectTo", subjectTo))
        e.Add(New XElement("id", id))
        e.Add(New XElement("state", state))
        e.Add(New XElement("type", type))
        Return e
    End Function

    Public Sub fromXml(e As XElement)
        With e
            subjectFrom = (.Element("subjectFrom").Value)
            subjectTo = (.Element("subjectTo").Value)
            id = (.Element("id").Value)
            state = (.Element("state").Value)
            type = (.Element("type").Value)
        End With
    End Sub

    Public Sub loadSubjects(C As BlockChain)
        Try
            sFrom = C.subjects.getElementById(subjectFrom)
            sTo = C.subjects.getElementById(subjectTo)
        Catch ex As Exception
            sFrom = New Subject
            sTo = New Subject
        End Try
    End Sub
End Class

Public Class PendingTransfers
    Inherits Dictionary(Of String, PendingTransferInfo)

    Public Sub New(Optional e As XElement = Nothing)
        If e IsNot Nothing Then
            fromXml(e)
        End If
    End Sub

    Public Function xml() As XElement
        Dim e As New XElement("pendingTransactions")
        For Each pti As PendingTransferInfo In Me.Values
            e.Add(pti.xml)
        Next
        Return e
    End Function

    Public Sub fromXml(e As XElement)
        Dim pti As PendingTransferInfo
        Me.Clear()
        For Each ee As XElement In e.Elements
            pti = New PendingTransferInfo(ee)
            Me.Add(pti.id, pti)
        Next
    End Sub

End Class
