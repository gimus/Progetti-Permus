Imports System.Data.SqlClient
Imports ServiziDB
Imports Permus

Public Class BlockMasterDataAdapter
    Protected db As ServiziDB.SERVIZI_DB

    Public Sub New(localDbConnectionString As String)
        db = New SERVIZI_DB
        db.ConnectionString = localDbConnectionString
    End Sub

#Region "Metodi Pubblici"

    Public Function ModificaAttributoProfilo(subjectId As String, attributo As String, valore As String) As DataTable
        Return GestioneProfilo(subjectId, "MODIFICA_ATTRIBUTO", attributo, valore)
    End Function

    Public Function GestioneProfilo(subjectId As String, funzione As String, Optional P1 As String = "", Optional P2 As String = "", Optional P3 As String = "", Optional P4 As String = "") As DataTable

        If funzione.ToLower = "modifica_attributo" And (P1 = "pfxPin" Or P1 = "pinPfx" Or P1 = "otpSecretKey") Then
            P2 = Convert.ToBase64String(Permus.AES128.EncryptText(P2, My.Settings.local_enc_key & "GPKEY"))
        End If

        Dim cmd As New SqlCommand("A_GestioneProfilo")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@subjectId", subjectId)
        cmd.Parameters.AddWithValue("@funzione", funzione)
        cmd.Parameters.AddWithValue("@P1", P1)
        cmd.Parameters.AddWithValue("@P2", P2)
        cmd.Parameters.AddWithValue("@P3", P3)
        cmd.Parameters.AddWithValue("@P4", P4)
        Dim dt As New DataTable
        db.CaricaDati(cmd, dt)
        Return dt
    End Function


    Public Function GestioneDevice(subjectId As String, id As String, funzione As String, Optional P1 As String = "", Optional P2 As String = "", Optional P3 As String = "", Optional P4 As String = "") As DataSet
        Dim cmd As New SqlCommand("A_GestioneDevice")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@subjectId", id)
        cmd.Parameters.AddWithValue("@funzione", funzione)
        cmd.Parameters.AddWithValue("@id", id)
        cmd.Parameters.AddWithValue("@P1", P1)
        cmd.Parameters.AddWithValue("@P2", P2)
        cmd.Parameters.AddWithValue("@P3", P3)
        cmd.Parameters.AddWithValue("@P4", P4)
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        Return ds
    End Function

    Public Function getSubjectProfiles(op As String, Optional P1 As String = "", Optional P2 As String = "") As DataTable
        Dim cmd As New SqlCommand("GetSubjectProfiles")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@op", op)
        cmd.Parameters.AddWithValue("@P1", P1)
        cmd.Parameters.AddWithValue("@P2", P2)
        Dim dt As DataTable = Nothing
        db.CaricaDati(cmd, dt)
        Return dt
    End Function

    Public Function getDeviceInfo(deviceHash As String, device As String) As DataTable
        Dim cmd As New SqlCommand("GetDeviceInfo")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@deviceHash", deviceHash)
        cmd.Parameters.AddWithValue("@device", device)
        Dim dt As DataTable = Nothing
        db.CaricaDati(cmd, dt)
        Return dt
    End Function

    Public Function getSystemInfo() As SystemInfo
        Dim ds As DataSet = loadSystemInfo()
        Return Factory.leggiSystemInfo(ds)
    End Function


    Public Function getBlock(serial As Long) As Block
        Dim ds As DataSet = loadBlockData(CLng(serial))
        Return Factory.leggiBlock(ds)
    End Function

    Public Function getPrivateBlock(blockHash As String) As EnvelopedBlock
        Dim ds As DataSet = loadPrivateBlockData(blockHash)
        Return Factory.leggiPrivateBlock(ds)
    End Function

    Public Function existsPrivateBlock(blockHash As String) As Boolean
        Dim ds As DataSet = loadPrivateBlockData(blockHash)
        Return ds.Tables(0).Rows.Count > 0
    End Function

    Public Function saveBlock(block As Block) As Boolean
        Dim cmd As New SqlCommand("setBlock")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@serial", block.serial)
        cmd.Parameters.AddWithValue("@blockText", block.getXmlText)
        If db.EseguiComando(cmd) Then
            Return db.LastActionSuccess
        Else
            Return False
        End If
    End Function

    Public Function savePrivateBlock(b As EnvelopedBlock, t As TransferTransaction) As Boolean
        Dim cmd As New SqlCommand("setPrivateBlock")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@blockHash", b.blockHash)
        cmd.Parameters.AddWithValue("@blockData", Convert.FromBase64String(b.envelopedBlockData))
        cmd.Parameters.AddWithValue("@publicBlockSerial", t.blockSerial)
        cmd.Parameters.AddWithValue("@transferTransactionSerial", t.serial)

        If db.EseguiComando(cmd) Then
            Return db.LastActionSuccess
        Else
            Return False
        End If
    End Function

#End Region

#Region "metodi protetti"
    Protected Function loadSystemInfo() As DataSet
        Dim cmd As New SqlCommand("getSystemInfo")
        cmd.CommandType = CommandType.StoredProcedure
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        Return ds
    End Function

    Protected Function loadBlockData(serial As Long) As DataSet
        Dim cmd As New SqlCommand("getBlock")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@serial", serial)
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        Return ds
    End Function

    Protected Function loadPrivateBlockData(blockHash As String) As DataSet
        Dim cmd As New SqlCommand("getPrivateBlock")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@blockHash", blockHash)
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        Return ds
    End Function

#End Region

End Class
