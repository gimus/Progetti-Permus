Imports System.Collections.ObjectModel
Imports Permus

Class PrivateSalePage
    Inherits TransferInputRoot
    Protected octe As New ObservableCollection(Of TransferElement)
    Protected C As ClientBlockChain

    Public Sub New()
        MyBase.New
        InitializeComponent()
        C = Application.C
        privateSale = New PrivateSale
        Me.DG.ItemsSource = octe
    End Sub

    Public Property privateSale As PrivateSale
        Get
            Return DataContext
        End Get
        Set(value As PrivateSale)
            DataContext = value
            Me.ttp.transaction = value
            octe.Clear()
            octe.Add(New TransferElement)
        End Set
    End Property

    Protected Overrides Function isContentValid() As Boolean
        Dim ltd As New List(Of TransferDetail)
        Dim almenoUnTE As Boolean = False
        Dim totCoins As Double = 0

        For Each te As TransferElement In octe
            If te.transferObject.isValid Then
                almenoUnTE = True
                totCoins += te.transferObject.cost
                '      te.isCompensation = True
                te.fromSubject = Me.transferTransaction.fromSubject
                te.toSubject = Me.transferTransaction.toSubject
                ltd.Add(te)
            End If
        Next

        If almenoUnTE Then
            Me.ttp.privateBlock = Block.CreateFrom(ltd, C)
            ttp.privateBlock.transactions.correlateCompensations(C)

            privateSale.transferObject.id = "B"
            privateSale.transferObject.description = utility.bin2hex(Me.ttp.privateBlock.computeHash)
            privateSale.transferObject.quantity = Me.ttp.privateBlock.transactions.Count
            privateSale.transferObject.cost = totCoins
            privateSale.coinAmount = totCoins

            privateSale.loadSubjects(C)

            Me.ttp.envelopedPrivateBlock = New EnvelopedBlock(Me.ttp.privateBlock, Me.transferTransaction.sFrom.x509Certificate, Me.transferTransaction.sTo.x509Certificate)
            Return True
        Else
            Dialogs.warning("I dati inseriti non sono validi: inserire un elemento da trasferire!")
            Return False
        End If
    End Function

    Private Sub BtnKillRow_Click(sender As Object, e As RoutedEventArgs)
        Dim b As Button = sender
        If TypeOf b.DataContext Is TransferElement Then
            octe.Remove(b.DataContext)
            If octe.Count = 0 Then
                octe.Add(New TransferElement)
            End If
        End If
    End Sub

    Private Sub DG_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DG.SelectionChanged
        e.Handled = True
    End Sub

    Protected Sub totalizzaCoins()
        Dim ca As Double = 0
        For Each te As TransferElement In Me.octe
            ca += te.transferObject.cost
        Next
        Me.transferTransaction.coinAmount = ca
        tbCoinAmount.DataContext = Nothing
        tbCoinAmount.DataContext = Me.transferTransaction
    End Sub

    Private Sub DG_CellEditEnding(sender As Object, e As DataGridCellEditEndingEventArgs) Handles DG.CellEditEnding
        Dim c As DataGridColumn = e.Column
        Select Case c.Header
            Case "coins"
                Dim tb As TextBox = e.EditingElement
                Dim nCoins As Double = utility.getDoubleFromString(tb.Text)
                If nCoins < 0 Then nCoins = 0
        End Select

    End Sub

    Private Sub DG_CurrentCellChanged(sender As Object, e As EventArgs) Handles DG.CurrentCellChanged
        totalizzaCoins()
    End Sub
End Class
