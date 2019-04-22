Imports Permus
Imports Microsoft.Win32

Public Class OTPControl
    Protected WithEvents WB As WebBrowser
    Protected nocodeUri As String
    Dim otpSec As String = ""
    Public Sub New()
        InitializeComponent()
        WB = New WebBrowser
        Container.Children.Add(WB)
        onWbInitialized()
    End Sub

    Private Sub BtnNuovo_Click(sender As Object, e As RoutedEventArgs) Handles btnNuovo.Click
        otpSec = OtpUtility.generateSecretKey().ToLower
        Dim qrcodeUri = String.Format("{0}otp/qrcode.aspx?account={1}&secret={2}", Application.C.api.baseAddress, Application.C.currentUser.id, otpSec)
        NavTo(qrcodeUri)
    End Sub

    Private Sub NavTo(url As String)
        Try
            WB.Navigate(url)
        Catch ex As Exception

        End Try
    End Sub

    Private Sub onWbInitialized()
        NavTo(nocodeUri)
    End Sub

    Private Sub BtnSalva_Click(sender As Object, e As RoutedEventArgs) Handles btnSalva.Click
        If Application.C.ModifyCurrentUserProfile("otpSecretKey", otpSec) Then
            NavTo(nocodeUri)
            Dialogs.notify("Impostazione del codice OTP completata!")
        End If
    End Sub



    Private Sub OTPControl_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        nocodeUri = String.Format("{0}otp/index.html", Application.C.api.baseAddress)
        NavTo(nocodeUri)
    End Sub

    Private Sub WB_Initialized(sender As Object, e As EventArgs) Handles WB.Initialized
        NavTo(nocodeUri)
    End Sub

    Private Sub BtnTestPfx_Click(sender As Object, e As RoutedEventArgs) Handles btnTestPfx.Click
        If Application.C.IsPfxTestOk() Then
            Dialogs.notify("test firma remota superato!")
        Else
            Dialogs.warning("firma remota non correttamente impostata!")
        End If
    End Sub

    Private Sub BtnSetPin_Click(sender As Object, e As RoutedEventArgs) Handles btnSetPin.Click
        Dim pin As String = InputBox("pin di accesso al file .pfx contenente il certificato di firma")
        If pin <> "" Then
            If Application.C.ModifyCurrentUserProfile("pinPfx", pin) Then
                Dialogs.notify("impostazione del pin di accesso al certificato di firma in formato .pfx avvenuto con successo!")
            End If
        End If

    End Sub

    Private Sub BtnUploadPfx_Click(sender As Object, e As RoutedEventArgs) Handles btnUploadPfx.Click
        Dim ofd As New OpenFileDialog
        ofd.CheckFileExists = True
        ofd.Multiselect = False
        ofd.Filter = "files *.pfx|*.pfx"
        If ofd.ShowDialog() Then
            Dim data() As Byte = IO.File.ReadAllBytes(ofd.FileName)
            Dim s As String = Convert.ToBase64String(data)
            If Application.C.ModifyCurrentUserProfile("pfx", s) Then
                Dialogs.notify("L'upload del certificato di firma in formato .pfx avvenuto con successo!")
            End If

        End If
    End Sub

    Private Sub BtnTestOtp_Click(sender As Object, e As RoutedEventArgs) Handles btnTestOtp.Click

        Dim otp As String = InputBox("attuale OTP?")
        If otp <> "" Then
            If Application.C.IsOtpTestOk(otp) Then
                Dialogs.notify("OTP Verificato OK!")
            Else
                Dialogs.warning("OTP non verificato!")
            End If
        End If
    End Sub

    Private Sub BtnSetRfId_Click(sender As Object, e As RoutedEventArgs) Handles btnSetRfId.Click
        Dim rfid As String = InputBox("Numero seriale del dispositivo RFID (in esadecimale)")
        If rfid <> "" Then
            Try
                Dim sl As Long = Convert.ToInt64(rfid.ToUpper.Replace(":", ""), 16)

                If Application.C.ModifyCurrentUserProfile("rfidSerial", sl) Then
                    Dialogs.notify("impostazione numero seriale del dispositivo RFID avvenuto con successo!")
                End If

            Catch ex As Exception

            End Try

        End If

    End Sub
End Class
