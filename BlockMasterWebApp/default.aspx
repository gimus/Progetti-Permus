<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="default.aspx.vb" Inherits="BlockMasterWebApp._default" %>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
<title>BlockMaster Welcome Page</title>
    <link href="css/bootstrap.css" rel="stylesheet" />
    <link href="css/Site.css" rel="stylesheet" />
    <meta http-equiv="refresh" content="10" />
</head>
<body>
    <h1>Welcome to Permus BlockMaster</h1>
    <div>scarica l'App di Permus <a href="http://www.pardesca.it:4080/permus/app/PermusClient/PermusClient.application">qui</a></div>
    <div>connettiti al bot telegram <a href="https://web.telegram.org/#/im?p=@Permus_bot">qui</a></div>

    <hr />
    <div class="data" >
        <table>
            <tr><td >Blockchain status: </td><td class="data font-weight-bold"  ><%=bc_status%></td></tr>
            <tr><td >Blockchain version: </td><td class="data font-weight-bold"  ><%=si.blockChainVersion%></td></tr>
            <tr><td >Current Block serial: </td><td class="data font-weight-bold"  ><%=si.currentBlockSerial%></td></tr>
            <tr><td >Current transaction serial: </td><td class="data font-weight-bold"  ><%= si.currentTransactionSerial  %></td></tr>
            <tr><td >total transfer in memory: </td><td class="data font-weight-bold"  ><%=nTransferTransactions %></td></tr>
            <tr><td >pending transfers: </td><td class="data font-weight-bold"  ><%=nPendingTransferTransactions%></td></tr>
        </table>
     <hr />
     <h4>Enrolled subjects: </h4>
     <%=users%>
   </div> 
</body>
</html>
