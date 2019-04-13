Public Class qrcode1
    Inherits System.Web.UI.Page
    Public account As String = "GMNGPP63P10D976K"
    Public issuer As String = "Permus Blockchain"
    Public secret As String = ""

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        account = Request("account")
        secret = Request("secret")
    End Sub

End Class