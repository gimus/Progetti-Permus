Imports Permus

Public Class BlockMasterConnectWindow
    Public url As String
    Public Sub New()
        InitializeComponent()
        btnConnect.Visibility = Visibility.Collapsed

    End Sub

    Private Sub BlockMasterConnectWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

    End Sub

    Private Sub BtnClose_Click(sender As Object, e As RoutedEventArgs)
        Me.url = ""
        Me.Close()
    End Sub

    Private Sub BtnConnect_Click(sender As Object, e As RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub BtnTest_Click(sender As Object, e As RoutedEventArgs) Handles btnTest.Click
        btnConnect.Visibility = Visibility.Collapsed

        url = Trim(tbUrl.Text).Replace("\", "/")
        If Not url.EndsWith("/") Then
            url &= "/"
        End If
        Dim c As New BlockMasterWebApiClient(New Uri(url))
        Dim b As Block = c.getBlock(1).Result
        If b IsNot Nothing Then
            tbResult.Text = String.Format("BlockMaster is ONLINE at:{1}{0}{1}{1}Chain version is: {2}{1}{1}First block hash is:{1}{3}{1}{1}", url, vbCrLf, b.version, utility.bin2hex(b.computeHash()))
            btnConnect.Visibility = Visibility.Visible
        Else
            tbResult.Text = String.Format("BlockMaster is OFFLINE at:{1}{0}{1}", url, vbCrLf)
        End If


    End Sub

End Class
