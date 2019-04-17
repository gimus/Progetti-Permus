Imports System.Text
Imports Permus

Public Class InitiateTransfer
    Inherits commandableControl
    Protected WithEvents transferInput As TransferInputRoot
    Protected WithEvents feedbackPage As TransferFeedbackPage

    Protected ttp As TransferTransactionPackage
    '    Protected interfaceValid As Boolean = False
    Protected opMode As OpModeEnum = OpModeEnum.input
    Protected lastFrameName As String = ""
    Protected transferIdToBeAccepted As String = ""
    Protected Enum OpModeEnum
        input = 0
        feedback = 1
    End Enum

    Public Sub New()
        InitializeComponent()
        Me.DataContext = Nothing
        setMode(OpModeEnum.input)
    End Sub

    Public Property subject As Subject
        Get
            Return DataContext
        End Get
        Set(value As Subject)
            DataContext = value
            If DataContext Is Nothing Then
                Me.Visibility = Visibility.Hidden
            Else
                Me.Visibility = Visibility.Visible
            End If
        End Set
    End Property

    Private Sub ItemDetail_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        setupInterface()
        CheckPendingTransfers()

        If opMode = OpModeEnum.input Then
            cbInputMode.SelectedIndex = -1
            cbInputMode.SelectedIndex = 0
        End If
    End Sub

    Protected Sub CheckPendingTransfers(Optional pt As PendingTransfers = Nothing)
        If pt Is Nothing Then
            pt = C.api.pendigTransfers(C.prepareSignedCommand(C.currentUser.x509Certificate)).Result
        End If
        Try
            If pt.Values.Where(Function(x) x.subjectTo = subject.id).Count > 0 Then
                transferIdToBeAccepted = pt.Values.Where(Function(x) x.subjectTo = subject.id).First.id
                If ttp Is Nothing OrElse ttp.transaction.transferId <> transferIdToBeAccepted Then
                    ttp = C.api.transferTransaction(C.prepareSignedCommand(C.currentUser.x509Certificate, "<transferId>" & transferIdToBeAccepted & "</transferId>")).Result
                    If ttp.isPrivate Then
                        ttp.privateBlock.transactions.correlateCompensations(C)
                    End If

                End If

                setMode(OpModeEnum.feedback, TransferFeedbackPage.TransferFeedbackModeEnum.waiting)
            Else
                setMode(OpModeEnum.input)
            End If

        Catch ex As Exception

        End Try
    End Sub

    Public Overrides Sub newCommand()
        Select Case cmd
            Case "TICK"
                If My.Settings.DEBUG_autoproponi Then
                    Try
                        initiateTransfer(New TransferTransactionPackage(My.Resources.test_trans))
                    Catch ex As Exception

                    End Try

                End If
        End Select
    End Sub

    Protected Sub setMode(newMode As OpModeEnum, Optional submode As TransferFeedbackPage.TransferFeedbackModeEnum = TransferFeedbackPage.TransferFeedbackModeEnum.check)
        Me.opMode = newMode
        btnOk.Visibility = Visibility.Collapsed
        btnCancel.Visibility = Visibility.Collapsed
        btnTrasmit.Visibility = Visibility.Collapsed
        btnClose.Visibility = Visibility.Collapsed
        Select Case opMode
            Case OpModeEnum.input
                transferIdToBeAccepted = ""
                rd0.Height = New GridLength(24)
                cbInputMode.IsReadOnly = False
                mainFrame.Content = transferInput
                btnOk.Visibility = Visibility.Visible
            Case OpModeEnum.feedback
                rd0.Height = New GridLength(0)

                Select Case submode
                    Case TransferFeedbackPage.TransferFeedbackModeEnum.check
                        btnCancel.Visibility = Visibility.Visible
                        btnTrasmit.Visibility = Visibility.Visible

                    Case TransferFeedbackPage.TransferFeedbackModeEnum.Result
                        btnClose.Visibility = Visibility.Visible

                    Case TransferFeedbackPage.TransferFeedbackModeEnum.waiting
                        btnCancel.Visibility = Visibility.Visible
                End Select

                cbInputMode.IsReadOnly = True
                If feedbackPage Is Nothing Then
                    feedbackPage = New TransferFeedbackPage()
                End If
                feedbackPage.mode = submode
                feedbackPage.DataContext = Nothing
                feedbackPage.DataContext = ttp
                mainFrame.Content = feedbackPage
        End Select
    End Sub

    Private Sub cbInputMode_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbInputMode.SelectionChanged
        If e.AddedItems.Count > 0 And mainFrame IsNot Nothing And subject IsNot Nothing Then
            Dim cbi As ComboBoxItem = e.AddedItems(0)
            selectInputFrame(cbi.Name)
        End If
        e.Handled = True
    End Sub

    Protected Sub selectInputFrame(Optional frameName As String = "")
        If frameName <> "" Then
            lastFrameName = frameName

            Select Case frameName
                Case "ciNoOperation"
                    transferInput = New NoOperationPage
                Case "ciCoinCreation"
                    transferInput = New CoinCreationPage With {.coinCreation = New CoinCreation With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciCoinTransfer"
                    transferInput = New CoinTransferPage With {.coinTransfer = New CoinTransfer With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciPublicTransfer"
                    transferInput = New PublicTransferPage With {.publicTransfer = New PublicTransfer With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciPublicSale"
                    transferInput = New PublicSalePage With {.publicSale = New PublicSale With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciPrivateSale"
                    transferInput = New PrivateSalePage With {.privateSale = New PrivateSale With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciPrivateTransfer"
                    transferInput = New PrivateTransferPage With {.privateTransfer = New PrivateTransfer With {.fromSubject = C.currentUser.id, .toSubject = subject.id}}

                Case "ciPrivateCompensation"
                    transferInput = New PrivateCompensationPage(New PrivateCompensation With {.fromSubject = C.currentUser.id, .toSubject = subject.id})
            End Select

            mainFrame.Content = transferInput
        End If
    End Sub

    Protected Sub setupInterface()

        For Each cbi As ComboBoxItem In cbInputMode.Items
            cbi.Visibility = Visibility.Visible
        Next
        ciCoinCreation.Visibility = Visibility.Collapsed

        If C.currentUser.isAuthority Then
            For Each cbi As ComboBoxItem In cbInputMode.Items
                cbi.Visibility = Visibility.Collapsed
            Next
            If Me.subject.isPublic Then
                ciCoinCreation.Visibility = Visibility.Visible
            End If

        Else
            If C.currentUser.isPublic Or subject.isPublic Then
                ciPrivateSale.Visibility = Visibility.Collapsed
                ciPrivateTransfer.Visibility = Visibility.Collapsed
                ciPrivateCompensation.Visibility = Visibility.Collapsed
            End If
        End If
        cbInputMode.SelectedIndex = -1
        cbInputMode.SelectedIndex = 0
    End Sub

    Protected Sub initiateTransfer(ttp As TransferTransactionPackage)
        Dim s As String = ttp.xml.ToString
        ttp = C.api.transferTransactionInit(ttp, C.prepareSignedCommand(C.currentUser.x509Certificate)).Result

        If ttp Is Nothing Then
            Dialogs.warning(C.api.LastResponseMessage)
        Else
            Dim requireAcceptance As Boolean = ttp.transaction.requireAcceptance

            ttp.transaction.computeSignatureFrom(C)
            ttp = C.api.transferTransactionPropose(ttp, C.prepareSignedCommand(C.currentUser.x509Certificate)).Result
            If ttp IsNot Nothing Then
                If requireAcceptance Then
                    CheckPendingTransfers()
                Else
                    If ttp.transaction.serial > 0 Then
                        setMode(OpModeEnum.feedback, TransferFeedbackPage.TransferFeedbackModeEnum.Result)
                    Else
                        Dialogs.warning("?")
                    End If
                End If
            Else
                If requireAcceptance Then
                    Dialogs.warning(C.api.LastResponseMessage)
                End If
            End If
        End If
    End Sub

    Private Sub cancelTransfer()
        If transferIdToBeAccepted <> "" Then
            Dim pt As PendingTransfers = C.api.transferTransactionCancel(C.prepareSignedCommand(C.currentUser.x509Certificate, "<transferId>" & transferIdToBeAccepted & "</transferId>")).Result
            CheckPendingTransfers(pt)
        Else
            setMode(OpModeEnum.input)
        End If

    End Sub

    Private Sub BtnOk_Click(sender As Object, e As RoutedEventArgs)
        ttp = transferInput.getContent()
        If ttp IsNot Nothing Then
            setMode(OpModeEnum.feedback)
        End If
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
        cancelTransfer()
    End Sub

    Private Sub BtnTrasmit_Click(sender As Object, e As RoutedEventArgs)
        initiateTransfer(ttp)
    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs)
        setMode(OpModeEnum.input)
    End Sub

End Class
