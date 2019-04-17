Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports Permus

Public MustInherit Class TransferDetail
    Inherits Transaction
    Protected _transferDetailType As TransferDetailTypeEnum

    Public Property fromSubject As String                   ' identificativo di chi cede il bene convenzionalmente accettato dagli utenti della chain
    Public Property toSubject As String                     ' identificativo di chi accetta il bene.  

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.TransferDetail
    End Sub

    Public ReadOnly Property transferDetailType As TransferDetailTypeEnum
        Get
            Return _transferDetailType
        End Get
    End Property

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)

            t.Append(fromSubject)
            t.Append(toSubject)

            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(New XAttribute("transferDetailType", Me.transferDetailType))
        e.Add(New XElement("fromSubject", Me.fromSubject))
        e.Add(New XElement("toSubject", Me.toSubject))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            fromSubject = .Element("fromSubject").Value
            toSubject = .Element("toSubject").Value
        End With
    End Sub

End Class

Public Class TransferElement
    Inherits TransferDetail
    Public Property transferObject As New TransferObject

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.TransferDetail
        Me._transferDetailType = TransferDetailTypeEnum.TransferElement
    End Sub

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(MyBase.hashBase())
            t.Append(transferObject.hashBase)
            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(transferObject.xml())
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            transferObject.fromXml(.Element("transferObject"))
        End With
    End Sub

    Public Overrides Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Select Case type
            Case "private_table_row"
                t.AppendFormat("- {0} {1}", Me.transferObject.description, Me.transferObject.cost.ToString("#.##"))
                t.AppendLine()
        End Select
        Return t.ToString
    End Function

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Select Case type
            Case "private_table_header", "private_table_header_no_coins"
                t.Append("<tr>")
                t.AppendFormat("<th width='2%' ></th>")
                t.AppendFormat("<th width='98%'>description</th>")
                If type = "private_table_header" Then
                    t.AppendFormat("<th width='4%'>coins</th>")
                End If
                t.Append("</tr>")
            Case "private_table_row", "private_table_row_no_coins"
                t.Append("<tr>")
                t.AppendFormat("<td><img style=""width:48px"" src=""data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADAAAAAwCAMAAABg3Am1AAAABGdBTUEAAK/INwWK6QAAABl0RVh0U29mdHdhcmUAQWRvYmUgSW1hZ2VSZWFkeXHJZTwAAAEOUExURdUySMstQuc5UPiWgfubhfiUf2l8hv+okEVZYPmYgug4T2KAifqZhOI7Uuc4TzxdY0RbYvmWgfmXgeE8U2p9h5Zkc/mXgvmZg/iUgN0vRd4uRUVbYqE8TOg5UHxyftAsQmV+iPiWgHpzf3l0f4RHVM0uRPmYg1NXX05iamV+h7w0SHBQXNQsQj9cYviXgc5HW9YrQVJmbodFU1Jlbuo3TrU2SXZ1gEFbYkZZYHJ3gtQrQW95g0ZaYtIsQYJHVDpeY41EUs4tQkRYX0phaEljarc0R2GBiuU5UUJcYs0tQ+U6UtQsQaE9TVRocPuchp89TUZaYH5xfdowRqQ7TOM7UswuQ+Y5UPiVgP+njwAAABDVFXYAAABadFJOU///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////ANHWim8AAAIxSURBVHjaYojECthEjDmwywAEEAN29Wwihk4cGtikAAKIAbt6NhFfjgAbbHIAAYRLQ2SkswVWRwEEEFYNCmyaQFKawwWLHEAAYdVgqWAdGengoauNRQ4ggNA1hIWFcbkHcit5SQUL8BsZhIaGqqIqAAggBgz1YVy8ctzKwpIyAvwq3kANoagqAAIIvwYdTA0AAYRVg5KbmbCUv7m9nRWmBoAAwqKBiU+UW1Q2yFVd2scWUwNAAGHREMbHqx8iy+CoZeoZiqkBIICwaQjjM+ELY2CUlw/FogEggLBqALmLgTE0FJsGgAAiWQNAAJGsASCASNYAEEAkawAIIJI1AAQQyRoAAohkDQABRLIGgAAiWQNAABGtQU0xPFwQSAMEEJYch6YBkuOYw8MhGgACCHu5hAHEQOrDQSyAACJSA1i9EIgFEEDEaRCEWxAJEEDoGthhwC+cGQT0QII8CAsiAQIIVUMEArCwglVJcMJ8DLEgEiCAGHCoh2kIh1sgBlEDEEAMONTDNKBbEAkQQAw41MM0AMXFJWA6QQAggBhwqIdq4EG3IBIggBiwqEcNN85wZAsiAQKIAYv5WOIMbkEkQAAxYHEPsnIhiHo1uABAADFgcz9ysIIBM8IEgABiwKEeVQOSlQABxIBDPYoGHiQNAAFEhAZFlDAACCCCTmIWRw00gABiwB9ImAAggBgIRBsGAAggjIgjlJcAAgg9aRDMfAABhJb4COdWgABCTd5EZG+AAAMA8kLh0XlOxZsAAAAASUVORK5CYII=""></td>")
                t.AppendFormat("<td>{0}</td>", Me.transferObject.description)
                If type = "private_table_row" Then
                    t.AppendFormat("<td>{0}</td>", Me.transferObject.cost)
                End If
                t.Append("</tr>")
        End Select
        Return t.ToString
    End Function

End Class

Public Class TransferCompensation
    Inherits TransferDetail
    Implements INotifyPropertyChanged
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Property blockId As Long
    Public Property transferId As String
    Public Property percentCompensated As Double

    Public relatedTransferTransaction As TransferTransaction
    Public Property description As String
    Public Property sComp As String
    Protected _newComp As Integer
    Public elements As List(Of TransferElement)

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.TransferDetail
        Me._transferDetailType = TransferDetailTypeEnum.TransferCompensation
    End Sub

    Public Sub New(tt As TransferTransaction)
        Me.New()
        Me.relatedTransferTransaction = tt
        Me.fromSubject = tt.fromSubject
        Me.toSubject = tt.toSubject
        Me.transferId = relatedTransferTransaction.transferId
        Me.percentCompensated = tt.compensation
        Me.newPercentCompensated = tt.compensation
        Me.blockId = tt.blockSerial
        Me.description = tt.sAction
        Me.sComp = tt.sComp
    End Sub

    Public ReadOnly Property currentObjectCompensation As Double
        Get
            Return relatedTransferTransaction.compensation
        End Get
    End Property

    Public ReadOnly Property relatedTimestamp As DateTime
        Get
            Return relatedTransferTransaction.timestamp
        End Get
    End Property

    Public ReadOnly Property relatedDescription As String
        Get
            Return relatedTransferTransaction.sAction
        End Get
    End Property


    Public Property newPercentCompensated As Integer
        Get
            Return Me._newComp
        End Get
        Set(value As Integer)
            Me._newComp = value
            NotifyPropertyChanged("newPercentCompensated")
        End Set
    End Property

    Private Sub NotifyPropertyChanged(ByVal info As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(info))
    End Sub

    Public ReadOnly Property referencedItemId As String
        Get
            Return String.Format("{0}", transferId)
        End Get
    End Property

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(MyBase.hashBase())
            t.Append(transferId)
            t.Append(percentCompensated.ToString)

            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(New XElement("blockId", Me.blockId))
        e.Add(New XElement("transferId", Me.transferId))
        e.Add(New XElement("percentCompensated", Me.percentCompensated.ToString))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            Try
                blockId = .Element("blockId").Value
            Catch ex As Exception
                blockId = 0
            End Try
            transferId = .Element("transferId").Value
            percentCompensated = .Element("percentCompensated").Value
        End With
    End Sub

    Public Overrides Function plainText(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Select Case type
            Case "private_table_row"
                If Me.relatedTransferTransaction IsNot Nothing Then
                    t.AppendFormat("- {0} {1}", Me.relatedTimestamp.ToString("dd/MM/yy"), Me.relatedDescription)
                    t.AppendFormat(" al {0}", String.Format("{0}%", Me.percentCompensated))
                    t.AppendLine()
                End If

        End Select
        Return t.ToString
    End Function


    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Select Case type
            Case "private_table_header"
                t.Append("<tr>")
                t.AppendFormat("<th width='1%' ></th>")
                t.AppendFormat("<th width='2%' 'nowarp' ></th>")
                t.AppendFormat("<th width='90%' ></th>")
                t.AppendFormat("<th width='9%'></th>")
                t.Append("</tr>")
            Case "private_table_row"
                If Me.relatedTransferTransaction IsNot Nothing Then
                    t.Append("<tr>")
                    t.AppendFormat("<td><img style=""width:48px"" src=""data:image/png;base64,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""></td>")

                    t.AppendFormat("<td>{0}</td>", Me.relatedTimestamp.ToString("dd/MM/yy"))
                    t.AppendFormat("<td>{0}</td>", Me.relatedDescription)
                    t.AppendFormat("<td>{0}</td>", String.Format("{0}%", Me.percentCompensated))
                    t.Append("</tr>")
                End If

        End Select
        Return t.ToString
    End Function

End Class

Public Class TransferMessage
    Inherits TransferDetail

    Public Property messageText As String

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.TransferDetail
        Me._transferDetailType = TransferDetailTypeEnum.TransferMessage
    End Sub

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(MyBase.hashBase())
            t.Append(messageText)
            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(New XElement("messageText", Me.messageText))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            messageText = .Element("messageText").Value
        End With
    End Sub

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        t.AppendFormat("<div style='border:solid 1px orange; border-radius: 25px; width:95%;padding:10px;margin:10px; background-color:lightyellow'>{0}</div><br />", messageText)
        Return t.ToString
    End Function

End Class

Public Class TransferDocument
    Inherits TransferDetail

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.TransferDetail
        Me._transferDetailType = TransferDetailTypeEnum.TransferDocument
    End Sub

    Public Property DocumentMimeType As String
    Public Property DocumentName As String
    Public Property DocumentContent As String

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(200)
            t.Append(MyBase.hashBase())
            t.Append(DocumentMimeType)
            t.Append(DocumentName)
            t.Append(DocumentContent)
            Return t.ToString
        End Get
    End Property

    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml()
        e.Add(New XElement("documentMimeType", Me.DocumentMimeType))
        e.Add(New XElement("documentName", Me.DocumentName))
        e.Add(New XElement("documentContent", Me.DocumentContent))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            DocumentMimeType = .Element("DocumentMimeType").Value
            DocumentName = .Element("DocumentName").Value
            DocumentContent = .Element("DocumentContent").Value
        End With
    End Sub

    Public Overrides Function html(Optional type As String = "") As String
        Dim t As New StringBuilder(1000)
        Return t.ToString
    End Function

End Class

