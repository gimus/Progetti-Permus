Imports System.Text
Imports Permus

Public Class transferTransactionDetail
    Inherits commandableControl
    Dim tmpXmlName As String = IO.Path.Combine(IO.Path.GetTempPath(), "tmp.xml")
    Dim tmpTxtName As String = IO.Path.Combine(IO.Path.GetTempPath(), "tmp.txt")
    Dim ttdComp As transferTransactionDetail

    Public Sub New()
        InitializeComponent()
        init(0)
    End Sub

    Public Sub New(Optional nl As Integer = 0)
        InitializeComponent()
        init(nl)
    End Sub

    Protected Sub init(nl As Integer)
        Me.DataContext = Nothing
    End Sub

    Public Property TransferTransaction As TransferTransaction
        Get
            Return DataContext
        End Get
        Set(value As TransferTransaction)
            DataContext = value
            If DataContext Is Nothing Then
                Me.Visibility = Visibility.Hidden
            Else
                Me.Visibility = Visibility.Visible
            End If
        End Set
    End Property

    Private Sub ItemDetail_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles Me.DataContextChanged

        Dim pi As Integer = cbXMLMode.SelectedIndex
        cbXMLMode.SelectedIndex = -1
        cbXMLMode.SelectedIndex = 0
        If ttdComp IsNot Nothing Then
            If TransferTransaction.isCompensation Then

            Else
                Dim o As Object = TransferTransaction.compensations

            End If



        End If
    End Sub


    Public Overrides Sub newCommand()
        Select Case cmd
        End Select
    End Sub

    Private Sub cbXMLMode_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles cbXMLMode.SelectionChanged
        e.Handled = True
        If (e.AddedItems.Count > 0 And wbSource IsNot Nothing And TransferTransaction IsNot Nothing) Then
            Dim ci As ComboBoxItem = e.AddedItems(0)
            Select Case ci.Name
                Case "ciTransaction"
                    IO.File.WriteAllText(tmpXmlName, TransferTransaction.xml.ToString)
                    wbSource.Navigate(tmpXmlName)


                Case "ciPrivateBlock"
                    If TransferTransaction.isPrivate Then
                        Dim pt As PrivateCoSignedTransferTransaction = TransferTransaction
                        If pt.privateBlock Is Nothing Then
                            pt.privateBlock = C.obtainPrivateBlock(pt.transferObject.description)
                        End If
                        If pt IsNot Nothing AndAlso pt.privateBlock IsNot Nothing Then
                            IO.File.WriteAllText(tmpXmlName, pt.privateBlock.xml.ToString)
                        Else
                            IO.File.WriteAllText(tmpXmlName, "")
                        End If
                        wbSource.Navigate(tmpXmlName)
                    End If


                Case "ciTransferHtml", "ciTransferText"
                    Dim ttp As New TransferTransactionPackage
                    ttp.transaction = TransferTransaction
                    If TransferTransaction.isPrivate Then
                        Dim pt As PrivateCoSignedTransferTransaction = TransferTransaction
                        ttp.privateBlock = C.obtainPrivateBlock(pt.transferObject.description)
                        ttp.correlateCompensations(C)
                    End If
                    If ci.Name = "ciTransferHtml" Then
                        wbSource.NavigateToString(utility.htmlPage(ttp.html()))
                    Else
                        IO.File.WriteAllText(tmpTxtName, ttp.plainText())
                        wbSource.Navigate(tmpTxtName)
                    End If
            End Select
        Else
            If wbSource IsNot Nothing Then
                wbSource.Navigate("about:blank")
            End If
        End If
    End Sub

    Private Sub wbSource_LoadCompleted(sender As Object, e As NavigationEventArgs) Handles wbSource.LoadCompleted
        wbSource.Document.charset = "utf-8"
        wbSource.Refresh()
    End Sub

    Private Sub TC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles TC.SelectionChanged
        If e.AddedItems.Count > 0 Then
            Dim ti As TabItem = e.AddedItems(0)
            If ti.Name = "tabCompensation" Then
            End If
        End If
    End Sub
End Class
