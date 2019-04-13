Imports System.Text
Imports Permus

Class TransferFeedbackPage
    Public Property mode As TransferFeedbackModeEnum = TransferFeedbackModeEnum.check

    Public Enum TransferFeedbackModeEnum
        check = 0
        Result = 1
        waiting = 2
    End Enum

    Public Sub New()
        InitializeComponent()
        Me.DataContext = Nothing
    End Sub

    Public Property transferTransactionPackage As TransferTransactionPackage
        Get
            Return DataContext
        End Get
        Set(value As TransferTransactionPackage)
            DataContext = value
        End Set
    End Property

    Private Sub TransferFeedbackPage_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        Dim t As New StringBuilder(1000)
        If transferTransactionPackage IsNot Nothing Then
            transferTransactionPackage.transaction.loadSubjects(Application.C)

            Select Case mode
                Case TransferFeedbackModeEnum.check
                    t.Append("<div>Controlla questa operazione prima di lanciarla:</div>")
                Case TransferFeedbackModeEnum.Result
                    t.Append("<div>la seguente operazione è andata a buon fine:</div>")
                Case TransferFeedbackModeEnum.waiting
                    t.AppendFormat("<div>la seguente operazione è in attesa di essere approvata da {0}:</div>", transferTransactionPackage.transaction.sTo.name)
            End Select
            t.Append("<br />")
            t.Append(transferTransactionPackage.html())

            WB.NavigateToString(utility.htmlPage(t.ToString))
        Else
            WB.Navigate("about:blank")
        End If
    End Sub
End Class
