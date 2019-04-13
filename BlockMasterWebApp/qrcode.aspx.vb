Public Class qrcode
    Inherits System.Web.UI.Page
    Public code As String = ""

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        code = Request("code")
    End Sub

End Class