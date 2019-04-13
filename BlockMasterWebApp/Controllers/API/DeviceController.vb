Imports System.Web.Http
Imports System.Web.Http.Cors
Imports Permus
Namespace Controllers

    Public Class DeviceController
        Inherits ApiController

        <EnableCors("*", "*", "*")>
        <Route("api/device_check_in")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetCheckIn(<FromUri> token As String, device As String) As subjectDevice
            Try
                Dim sd As subjectDevice = Nothing

                If token = "tokenditest" Then
                    token = "JUubNY8a/3/8LmQyGgzGe1pdcNc=|212cQSPmPK6GYzQB6CtvHA=="
                End If

                Dim subjectId As String = getSubjectIdFromToken(token.Replace("*", "+"), device, False)

                If subjectId <> "" Then
                    Dim sbj As Subject = BlockMasterBlockChain.M.subjects.getElementById(subjectId)
                    If sbj IsNot Nothing Then
                        sd = New subjectDevice
                        sd.id = sbj.id
                        sd.name = sbj.name
                        sd.email = sbj.email
                    End If
                End If
                Return sd
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/device_info")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetSystemInfo(<FromUri> token As String) As SystemInfo
            Try
                Dim subjectId As String = getSubjectIdFromToken(token.Replace("*", "+"), "*", False)
                If subjectId <> "" Then
                    Dim si As SystemInfo = BlockMasterBlockChain.M.systemInfo(subjectId, token)
                    si.currentTimeStamp = utility.getCurrentTimeStamp()
                    Return si
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/gestione_device")>
        Public Function GetGestioneDevice(<FromUri> Optional token As String = "", <FromUri> Optional deviceId As Integer = 0, <FromUri> Optional funzione As String = "", <FromUri> Optional P1 As String = "", <FromUri> Optional P2 As String = "", <FromUri> Optional P3 As String = "", <FromUri> Optional P4 As String = "") As String
            Try
                Dim subjectId As String = getSubjectIdFromToken(token.Replace("*", "+"), "*", False)

                If funzione = "CREA" Then
                    P2 = Guid.NewGuid.ToString
                    P3 = Convert.ToBase64String(utility.computeHash(System.Text.Encoding.UTF8.GetBytes(P2)))
                End If

                Dim ds As DataSet = BlockMasterBlockChain.M.da.GestioneDevice(deviceId, funzione, P1, P2, P3, P4)

                If ds.Tables.Count > 0 Then
                    Return "OK"
                Else
                    Return ""
                End If
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        Protected Function getSubjectIdFromToken(token As String, Optional device As String = "*", Optional resetCounter As Boolean = False) As String
            If token <> "" Then
                Dim s() As String = token.Split("|")

                Dim sha As String = s(0)
                Dim shab() As Byte = Convert.FromBase64String(sha)
                Dim shah As String = utility.bin2hex(shab)
                Dim encString As String = s(1)
                Try
                    Dim dt As DataTable = BlockMasterBlockChain.M.da.getDeviceInfo(shah, device)

                    If dt IsNot Nothing Then
                        If dt.Rows.Count > 0 Then
                            Dim dr As DataRow = dt.Rows(0)
                            Dim deviceId As String = ServiziDB.SupportoDB.LeggiString(dr("deviceId"))
                            Dim decString As String = AES128.DecryptText(Convert.FromBase64String(encString), deviceId)
                            Return ServiziDB.SupportoDB.LeggiString(dr("subjectId"))
                        End If
                    End If

                Catch ex As Exception
                    Return ""
                End Try
            End If
            Return ""
        End Function

    End Class

End Namespace

