Imports System.Security.Cryptography
Imports System.Security.Cryptography.Pkcs
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Web

Public Class utility
    Public Shared ReadOnly nullSHA256() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
    Protected Shared gen170 As DateTime = New DateTime(1970, 1, 1, 0, 0, 0)

    Public Shared Function htmlPage(content As String) As String
        Dim t As New StringBuilder(1000)
        t.Append(My.Resources.htmlPage)
        t.Append(content)
        t.Append("</body></html>")
        Return t.ToString
    End Function

    Public Shared Function ConvertToUrlSafeBase64String(data() As Byte) As String
        Dim s As String = Convert.ToBase64String(data)
        Return escapeForUrl(s)
    End Function

    Public Shared Function ConvertFromUrlSafeBase64String(s As String) As Byte()
        Dim ds As String = unEscapeForUrl(s)
        Return Convert.FromBase64String(ds)
    End Function

    ' replace base64 characters filtered by HTTP in the url string  with safe ones
    Public Shared Function escapeForUrl(s As String) As String
        Dim t As New StringBuilder(s)
        t.Replace("/", "-")
        t.Replace("+", "*")
        t.Replace("=", "_")

        Return t.ToString
    End Function

    ' replace back base64 characters filtered by HTTP in the url string  
    Public Shared Function unEscapeForUrl(s As String) As String
        Dim t As New StringBuilder(s)
        t.Replace("*", "+")
        t.Replace("-", "/")
        t.Replace("_", "=")
        Return t.ToString
    End Function

    Public Shared Function computeHash(data1 As Byte(), data2 As Byte()) As Byte()
        Dim merged(data1.Length + data2.Length - 1) As Byte
        data1.CopyTo(merged, 0)
        data2.CopyTo(merged, data1.Length)
        Return computeHash(merged)
    End Function

    Public Shared Function computeHash(data() As Byte) As Byte()
        Dim crypto As New SHA256CryptoServiceProvider
        Dim hash() As Byte = crypto.ComputeHash(data)
        Return hash
    End Function

    Public Shared Function computeHash(s As String) As Byte()
        Dim data() As Byte = Encoding.UTF8.GetBytes(s)
        Return computeHash(data)
    End Function

    Public Shared Function encryptAndEnvelope(data() As Byte, cer1 As X509Certificate2, Optional cer2 As X509Certificate2 = Nothing)
        Dim certs As New X509Certificate2Collection
        certs.Add(cer1)
        If cer2 IsNot Nothing Then
            certs.Add(cer2)
        End If
        Return encryptAndEnvelope(data, certs)
    End Function

    Public Shared Function encryptAndEnvelope(data() As Byte, cers As X509Certificate2Collection) As Byte()
        Dim ecms As New EnvelopedCms(New ContentInfo(data))
        Dim r As New CmsRecipientCollection
        For Each cer As X509Certificate2 In cers
            r.Add(New CmsRecipient(cer))
        Next
        ecms.Encrypt(r)
        Return ecms.Encode()
    End Function

    Public Shared Function computeDigitalSignatureAndGenerateP7M(data() As Byte, cer As X509Certificate2, Optional timestamp As Long = 0) As Byte()
        If cer IsNot Nothing Then
            Dim scms As New SignedCms(New ContentInfo(data), False)
            '        Dim signer As New CmsSigner(utility.getCertificateFromStoreByCertificateHash(cer.Thumbprint))

            If cer.PrivateKey Is Nothing Then
                cer = utility.getCertificateFromStoreByCertificateHash(cer.Thumbprint)
            End If

            Dim signer As New CmsSigner(cer)

            If timestamp > 0 Then
                signer.SignedAttributes.Add(New Pkcs9SigningTime(utility.dateTimeFromUnixTime(timestamp)))
            End If
            Try
                scms.ComputeSignature(signer, True)
                Return scms.Encode()
            Catch ex As Exception
                Return Nothing
            End Try
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function verifyDigitalSignatureSHA1(cer As X509Certificate2, s As String, signature As Byte()) As Boolean
        Dim data() As Byte = Encoding.UTF8.GetBytes(s)
        Dim csp As RSACryptoServiceProvider = cer.PublicKey.Key
        Return csp.VerifyData(data, CryptoConfig.MapNameToOID("SHA1"), signature)
    End Function

    Public Shared Function computeDigitalSignatureSHA1(cer As X509Certificate2, s As String) As Byte()

        If cer IsNot Nothing Then

            Dim data() As Byte = Encoding.UTF8.GetBytes(s)

            If cer.PrivateKey Is Nothing Then
                cer = utility.getCertificateFromStoreByCertificateHash(cer.Thumbprint)
            End If

            Try
                Dim csp As RSACryptoServiceProvider = cer.PrivateKey
                Dim signature As Byte() = csp.SignData(data, CryptoConfig.MapNameToOID("SHA1"))
                Return signature

            Catch ex As Exception
                Return Nothing
            End Try

        Else
            Return Nothing
        End If
    End Function

    Public Shared Function getCurrentTimeStamp() As Long
        Return utility.getUnixTime(utility.getUTC)
    End Function

    Public Shared Function getUTC() As DateTime
        Return Now().ToUniversalTime
    End Function

    Public Shared Function getUnixTime(d As DateTime) As Long
        Return d.Subtract(gen170).TotalSeconds
    End Function

    Public Shared Function dateTimeFromUnixTime(unixTime As Long) As DateTime
        Return gen170.AddSeconds(unixTime)
    End Function

    Public Shared Function bin2hex(bin() As Byte) As String
        Dim t As New StringBuilder(50)
        For i As Integer = 0 To bin.Length - 1
            t.Append(bin(i).ToString("X2"))
        Next
        Return t.ToString
    End Function

    Public Shared Function isHashValid(hash() As Byte, difficulty As Integer) As Boolean
        For i As Integer = 0 To hash.Length - 1
            If hash(i) <> 0 Then
                Return False
            Else
                If i >= difficulty - 1 Then
                    Return True
                End If
            End If
        Next
        Return False
    End Function



    Public Shared Function areEquals(a As Byte(), b As Byte()) As Boolean
        If a.Length <> b.Length Then Return False
        For i As Integer = 0 To a.Length - 1
            If a(i) <> b(i) Then
                Return False
            End If
        Next
        Return True
    End Function


    Public Shared Function getCertificateFromStoreByCertificateHash(hash As String, Optional storeName As StoreName = StoreName.My, Optional storeLocation As StoreLocation = StoreLocation.CurrentUser) As X509Certificate
        Dim st As X509Store = New X509Store(storeName, storeLocation)
        st.Open(OpenFlags.ReadOnly)
        Dim cl As X509Certificate2Collection = st.Certificates.Find(X509FindType.FindByThumbprint, hash, True)
        st.Close()

        If cl.Count > 0 Then
            Return cl.Item(0)
        Else
            Return Nothing
        End If

    End Function


    Public Shared Function getCertificateFromStoreBySubjectName(SubjectName As String, Optional storeName As StoreName = StoreName.My, Optional storeLocation As StoreLocation = StoreLocation.CurrentUser) As X509Certificate
        Dim st As X509Store = New X509Store(storeName, storeLocation)
        st.Open(OpenFlags.ReadOnly)
        Dim cl As X509Certificate2Collection = st.Certificates.Find(X509FindType.FindBySubjectName, SubjectName, True)
        st.Close()

        If cl.Count > 0 Then
            Return cl.Item(0)
        Else
            Return Nothing
        End If

    End Function

    Public Shared Function pickCertificateFromStore(Optional storeName As StoreName = StoreName.My, Optional storeLocation As StoreLocation = StoreLocation.CurrentUser) As X509Certificate2
        Dim st As X509Store = New X509Store(storeName, storeLocation)
        st.Open(OpenFlags.ReadOnly)
        Dim sc As X509Certificate2Collection = X509Certificate2UI.SelectFromCollection(st.Certificates, "Certificate Select", "Select a certificate from the following list", X509SelectionFlag.SingleSelection)

        If sc.Count > 0 Then
            Return sc.Item(0)
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function isIssuerCertificateTrusted(certificateToTest As X509Certificate2, trustedIssuerCertificatesHashesCommaDelimited As String) As Boolean
        Dim ic As X509Certificate2 = utility.getIssuerCertificate(certificateToTest)
        For Each s As String In trustedIssuerCertificatesHashesCommaDelimited.Split(",")
            If ic.Thumbprint.ToUpper = s.ToUpper Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Shared Function getIssuerCertificate(ByVal Cer As X509Certificate) As X509Certificate2
        Dim chain As New X509Certificates.X509Chain
        chain.ChainPolicy.RevocationMode = X509Certificates.X509RevocationMode.NoCheck
        Try
            chain.Build(Cer)
            Dim element As X509Certificates.X509ChainElement = chain.ChainElements(1)
            Return element.Certificate
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function getChildElementValue(e As XElement, childElementName As String) As String
        Try
            Return e.Element(childElementName).Value
        Catch ex As Exception
            Return ""
        End Try
    End Function

    Public Shared Function getChildElementValueDouble(e As XElement, childElementName As String) As Double
        Dim value As String = getChildElementValue(e, childElementName)

        If value = "" Then
            Return 0
        Else
            Return CDbl(value)
        End If
    End Function

    Public Shared Function getChildElementValueLong(e As XElement, childElementName As String) As Long
        Dim value As String = getChildElementValue(e, childElementName)

        If value = "" Then
            Return 0
        Else
            Return CLng(value)
        End If
    End Function

    Public Shared Function getBytesFromString(s As String) As Byte()
        If s IsNot Nothing AndAlso s.Length > 0 Then
            Return System.Text.Encoding.UTF8.GetBytes(s)
        Else
            Return New Byte() {}
        End If
    End Function

    Public Shared Function getStringFromBytes(b() As Byte) As String
        If b Is Nothing Then
            Return ""
        Else
            Return System.Text.Encoding.UTF8.GetString(b)
        End If
    End Function

    Public Shared Function getChildElementValue(parentElement As XElement, childElementName As String, Optional defaultValueIfNotFound As String = "") As String
        Dim e As XElement = parentElement.Elements(childElementName).FirstOrDefault
        If e Is Nothing Then
            Return defaultValueIfNotFound
        Else
            Return e.Value
        End If
    End Function

    Public Shared Function getDoubleFromString(s As String) As Double
        Try
            Return CDbl(s.Replace(".", ","))
        Catch ex As Exception
            Return 0
        End Try
    End Function

End Class

Public Class EnvelopedData
    Protected _envelopedCMS As EnvelopedCms
    Protected _envelopedContentText As String
    Public Sub New(Optional Base64EncodedString As String = Nothing)
        If Base64EncodedString IsNot Nothing Then
            decode(Convert.FromBase64String(Base64EncodedString))
        End If
    End Sub

    Public Sub New(Optional data() As Byte = Nothing, Optional cer As X509Certificate2 = Nothing)
        If data IsNot Nothing Then
            decode(data, cer)
        End If
    End Sub


    Public ReadOnly Property envelopedCMS() As EnvelopedCms
        Get
            Return _envelopedCMS
        End Get
    End Property

    Public Function decode(data() As Byte, Optional cer As X509Certificate2 = Nothing) As Byte()
        _envelopedCMS = New EnvelopedCms
        Try
            _envelopedCMS.Decode(data)
            If cer Is Nothing Then
                _envelopedCMS.Decrypt()
            Else
                Dim es As New X509Certificate2Collection
                es.Add(cer)
                _envelopedCMS.Decrypt(es)
            End If
            Try
                _envelopedContentText = utility.getStringFromBytes(content)
            Catch ex As Exception
                _envelopedContentText = ""
            End Try

        Catch ex As Exception
        End Try

        Return content
    End Function

    Public ReadOnly Property envelopedContentText As String
        Get
            Return _envelopedContentText
        End Get
    End Property

    Public ReadOnly Property content As Byte()
        Get
            If _envelopedCMS IsNot Nothing AndAlso _envelopedCMS.ContentInfo IsNot Nothing Then
                Return _envelopedCMS.ContentInfo.Content
            Else
                Return Nothing
            End If
        End Get
    End Property

End Class


Public Class SignedData
    Protected _signedCMS As SignedCms
    Protected _signedAttributes As New Dictionary(Of String, Byte())
    Protected _signedSignignTime As DateTime = Nothing
    Protected _signedContentText As String

    Public Sub New(Optional Base64EncodedString As String = Nothing)
        If Base64EncodedString IsNot Nothing Then
            decode(Convert.FromBase64String(Base64EncodedString))
        End If
    End Sub

    Public Sub New(Optional data() As Byte = Nothing)
        If data IsNot Nothing Then
            decode(data)
        End If
    End Sub

    Public ReadOnly Property signedCMS() As SignedCms
        Get
            Return _signedCMS
        End Get
    End Property

    Public Function decode(data() As Byte) As Byte()
        _signedCMS = New SignedCms
        _signedCMS.Decode(data)
        _signedCMS.CheckSignature(True)
        _signedAttributes.Clear()
        _signedSignignTime = Nothing
        If _signedCMS.SignerInfos.Count > 0 Then
            Dim si As SignerInfo = _signedCMS.SignerInfos(0)
            For Each sa As CryptographicAttributeObject In si.SignedAttributes
                _signedAttributes.Add(sa.Oid.FriendlyName, sa.Values(0).RawData)
                If sa.Oid.Value = "1.2.840.113549.1.9.5" Then
                    Dim time As New Pkcs9SigningTime(sa.Values(0).RawData)
                    _signedSignignTime = time.SigningTime
                End If
            Next
        End If

        Try
            _signedContentText = utility.getStringFromBytes(content)
        Catch ex As Exception
            _signedContentText = ""
        End Try

        Return content
    End Function

    Public ReadOnly Property signedContentText As String
        Get
            Return _signedContentText
        End Get
    End Property

    Public ReadOnly Property content As Byte()
        Get
            If _signedCMS IsNot Nothing AndAlso _signedCMS.ContentInfo IsNot Nothing Then
                Return _signedCMS.ContentInfo.Content
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public ReadOnly Property certificate As X509Certificate2
        Get
            If _signedCMS IsNot Nothing AndAlso _signedCMS.Certificates.Count > 0 Then
                Return _signedCMS.Certificates(0)
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public ReadOnly Property SignedSigningTime() As DateTime
        Get
            Return _signedSignignTime
        End Get
    End Property

    Public ReadOnly Property unixSignedSigningTime() As Long
        Get
            Try
                Return utility.getUnixTime(_signedSignignTime)
            Catch ex As Exception
                Return 0
            End Try
        End Get
    End Property



End Class
