Imports System.Threading.Tasks
Imports Telegram.Bot
Imports Telegram.Bot.Args
Public Class TelegramBotClient
    Public name As String = "BOT"
    Protected bot As Telegram.Bot.TelegramBotClient
    Public Event TextMessageReceived(sender As TelegramBotClient, messageText As String, e As MessageEventArgs)
    Protected adminLogChatId As Long = 237561278

    Public Sub New(name As String, botKey As String)
        Me.name = name
        bot = New Telegram.Bot.TelegramBotClient(botKey)
        AddHandler bot.OnMessage, AddressOf bot_OnMessage

    End Sub

    Public Sub StartReceiving()
        bot.StartReceiving()
    End Sub

    Public Sub StopReceiving()
        bot.StopReceiving()
    End Sub

    Public Sub sendMessageAsync(chatId As Integer, messageText As String, Optional silent As Boolean = True)
        Try
            If chatId <> 0 Then
                Dim t = bot.SendTextMessageAsync(chatId, messageText)
            End If
        Catch ex As Exception
            If Not silent Then
                Throw New Exception("Errore inviando un messaggio con telegram: " & ex.Message, ex)
            End If
        End Try
    End Sub

    Public Sub sendVideoAsync(chatId As Integer, filename As String, content() As Byte)
        Dim ms As New IO.MemoryStream(content)
        sendVideoAsync(chatId, filename, ms)
    End Sub

    Public Sub sendVideoAsync(chatId As Integer, filename As String, str As IO.Stream)
        Dim fts As Types.InputFiles.InputOnlineFile = New Types.InputFiles.InputOnlineFile(str, filename)
        Dim t = bot.SendVideoAsync(chatId, fts)
    End Sub


    Public Sub sendPhotoAsync(chatId As Integer, filename As String, content() As Byte, Optional message As String = "")
        Dim ms As New IO.MemoryStream(content)
        Try
            sendPhotoAsync(chatId, filename, ms, message)
        Catch ex As Exception

        End Try
    End Sub

    Public Sub sendPhotoAsync(chatId As Integer, filename As String, str As IO.Stream, Optional message As String = "")
        Dim fts As Types.InputFiles.InputOnlineFile = New Types.InputFiles.InputOnlineFile(str, filename)
        Try
            Dim t = bot.SendPhotoAsync(chatId, fts, message)
        Catch ex As Exception

        End Try
    End Sub

    Public Sub sendPhotoAsync(chatId As Integer, filename As String, filePath As String, Optional message As String = "")
        Dim str As New IO.FileStream(filePath, IO.FileMode.Open)
        Try
            sendPhotoAsync(chatId, filename, str, message)
        Catch ex As Exception
        End Try
    End Sub

    Public Sub sendFileAsync(chatId As Integer, filePath As String)
        Try
            Dim str As New IO.FileStream(filePath, IO.FileMode.Open)
            Dim fi As New IO.FileInfo(filePath)
            Dim fts As Types.InputFiles.InputOnlineFile = New Types.InputFiles.InputOnlineFile(str, fi.Name)
            Dim t = bot.SendDocumentAsync(chatId, fts)

        Catch ex As Exception

        End Try
    End Sub

    Private Sub bot_OnMessage(sender As Object, e As MessageEventArgs)
        Dim m As Types.Message = e.Message
        Select Case m.Type
            Case Types.Enums.MessageType.Text
                RaiseEvent TextMessageReceived(Me, m.Text, e)
            Case Else
        End Select
    End Sub

    Public Sub SendAdminLogMessage(msg As String)
        sendMessageAsync(Me.adminLogChatId, msg, True)
    End Sub

End Class

