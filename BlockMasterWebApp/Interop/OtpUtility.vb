Imports OtpNet
' https://github.com/kspearrin/Otp.NET
Public Class OtpUtility

    Public Shared Function verify(secretKey As String, otp As String) As Boolean
        Dim totp As New OtpNet.Totp(Base32Encoding.ToBytes(secretKey), 30, OtpHashMode.Sha256, 6, Nothing)
        Return totp.VerifyTotp(otp, 30)
    End Function

    Public Function generateSecretKey() As String
        Dim s(34) As Byte
        Dim rn As New Random(Guid.NewGuid.GetHashCode)
        For i As Integer = 0 To 34
            s(i) = rn.Next(0, 255)
        Next
        Return OtpNet.Base32Encoding.ToString(s)
    End Function

    Public Shared Function makeUri(subjectId As String, secretKeyBase32Encoded As String)
        Dim algorithm As String = "SHA256"
        Dim account As String = "OTP-" & subjectId
        Dim issuer As String = "Permus Blockchain"
        Dim secret As String = secretKeyBase32Encoded
        Dim digits As String = 6
        Dim period As String = 30
        Dim image As String = ""
        Dim type As String = "totp"
        Dim t As New StringBuilder(100)

        t.AppendFormat("otpauth://{0}/", type)
        If issuer <> "" Then
            t.AppendFormat("{0}:", HttpUtility.UrlEncode(issuer))
        End If
        t.AppendFormat("{0}?secret={1}&algorithm={2}&digits={3}&period={4}", HttpUtility.UrlEncode(account), secret, algorithm, digits, period)

        If type = "hotp" Then
            t.Append("&counter=0")
        End If

        If image <> "" Then
            t.AppendFormat("&image={0}:", HttpUtility.UrlEncode(image))
        End If
        Return t.ToString

    End Function

End Class
