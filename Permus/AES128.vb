Imports System.Security.Cryptography
Imports System.Security.Authentication

Imports System.IO

Public Class AES128

    Protected Shared Sub GetKeyAndIVFromString(ByVal s As String, ByRef KEY() As Byte, ByRef IV() As Byte)
        Dim Hash() As Byte = utility.computeHash(System.Text.Encoding.UTF8.GetBytes(s))
        Array.Copy(Hash, KEY, 16)
        Array.Copy(Hash, 16, IV, 0, 16)
    End Sub

    Public Shared Function EncryptText(ByVal Text As String, ByVal password As String) As Byte()
        Return EncryptData(System.Text.Encoding.UTF8.GetBytes(Text), password)
    End Function

    Public Shared Function DecryptText(ByVal d() As Byte, ByVal password As String) As String
        Return System.Text.Encoding.UTF8.GetString(DecryptData(d, password))
    End Function

    Public Shared Function EncryptText(ByVal Text As String, ByVal KEY() As Byte, ByVal IV() As Byte) As Byte()
        Return EncryptData(System.Text.Encoding.UTF8.GetBytes(Text), KEY, IV)
    End Function

    Public Shared Function DecryptText(ByVal d() As Byte, ByVal KEY() As Byte, ByVal IV() As Byte) As String
        Return System.Text.Encoding.UTF8.GetString(DecryptData(d, KEY, IV))
    End Function

    Public Shared Function EncryptData(ByVal d() As Byte, ByVal password As String) As Byte()

        Dim myAes As New AesCryptoServiceProvider()


        Dim KEY(15) As Byte
        Dim IV(15) As Byte
        GetKeyAndIVFromString(password, KEY, IV)
        Return EncryptData(d, KEY, IV)
    End Function

    Public Shared Function DecryptData(ByVal d() As Byte, ByVal password As String) As Byte()
        Dim KEY(15) As Byte
        Dim IV(15) As Byte
        GetKeyAndIVFromString(password, KEY, IV)
        Return DecryptData(d, KEY, IV)
    End Function

    Public Shared Function EncryptData(ByVal d() As Byte, ByVal KEY() As Byte, ByVal IV() As Byte) As Byte()
        Dim aes128Provider As AesCryptoServiceProvider = New AesCryptoServiceProvider
        Dim cryptoTransform As ICryptoTransform = aes128Provider.CreateEncryptor(KEY, IV)
        Dim encryptedStream As MemoryStream = New MemoryStream()
        Dim cryptStream As CryptoStream = New CryptoStream(encryptedStream, cryptoTransform, CryptoStreamMode.Write)
        cryptStream.Write(d, 0, d.Length)
        cryptStream.FlushFinalBlock()
        encryptedStream.Position = 0
        Dim result(encryptedStream.Length - 1) As Byte
        encryptedStream.Read(result, 0, encryptedStream.Length)
        cryptStream.Close()
        Return result
    End Function

    Public Shared Function DecryptData(ByVal d() As Byte, ByVal KEY() As Byte, ByVal IV() As Byte) As Byte()
        Dim aes128Provider As AesCryptoServiceProvider = New AesCryptoServiceProvider
        Dim cryptoTransform As ICryptoTransform = aes128Provider.CreateDecryptor(KEY, IV)
        Dim decryptedStream As MemoryStream = New MemoryStream()
        Dim cryptStream As CryptoStream = New CryptoStream(decryptedStream, cryptoTransform, CryptoStreamMode.Write)
        cryptStream.Write(d, 0, d.Length)
        cryptStream.FlushFinalBlock()
        decryptedStream.Position = 0
        Dim result(decryptedStream.Length - 1) As Byte
        decryptedStream.Read(result, 0, decryptedStream.Length)
        cryptStream.Close()
        Return result
    End Function

End Class

