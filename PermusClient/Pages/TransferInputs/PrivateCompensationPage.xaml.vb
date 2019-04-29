Imports System.Collections.ObjectModel
Imports Permus
Imports PermusWpf

Class PrivateCompensationPage
    Inherits TransferInputRoot
    Protected octe As New ObservableCollection(Of TransferElement)
    Protected octc As New ObservableCollection(Of TransferCompensation)
    Protected C As ClientBlockChain

    Public Sub New(Optional value As PrivateCompensation = Nothing)
        MyBase.New
        InitializeComponent()
        C = Application.C
        If value Is Nothing Then
            value = New PrivateCompensation()
        End If
        Me.ttp.transaction = value
        Me.DataContext = value

        DGC.ItemsSource = octc
        DG.ItemsSource = octe
    End Sub

    Public Property privateCompensation As PrivateCompensation
        Get
            Return DataContext
        End Get
        Set(value As PrivateCompensation)
            DataContext = value
            Me.ttp.transaction = value
        End Set
    End Property


    Protected Overrides Function isContentValid() As Boolean
        Dim ltd As New List(Of TransferDetail)
        Dim almenoUnTE As Boolean = False
        Dim almenoUnTC As Boolean = False
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

        For Each tc As TransferCompensation In octc
            If tc.newPercentCompensated > tc.currentObjectCompensation Then
                ' ci interessa solo la differenza
                tc.percentCompensated = tc.newPercentCompensated - tc.currentObjectCompensation
                almenoUnTC = True
                ltd.Add(tc)
            End If
        Next

        If almenoUnTC And almenoUnTE Then
            Me.ttp.privateBlock = Block.CreateFrom(ltd, C)
            ttp.privateBlock.transactions.correlateCompensations(C)
            Dim pc As PrivateCompensation = Me.transferTransaction

            pc.transferObject.id = "B"
            pc.transferObject.description = utility.bin2hex(Me.ttp.privateBlock.computeHash)
            pc.transferObject.quantity = Me.ttp.privateBlock.transactions.Count
            pc.transferObject.cost = totCoins
            pc.coinAmount = totCoins

            pc.loadSubjects(C)

            Me.ttp.envelopedPrivateBlock = New EnvelopedBlock(Me.ttp.privateBlock, Me.transferTransaction.sFrom.x509Certificate, Me.transferTransaction.sTo.x509Certificate)
            Return True
        Else
            If Not almenoUnTC And Not almenoUnTE Then
                Dialogs.warning("I dati inseriti non sono validi: inserire almeno un elemento da compensare e un elemento compensante!")
            Else
                If Not almenoUnTC Then
                    Dialogs.warning("I dati inseriti non sono validi: inserire un elemento da compensare!")
                Else
                    Dialogs.warning("I dati inseriti non sono validi: inserire un elemento utilizzato per compensare!")
                End If
            End If
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

    Private Sub Btn100Row_Click(sender As Object, e As RoutedEventArgs)
        Dim b As Button = sender
        If TypeOf b.DataContext Is TransferCompensation Then
            Dim tc As TransferCompensation = b.DataContext
            tc.newPercentCompensated = 100
        End If

    End Sub

    Private Sub DG_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DG.SelectionChanged
        e.Handled = True
    End Sub

    Private Sub DGC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DGC.SelectionChanged
        e.Handled = True
    End Sub

    Private Sub PrivateCompensationPage_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged
        If DataContext IsNot Nothing Then
            octe.Clear()
            octe.Add(New TransferElement)

            octc.Clear()
            Dim tc As TransferCompensation
            For Each tt As TransferTransaction In C.userTransferTransactions.Values.Where(Function(x) x.isCompensation = False And x.compensation < 100 And x.fromSubject = Me.transferTransaction.toSubject)
                tc = New TransferCompensation(tt)
                octc.Add(tc)
            Next
        End If
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
