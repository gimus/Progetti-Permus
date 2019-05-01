Public Class Relay
    Protected sensors As New Dictionary(Of String, Sensor)

    Protected Function getSensor(sensorId As String) As Sensor
        Dim s As Sensor
        If sensors.ContainsKey(sensorId) Then
            s = sensors(sensorId)
        Else
            s = New Sensor(sensorId, 10)
            sensors.Add(sensorId, s)
        End If
        Return s
    End Function

    Public Function pushReading(sensorId As String, value As String) As SensorReading
        Dim s As Sensor = getSensor(sensorId)
        Dim sr As SensorReading = New SensorReading(sensorId, value)
        s.pushReading(sr)
        Return sr
    End Function
    Public Function getReading(sensorId As String) As SensorReading
        Dim s As Sensor = getSensor(sensorId)
        Return s.getReading()
    End Function
    Public Function getAllReadings(sensorId As String) As List(Of SensorReading)
        Dim s As Sensor = getSensor(sensorId)
        Return s.getAllReadings()
    End Function
End Class

Public Class Sensor

    Public max As Integer = 10
    Public Property sensorId As String
    Protected readings As New Queue(Of SensorReading)

    Public Sub New(id As String, Optional maxElements As Integer = 10)
        sensorId = id
        max = maxElements
    End Sub

    Public Sub pushReading(sr As SensorReading)
        readings.Enqueue(sr)
        If readings.Count > max Then
            readings.Dequeue()
        End If
    End Sub
    Public Function getReading() As SensorReading
        If readings.Count > 0 Then
            Return readings.Dequeue()
        Else
            Return Nothing
        End If
    End Function
    Public Function getAllReadings() As List(Of SensorReading)
        Dim l As New List(Of SensorReading)
        For i = 1 To readings.Count
            l.Add(readings.Dequeue())
        Next
        Return l
    End Function
End Class

Public Class SensorReading
    Public Sub New(sensorId As String, value As String)
        Me.sensorId = sensorId
        Me.value = value
        Me.time = Now()
    End Sub

    Public Property sensorId As String
    Public Property time As DateTime
    Public Property value As String
End Class
