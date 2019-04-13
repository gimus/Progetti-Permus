Imports System.Text
Imports Permus

Public Class BlockDetail
    Inherits commandableControl
    Dim tmpXmlName As String = IO.Path.Combine(IO.Path.GetTempPath(), "tmp.xml")

    Public Sub New()
        InitializeComponent()
        Me.DataContext = Nothing
    End Sub

    Public Property block As Block
        Get
            Return DataContext
        End Get
        Set(value As Block)
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
        If (e.AddedItems.Count > 0 And wbSource IsNot Nothing And block IsNot Nothing) Then
            Dim ci As ComboBoxItem = e.AddedItems(0)
            Select Case ci.Name
                Case "ciPublicBlock"
                    IO.File.WriteAllText(tmpXmlName, block.xml.ToString)
                    wbSource.Navigate(tmpXmlName)
            End Select
        Else
            If wbSource IsNot Nothing Then
                wbSource.Navigate("about:blank")
            End If
        End If
    End Sub
End Class
