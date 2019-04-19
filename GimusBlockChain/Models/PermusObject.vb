Public Class PermusObject
    Public Overridable Function xml() As XElement
        Return Nothing
    End Function
    Public Overridable Sub fromXml(e As XElement)
    End Sub

    Public Overridable Function html(Optional type As String = "") As String
        Return ""
    End Function

    Public Overridable Function plainText(Optional type As String = "") As String
        Return ""
    End Function

End Class
