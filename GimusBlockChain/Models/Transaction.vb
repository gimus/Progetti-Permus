Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text

Public Enum TransactionTypeEnum
    SubjectIdentity = 0    ' transazione per la pubblicazione nei blocchi principali di una nuova identità di un soggetto
End Enum

Public MustInherit Class Transaction
    Inherits PermusObject

    Protected _transactionType As TransactionTypeEnum

    Public Property timestamp As DateTime      ' timestamp di creazione della transazione
    Public Property serial As Long = 0         ' numero seriale della transazione all'interno della sequenza 
    Public Property blockSerial As Long = 0    ' numero seriale del blocco a cui è assegnata la transazione

    Public owner As Block

#Region "Public Methods"

    Public ReadOnly Property transactionType As TransactionTypeEnum
        Get
            Return _transactionType
        End Get
    End Property

    Public Function computeHash() As Byte()
        Return utility.computeHash(hashBase)
    End Function

    Public Overridable Function isIntegrityVerified() As Boolean
        Return True
    End Function

    Public Overrides Function xml() As XElement
        Dim e As XElement = New XElement("transaction", New XAttribute("type", Me.transactionType.ToString))
        e.Add(New XElement("blockSerial", Me.blockSerial))
        e.Add(New XElement("serial", Me.serial))
        e.Add(New XElement("timestamp", utility.getUnixTime(timestamp).ToString))
        Return e

    End Function

    Public Overrides Sub fromXml(e As XElement)
        With e
            blockSerial = .Element("blockSerial").Value
            serial = .Element("serial").Value
            timestamp = utility.dateTimeFromUnixTime(CLng(.Element("timestamp").Value))
        End With
    End Sub

    Public Shared Function createFromXml(e As XElement) As Transaction
        Dim tr As Transaction = Nothing
        Dim type As String = e.Attribute("type")
        Select Case type
            Case TransactionTypeEnum.SubjectIdentity.ToString
                tr = New SubjectIdentityTransaction

            Case Else
                Throw New Exception("Tipo di transazione non riconosciuto")
        End Select

        If tr IsNot Nothing Then
            tr.fromXml(e)
        End If
        Return tr
    End Function

    Public ReadOnly Property transactionId As String
        Get
            Return String.Format("{0}-{1}", blockSerial, serial)
        End Get
    End Property

#End Region

    Protected Overridable ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(transactionType.ToString)
            t.Append(serial.ToString)
            t.Append(blockSerial.ToString)
            t.Append(utility.getUnixTime(timestamp).ToString)
            Return t.ToString
        End Get
    End Property


End Class

Public Class TransactionsList
    Inherits List(Of Transaction)

    Public Function verifyIntegrity() As Boolean
        For Each tr As Transaction In Me
            If Not tr.isIntegrityVerified() Then
                Return False
            End If
        Next
        Return True
    End Function

    Public Function computeHash(Optional maxSerial As Integer = Integer.MaxValue) As Byte()
        If Me.Count = 0 Then
            Return utility.nullSHA256
        Else
            ' l'hash corrente parte con tutti zeri
            Dim hash() As Byte = utility.nullSHA256

            ' per ogni transazione, si prende il suo hash e si accoda all'hash corrente, poi si procede a fare l'hash di tutto
            For Each tr As Transaction In Me.Where(Function(x) x.serial <= maxSerial).OrderBy(Function(x) x.serial)
                hash = utility.computeHash(hash, tr.computeHash())
            Next
            Return hash
        End If
    End Function

    Public Function toXml() As XElement
        Dim e As XElement = New XElement("transactions", New XAttribute("count", Me.Count))
        For Each tr As Transaction In Me.OrderBy(Function(x) x.serial)
            e.Add(tr.xml)
        Next
        Return e
    End Function


    Public Sub fromXml(e As XElement)
        Dim tr As Transaction = Nothing
        Me.Clear()
        With e
            For Each ce As XElement In e.Elements
                tr = Transaction.createFromXml(ce)
                Me.Add(tr)
            Next
        End With
    End Sub

    Public Function filter(type As TransactionTypeEnum) As List(Of Transaction)
        Return Me.Where(Function(x) x.transactionType = type).ToList
    End Function


    Public Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Dim c As Integer = 0
        Select Case type
            Case Else
                t.AppendFormat("modalità di visualizzione NON IMPLEMENTATA")
        End Select

        Return t.ToString
    End Function


    Public Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Dim c As Integer = 0
        t.Append("<div>")
        Select Case type

            Case Else
                t.AppendFormat("<div>modalità di visualizzione NON IMPLEMENTATA</div>")

        End Select

        t.Append("</div>")
        Return t.ToString
    End Function

End Class

