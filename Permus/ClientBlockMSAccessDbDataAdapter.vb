Imports Permus
Imports System.Data.OleDb
Imports System.Environment

Public Class ClientBlockMSAccessDbDataAdapter
    Inherits ClientBlockChainDataAdapter
    Dim _localDbPath As String
    Dim dbConnectionString As String

    Public Sub New(C As ClientBlockChain)
        MyBase.New(C)
        _localDbPath = appDataFolder & "\BlockChain_" & C.systemInfo.blockChainVersion & ".lbcdb"
        If Not IO.File.Exists(_localDbPath) Then
            IO.File.WriteAllBytes(_localDbPath, My.Resources.masterLocalDb)
        End If
        dbConnectionString = "Provider=Microsoft.ACE.OLEDB.15.0;Data Source=" & _localDbPath
    End Sub

    Public ReadOnly Property localDbPath As String
        Get
            Return _localDbPath
        End Get
    End Property

    Protected Function LoadData(cmd As OleDbCommand) As DataTable
        Dim cnn As New OleDb.OleDbConnection(dbConnectionString)
        cnn.Open()
        cmd.Connection = cnn
        cmd.CommandType = CommandType.Text

        Dim ds As New DataSet
        Dim da As New OleDbDataAdapter(cmd)
        da.Fill(ds)
        cnn.Close()
        If ds.Tables.Count > 0 Then
            Return ds.Tables(0)
        Else
            Return Nothing
        End If
    End Function

    Protected Function executeCommand(cmd As OleDbCommand) As Integer
        Dim cnn As New OleDb.OleDbConnection(dbConnectionString)
        cnn.Open()
        cmd.Connection = cnn
        cmd.CommandType = CommandType.Text
        Dim nRecords As Integer = cmd.ExecuteNonQuery()
        cnn.Close()
        Return nRecords
    End Function


    Public Overrides Function purgeLocalData() As Boolean
        Try
            Dim cmd As New OleDb.OleDbCommand("delete from T_Block")
            executeCommand(cmd)
            cmd = New OleDb.OleDbCommand("delete from T_PrivateBlock")
            executeCommand(cmd)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function


    Public Overrides Function saveBlock(b As Block) As Boolean
        Try
            Dim cmd As New OleDb.OleDbCommand("select * from T_Block where serial=?")
            cmd.Parameters.Add("?", OleDbType.BigInt).Value = b.serial
            Dim dt As DataTable = LoadData(cmd)
            If dt.Rows.Count = 0 Then
                cmd = New OleDb.OleDbCommand("insert into T_Block (serial, blockData) values (?,?);")
                cmd.Parameters.Add("?", OleDbType.BigInt).Value = b.serial
                cmd.Parameters.Add("?", OleDbType.LongVarBinary).Value = Zipping.ComprimiTesto(b.xml.ToString)
                executeCommand(cmd)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Public Overrides Function blockExists(serial As Long) As Boolean
        Try
            Dim cmd As New OleDb.OleDbCommand("select * from T_Block where serial=?")
            cmd.Parameters.Add("?", OleDbType.BigInt).Value = serial
            Dim dt As DataTable = LoadData(cmd)
            Return dt.Rows.Count > 0
        Catch ex As Exception
            Return False
        End Try
    End Function


    Public Overrides Function getBlock(serial As Long) As Block
        Try
            Dim cmd As New OleDb.OleDbCommand("select * from T_Block where serial=?")
            cmd.Parameters.Add("?", OleDbType.BigInt).Value = serial
            Dim dt As DataTable = LoadData(cmd)
            If dt.Rows.Count > 0 Then
                Dim s As String = Zipping.EspandiTesto(dt.Rows(0)("blockData"))
                Return New Block(s)
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Overrides Function savePrivateBlock(b As Block, userId As String) As Boolean
        Try
            Dim blockHash As String = utility.bin2hex(b.computeHash)
            Dim cmd As New OleDb.OleDbCommand("select * from T_PrivateBlock where blockHash=? and userId=?")
            cmd.Parameters.Add("?", OleDbType.VarChar).Value = blockHash
            cmd.Parameters.Add("?", OleDbType.VarChar).Value = userId
            Dim dt As DataTable = LoadData(cmd)

            If dt.Rows.Count = 0 Then
                cmd = New OleDb.OleDbCommand("insert into T_PrivateBlock (blockHash, userId, blockData) values (?,?,?);")
                cmd.Parameters.Add("?", OleDbType.VarChar).Value = blockHash
                cmd.Parameters.Add("?", OleDbType.VarChar).Value = userId
                cmd.Parameters.Add("?", OleDbType.LongVarBinary).Value = Zipping.ComprimiTesto(b.xml.ToString)
                executeCommand(cmd)
            End If
            Return True
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Overrides Function getPrivateBlock(blockHash As String, userId As String) As Block
        Try
            Dim cmd As New OleDb.OleDbCommand("select * from T_PrivateBlock where blockHash=? and userId=?")
            cmd.Parameters.Add("?", OleDbType.VarChar).Value = blockHash
            cmd.Parameters.Add("?", OleDbType.VarChar).Value = userId
            Dim dt As DataTable = LoadData(cmd)
            If dt.Rows.Count > 0 Then
                Dim s As String = Zipping.EspandiTesto(dt.Rows(0)("blockData"))
                Return New Block(s)
            Else
                Return Nothing
            End If
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

End Class
