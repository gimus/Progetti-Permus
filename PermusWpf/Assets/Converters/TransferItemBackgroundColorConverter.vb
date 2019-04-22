Imports Permus
Friend Class TransferItemBackgroundColorConverter

        Implements IValueConverter

        Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.Convert
            Try
                If value Is Nothing Then
                Return Colors.Transparent
            Else
                Dim ti As TransferTransaction = value
                If ti.isCompensation Then
                    Return "Blue"
                Else
                    If ti.compensation = 0 Then
                        Return "Red"
                    Else
                        If ti.compensation >= 100 Then
                            Return "Green"
                        Else
                            Return "Orange"
                        End If
                    End If
                End If
            End If
            Catch ex As Exception
                Return " "
            End Try
            Return parameter
        End Function

        Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return value
        End Function

    End Class