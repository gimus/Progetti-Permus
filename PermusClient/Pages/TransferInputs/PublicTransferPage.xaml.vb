Imports Permus

Class PublicTransferPage
    Inherits TransferInputRoot

    Public Sub New()
        MyBase.New
        InitializeComponent()
        publicTransfer = New PublicTransfer
    End Sub

    Public Property publicTransfer As PublicTransfer
        Get
            Return DataContext
        End Get
        Set(value As PublicTransfer)
            DataContext = value
            Me.ttp.transaction = value
        End Set
    End Property

    Protected Overrides Function isContentValid() As Boolean
        publicTransfer.coinAmount = 0
        publicTransfer.transferObject.description = Trim(publicTransfer.transferObject.description)
        If publicTransfer.transferObject.description <> "" Then
            Return True
        Else
            validationMessage = "Inserire una descrizione dei beni o servizi trasferiti!"
            Return False
        End If
    End Function

End Class
