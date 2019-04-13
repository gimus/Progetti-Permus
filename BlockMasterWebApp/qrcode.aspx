<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="qrcode.aspx.vb" Inherits="BlockMasterWebApp.qrcode" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>qrcode</title>
    <script src="Scripts/jquery-3.3.1.min.js"></script>
    <script src="Scripts/qrcode.min.js"></script>
</head>
<body onload="update_cr_code(300);">
<div style="padding:100px; text-align:center">
    <div id="qrcode" style="display: inline-block; background-color:black;width: 300px;height: 300px;"></div>
    <div id="qrc" style="font-weight:500;  font-family:Verdana; font-size:18px; text-align:center"><%=code%></div>
</div>

</body>
</html>
