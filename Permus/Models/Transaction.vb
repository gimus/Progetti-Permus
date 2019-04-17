Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports Newtonsoft.Json
Imports Permus

Public Enum TransactionTypeEnum
    SubjectIdentity = 0    ' transazione per la pubblicazione nei blocchi principali di una nuova identità di un soggetto
    Transfer = 1           ' transazione di trasferimento bene che si utilizza nei blocchi pubblici o principali
    TransferDetail = 2     ' dettaglio di transazione utilizzato nei blocchi privati o secondari
    ServerStatus = 3
End Enum

Public Enum TransferDetailTypeEnum
    TransferTransaction = 0    ' descrittore del bene trasferito da fromSubject a toSubject attraverso una transferTransaction
    TransferElement = 1        ' descrittore del bene trasferito da fromSubject a toSubject attraverso un item in un blocco privato
    TransferCompensation = 2   ' descrittore del bene precedentemente trasferito da toSubject a fromSubject, punta al blocco principale della blockchain che contiene
    TransferMessage = 3
    TransferDocument = 4
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
            Case TransactionTypeEnum.Transfer.ToString
                Dim stype As String = e.Attribute("transferTransactionType")
                Select Case stype
                    Case TransferTransactionTypeEnum.CoinCreation.ToString
                        tr = New CoinCreation
                    Case TransferTransactionTypeEnum.CoinTransfer.ToString
                        tr = New CoinTransfer
                    Case TransferTransactionTypeEnum.PublicSale.ToString
                        tr = New PublicSale
                    Case TransferTransactionTypeEnum.PublicTransfer.ToString
                        tr = New PublicTransfer
                    Case TransferTransactionTypeEnum.PrivateCompensation.ToString
                        tr = New PrivateCompensation
                    Case TransferTransactionTypeEnum.PrivateSale.ToString
                        tr = New PrivateSale
                    Case TransferTransactionTypeEnum.PrivateTransfer.ToString
                        tr = New PrivateTransfer
                    Case Else

                End Select

            Case TransactionTypeEnum.SubjectIdentity.ToString
                tr = New SubjectIdentityTransaction

            Case TransactionTypeEnum.TransferDetail.ToString
                Dim stype As String = e.Attribute("transferDetailType")

                Select Case stype
                    Case TransferDetailTypeEnum.TransferCompensation.ToString
                        tr = New TransferCompensation
                    Case TransferDetailTypeEnum.TransferElement.ToString
                        tr = New TransferElement
                    Case TransferDetailTypeEnum.TransferMessage.ToString
                        tr = New TransferMessage
                    Case TransferDetailTypeEnum.TransferDocument.ToString
                        tr = New TransferDocument
                    Case Else
                        Throw New Exception("Tipo di transazione non riconosciuto")
                End Select

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

    Public Function getTransferMessages() As List(Of TransferMessage)
        Dim l As New List(Of TransferMessage)
        For Each i As TransferDetail In filter(TransactionTypeEnum.TransferDetail)
            If i.transferDetailType = TransferDetailTypeEnum.TransferMessage Then
                l.Add(i)
            End If
        Next
        Return l
    End Function


    Public Function getTransferElements() As List(Of TransferElement)
        Dim l As New List(Of TransferElement)
        For Each i As TransferDetail In filter(TransactionTypeEnum.TransferDetail)
            If i.transferDetailType = TransferDetailTypeEnum.TransferElement Then
                l.Add(i)
            End If
        Next
        Return l
    End Function

    Public Function getTransferCompensations() As List(Of TransferCompensation)
        Dim l As New List(Of TransferCompensation)
        For Each i As TransferDetail In filter(TransactionTypeEnum.TransferDetail)
            If i.transferDetailType = TransferDetailTypeEnum.TransferCompensation Then
                l.Add(i)
            End If
        Next
        Return l
    End Function

    Public Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Dim c As Integer = 0
        Select Case type
            Case "compensations"
                For Each te As TransferCompensation In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferCompensation)
                    t.Append(te.plainText("private_table_row"))
                    c += 1
                Next
            Case "elements"
                For Each te As TransferElement In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferElement)
                    t.Append(te.plainText("private_table_row"))
                    c += 1
                Next
            Case "elements_no_coins"
                For Each te As TransferElement In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferElement)
                    t.Append(te.plainText("private_table_row_no_coins"))
                    c += 1
                Next
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
            Case "compensations"

                For Each te As TransferCompensation In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferCompensation)
                    If c = 0 Then
                        t.Append("<table>")
                        t.Append(te.html("private_table_header"))
                    End If
                    t.Append(te.html("private_table_row"))
                    c += 1
                Next
                If c > 0 Then
                    t.Append("</table>")
                End If


            Case "elements"

                For Each te As TransferElement In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferElement)
                    If c = 0 Then
                        t.Append("<table>")
                        t.Append(te.html("private_table_header"))
                    End If
                    t.Append(te.html("private_table_row"))
                    c += 1
                Next

                If c > 0 Then
                    t.Append("</table>")
                End If

            Case "elements_no_coins"

                For Each te As TransferElement In Me.Where(Function(x) x.transactionType = TransactionTypeEnum.TransferDetail AndAlso DirectCast(x, TransferDetail).transferDetailType = TransferDetailTypeEnum.TransferElement)
                    If c = 0 Then
                        t.Append("<table>")
                        t.Append(te.html("private_table_header_no_coins"))
                    End If
                    t.Append(te.html("private_table_row_no_coins"))
                    c += 1
                Next

                If c > 0 Then
                    t.Append("</table>")
                End If

            Case Else
                t.AppendFormat("<div>modalità di visualizzione NON IMPLEMENTATA</div>")

        End Select

        t.Append("</div>")
        Return t.ToString
    End Function

    Public Function getTransferTransaction(transferId As String) As TransferTransaction
        Return Me.Where(Function(x) x.transactionType = TransactionTypeEnum.Transfer).Where(Function(y As TransferTransaction) y.transferId = transferId).FirstOrDefault
    End Function

    Public Function getTransferTransactions(subjectId As String) As List(Of TransferTransaction)
        Dim ret As New List(Of TransferTransaction)
        For Each t As Transaction In Me
            Dim tt As TransferTransaction
            If t.transactionType = TransactionTypeEnum.Transfer Then
                tt = t
                If tt.affectsSubject(subjectId) Then
                    ret.Add(tt)
                End If
            End If
        Next
        Return ret
    End Function

    Public Sub correlateCompensations(C As ClientBlockChain)
        Dim tt As TransferTransaction
        For Each tc As TransferCompensation In Me.getTransferCompensations()
            tt = C.userTransferTransactions(tc.referencedItemId)
            tc.relatedTransferTransaction = tt
        Next
    End Sub

    Public Sub correlateCompensations(C As BlockChain)
        Dim tt As TransferTransaction
        Dim bc As New BlockCache(1000)
        Dim b As Block

        For Each tc As TransferCompensation In Me.getTransferCompensations()
            If tc.blockId > 0 Then
                b = bc.getBlock(tc.blockId)

                If b Is Nothing Then
                    b = C.obtainBlock(tc.blockId)
                    If b IsNot Nothing Then
                        bc.Add(b)
                    End If
                End If

                If b IsNot Nothing Then
                    tt = b.transactions.getTransferTransaction(tc.referencedItemId)
                    tc.relatedTransferTransaction = tt
                End If

            End If
        Next
    End Sub

End Class

