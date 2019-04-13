Imports Permus

Class CoinTransferPage
    Inherits TransferInputRoot

    Public Sub New()
        MyBase.New
        InitializeComponent()
        coinTransfer = New CoinTransfer
    End Sub

    Public Property coinTransfer As CoinTransfer
        Get
            Return DataContext
        End Get
        Set(value As CoinTransfer)
            DataContext = value
            Me.ttp.transaction = value
        End Set
    End Property


    Protected Overrides Function isContentValid() As Boolean
        If coinTransfer.coinAmount > 0 Then
            Return True
        Else
            validationMessage = "il numero di coin da trasferire deve essere maggiore di zero!"
            Return False
        End If
    End Function

End Class
