Imports Permus
Imports ServiziDB

Public Class Factory

    Public Shared Function GetDataRow(ds As DataSet, Optional dtIndex As Integer = 0, Optional drIndex As Integer = 0) As DataRow
        Dim dr As DataRow = Nothing
        If ds IsNot Nothing Then
            If ds.Tables.Count > dtIndex Then
                Dim dt As DataTable = ds.Tables(dtIndex)
                If dt.Rows.Count >= drIndex Then
                    dr = dt.Rows(dtIndex)
                End If
            End If
        End If
        Return dr
    End Function

    Public Shared Function leggiBlock(ds As DataSet, Optional dtIndex As Integer = 0, Optional drIndex As Integer = 0, Optional o As Block = Nothing) As Block
        Dim dr As DataRow = GetDataRow(ds, dtIndex, drIndex)
        Return leggiBlock(dr, o)
    End Function

    Public Shared Function leggiBlock(dr As DataRow, Optional o As Block = Nothing) As Block
        If dr IsNot Nothing Then
            If o Is Nothing Then o = New Block
            o.parse(SupportoDB.LeggiString(dr("blockText")))
        End If
        Return o
    End Function

    Public Shared Function leggiPrivateBlock(ds As DataSet, Optional dtIndex As Integer = 0, Optional drIndex As Integer = 0, Optional o As EnvelopedBlock = Nothing) As EnvelopedBlock
        Try
            Dim dr As DataRow = GetDataRow(ds, dtIndex, drIndex)
            Return leggiPrivateBlock(dr, o)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function leggiPrivateBlock(dr As DataRow, Optional o As EnvelopedBlock = Nothing) As EnvelopedBlock
        If dr IsNot Nothing Then
            If o Is Nothing Then o = New EnvelopedBlock
            o.blockHash = SupportoDB.LeggiString(dr("blockHash"))
            o.envelopedBlockData = Convert.ToBase64String(dr("blockData"))
        End If
        Return o
    End Function


    Public Shared Function leggiSystemInfo(ds As DataSet, Optional o As SystemInfo = Nothing) As SystemInfo
        If ds IsNot Nothing AndAlso ds.Tables.Count > 0 Then
            Dim dt As DataTable = ds.Tables(0)
            Dim dic As New Dictionary(Of String, String)
            For Each dr In dt.Rows
                dic.Add(dr("name"), dr("value"))
            Next

            If o Is Nothing Then o = New SystemInfo
            With o
                .blockChainVersion = dic("blockChainVersion")
                .currentBlockSerial = CLng(dic("currentBlockSerial"))
                .maxTransactionsPerBlock = CInt(dic("maxTransactionsPerBlock"))
                .certificationAuthorities = dic("certificationAuthorities")
            End With
        End If
        Return o
    End Function

    Public Shared Function leggiProtectedSubjectProfile(dr As DataRow, Optional o As ProtectedSubjectProfile = Nothing) As ProtectedSubjectProfile
        If dr IsNot Nothing Then
            If o Is Nothing Then o = New ProtectedSubjectProfile
            o.subjectId = SupportoDB.LeggiString(dr("subjectId"))
            o.name = SupportoDB.LeggiString(dr("name"))
            o.email = SupportoDB.LeggiString(dr("email"))
            o.setPfxPin(leggiEDecripta(dr, "pfxPin"))
            o.telegramId = SupportoDB.LeggiLong(dr("telegramId"))
            o.setotpSecretKey(leggiEDecripta(dr, "otpSecretKey"))

            Try
                Dim s As String = SupportoDB.LeggiString(dr("pfx"))
                If s.Length > 100 Then
                    o.pfx = Convert.FromBase64String(s)
                End If
            Catch ex As Exception

            End Try

        End If
        Return o
    End Function

    Protected Shared Function leggiEDecripta(dr As DataRow, columnName As String) As String
        Try
            Dim enc_s As String = SupportoDB.LeggiString(dr(columnName))
            Return Permus.AES128.DecryptText(Convert.FromBase64String(enc_s), My.Settings.local_enc_key & "GPKEY")
        Catch ex As Exception
            Return ""
        End Try
    End Function


    Public Shared Function leggiPublicSubjectProfile(dr As DataRow, Optional o As PublicSubjectProfile = Nothing) As PublicSubjectProfile
        If dr IsNot Nothing Then
            If o Is Nothing Then o = New PublicSubjectProfile
            o.subjectId = SupportoDB.LeggiString(dr("subjectId"))
            o.name = SupportoDB.LeggiString(dr("name"))
            o.email = SupportoDB.LeggiString(dr("email"))
            Dim s As String = SupportoDB.LeggiString(dr("pfx"))
            o.pfx = s.Length > 100
            o.telegram = SupportoDB.LeggiLong(dr("telegramId")) <> 0
            o.otp = SupportoDB.LeggiString(dr("otpSecretKey")) <> ""
        End If
        Return o
    End Function



End Class
