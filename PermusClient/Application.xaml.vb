Imports System.Globalization
Imports System.Threading
Imports System.Windows.Markup
Imports Permus

Class Application
    Public Shared C As ClientBlockChain
    Public Shared autoFire As Boolean = False

    Public Shared Property mw As MainWindow


    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Thread.CurrentThread.CurrentCulture = New System.Globalization.CultureInfo("it-IT")
        Thread.CurrentThread.CurrentUICulture = New System.Globalization.CultureInfo("it-IT")
        FrameworkElement.LanguageProperty.OverrideMetadata(GetType(FrameworkElement), New FrameworkPropertyMetadata(
        XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)))
        C = New ClientBlockChain(My.Settings.BlockMasterPollingTimerIntervalSeconds, localStoreTypeEnum.xml_plain)
    End Sub

    ' Gli eventi a livello di applicazione, ad esempio Startup, Exit e DispatcherUnhandledException,
    ' possono essere gestiti in questo file.

End Class
