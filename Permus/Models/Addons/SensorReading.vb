
Public Class SensorReading
    Public Sub New()
    End Sub

    Public Sub New(sensorId As String, value As String)
        Me.sensorId = sensorId
        Me.value = value
        Me.time = Now()
    End Sub

    Public Property sensorId As String
    Public Property time As DateTime
    Public Property value As String
End Class
