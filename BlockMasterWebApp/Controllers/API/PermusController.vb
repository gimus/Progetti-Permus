Imports System.Net
Imports System.Net.Http
Imports System.Web.Http
Imports System.Web.Http.Cors
Imports Newtonsoft.Json
Imports Permus

Namespace Controllers
    Public Class PermusController
        Inherits ApiController

        <EnableCors("*", "*", "*")>
        <Route("api/block")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetBlock(<FromUri> serial As String) As XElement
            Try
                Dim b As Block = BlockMasterBlockChain.M.da.getBlock(CLng(serial))
                Return b.xml
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/private_block")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetPrivateBlock(<FromUri> hash As String) As XElement
            Try
                Dim b As EnvelopedBlock = BlockMasterBlockChain.M.da.getPrivateBlock(hash)
                If b IsNot Nothing Then
                    Return b.xml
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/timestamp")>
        Public Function GetTimestamp() As Long
            Try
                Return utility.getCurrentTimeStamp()
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/check_in")>
        <AcceptVerbs("POST", "GET")>
        Public Function getCheckIn(signedData As String) As SystemInfo
            Try
                Dim sd As New SignedData(utility.ConvertFromUrlSafeBase64String(signedData))
                If isSigningTimeOk(sd) Then
                    Dim requester As Subject = BlockMasterBlockChain.M.subjects.getElementByX509Certificate(sd.certificate)
                    If requester IsNot Nothing Then
                        requester.lastPing = Now()
                        requester.token = Guid.NewGuid().ToString
                        Dim si As SystemInfo = GetSystemInfo(requester.id, requester.token, "")
                        si.requesterInfo.token = requester.token
                        Return si
                    Else
                        Return Nothing
                    End If
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/info")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetSystemInfo(Optional userId As String = "", Optional token As String = "", Optional extendedInfo As String = "") As SystemInfo
            Try
                Dim si As SystemInfo = BlockMasterBlockChain.M.systemInfo(userId, token)
                If (extendedInfo <> "" And si.requesterInfo IsNot Nothing) AndAlso si.requesterInfo.userId <> "" Then
                    Dim ei() As String = extendedInfo.Split("|")
                    Select Case ei(0).ToLower
                        Case "getuserinfo", "userinfo", "getotheruserinfo"
                            si.otherUserInfo = BlockMasterBlockChain.M.getUserInfo(ei(1))
                    End Select
                End If
                si.currentTimeStamp = utility.getCurrentTimeStamp()
                Return si
            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/enroll")>
        <AcceptVerbs("POST", "GET")>
        Public Function GetEnroll(<FromUri> command As String) As XElement
            Try
                Dim sd As New SignedData(utility.ConvertFromUrlSafeBase64String(command))

                If isSigningTimeOk(sd) Then
                    Return BlockMasterBlockChain.M.enrollCertificate(sd.certificate).xml
                Else
                    Throw New Exception("error: " & "invalid request")
                End If

            Catch ex As Exception
                Throw New Exception("error: " & ex.Message)
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/transfer_transaction")>
        <AcceptVerbs("POST", "GET")>
        Public Function PostTransferTransaction(<FromUri> commandType As String, <FromUri> command As String) As XElement
            Try
                Dim sd As New SignedData(utility.ConvertFromUrlSafeBase64String(command))
                If isSigningTimeOk(sd) Then
                    Dim requester As Subject = Subject.fromX509Certificate(sd.certificate)
                    Dim xttp As String = Request.Content.ReadAsStringAsync().Result
                    Dim ttp As TransferTransactionPackage = Nothing
                    Dim xcmd As XElement

                    Try
                        xcmd = XElement.Parse(sd.signedContentText)
                    Catch ex As Exception
                        xcmd = New XElement("command")
                    End Try

                    If xttp IsNot Nothing Then
                        Select Case commandType
                            Case "TransferTransactionInit", "TransferTransactionPropose", "TransferTransactionAccept"
                                ttp = New TransferTransactionPackage(xttp)
                        End Select
                    End If

                    Dim rttp As TransferTransactionPackage = Nothing

                    Select Case commandType
                        Case "PendingTransfers"
                            Dim pt As PendingTransfers = BlockMasterBlockChain.M.UserPendingTransfers(requester.id)
                            Return pt.xml
                        Case "TransferTransactionCancel"
                            Dim pt As PendingTransfers = BlockMasterBlockChain.M.cancelPendingTransfer(requester, xcmd.Value)
                            Return pt.xml
                        Case "TransferTransaction"
                            rttp = BlockMasterBlockChain.M.getPendingTransfer(requester, xcmd.Value)

                        Case "TransferTransactionInfo"
                            Dim tti As New TransferTransactionInfo
                            tti.subject = requester.id
                            If rttp IsNot Nothing Then
                                tti.id = rttp.transaction.transferId
                                tti.state = rttp.transaction.transferTransactionState
                                If rttp.transaction.fromSubject = requester.id Then
                                    tti.type = "From"
                                Else
                                    tti.type = "To"
                                End If
                            End If

                            Return tti.xml

                        Case Else
                            rttp = BlockMasterBlockChain.M.manageTransferTransaction(commandType, requester, ttp, xcmd)
                    End Select
                    Return rttp.xml
                Else
                    Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest) With {.ReasonPhrase = "request not in sync!"})
                End If
            Catch ex As Exception
                Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest) With {.ReasonPhrase = ex.Message})
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/subject_profile")>
        <AcceptVerbs("POST", "GET")>
        Public Function PostSubjectProfile(<FromUri> command As String) As PublicSubjectProfile
            Try
                Dim sd As New SignedData(utility.ConvertFromUrlSafeBase64String(command))
                Dim requester As Subject = Subject.fromX509Certificate(sd.certificate)
                Dim subjRequester As Subject = BlockMasterBlockChain.M.subjects.getElementById(requester.id)

                If isSigningTimeOk(sd) And subjRequester IsNot Nothing Then
                    Dim xttp As String = Request.Content.ReadAsStringAsync().Result
                    Dim ttp As TransferTransactionPackage = Nothing
                    Dim xcmd As XElement

                    Try
                        xcmd = XElement.Parse(sd.signedContentText)
                    Catch ex As Exception
                        xcmd = New XElement("command")
                    End Try

                    Dim funzione As String = xcmd.Element("funzione").Value
                    Select Case funzione
                        Case "otp_test"
                            Try
                                If subjRequester.profile.asProtected.isOtpVerified(xcmd.Element("otp").Value) Then
                                    Dim dt As DataTable = BlockMasterBlockChain.M.da.getSubjectProfiles("subjectId", subjRequester.id)
                                    Return Factory.leggiPublicSubjectProfile(dt.Rows(0))
                                Else
                                    Return Nothing
                                End If
                            Catch ex As Exception
                                Return Nothing
                            End Try

                        Case "pfx_test"
                            Try
                                Dim d() As Byte = subjRequester.profile.asProtected.computeDigitalSignatureAndGenerateP7M(ASCIIEncoding.UTF8.GetBytes("TEST"), utility.getCurrentTimeStamp)
                                If d.Length > 0 Then
                                    Dim dt As DataTable = BlockMasterBlockChain.M.da.getSubjectProfiles("subjectId", subjRequester.id)
                                    Return Factory.leggiPublicSubjectProfile(dt.Rows(0))
                                Else
                                    Return Nothing
                                End If
                            Catch ex As Exception
                                Return Nothing
                            End Try

                        Case Else
                            Dim dt As DataTable = BlockMasterBlockChain.M.da.GestioneProfilo(requester.id, xcmd.Element("funzione").Value, xcmd.Element("P1").Value, xcmd.Element("P2").Value, xcmd.Element("P3").Value, xcmd.Element("P4").Value)
                            If dt IsNot Nothing Then
                                subjRequester.profile = Factory.leggiProtectedSubjectProfile(dt.Rows(0))
                                Return Factory.leggiPublicSubjectProfile(dt.Rows(0))
                            Else
                                Return Nothing
                            End If
                    End Select


                Else
                    Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest) With {.ReasonPhrase = "request not in sync!"})
                End If
            Catch ex As Exception
                Throw New HttpResponseException(New HttpResponseMessage(HttpStatusCode.BadRequest) With {.ReasonPhrase = ex.Message})
            End Try
        End Function


        Protected Shared Function isSigningTimeOk(request As String) As Boolean
            Dim sd As New SignedData(utility.ConvertFromUrlSafeBase64String(request))
            Return isSigningTimeOk(sd)
        End Function

        Protected Shared Function isSigningTimeOk(sd As SignedData) As Boolean
            ' we do not accept an old requests
            Return Math.Abs(sd.unixSignedSigningTime - utility.getCurrentTimeStamp()) < 1000
        End Function

    End Class

End Namespace