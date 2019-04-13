Imports Permus

Public Class AppMail

    Public Shared Sub sendTelegramPin(s As Subject)
        Dim dr As DataRow = App.mailer.getTemplateRow("PIN_AUTENTICAZIONE_TELEGRAM")
        Dim subject As String = dr("subject")
        Dim body As String = dr("body")
        s.tag = Right("000000" & Guid.NewGuid.ToString.GetHashCode.ToString, 6)

        body = body.Replace("{PIN}", s.tag)

        App.mailer.setMailToSend(dr("templateId"), String.Format("{0} <{1}>", s.name, s.email), subject, body, True)
    End Sub



End Class
