Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Newtonsoft
Imports Newtonsoft.Json

Public Class BlockMasterWebApiClient
    Inherits WebApiClient

    Public Sub New(Optional ApibaseAddress As Uri = Nothing)
        MyBase.New(ApibaseAddress)
    End Sub

    Protected Overrides Function getProperResponseObject(response As HttpResponseMessage, expectedObjectType As String) As Object
        Try
            Select Case expectedObjectType
                Case "Transaction"
                    Return Transaction.createFromXml(response.Content.ReadAsAsync(Of XElement).Result)
                Case "TransferTransactionPackage"
                    Dim ttp As New TransferTransactionPackage(response.Content.ReadAsAsync(Of XElement).Result)
                    ttp.ensureBlockDecrypted()
                    Return ttp

                Case "SubjectProfile"
                    Return response.Content.ReadAsAsync(Of SubjectProfile).Result
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
    Public Async Function SubjectProfile(SignedCommand As Byte(), Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of SubjectProfile)
        Dim requestURI As String = String.Format("api/subject_profile?command={0}", utility.ConvertToUrlSafeBase64String(SignedCommand))
        Return Await processRequest(requestURI, receiver, "SubjectProfile")
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