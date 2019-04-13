Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Newtonsoft
Imports Newtonsoft.Json

Public Class BlockMasterWebApiClient
    Private Client As HttpClient
    Protected _LastResponseOk As Boolean
    Protected _LastResponseMessage As String
    Protected _baseAddress As Uri
    Protected lastRequestTime As Long
    Protected _lastPing As Long

    Public Sub New(Optional ApibaseAddress As Uri = Nothing)
        _baseAddress = ApibaseAddress

        Dim handler As New HttpClientHandler()
        handler.UseDefaultCredentials = True
        Client = New HttpClient(handler)
        If ApibaseAddress IsNot Nothing Then
            baseAddress = ApibaseAddress
        End If
        Client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/xml"))
        Client.Timeout = New TimeSpan(0, 0, 4)
    End Sub

    Public ReadOnly Property lastPingTime As Long
        Get
            Return _lastPing
        End Get
    End Property

    Public ReadOnly Property LastResponseOK As Boolean
        Get
            Return _LastResponseOk
        End Get
    End Property

    Public ReadOnly Property LastResponseMessage As String
        Get
            Return _LastResponseMessage
        End Get
    End Property

    Public Property baseAddress() As Uri
        Get
            Return Client.BaseAddress
        End Get
        Set(value As Uri)
            Client.BaseAddress = value
        End Set
    End Property

    Protected Function getProperResponseObject(response As HttpResponseMessage, expectedObjectType As String) As Object
        Try
            Select Case expectedObjectType
                Case "Transaction"
                    Return Transaction.createFromXml(response.Content.ReadAsAsync(Of XElement).Result)
                Case "TransferTransactionPackage"
                    Dim ttp As New TransferTransactionPackage(response.Content.ReadAsAsync(Of XElement).Result)
                    ttp.ensureBlockDecrypted()
                    Return ttp

                Case "PublicSubjectProfile"
                    Return response.Content.ReadAsAsync(Of PublicSubjectProfile).Result
                Case "PendingTransfers"
                    Return New PendingTransfers(response.Content.ReadAsAsync(Of XElement).Result)
                Case "TransferTransactionInfo"
                    Return New TransferTransactionInfo(response.Content.ReadAsAsync(Of XElement).Result)
                Case "Block"
                    Dim e As XElement = response.Content.ReadAsAsync(Of XElement).Result
                    Return New Block(ManInTheMiddleOrBadBlockMasterSimulator.processBlockData(e))
                Case "EnvelopedBlock"
                    Return New EnvelopedBlock(response.Content.ReadAsAsync(Of XElement).Result)
                Case "SystemInfo"
                    Return response.Content.ReadAsAsync(Of SystemInfo).Result
                Case "Long"
                    Return response.Content.ReadAsAsync(Of Long).Result
                Case Else
                    Throw New Exception("invalid expectedObjectType")
            End Select
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Async Function getSystemTimestamp(Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of Long)
        Dim requestURI As String = String.Format("api/timestamp")
        Return Await processRequest(requestURI, receiver, "Long")
    End Function

    Public Async Function getCheckIn(SignedRequestData As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of SystemInfo)
        Dim requestURI As String = String.Format("api/check_in?signedData={0}", utility.ConvertToUrlSafeBase64String(SignedRequestData))
        Return Await processRequest(requestURI, receiver, "SystemInfo")
    End Function

    Public Async Function getBlockChainInfo(Optional userId As String = "", Optional token As String = "", Optional extendedInfo As String = "", Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of SystemInfo)
        Dim requestURI As String = String.Format("api/info?userId={0}&token={1}&extendedInfo={2}", userId, token, extendedInfo)
        Return Await processRequest(requestURI, receiver, "SystemInfo")
    End Function

    Public Async Function getPrivateBlock(blockHash As String, Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of EnvelopedBlock)
        Dim requestURI As String = String.Format("api/private_block?hash={0}", blockHash)
        Return Await processRequest(requestURI, receiver, "EnvelopedBlock")
    End Function

    Public Async Function getBlock(blockSerial As Long, Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of Block)
        Dim requestURI As String = String.Format("api/block?serial={0}", blockSerial.ToString)
        Return Await processRequest(requestURI, receiver, "Block")
    End Function

    Public Async Function enrollCertificate(SignedRequestData As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of Block)
        Dim requestURI As String = String.Format("api/enroll?command={0}", utility.ConvertToUrlSafeBase64String(SignedRequestData))
        Return Await processRequest(requestURI, receiver, "Block")
    End Function
    Public Async Function SubjectProfile(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of PublicSubjectProfile)
        Dim requestURI As String = String.Format("api/subject_profile?command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "PublicSubjectProfile")
    End Function

    Public Async Function transferTransactionInit(ttp As TransferTransactionPackage, SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of TransferTransactionPackage)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransactionInit&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "TransferTransactionPackage", ttp)
    End Function

    Public Async Function transferTransactionCancel(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of PendingTransfers)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransactionCancel&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "PendingTransfers",, True)
    End Function

    Public Async Function pendigTransfers(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of PendingTransfers)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=PendingTransfers&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "PendingTransfers",, True)
    End Function

    Public Async Function transferTransactionInfo(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of TransferTransactionInfo)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransactionInfo&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "TransferTransactionInfo",, True)
    End Function

    Public Async Function transferTransaction(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of TransferTransactionPackage)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransaction&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "TransferTransactionPackage",, True)
    End Function

    Public Async Function transferTransactionPropose(ttp As TransferTransactionPackage, SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of TransferTransactionPackage)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransactionPropose&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "TransferTransactionPackage", ttp)
    End Function

    Public Async Function transferTransactionAccept(ttp As TransferTransactionPackage, SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of TransferTransactionPackage)
        Dim requestURI As String = String.Format("api/transfer_transaction?commandType=TransferTransactionAccept&command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "TransferTransactionPackage", ttp)
    End Function

    Public Async Function processRequest(requestUri As String, Optional receiver As iWebApiAsyncReceiver = Nothing, Optional objectType As String = "String", Optional permusObject As PermusObject = Nothing, Optional forcePostMethod As Boolean = False) As Task(Of Object)
        Dim method As HttpMethod = HttpMethod.Get
        Dim content As StringContent = Nothing
        Dim response As HttpResponseMessage

        If permusObject IsNot Nothing Or forcePostMethod Then
            Client.DefaultRequestHeaders.Accept.Clear()
            Client.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue("application/xml"))
            If permusObject IsNot Nothing Then
                content = New StringContent(permusObject.xml.ToString, Encoding.UTF8, "application/xml")
            End If
            method = HttpMethod.Post
        End If

        If receiver Is Nothing Then
            ' SYNC CALL

            Try
                _LastResponseMessage = "OK"
                _LastResponseOk = True
                lastRequestTime = Environment.TickCount
                If method = HttpMethod.Post Then
                    Debug.Print(Now().ToString("HH:mm:ss") & " Posting: " & requestUri)
                    response = Client.PostAsync(requestUri, content).Result
                Else
                    Debug.Print(Now().ToString("HH:mm:ss") & " Getting: " & requestUri)
                    response = Client.GetAsync(requestUri).Result
                End If
                _lastPing = Environment.TickCount - lastRequestTime

                If Not response.IsSuccessStatusCode Then
                    _LastResponseMessage = "il metodo GetAsync è stato eseguito senza successo: " & response.ReasonPhrase
                    _LastResponseOk = False
                    Debug.Print(_LastResponseMessage)
                End If

                If Me._LastResponseOk Then
                    Debug.Print(Now().ToString("HH:mm:ss") & " Response OK!")

                    Return getProperResponseObject(response, objectType)
                Else
                    Throw New Exception(_LastResponseMessage)
                End If

            Catch ex As Exception
                _LastResponseMessage = "Si è verificato un errore chiamando il metodo GetAsync: " & vbCrLf & ex.Message
                _LastResponseOk = False
                Return Nothing
            End Try

        Else
            ' ASYNC CALL
            Try
                If method = HttpMethod.Post Then
                    response = Await Client.PostAsync(requestUri, content)
                Else
                    response = Await Client.GetAsync(requestUri)
                End If

                If response.IsSuccessStatusCode Then
                    receiver.dataReady(requestUri, getProperResponseObject(response, objectType))
                Else
                    receiver.exception(requestUri, New Exception("_Async: Il metodo Get/PostAsync è stato eseguito senza successo: " & response.ReasonPhrase))
                End If

            Catch ex As Exception
                receiver.exception(requestUri, New Exception("_Async: Si è verificato un errore chiamando il metodo Get/PostAsync: " & vbCrLf & ex.Message, ex))
            End Try

            Return Nothing
        End If

    End Function

End Class

Public Interface iWebApiAsyncReceiver
    Sub dataReady(requestURI As String, data As Object)
    Sub exception(requestURI As String, ex As Exception)
End Interface


Public Class ManInTheMiddleOrBadBlockMasterSimulator
    Public Shared Function processBlockData(e As XElement) As XElement
        Return e
    End Function

End Class