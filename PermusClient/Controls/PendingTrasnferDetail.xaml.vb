Imports System.Text
Imports Permus

Public Class PendingTransferDetail
    Inherits commandableControl
    Dim feedbackPage As New TransferFeedbackPage

    Public Sub New()
        InitializeComponent()
        Me.DataContext = Nothing
        Me.MainFrame.Content = feedbackPage
        Me.Visibility = Visibility.Hidden
    End Sub

    Public Property transferTransactionPackage As TransferTransactionPackage
        Get
            Return DataContext
        End Get
        Set(value As TransferTransactionPackage)
            DataContext = value
            If DataContext Is Nothing Then
                Me.Visibility = Visibility.Hidden
            Else
                Me.Visibility = Visibility.Visible
            End If
        End Set
    End Property

    Private Sub ItemDetail_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        feedbackPage.mode = TransferFeedbackPage.TransferFeedbackModeEnum.waiting
        feedbackPage.DataContext = DataContext
    End Sub

    Public Overrides Sub newCommand()
        Select Case cmd
        End Select
    End Sub

    Private Sub BtnOk_Click(sender As Object, e As RoutedEventArgs)
        If C.acceptTransaction(transferTransactionPackage) Then
            Dialogs.notify("Transazione correttamete registrata nella blockchain")
            Me.transferTransactionPackage = Nothing
        End If
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
        If transferTransactionPackage IsNot Nothing Then
            Dim pt As PendingTransfers = C.api.transferTransactionCancel(C.prepareSignedCommand(C.currentUser.x509Certificate, "<transferId>" & transferTransactionPackage.transaction.transferId & "</transferId>")).Result
            If Not pt.ContainsKey(transferTransactionPackage.transaction.transferId) Then
                Dialogs.notify("Transazione annullata/rifutata")
                Me.transferTransactionPackage = Nothing
            End If
        End If
    End Sub
End Class
