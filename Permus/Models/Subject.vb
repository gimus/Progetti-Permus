' un soggetto è un'identità che opera nella blockchain. può essere una persona oppure un ente
' ai fini del sistema la cosa importante è che il soggetto sia in possesso di un certificato digitale X509 emesso da una
' Certification Authority riconosciuta dal BlockMaster, altri requisiti possono essere applicati alla subject string del certificato

Imports System.Security.Cryptography.X509Certificates

Public Class Subject
    Public Property id As String = Nothing
    Public Property distinguishedName As String = Nothing
    Public Property name As String = Nothing
    Public Property email As String = Nothing
    Public Property x509Certificates As New Certificates

    Public Property coinBalance As New CoinBalance
    Public Property isAuthority As Boolean = False
    Public Property isPublic As Boolean = False
    Public Property profile As SubjectProfile

    Public token As String = ""
    Public lastPing As DateTime = DateTime.MinValue

    Public tag As Object

    Public ReadOnly Property isValid As Boolean
        Get
            Return id IsNot Nothing And distinguishedName IsNot Nothing And name IsNot Nothing And email IsNot Nothing And x509Certificate IsNot Nothing
        End Get
    End Property

    Public Function isOnline(Optional seconds As Integer = 10) As Boolean
        Return Now().Subtract(Me.lastPing).TotalSeconds < seconds
    End Function

    Public ReadOnly Property x509Certificate As X509Certificate2
        Get
            If x509Certificates.Count > 0 Then
                Return x509Certificates.Values.First
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Shared Function parse(subj As String) As Subject
        Dim subject As New Subject
        Dim s() As String = subj.Split(",")
        For Each ss In s
            ss = LTrim(ss)
            If ss.StartsWith("E") Then
                subject.email = ss.Split("=")(1)
            ElseIf ss.StartsWith("CN") Then
                Dim sss() As String = ss.Split("=")
                Dim ssss() As String = sss(1).Split("/")
                subject.name = ssss(0)
                subject.id = ssss(1)
            End If
        Next

        ' un modo grezzo per determinare se il soggetto è pubblico, 
        ' in seguito occorrerà definire uno standard per codificare tale informazione nella subject string
        If subject.id.ToUpper.StartsWith("AAL0") Then
            subject.isPublic = True
        End If

        Return subject
    End Function

    Public Shared Function fromX509Certificate(cer As X509Certificate2) As Subject

        Try
            Dim subject As Subject = Subject.parse(cer.Subject)

            subject.distinguishedName = cer.Subject
            subject.x509Certificates.Add(cer.Thumbprint, cer)
            If subject.isValid() Then
                Return subject
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function getIdFromCertificate(cer As X509Certificate2) As String
        Dim s() As String = cer.Subject.Split(",")
        For Each ss In s
            ss = LTrim(ss)
            If ss.StartsWith("CN") Then
                Return ss.Split("/")(1)
            End If
        Next
        Return ""
    End Function

    Public Function xml() As XElement
        Dim e As XElement = New XElement("subject")
        e.Add(New XAttribute("id", Me.id))
        e.Add(New XElement("name", name))
        e.Add(New XElement("isAuthority", isAuthority))
        e.Add(coinBalance.xml())

        Dim ce As New XElement("x509Certificates")
        Dim cce As XElement
        For Each c As X509Certificate2 In Me.x509Certificates.Values
            cce = New XElement("x509Certificate")
            cce.Add(New XAttribute("thumbprint", c.Thumbprint))
            cce.Add(New XElement("data", Convert.ToBase64String(c.RawData)))
            ce.Add(cce)
        Next
        e.Add(ce)
        Return e
    End Function

    Public Shared Function createFromXml(e As XElement) As Subject
        Dim s As Subject = Nothing
        Dim c As X509Certificate2
        For Each ce As XElement In e.Element("x509Certificates").Elements
            Dim d() As Byte = Convert.FromBase64String(ce.Element("data").Value)
            c = New X509Certificate2(d)
            If s Is Nothing Then
                s = Subject.fromX509Certificate(c)
            Else
                s.x509Certificates.Add(c)
            End If
        Next
        If s IsNot Nothing Then
            s.isAuthority = e.Element("isAuthority").Value
            s.coinBalance = CoinBalance.createFromXml(e.Element("coinBalance"))
        End If
        Return s
    End Function
End Class

Public Class Subjects
    Inherits Dictionary(Of String, Subject)
    Public allX509Certificates As New List(Of String)
    Dim auth As Subject

    Public Overloads Sub Clear()
        MyBase.Clear()
        allX509Certificates.Clear()
    End Sub

    Public Function getAuthority() As Subject
        Return auth
    End Function

    Public Function certificateExists(Thumbprint As String) As Boolean
        Return allX509Certificates.Contains(Thumbprint)
    End Function

    Public Function addCertificate(cer As X509Certificate2) As Subject
        Dim id As String = Subject.getIdFromCertificate(cer)
        Dim s As Subject
        If Not Me.ContainsKey(id) Then
            s = Subject.fromX509Certificate(cer)
            If Me.Count = 0 Then
                s.isAuthority = True
                Me.auth = s
            End If
            Me.Add(s.id, s)
        Else
            s = Me(id)
        End If
        If Not s.x509Certificates.ContainsKey(cer.Thumbprint) Then
            s.x509Certificates.Add(cer.Thumbprint, cer)
        End If

        If Not certificateExists(cer.Thumbprint) Then
            Me.allX509Certificates.Add(cer.Thumbprint)
        End If

        Return s
    End Function

    Public Sub addSubject(s As Subject)
        Me.Add(s.id, s)
        If s.isAuthority Then
            auth = s
        End If
        For Each c As X509Certificate2 In s.x509Certificates.Values
            If Not certificateExists(c.Thumbprint) Then
                allX509Certificates.Add(c.Thumbprint)
            End If

        Next
    End Sub

    Public Function getElementByName(name As String) As Subject
        Return Me.Values.Where(Function(x) x.name.ToUpper = name.ToUpper).FirstOrDefault
    End Function

    Public Function getElementById(id As String) As Subject
        If Me.ContainsKey(id) Then
            Return Me(id)
        Else
            Return Nothing
        End If
    End Function
    Public Function getElementByTelegramId(tid As String) As Subject
        Return Me.Values.Where(Function(x) x.profile.asProtected.telegramId = tid).FirstOrDefault
    End Function
    Public Function getElementByRfId(rfid As String) As Subject
        Return Me.Values.Where(Function(x) x.profile.asProtected.rfidSerial = rfid).FirstOrDefault
    End Function

    Public Function getElementByX509Certificate(cer As X509Certificate2) As Subject
        Dim subj As Subject = Subject.fromX509Certificate(cer)

        If subj IsNot Nothing Then
            If Me.ContainsKey(subj.id) Then
                Return Me(subj.id)
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If

    End Function

    Public Function xml() As XElement
        Dim e As XElement = New XElement("subjects")
        For Each s As Subject In Me.Values
            e.Add(s.xml)
        Next
        Return e
    End Function

    Public Shared Function createFromXml(e As XElement) As Subjects
        Dim o As New Subjects
        Dim s As Subject
        For Each ce As XElement In e.Elements
            s = Subject.createFromXml(ce)
            o.addSubject(s)
        Next
        Return o
    End Function

End Class

Public Class Certificates
    Inherits Dictionary(Of String, X509Certificate2)
    Public Overloads Sub Add(cer As X509Certificate2)
        If Not Me.ContainsKey(cer.Thumbprint) Then
            Me.Add(cer.Thumbprint, cer)
        End If
    End Sub
End Class

Public Class CoinBalance
    Protected _opsCount As Long

    Protected Property _earned As Double
    Protected Property _spent As Double

    Public ReadOnly Property balance As Double
        Get
            Return _earned - _spent
        End Get
    End Property

    Public ReadOnly Property opsCount As Long
        Get
            Return _opsCount
        End Get
    End Property

    Public Sub addEarned(c As Double)
        _earned += c
        _opsCount += 1
    End Sub

    Public Sub addSpent(c As Double)
        _spent += c
        _opsCount += 1
    End Sub

    Public ReadOnly Property earned As Double
        Get
            Return _earned
        End Get
    End Property

    Public ReadOnly Property spent As Double
        Get
            Return _spent
        End Get
    End Property

    Public Function xml() As XElement
        Dim e As XElement = New XElement("coinBalance")
        e.Add(New XElement("opsCount", _opsCount))
        e.Add(New XElement("spent", _spent.ToString))
        e.Add(New XElement("earned", _earned.ToString))
        Return e
    End Function

    Public Shared Function createFromXml(e As XElement) As CoinBalance
        Dim cb As New CoinBalance
        With e
            cb._opsCount = .Element("opsCount").Value
            cb._spent = utility.getDoubleFromString(.Element("spent").Value)
            cb._earned = utility.getDoubleFromString(.Element("earned").Value)
        End With
        Return cb
    End Function

End Class

Public Class SubjectProfile
    Public subjectId As String
    Public name As String
    Public email As String

    Public hasTelegram As Boolean = False
    Public hasPfx As Boolean = False
    Public hasOtp As Boolean = False
    Public hasRfid As Boolean = False

    Public ReadOnly Property asProtected As ProtectedSubjectProfile
        Get
            Return DirectCast(Me, ProtectedSubjectProfile)
        End Get
    End Property
End Class
Public Class ProtectedSubjectProfile
    Inherits SubjectProfile

    Public telegramId As Long
    Public pfx As Byte()
    Protected Friend pfxPin As String
    Protected Friend otpSecretKey As String
    Protected Friend rfidSerial As Long

    Public Sub setRfidSerial(i As Long)
        rfidSerial = i
    End Sub

    Public Sub setPfxPin(p As String)
        pfxPin = p
    End Sub
    Public Sub setotpSecretKey(k As String)
        otpSecretKey = k
        hasOtp = True
    End Sub

    Public ReadOnly Property hasCertificate As Boolean
        Get
            Return pfx.Length > 100 And pfxPin <> ""
        End Get
    End Property

    Public ReadOnly Property X509Certificate2() As X509Certificate2
        Get
            Try
                If pfx.Length > 100 And pfxPin <> "" Then
                    Return New X509Certificate2(pfx, pfxPin, X509KeyStorageFlags.PersistKeySet Or X509KeyStorageFlags.MachineKeySet)
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                Return Nothing
            End Try
        End Get
    End Property

    Public Function computeDigitalSignatureAndGenerateP7M(data As Byte(), timestamp As Long) As Byte()
        Try
            Dim cer As X509Certificate2 = Me.X509Certificate2
            If cer IsNot Nothing Then
                Return utility.computeDigitalSignatureAndGenerateP7M(data, cer, timestamp)
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Function isOtpVerified(otp As String) As Boolean
        Return OtpUtility.verify(otpSecretKey, otp)
    End Function

    Public Function getTransferTransactionInAcceptedState(ttp As TransferTransactionPackage, otp As String) As TransferTransactionPackage
        If isOtpVerified(otp) And Me.hasCertificate Then

            If ttp IsNot Nothing Then
                Dim cstt As CoSignedTransferTransaction = ttp.transaction
                Dim cer As X509Certificate2 = Me.X509Certificate2
                cstt.signatureTo = cstt.computeSignature(cer, utility.getCurrentTimeStamp)
                cstt.updateState()
                Return ttp
            Else
                Return Nothing
            End If
        Else
            Return Nothing
        End If
    End Function


End Class
