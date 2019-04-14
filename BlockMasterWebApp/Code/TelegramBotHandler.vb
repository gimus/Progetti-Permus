Imports BlockMasterWebApp
Imports Permus
Imports Telegram.Bot.Args

Public Class TelegramBotHandler
    Protected WithEvents bot As TelegramBotClient
    Protected subjects As New Dictionary(Of Integer, Subject)

    Public Sub New()
        bot = New TelegramBotClient("BOT", My.Settings.iot_bot_key)
    End Sub

    Protected Sub handleAuthenticatedMessage(sender As Integer, s As Subject, msg As String)
        Dim t() As String = msg.Split(" ")
        Select Case t(0).ToLowerInvariant
            Case "otp"
                If s.profile.hasOtp Then
                    If t.Length > 1 Then
                        If s.profile.asProtected.isOtpVerified(t(1)) Then
                            bot.sendMessageAsync(sender, String.Format("OTP OK {0}", s.name))
                        Else
                            bot.sendMessageAsync(sender, String.Format("OTP NOT VERIFIED {0}", s.name))
                        End If
                    Else
                        repSyntaxError(sender)
                    End If
                Else
                    bot.sendMessageAsync(sender, String.Format("OTP CODE NOT SET {0}", s.name))
                End If

            Case Else
                bot.sendMessageAsync(sender, String.Format("Ciao {0}", s.name))
                repCoinBalance(s)
        End Select
    End Sub

    Protected Sub repCoinBalance(s As Subject)
        bot.sendMessageAsync(s.profile.asProtected.telegramId, String.Format("your coin balance: {0} ", s.coinBalance.balance.ToString("#.##")))
    End Sub

    Protected Sub repSyntaxError(chatId As Integer)
        bot.sendMessageAsync(chatId, String.Format("command syntax error"))
    End Sub

    Protected Friend Sub repNotifySubjectsTransferTransaction(ttp As TransferTransactionPackage)
        Try
            If ttp.transaction.sFrom.profile IsNot Nothing Then
                If ttp.transaction.sFrom.profile.hasTelegram Then
                    sendTransferNotification(ttp.transaction.sFrom, ttp)
                    repCoinBalance(ttp.transaction.sFrom)
                End If
            End If

            If ttp.transaction.sTo.profile IsNot Nothing Then
                If ttp.transaction.sTo.profile.hasTelegram <> 0 Then
                    sendTransferNotification(ttp.transaction.sTo, ttp)
                    repCoinBalance(ttp.transaction.sTo)
                End If
            End If
        Catch ex As Exception

        End Try

    End Sub

    Protected Sub sendTransferNotification(s As Subject, ttp As TransferTransactionPackage)
        sendSubjectMessage(s.profile.asProtected.telegramId, ttp.transaction.transferNotification)

        If s.profile.asProtected.hasCertificate And ttp.isPrivate Then
            Try
                ttp.ensureBlockDecrypted(s.profile.asProtected.X509Certificate2)
                If ttp.privateBlock IsNot Nothing Then
                    sendSubjectMessage(s.profile.asProtected.telegramId, ttp.plainText())
                End If
            Catch ex As Exception
            End Try
        End If
    End Sub

    Public Sub StartReceivingMessages()
        bot.StartReceiving()
    End Sub

    Public Sub StopReceivingMessages()
        bot.StopReceiving()
    End Sub

    Private Sub bot_TextMessageReceived(sender As TelegramBotClient, messageText As String, e As MessageEventArgs) Handles bot.TextMessageReceived
        Dim senderId As Long = e.Message.From.Id
        Dim s As Subject = subject(senderId)

        If s Is Nothing Then
            If messageText.ToLower = "ciao" Or messageText.ToLower = "hello" Then
                bot.sendMessageAsync(senderId, String.Format("chi sei? non ti conosco!"))
                s = New Subject
                s.profile = New ProtectedSubjectProfile
                s.profile.asProtected.telegramId = senderId
                AddSubject(s)
            End If
        Else
            If s.id = "" Then
                Dim cs As Subject = BlockMasterBlockChain.M.subjects.getElementById(Trim(messageText))
                If cs Is Nothing Then
                    bot.sendMessageAsync(senderId, String.Format("?"))
                Else
                    s.id = cs.id
                    AppMail.sendTelegramPin(cs)
                    bot.sendMessageAsync(senderId, String.Format("codicillo che ti ho mandato via mail?"))
                End If
            Else
                If s.name = "" Then
                    Dim cs As Subject = BlockMasterBlockChain.M.subjects.getElementById(s.id)
                    If Trim(messageText) = cs.tag Then
                        cs.profile.asProtected.telegramId = senderId
                        bot.sendMessageAsync(senderId, String.Format("benvento su permus telegram {0}", cs.name))
                        Dim dt As DataTable = BlockMasterBlockChain.M.da.ModificaAttributoProfilo(cs.id, "telegramId", senderId)
                        cs.profile = Factory.leggiProtectedSubjectProfile(dt.Rows(0))
                        subjects.Remove(senderId)
                        AddSubject(cs)
                    Else
                        bot.sendMessageAsync(senderId, String.Format("ricominciamo da capo!"))
                        Me.subjects.Remove(senderId)
                    End If
                Else
                    Try
                        handleAuthenticatedMessage(senderId, s, messageText)
                    Catch ex As Exception
                        bot.sendMessageAsync(senderId, String.Format("ERROR {0}", ex.Message))

                    End Try
                End If
            End If
        End If

        'Select Case messageText.ToLower
        '    Case "whoami"
        '        bot.sendMessageAsync(senderId, String.Format("you are: {0} ({1} {2})", senderId, e.Message.From.FirstName, e.Message.From.LastName))
        '    Case Else
        '        bot.sendMessageAsync(senderId, "ok")
        'End Select

    End Sub

    Public Sub sendSubjectMessage(subjectTelegramId As Long, msg As String)
        bot.sendMessageAsync(subjectTelegramId, msg)
    End Sub

    Public Sub AddSubject(s As Subject)
        If Not subjects.ContainsKey(s.profile.asProtected.telegramId) Then
            subjects.Add(s.profile.asProtected.telegramId, s)
        End If
    End Sub

    Protected Function subject(id As Long) As Subject
        If subjects.ContainsKey(id) Then
            Return subjects(id)
        Else
            Return Nothing
        End If
    End Function

End Class
