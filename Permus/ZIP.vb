Imports System.IO.Compression
Imports System.Text
Public Class Zipping
    Public Shared Function LoadAndExpandText(path As String) As String
        Return EspandiTesto(IO.File.ReadAllBytes(path))
    End Function

    Public Shared Sub CompressAndSaveText(txt As String, path As String)
        IO.File.WriteAllBytes(path, ComprimiTesto(txt))
    End Sub

    Public Shared Function ComprimiTesto(txt As String) As Byte()
        Dim encoding As New System.Text.UnicodeEncoding()
        Return ComprimiBytes(encoding.GetBytes(txt))
    End Function

    Public Shared Function EspandiTesto(b() As Byte) As String
        Dim encoding As New System.Text.UnicodeEncoding()
        Return encoding.GetString(EspandiBytes(b))
    End Function

    Public Shared Function ComprimiBytes(b() As Byte) As Byte()
        Dim ms As New IO.MemoryStream(100)
        Dim gzip As New GZipStream(ms, CompressionMode.Compress, True)
        gzip.Write(b, 0, b.Length)
        gzip.Flush()
        gzip.Close()
        Dim buf() As Byte = ms.GetBuffer

        ms.Close()
        Return buf
    End Function

    Public Shared Function EspandiBytes(b() As Byte) As Byte()
        Dim BufSize As Integer = 100000
        Dim ms As New IO.MemoryStream(b)
        Dim oms As New IO.MemoryStream(b.Length)
        Dim gzip As New GZipStream(ms, CompressionMode.Decompress, True)

        gzip.CopyTo(oms)
        gzip.Flush()
        gzip.Close()
        oms.Capacity = oms.Length
        Dim bufOut() As Byte = oms.GetBuffer()
        ms.Close()
        oms.Close()
        Return bufOut
    End Function

End Class

