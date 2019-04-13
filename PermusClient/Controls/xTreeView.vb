Imports System.Collections.ObjectModel
Imports System.ComponentModel

Public Class xTreeView
    Inherits TreeView

    Public Event NuovoElementoSelezionato(Source As xTreeView, e As XElement)

    Public Sub New()
        Try
            ItemTemplate = Application.Current.FindResource("nodoConImmagine")
        Catch ex As Exception
        End Try
    End Sub

    Public Sub Rigenera(xDoc As XDocument, Optional SelectedNodeID As String = "P")
        Dim cn As New ObservableCollection(Of xNodo)
        For Each e As XElement In xDoc.Root.Elements
            Dim xn As xNodo = New xNodo(e)

            cn.Add(xn)
        Next

        Me.ItemsSource = Nothing
        Me.ItemsSource = cn

        If SelectedNodeID <> "" Then
            xNodo.SelezionaNodoByID(Me, SelectedNodeID)
        End If

    End Sub

    Public Sub Rigenera(xItems As String, Optional SelectedNodeID As String = "P")
        Dim xdoc As XDocument = XDocument.Parse(xItems)
        Rigenera(xdoc, SelectedNodeID)
    End Sub

    Private Sub xTreeView_SelectedItemChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles Me.SelectedItemChanged
        If e.NewValue IsNot Nothing Then
            Dim n As xNodo = e.NewValue
            RaiseEvent NuovoElementoSelezionato(Me, n.e)
        End If
    End Sub
End Class

Public Class xNodo
    Implements INotifyPropertyChanged
    Public e As XElement
    Public parent As xNodo
    Public path As String = ""
    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Public Sub New(_e As XElement)
        e = _e
    End Sub
    Public Sub Aggiorna(xd As XDocument, id As String)
        Dim xe As XElement = xd.Descendants().Where(Function(x) x.Attribute("id") = id).FirstOrDefault
        If xe IsNot Nothing Then
            Aggiorna(xe)
        End If
    End Sub

    Public Sub Aggiorna(_e As XElement)
        e = _e

        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("NodiDipendenti"))
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Label"))
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("Image"))
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("isExpanded"))
    End Sub

    Public Property isExpanded As Boolean
        Get
            If e.Attribute("expanded") IsNot Nothing Then
                Return e.Attribute("expanded").Value
            Else
                Return False
            End If
        End Get
        Set(value As Boolean)
            e.Attribute("expanded").Value = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs("isExpanded"))
        End Set
    End Property

    Public ReadOnly Property Image() As String
        Get
            If e.Attribute("image") IsNot Nothing Then
                Return "/Images/" & e.Attribute("image").Value
            Else
                Return "/Images/noimage.png"
            End If
        End Get
    End Property

    Public ReadOnly Property Label() As String
        Get
            Return e.Attribute("header").Value
        End Get
    End Property

    Public ReadOnly Property ToolTip() As String
        Get
            If e.Attribute("tooltip") IsNot Nothing Then
                Return e.Attribute("tooltip").Value
            Else
                Return ""
            End If
        End Get
    End Property

    Public ReadOnly Property name() As String
        Get

            If e.Attribute("name") IsNot Nothing Then
                Return e.Attribute("name").Value
            Else
                Return e.Name.LocalName
            End If
        End Get
    End Property

    Public ReadOnly Property id() As String
        Get
            If e.Attribute("id") IsNot Nothing Then
                Return e.Attribute("id").Value
            Else
                Return ""
            End If
        End Get
    End Property

    Public ReadOnly Property itemId() As String
        Get
            Return Me.name & "_" & Me.id
        End Get
    End Property

    Public ReadOnly Property NodiDipendenti() As ObservableCollection(Of xNodo)
        Get
            Dim cn As New ObservableCollection(Of xNodo)
            Dim xn As xNodo
            For Each ce As XElement In e.Elements
                xn = New xNodo(ce)
                xn.parent = Me
                cn.Add(xn)
            Next
            Return cn
        End Get
    End Property

    Public Shared Function CercaNodoByItemID(ID As String, Nodi As IEnumerable(Of xNodo)) As xNodo
        Dim n As xNodo
        For Each n In Nodi
            If n.itemId = ID Then
                Return n
            Else
                n = xNodo.CercaNodoByItemID(ID, n.NodiDipendenti.ToList)
                If n IsNot Nothing Then
                    Return n
                End If
            End If
        Next
        Return Nothing
    End Function

    Public Shared Function CercaTreeViewItemByID(ID As String, tv As xTreeView) As TreeViewItem
        Dim n As xNodo = xNodo.CercaNodoByItemID(ID, tv.ItemsSource)
        If n Is Nothing Then
            Return Nothing
        Else
            Return tv.ItemContainerGenerator.ContainerFromItem(n)
        End If
    End Function


    Public Shared Function FindTreeViewItem(tv As xTreeView, item As xNodo) As TreeViewItem
        Dim n As TreeViewItem = Nothing
        Dim d As xNodo
        tv.UpdateLayout()
        For Each d In tv.Items
            n = tv.ItemContainerGenerator.ContainerFromItem(d)
            If n IsNot Nothing Then
                If d Is item Then
                    Exit For
                End If

                n = FindTreeViewItemInChildren(n, item)

                If n IsNot Nothing Then
                    Exit For
                End If
            End If
        Next
        Return n
    End Function

    Public Shared Function FindTreeViewItemInChildren(parent As TreeViewItem, item As xNodo)
        If item Is Nothing Then
            Return Nothing
        Else
            Dim n As TreeViewItem = Nothing
            Dim d As xNodo
            Dim isExpanded As Boolean = parent.IsExpanded
            If Not isExpanded Then
                parent.IsExpanded = True
                parent.UpdateLayout()
            End If

            For Each d In parent.Items
                n = parent.ItemContainerGenerator.ContainerFromItem(d)
                If n IsNot Nothing And d.itemId = item.itemId Then
                    Exit For
                End If
                n = FindTreeViewItemInChildren(n, item)
                If n IsNot Nothing Then
                    Exit For
                End If
            Next
            If n Is Nothing And parent.IsExpanded <> isExpanded Then
                parent.IsExpanded = isExpanded
            End If
            If n IsNot Nothing Then
                parent.IsExpanded = True
            End If
            Return n

        End If
    End Function

    Public Shared Sub SelezionaNodoByID(tv As xTreeView, ID As String)
        If ID <> "" Then
            Dim n As xNodo = xNodo.CercaNodoByItemID(ID, tv.ItemsSource)
            Dim tvi As TreeViewItem = xNodo.FindTreeViewItem(tv, n)
            If tvi IsNot Nothing Then
                tvi.IsSelected = True
            End If
        End If

    End Sub
End Class

