Imports System.ComponentModel
Imports System.Security.Cryptography.X509Certificates
Imports System.Text
Imports System.Threading
Imports Permus
Imports PermusWpf

Class MainWindow
    Dim WithEvents C As ClientBlockChain
    Protected xdocMenu As XDocument
    Dim lastMenuId As String = "currentUser_0"
    Protected lastStatusText As String = ""
    Dim BCeventNotificationWindow As BlockChainEventNotificationWindow

    Public Sub New()
        InitializeComponent()
        Application.mw = Me
        C = Application.C
        StatoWindow.RipristinaStato(Me)
        setStatusText()

        BCeventNotificationWindow = New BlockChainEventNotificationWindow(Application.C)
        BCeventNotificationWindow.Visibility = Visibility.Hidden

    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If ConnectToCurrentChain() Then
            selectUserCertificate(My.Settings.lastUserCertificate)
        Else
            If tryConnect() Then
                If ConnectToCurrentChain() Then
                    selectUserCertificate(My.Settings.lastUserCertificate)
                End If
            Else
                Dialogs.warning("CIAO CIAO!")
                Me.Close()
            End If


        End If
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        StatoWindow.SalvaStato(Me)
    End Sub


    Public Sub aggiornaMenuData()
        Dim e As XElement


        Dim eep As XElement = xdocMenu.Descendants.Where(Function(x) x.Attribute("name") = "currentUser").First
        If C.currentUser IsNot Nothing Then
            eep.Attributes("header").FirstOrDefault.Value = C.currentUser.name
        Else
            eep.Attributes("header").FirstOrDefault.Value = "..."
        End If

        eep = xdocMenu.Descendants.Where(Function(x) x.Attribute("name") = "users").First
        For Each s As Subject In C.subjects.Values

            If s.id <> C.currentUser.id Then
                e = New XElement("menuItem")

                e.Add(New XAttribute("name", "user"))
                e.Add(New XAttribute("id", s.id))
                e.Add(New XAttribute("header", s.name))
                e.Add(New XAttribute("expanded", "false"))
                If s.isAuthority Then
                    e.Add(New XAttribute("image", "bank.png"))
                Else
                    If s.isPublic Then
                        e.Add(New XAttribute("image", "public.png"))
                    Else
                        e.Add(New XAttribute("image", "user.png"))
                    End If
                End If
                eep.Add(e)
            End If
        Next
    End Sub

    Private Sub BtnUser_Click(sender As Object, e As RoutedEventArgs)
        selectUserCertificate()
    End Sub

    Protected Sub selectUserCertificate(Optional hash As String = Nothing)

        Dim cer As X509Certificate2
        If hash Is Nothing Or hash = "" Then
            cer = utility.pickCertificateFromStore()
        Else
            cer = utility.getCertificateFromStoreByCertificateHash(hash)
        End If

        If cer IsNot Nothing Then
            newUserCertificateSelected(Subject.fromX509Certificate(cer))
        End If
    End Sub

    Protected Sub newUserCertificateSelected(subj As Subject)
        Dim si As SystemInfo = Nothing
        Try
            si = C.api.getCheckIn(C.prepareSignedCommand(subj.x509Certificate)).Result

            If si Is Nothing Then
                ConnectToCurrentChain()
                si = C.api.getCheckIn(C.prepareSignedCommand(subj.x509Certificate)).Result
            End If
        Catch ex As Exception

        End Try

        If si IsNot Nothing Then
            subj.token = si.requesterInfo.token
            C.currentUser = subj

            If C.currentUser IsNot Nothing Then
                Me.lblUser.Text = C.currentUser.name
                Me.lblUser.ToolTip = C.currentUser.distinguishedName
                My.Settings.lastUserCertificate = subj.x509Certificate.Thumbprint
                If My.Settings.serverUrl <> "" Then
                    ConnectToCurrentChain()
                Else
                    If tryConnect() Then
                        ConnectToCurrentChain()
                    End If
                End If
            Else
                Dialogs.notify("test: l'utente corrente è annullato")

                Me.lblUser.Text = "..."
                My.Settings.lastUserCertificate = ""
            End If
        Else
            Dialogs.warning("Impossibile connettere l'utente alla blockchain... questo programma sarà terminato!")
            My.Settings.lastUserCertificate = ""
            My.Settings.Save()
            Me.Close()
        End If


    End Sub

    Protected Sub updateServerStatusText()
        If C.systemInfo IsNot Nothing Then
            C.currentUser.coinBalance = C.subjects.getElementById(C.currentUser.id).coinBalance
            lblCoins.Text = C.currentUser.coinBalance.balance.ToString("#.##")
            Me.lblServer.Text = String.Format("Block Master @ {3} {0}; B={1}; T={2} ping: {4}ms", IIf(C.systemInfo.blockMasterReady, "READY", "OFF LINE"), C.systemInfo.currentBlockSerial, C.systemInfo.currentTransactionSerial, C.api.baseAddress, C.api.lastPingTime)
        End If
    End Sub


    Private Sub C_BlockChainTick() Handles C.BlockChainTick
        updateServerStatusText()


    End Sub

    Private Sub C_LocalBlockChainUpdated() Handles C.LocalBlockChainUpdated
        '      Me.Dispatcher.Invoke(AddressOf aggiornaMenu, Windows.Threading.DispatcherPriority.Render)
        Me.Dispatcher.Invoke(AddressOf UpdateControls, Windows.Threading.DispatcherPriority.Render)
    End Sub

    Private Sub C_Notification(eventType As BlockChainNotifyEventType, eventMessage As String) Handles C.Notification
        Select Case eventType

            Case BlockChainNotifyEventType.currentUserHasIncomingPendingTransfers

            Case BlockChainNotifyEventType.NeedsResyc
                If Me.WindowState <> WindowState.Minimized Then
                    BCeventNotificationWindow.Visibility = Visibility.Visible
                    Me.lblServer.Text = String.Format("RESYNCING Blockchain ...")
                End If

            Case BlockChainNotifyEventType.Ready
                Me.Dispatcher.Invoke(AddressOf onBlockChainReadyEvent, Windows.Threading.DispatcherPriority.Render)

        End Select
    End Sub

    Protected Sub onBlockChainReadyEvent()
        BCeventNotificationWindow.Visibility = Visibility.Hidden
        setStatusText()
        UpdateControls()
    End Sub

    Protected Function tryConnect() As Boolean
        Dim bccw As New BlockMasterConnectWindow
        bccw.ShowDialog()
        If bccw.url <> "" Then
            My.Settings.serverUrl = bccw.url
            My.Settings.Save()
            Return True
        Else
            Return False
        End If

    End Function

    Public Function ConnectToCurrentChain() As Boolean
        If C.ConnectToBlockMaster(New Uri(My.Settings.serverUrl)) Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub MniConnect_Click(sender As Object, e As RoutedEventArgs)
        If tryConnect() Then
            ConnectToCurrentChain()
        End If
    End Sub

    Private Sub MniExit_Click(sender As Object, e As RoutedEventArgs)
        tryExit()
    End Sub

    Private Sub BtnQuit_Click(sender As Object, e As RoutedEventArgs)
        tryExit()
    End Sub

    Protected Sub tryExit()
        If MsgBox("do you really want to quit?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            Me.Close()
        End If
    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        System.Windows.Application.Current.Shutdown()
    End Sub

    Private Sub MniPurgeLocalData_Click(sender As Object, e As RoutedEventArgs)
        If MsgBox("Are you sure you want to purge local data?", MsgBoxStyle.YesNo) = MsgBoxResult.Yes Then
            C.purgeLocalData()
            C.syncWithBlockMaster()
        End If
    End Sub

    Private Sub C_NewTransactionsFetched(newTransactionsCount As Long, newTransferTransactions As List(Of TransferTransaction)) Handles C.NewTransactionsFetched
        Me.Dispatcher.Invoke(AddressOf UpdateControls, Windows.Threading.DispatcherPriority.Render)
    End Sub

    Protected Sub UpdateControls()

    End Sub

    Private Sub MniShowLog_Click(sender As Object, e As RoutedEventArgs)
        BCeventNotificationWindow.Visibility = Visibility.Visible
    End Sub

    Private Sub MniSaveSnapshot_Click(sender As Object, e As RoutedEventArgs)
        C.saveSnapshotAsync()
    End Sub

    Private Sub MniTest_Click(sender As Object, e As RoutedEventArgs)

    End Sub

    Private Sub MniOpenLocalDir_Click(sender As Object, e As RoutedEventArgs)
        C.browseAppDataDirectory()
    End Sub

    Public Sub setStatusText(Optional txt As String = "Ready")
        Me.lastStatusText = txt
        Me.Dispatcher.Invoke(AddressOf updateStatusText, Windows.Threading.DispatcherPriority.Render)
    End Sub
    Protected Sub updateStatusText()
        tbStatus.Text = Me.lastStatusText
    End Sub

End Class
