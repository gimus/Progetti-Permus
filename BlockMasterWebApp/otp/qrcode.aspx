<%@ Page Language="vb" AutoEventWireup="false" CodeBehind="qrcode.aspx.vb" Inherits="BlockMasterWebApp.qrcode1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<!-- vim: set nowrap ts=2 sw=2: -->

<html xmlns="http://www.w3.org/1999/xhtml">
  <head>
    <meta http-equiv="X-UA-Compatible" content="IE=10" />
    <title>FreeOTP - QR Code Generator</title>
	<link rel="stylesheet" type="text/css" href="common.css" media="screen" />
	<link rel="stylesheet" type="text/css" href="qrcode.css" media="screen" />
    <script type="text/javascript" src="lib/base32.js"></script>
    <script type="text/javascript" src="lib/qrcode.js"></script>
	<script type="text/javascript" src="qrcode.js"></script>
    <meta name="viewport" content="width=device-width,initial-scale=1.0" />
	<meta charset="UTF-8" />
  </head>

  <body>
		<header>
            <br />
          <h1>FreeOTP</h1>
			Two-Factor Authentication
		</header>


		<main>
      <a style="text-decoration:none" id="urilink"></a>
            <br />
            <div id="cont" >
        <div  id="qrcode">
          <img id="preview" src="img/freeotp.svg" onError="onImageError()" />
        </div>
            </div>
            <br />
            <br />
            <br />
            <br />
            <hr />
                    <div> scannerizzare il codice con l'applicazione FreeOTP e poi premere il pulsante salva.
                </div>


      <div style="visibility:hidden" id="ccode">???</div>

		  <table style="visibility:hidden">
			  <tr>
				  <td colspan="3">
					  <input
					    id="issuer"
						  type="text"
						  name="issuer"
						  placeholder="Issuer (optional)"
						  onInput="onValueChanged()"
                          value="<%=issuer %>"   
						  />
				  </td>
				  <td class="shrink left">
					  <select id="algorithm" name="algorithm"
					    onChange="onValueChanged()"
					    onBlur="onValueChanged()">
						  <option value="SHA1">SHA1</option>
						  <option value="SHA224">SHA224</option>
						  <option value="SHA256" selected="selected">SHA256</option>
						  <option value="SHA384">SHA384</option>
						  <option value="SHA512">SHA512</option>
					  </select>
				  </td>
			  </tr>
			  <tr>
				  <td colspan="3">
					  <input
					    id="account"
						  type="text"
						  name="account"
						  placeholder="Account"
						  onInput="onValueChanged()"
                             value="<%=account%>"   
                          />
				  </td>
				  <td class="shrink left">
					  <label>
						  <input type="radio" name="type" value="hotp" onChange="onValueChanged()" > Counter</input>
					  </label>
				  </td>
			  </tr>
			  <tr>
				  <td colspan="3">
					  <input id="image" type="text" name="image" placeholder="Image URL (optional)" onInput="onValueChanged()" />
				  </td>
				  <td class="shrink left">
					  <label>
						  <input type="radio" name="type" value="totp" checked="checked" onChange="onValueChanged()"> Timeout</input>
					  </label>
				  </td>
			  </tr>
			  <tr>
				  <td colspan="3">
					  <input id="secret" type="text" name="secret" value="<%=secret %>"
					    placeholder="Secret (Base32)" onInput="onValueChanged()" />
				  </td>
				  <td>
<%--					  <button id="random" onClick="onRandomClicked()">Random</button>--%>
				  </td>
			  </tr>
			  <tr>
				  <td class="shrink left">
					  <label for="digits">Digits</label>
				  </td>
				  <td>
					  <input
						  onInput="update(this, 'digits-display')"
						  name="digits"
						  type="range"
						  id="digits"
						  value="6"
						  step="1"
						  max="12"
						  min="6"
						  />
				  </td>
				  <td id="digits-display" class="shrink left">
					  6
				  </td>
			    <td>
					  <select id="period" name="period"
					    onChange="onValueChanged()"
					    onBlur="onValueChanged()">
						  <option value="15">15s</option>
						  <option value="30" selected="selected">30s</option>
						  <option value="60">1m</option>
						  <option value="120">2m</option>
						  <option value="300">5m</option>
					    <option value="600">10m</option>
					  </select>
			    </td>
			  </tr>
		  </table>
		</main>


		<script type="text/javascript">
            var e = document.getElementById("qrcode");

            var qrcode = new QRCode(e, {
                correctLevel: QRCode.CorrectLevel.H,
                text: window.location.href,
                colorLight: "#ffffff",
                colorDark: "#000000"

            });

            function update(from, to) {
                document.getElementById(to).innerHTML = from.value;
                onValueChanged();
            }

            onValueChanged();
		</script>
  </body>
</html>
