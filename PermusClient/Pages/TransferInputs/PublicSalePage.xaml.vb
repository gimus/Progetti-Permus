Imports Permus

Class PublicSalePage
    Inherits TransferInputRoot

    Public Sub New()
        MyBase.New
        InitializeComponent()
        publicSale = New PublicSale
    End Sub

    Public Property publicSale As PublicSale
        Get
            Return DataContext
        End Get
        Set(value As PublicSale)
            DataContext = value
            Me.ttp.transaction = value
        End Set
    End Property

    Protected Overrides Function isContentValid() As Boolean
        publicSale.transferObject.description = Trim(publicSale.transferObject.description)
        If publicSale.transferObject.description <> "" And publicSale.coinAmount > 0 Then
            publicSale.transferObject.cost = publicSale.coinAmount
            Return True
        Else
            validationMessage = "Assicurarsi di avere inserito la descrizione dei beni o servizi da scambiare e un numero di coins maggiore di zero!"
            Return False
        End If
    End Function

End Class
