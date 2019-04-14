Imports BlockMasterWebApp.Controllers
Imports Permus
Public Class _default
    Inherits System.Web.UI.Page
    Public Property bc_status As String = "unknown"
    Public Property si As New SystemInfo
    Public Property users As String
    Dim M As BlockMasterBlockChain
    Public nPendingTransferTransactions As Integer
    Public nTransferTransactions As Integer

    Public Sub New()
        M = BlockMasterBlockChain.M
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim pc As New PermusController
        si = pc.GetSystemInfo()
        bc_status = "ONLINE"

        nPendingTransferTransactions = M.pendingTransferTransactions.Values.Where(Function(x) (x.transaction.isAwaitingApproval)).Count
        nTransferTransactions = M.pendingTransferTransactions.Count

        users = getUsersTable()
    End Sub

    Protected Function getUsersTable() As String
        Dim tu As New StringBuilder(1000)
        tu.AppendLine("<table cellpadding='2'>")
        tu.AppendFormat("<tr style='background-color:#EFEFEF'><th></th><th>Subject Name</th><th>coin balance</th></tr>")
        For Each s As Subject In M.subjects.Values
            tu.AppendFormat("<tr style='border-bottom: solid 1px #DFDFDF' ><td>{0}</td><td style=' font-weight: bold;'>{1}</td><td style='text-align: right;'  >{2}</td><td><span class='blink'>{3}</span></td></tr>", "", s.name, s.coinBalance.balance.ToString("0.00"), IIf(s.isOnline(), " on line", ""))
            tu.AppendLine()
        Next
        tu.AppendLine("</table>")
        Return tu.ToString
    End Function


End Class