Imports Permus

Class CoinCreationPage
    Inherits TransferInputRoot

    Public Sub New()
        MyBase.New
        InitializeComponent()
        coinCreation = New CoinCreation()
    End Sub

    Public Property coinCreation As CoinCreation
        Get
            Return DataContext
        End Get
        Set(value As CoinCreation)
            DataContext = value
            Me.ttp.transaction = value
        End Set
    End Property

    Protected Overrides Function isContentValid() As Boolean
        If coinCreation.coinAmount > 0 Then
            Return True
        Else
            validationMessage = "il numero di coin da trasferire deve essere maggiore di zero!"
            Return False
        End If
    End Function

End Class
