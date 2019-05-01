Imports System.Web.Http
Imports System.Web.Http.Cors
Imports Permus

Namespace Controllers

    Public Class RelayController
        Inherits ApiController

        <EnableCors("*", "*", "*")>
        <Route("api/relay_push")>
        <AcceptVerbs("POST", "GET", "PUT")>
        Public Function RelayPush(<FromUri> sensorId As String, sensorValue As String) As SensorReading
            Try
                Return App.R.pushReading(sensorId, sensorValue)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/relay_get")>
        <AcceptVerbs("POST", "GET", "PUT")>
        Public Function RelayGet(<FromUri> sensorId As String) As SensorReading
            Try
                Return App.R.getReading(sensorId)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        <EnableCors("*", "*", "*")>
        <Route("api/relay_getall")>
        <AcceptVerbs("POST", "GET", "PUT")>
        Public Function RelayGetAll(<FromUri> sensorId As String) As List(Of SensorReading)
            Try
                Return App.R.getAllReadings(sensorId)
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

    End Class

End Namespace

