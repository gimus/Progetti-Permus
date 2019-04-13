Imports System.Collections.ObjectModel
Imports Permus

Class PrivateTransferPage
    Inherits TransferInputRoot
    Protected octe As New ObservableCollection(Of TransferElement)
    Protected C As ClientBlockChain

    Public Sub New()
        MyBase.New
        InitializeComponent()
        C = Application.C
        privateTransfer = New PrivateTransfer
        Me.DG.ItemsSource = octe
    End Sub

    Public Property privateTransfer As PrivateTransfer
        Get
            Return DataContext
        End Get
        Set(value As PrivateTransfer)
            DataContext = value
            Me.ttp.transaction = value
            octe.Clear()
            octe.Add(New TransferElement)
        End Set
    End Property

    Protected Overrides Function isContentValid() As Boolean
        Dim ltd As New List(Of TransferDetail)
        Dim almenoUnTE As Boolean = False

        For Each te As TransferElement In octe
            If te.transferObject.isValid Then
                almenoUnTE = True
                te.fromSubject = Me.transferTransaction.fromSubject
                te.toSubject = Me.transferTransaction.toSubject
                ltd.Add(te)
            End If
        Next

        If almenoUnTE Then
            Me.ttp.privateBlock = Block.CreateFrom(ltd, C)
            ttp.privateBlock.transactions.correlateCompensations(C)

            privateTransfer.transferObject.id = "B"
            privateTransfer.transferObject.description = utility.bin2hex(Me.ttp.privateBlock.computeHash)
            privateTransfer.transferObject.quantity = Me.ttp.privateBlock.transactions.Count
            privateTransfer.transferObject.cost = 0
            privateTransfer.coinAmount = 0
            privateTransfer.loadSubjects(C)

            Me.ttp.envelopedPrivateBlock = New EnvelopedBlock(Me.ttp.privateBlock, Me.transferTransaction.sFrom.x509Certificate, Me.transferTransaction.sTo.x509Certificate)
            Return True
        Else
            Dialogs.warning("I dati inseriti non sono validi: inserire almeno un elemento da trasferire!")
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

End Class
