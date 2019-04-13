Imports Permus
Imports System.Data.SqlClient
Imports System.Environment

Public MustInherit Class ClientBlockChainDataAdapter
    Protected appDataFolder As String
    Public Sub New(C As ClientBlockChain)
        appDataFolder = GetFolderPath(SpecialFolder.ApplicationData) & "\PermusClient\BlockChain_" & C.systemInfo.blockChainVersion

        If Not IO.Directory.Exists(appDataFolder) Then
            IO.Directory.CreateDirectory(appDataFolder)
        End If

    End Sub

    Public ReadOnly Property applicationDataFolder As String
        Get
            Return appDataFolder
        End Get
    End Property


    Public Overridable Sub SaveSnapshot(ByRef C As ClientBlockChain)
    End Sub

    Public Overridable Function InitBlockChainFromSnapshot(ByRef C As ClientBlockChain) As Boolean
        Return False
    End Function

    Public Overridable Function isSnapshotAvailable(userId As String) As Boolean
        Return False
    End Function

    Public Overridable Function purgeLocalData() As Boolean
        Return False
    End Function

    Public Overridable Sub deleteBlockInfo(serial As Long)
    End Sub

    Public Overridable Function saveBlock(b As Block) As Boolean
        Return Nothing
    End Function

    Public Overridable Function blockExists(serial As Long) As Boolean
        Return False
    End Function

    Public Overridable Function getBlock(serial As Long) As Block
        Return Nothing
    End Function

    Public Overridable Function savePrivateBlock(b As Block, userId As String) As Boolean
        Return False
    End Function

    Public Overridable Function getPrivateBlock(hashString As String, userId As String) As Block
        Return Nothing
    End Function

End Class

Public Class ClientBlockChainFileSystemDataAdapter
    Inherits ClientBlockChainDataAdapter

    Protected userDataFolder As String
    Protected blockChainDataFolder As String
    Protected privateBlocksDataFolder As String

    Public Sub New(C As ClientBlockChain)
        MyBase.New(C)

        blockChainDataFolder = appDataFolder & "\BlockChain"
        privateBlocksDataFolder = appDataFolder & "\PrivateBlocks"

        If Not IO.Directory.Exists(blockChainDataFolder) Then
            IO.Directory.CreateDirectory(blockChainDataFolder)
        End If

        If Not IO.Directory.Exists(privateBlocksDataFolder) Then
            IO.Directory.CreateDirectory(privateBlocksDataFolder)
        End If

    End Sub

    Public Overrides Sub SaveSnapshot(ByRef C As ClientBlockChain)
        Dim doc As New XElement("Snapshot")

        Dim b As Block = C.getBlock(C.systemInfo.currentBlockSerial - 1)
        doc.Add(b.xml)
        doc.Add(C.subjects.xml)
        Dim e As XElement

        e = New XElement("all_transfer_transactions")
        For Each s As String In C.allTransferTransactions
            e.Add(New XElement("t", s))
        Next
        doc.Add(e)

        e = New XElement("user_transfer_transactions")
        For Each tt As TransferTransaction In C.userTransferTransactions.Values
            e.Add(tt.xml)
        Next

        doc.Add(e)
        Dim buf() As Byte = Zipping.ComprimiTesto(doc.ToString())
        Dim xbuf() As Byte = utility.encryptAndEnvelope(buf, C.currentUser.x509Certificate)
        IO.File.WriteAllBytes(getPrivateShapshotFileName(C.currentUser.id), xbuf)

        ' doc.Save(getPrivateShapshotFileName(C.currentUser.id))
        '     Zipping.CompressAndSaveText(doc.ToString, getPrivateShapshotFileName(C.currentUser.id))
    End Sub

    Public Overrides Function InitBlockChainFromSnapshot(ByRef C As ClientBlockChain) As Boolean
        Try
            Dim xbuf() As Byte = IO.File.ReadAllBytes(getPrivateShapshotFileName(C.currentUser.id))
            Dim ed As New EnvelopedData(xbuf)
            Dim s As String = Zipping.EspandiTesto(ed.content)
            Dim doc As XElement = XElement.Parse(s)

            '            Dim doc As XElement = XElement.Load(getPrivateShapshotFileName(C.currentUser.id))

            C.allTransferTransactions.Clear()
            For Each e In doc.Element("all_transfer_transactions").Elements
                C.allTransferTransactions.Add(e.Value)
            Next

            C._subjects = Subjects.createFromXml(doc.Element("subjects"))

            Dim tt As TransferTransaction = Nothing
            For Each e As XElement In doc.Element("user_transfer_transactions").Elements
                tt = Transaction.createFromXml(e)

                C.parseNewUserTransferTransaction(tt)
            Next
            C.currentBlock = New Block(doc.Element("Block"))

            Return True

        Catch ex As Exception
            Throw New Exception("Si è verificato un errore ripristinando uno snapshot.", ex)
        End Try

    End Function

    Public Overrides Function purgeLocalData() As Boolean
        IO.Directory.Delete(blockChainDataFolder, True)
        IO.Directory.CreateDirectory(blockChainDataFolder)
        Return True
    End Function

    Protected Overridable Function getBlockFileName(blockSerial As Long) As String
        Return String.Format("{0}\block_{1}.xml", blockChainDataFolder, blockSerial.ToString)
    End Function

    Protected Overridable Function getPrivateBlockFileName(blockHash As String, userId As String) As String
        Return String.Format("{0}\{2}\private_block_{1}.xml", privateBlocksDataFolder, blockHash, userId)
    End Function

    Protected Overridable Function getUserDataFolderName(userId As String) As String
        Dim dfn As String = String.Format("{0}\{1}", privateBlocksDataFolder, userId)
        If Not IO.Directory.Exists(dfn) Then
            IO.Directory.CreateDirectory(dfn)
        End If
        Return dfn
    End Function

    Public Overridable Function getPrivateShapshotFileName(userId As String) As String
        Return String.Format("{0}\snapshot.bin", getUserDataFolderName(userId))
    End Function

    Public Overrides Function isSnapshotAvailable(userId As String) As Boolean
        Return IO.File.Exists(getPrivateShapshotFileName(userId))
    End Function


    Public Overrides Sub deleteBlockInfo(serial As Long)
        Try
            Dim fn As String = getBlockFileName(serial)
            If IO.File.Exists(fn) Then
                IO.File.Delete(fn)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Overrides Function saveBlock(b As Block) As Boolean
        Try
            Dim fn As String = getBlockFileName(b.serial)
            b.xml.Save(fn)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Overrides Function blockExists(serial As Long) As Boolean
        Return IO.File.Exists(getBlockFileName(serial))
    End Function


    Public Overrides Function getBlock(serial As Long) As Block
        Dim fn As String = getBlockFileName(serial)
        If IO.File.Exists(fn) Then
            Dim d As XDocument = XDocument.Load(fn)
            Return New Block(d.Root)
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
            b.xml.Save(fn)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Overrides Function getPrivateBlock(hashString As String, userId As String) As Block
        Dim fn As String = getPrivateBlockFileName(hashString, userId)
        If IO.File.Exists(fn) Then
            Dim d As XDocument = XDocument.Load(fn)
            Return New Block(d.Root)
        Else
            Return Nothing
        End If
    End Function

End Class
