Imports Permus

Public Class TransferInputRoot
    Inherits Page
    Protected ttp As TransferTransactionPackage
    Protected validationMessage As String
    Public Sub New()
        Me.DataContext = Nothing
        ttp = New TransferTransactionPackage
    End Sub

    Public Property transferTransaction As TransferTransaction
        Get
            Return DataContext
        End Get
        Set(value As TransferTransaction)
            ttp.transaction = value
            DataContext = value
        End Set
    End Property

    Protected Overridable Function isContentValid() As Boolean
        Return False
    End Function

    Public Function getContent() As TransferTransactionPackage
        If isContentValid() Then
            Return ttp
        Else
            Return Nothing
        End If
    End Function
End Class
