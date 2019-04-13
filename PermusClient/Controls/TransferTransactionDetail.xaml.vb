Imports System.Text
Imports Permus

Public Class transferTransactionDetail
    Inherits commandableControl
    Dim tmpXmlName As String = IO.Path.Combine(IO.Path.GetTempPath(), "tmp.xml")
    Dim tmpTxtName As String = IO.Path.Combine(IO.Path.GetTempPath(), "tmp.txt")

    Public Sub New()
        InitializeComponent()
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
        cbXMLMode.SelectedIndex = pi
    End Sub


    Public Overrides Sub newCommand()
        Select Case cmd
        End Select
    End Sub

    Private Sub TC_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)

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
End Class
