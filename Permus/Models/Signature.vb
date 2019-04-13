Imports System.Security.Cryptography.X509Certificates

Public Class Signature
    Public Property x509CertificateHash As String
    Public Property signature As String
    Public Property timestamp As Long
    Public Property x509Certificate As X509Certificate
    Public ReadOnly Property hashBase As String
        Get
            Return x509CertificateHash & signature
        End Get
    End Property

    Public Function xml(subject As String) As XElement
        Dim e As XElement = New XElement("signature" + subject)
        e.Add(New XElement("x509CertificateHash", x509CertificateHash))
        e.Add(New XElement("signature", signature))
        e.Add(New XElement("timestamp", timestamp))
        Return e
    End Function

    Public Sub fromXml(e As XElement)
        With e
            x509CertificateHash = .Element("x509CertificateHash").Value
            signature = .Element("signature").Value
            timestamp = utility.getChildElementValueLong(e, "timestamp")
        End With
    End Sub

    Public Sub obtainCertificateFromList(l As Certificates)
        If l.ContainsKey(x509CertificateHash) Then
            x509Certificate = l.Item(x509CertificateHash)
        Else
            x509Certificate = Nothing
        End If
    End Sub

    Public Sub obtainCertificateFromSubject(s As Subject)
        If s IsNot Nothing Then
            obtainCertificateFromList(s.x509Certificates)
        Else
            x509Certificate = Nothing
        End If
    End Sub


End Class


