Imports Permus
Imports System.Data.SqlClient
Imports System.Environment


Public Class ClientBlockChainZippedFileSystemDataAdapter
    Inherits ClientBlockChainFileSystemDataAdapter


    Public Sub New(C As ClientBlockChain)
        MyBase.New(C)
    End Sub

    Protected Overrides Function getBlockFileName(blockSerial As Long) As String
        Return String.Format("{0}\block_{1}.xzd", blockChainDataFolder, blockSerial.ToString)
    End Function

    Protected Overrides Function getPrivateBlockFileName(blockHash As String, userId As String) As String
        Return String.Format("{0}\{2}\private_block_{1}.xzd", privateBlocksDataFolder, blockHash, userId)
    End Function

    Public Overrides Function saveBlock(b As Block) As Boolean
        Try
            Dim fn As String = getBlockFileName(b.serial)
            Zipping.CompressAndSaveText(b.xml.ToString, fn)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Overrides Function getBlock(serial As Long) As Block
        Dim fn As String = getBlockFileName(serial)
        If IO.File.Exists(fn) Then
            Dim s As String = Zipping.LoadAndExpandText(fn)
            Return New Block(s)
        Else
            Return Nothing
        End If
    End Function

    Public Overrides Function savePrivateBlock(b As Block, userId As String) As Boolean
        Try
            Dim dn As String = String.Format("{0}\{1}", privateBlocksDataFolder, userId)

            If Not IO.Directory.Exists(dn) Then
                IO.Directory.CreateDirectory(dn)
            End If

            Dim fn As String = getPrivateBlockFileName(utility.bin2hex(b.computeHash), userId)
            Zipping.CompressAndSaveText(b.xml.ToString, fn)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Overrides Function getPrivateBlock(hashString As String, userId As String) As Block
        Dim fn As String = getPrivateBlockFileName(hashString, userId)
        If IO.File.Exists(fn) Then
            Dim s As String = Zipping.LoadAndExpandText(fn)
            Return New Block(s)
        Else
            Return Nothing
        End If
    End Function

End Class
