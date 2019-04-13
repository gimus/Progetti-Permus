Imports System.Security.Cryptography.X509Certificates
Imports System.Text

Public Class SubjectIdentityTransaction
    Inherits Transaction

    Public Sub New()
        Me._transactionType = TransactionTypeEnum.SubjectIdentity
    End Sub

    Public Property subject As String
    Public Property subjectCertificate As X509Certificate2

#Region "Public Methods"
    Public Overrides Function xml() As XElement
        Dim e As XElement = MyBase.xml
        e.Add(New XElement("subject", subject))
        e.Add(New XElement("subjectCertificate", Convert.ToBase64String(subjectCertificate.RawData)))
        Return e
    End Function

    Public Overrides Sub fromXml(e As XElement)
        MyBase.fromXml(e)
        With e
            subject = .Element("subject").Value
            subjectCertificate = New X509Certificate2(Convert.FromBase64String(.Element("subjectCertificate").Value))
        End With
    End Sub

#End Region

    Protected Overrides ReadOnly Property hashBase As String
        Get
            Dim t As New StringBuilder(MyBase.hashBase)
            t.Append(subject)
            t.Append(utility.bin2hex(subjectCertificate.RawData))
            Return t.ToString
        End Get
    End Property

End Class
