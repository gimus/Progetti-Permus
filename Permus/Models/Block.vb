Imports System.Text

Public Class Block
    Inherits PermusObject

    Public Property version As String         ' una stringa che rappresenta la versione di questo bolcco
    Public Property timestamp As DateTime     ' timestamp di creazione blocco
    Public Property serial As Long = 1           ' numero seriale del blocco all'interno della sequenza 
    Public Property prev_vers As Byte() = utility.nullSHA256       ' hash con pow del blocco con numero seriale immediatamente precedente
    Public Property trans_root As Byte() = utility.nullSHA256      ' hash degli hash delle transazioni attualmente contenute nel blocco
    Public Property transactions As New TransactionsList  ' elenco delle transazioni contenute nel blocco

    Public Sub New()
    End Sub

    Public Sub New(e As XElement)
        fromXml(e)
    End Sub

    Public Sub New(Optional xml As String = "")
        If xml <> "" Then
            parse(xml)
        End If
    End Sub

    Public Shared Function CreateFrom(items As List(Of TransferDetail), C As ClientBlockChain)
        Dim tb As New Block
        With tb
            .version = C.systemInfo.blockChainVersion
            .timestamp = utility.dateTimeFromUnixTime(C.synced_timestamp)
            .serial = 0
            .prev_vers = utility.nullSHA256
            .trans_root = utility.nullSHA256
        End With
        For Each td As TransferDetail In items
            tb.addTransaction(td)
        Next
        Return New Block(tb.xml)
    End Function


    Public ReadOnly Property hashBase() As String
        Get
            ' prima ricalcoliamo l'hash delle transazioni contenute in questo blocco
            trans_root = transactions.computeHash()
            Dim t As New StringBuilder(200)
            t.Append(version)
            t.Append(serial.ToString)
            t.Append(utility.getUnixTime(timestamp).ToString)
            t.Append(utility.bin2hex(prev_vers))
            t.Append(utility.bin2hex(trans_root))
            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = New XElement("Block")
        e.Add(New XElement("version", Me.version))
        e.Add(New XElement("serial", Me.serial))
        e.Add(New XElement("timestamp", utility.getUnixTime(timestamp).ToString))
        e.Add(New XElement("prev_vers", Convert.ToBase64String(prev_vers)))
        e.Add(New XElement("trans_root", Convert.ToBase64String(trans_root)))
        e.Add(transactions.toXml)
        Return e
    End Function

    Public Sub parse(xml As String)
        Dim doc As XDocument = XDocument.Parse(xml)
        fromXml(doc.Root)
    End Sub

    Public Overrides Sub fromXml(e As XElement)
        With e
            version = .Element("version").Value
            serial = CLng(.Element("serial").Value)
            timestamp = utility.dateTimeFromUnixTime(CLng(.Element("timestamp").Value))
            prev_vers = Convert.FromBase64String(.Element("prev_vers").Value)
            trans_root = Convert.FromBase64String(.Element("trans_root").Value)
            transactions.fromXml(.Element("transactions"))
        End With
    End Sub

    Public ReadOnly Property getXmlText() As String
        Get
            Return xml().ToString
        End Get
    End Property

    Public Function addTransaction(tr As Transaction) As Transaction
        If tr.isIntegrityVerified() Then
            tr.blockSerial = Me.serial
            tr.serial = transactions.Count + 1
            tr.timestamp = utility.getUTC
            transactions.Add(tr)
            trans_root = utility.computeHash(trans_root, tr.computeHash())
            tr.owner = Me
            Return tr
        Else
            Throw New Exception("Impossibile aggiungere al blocco una transazione la cui integrità non è verificata")
        End If
    End Function

    Public Function computeHash() As Byte()
        Return utility.computeHash(Me.hashBase)
    End Function

End Class

Public Class BlockCache
    Inherits Queue(Of Block)
    Protected max As Integer
    Public Sub New(maxSize As Integer)
        max = maxSize
    End Sub

    Public Sub Add(b As Block)
        Me.Enqueue(b)
        If Me.Count > max Then
            Me.Dequeue()
        End If
    End Sub

    Public Function getBlock(serial As Long) As Block
        Return Me.Where(Function(x) x.serial = serial).FirstOrDefault
    End Function

End Class
