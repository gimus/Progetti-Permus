Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports Newtonsoft
Imports Newtonsoft.Json

Public Class RelayWebApiClient
    Inherits WebApiClient

    Public Sub New(Optional ApibaseAddress As Uri = Nothing)
        MyBase.New(ApibaseAddress)
    End Sub

    Protected Overrides Function getProperResponseObject(response As HttpResponseMessage, expectedObjectType As String) As Object
        Try
            Select Case expectedObjectType
                Case "SensorReading"
                    Return response.Content.ReadAsAsync(Of SensorReading).Result
                Case Else
                    Throw New Exception("invalid expectedObjectType")
            End Select
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Async Function getSensorReading(Optional sensorId As String = "", Optional receiver As iWebApiAsyncReceiver = Nothing) As Task(Of SensorReading)
        Dim requestURI As String = String.Format("api/relay_get?sensorId={0}", sensorId)
        Return Await processRequest(requestURI, receiver, "SensorReading")
    End Function

End Class
