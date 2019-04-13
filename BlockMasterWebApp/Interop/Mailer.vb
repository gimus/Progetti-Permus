Imports System.Data.SqlClient
Imports System.Net.Mail
Imports System.Timers
Imports ServiziDB

Public Class Mailer
    Protected smtp_from As String
    Protected client As SmtpClient
    Public lastEx As Exception
    Dim WithEvents mailTimer As Timer
    Public db As SERVIZI_DB
    Dim sending As Boolean = False

    Public Sub New(connectionString As String)
        smtp_from = My.Settings.smtp_from
        client = New SmtpClient(My.Settings.smtp_host, 25)
        client.Credentials = New System.Net.NetworkCredential(My.Settings.smtp_username, My.Settings.smtp_password)
        client.EnableSsl = False
        db = New SERVIZI_DB
        db.ConnectionString = connectionString

        mailTimer = New Timer(10000)
        mailTimer.Start()

    End Sub

    Public Async Function sendMail(recipients As String, subject As String, body As String, Optional isBodyHtml As Boolean = True) As Threading.Tasks.Task(Of Boolean)
        If Trim(body) = "" Then
            Return True
        End If

        Dim msg As New MailMessage
        lastEx = Nothing
        Dim r() As String = recipients.Split(";")

        For Each s As String In r
            If Trim(s) <> "" Then
                Try
                    msg.To.Add(New MailAddress(s))
                Catch ex As Exception
                End Try
            End If
        Next

        msg.From = New MailAddress(smtp_from)
        msg.Subject = subject
        msg.Body = body
        msg.IsBodyHtml = isBodyHtml

        Try
            Await client.SendMailAsync(msg)
        Catch ex As Exception
            lastEx = ex
            Return False
        End Try
        Return True
    End Function

    Private Sub mailTimer_Elapsed(sender As Object, e As ElapsedEventArgs) Handles mailTimer.Elapsed
        If Not sending Then
            Call sendUnsentMail()
        End If
    End Sub

    Private Async Sub sendUnsentMail()
        sending = True
        Try
            Dim dt As DataTable = LeggiMailDaInviare()
            Dim sentOk As Boolean
            For Each dr As DataRow In dt.Rows
                Try
                    sentOk = Await sendMail(dr("recipients"), dr("subject"), dr("body"), dr("htmlFormat"))
                    setMailSent(dr("id"), sentOk)
                Catch ex As Exception
                End Try
            Next
        Catch ex As Exception
        End Try
        sending = False
    End Sub

    Public Sub setMailSent(id As Long, sentOk As Boolean)
        Dim cmd As New SqlCommand("S_SetMailSent")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@id", id)
        cmd.Parameters.AddWithValue("@ok", sentOk)
        cmd.Parameters.AddWithValue("@clearMode", "")
        db.EseguiComando(cmd)
    End Sub

    Public Sub setMailToSend(templateId As Integer, recipients As String, subject As String, body As String, htmlFormat As Boolean)
        Dim cmd As New SqlCommand("S_SetMailToSend")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@templateId", templateId)
        cmd.Parameters.AddWithValue("@recipients", recipients)
        cmd.Parameters.AddWithValue("@subject", subject)
        cmd.Parameters.AddWithValue("@body", body)
        cmd.Parameters.AddWithValue("@htmlFormat", htmlFormat)
        db.EseguiComando(cmd)
    End Sub


    Public Function LeggiMailDaInviare() As DataTable
        Dim cmd As New SqlCommand("S_GetMailToSend")
        cmd.CommandType = CommandType.StoredProcedure
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        If db.LastActionSuccess Then
            Return ds.Tables(0)
        Else
            Throw New Exception("errore interno: " & db.LastException.Message, db.LastException)
        End If
    End Function

    Public Function getTemplateRow(template As String) As DataRow
        Dim cmd As New SqlCommand("S_GetMailTemplate")
        cmd.CommandType = CommandType.StoredProcedure
        cmd.Parameters.AddWithValue("@template", template)
        Dim ds As New DataSet
        db.CaricaDati(cmd, ds)
        If db.LastActionSuccess Then
            Return ds.Tables(0).Rows(0)
        Else
            Throw New Exception("errore interno: " & db.LastException.Message, db.LastException)
        End If
    End Function

End Class
