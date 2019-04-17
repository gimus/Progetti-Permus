Imports Permus
Friend Class SubjectImageConverter

    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Try
            If value Is Nothing Then
                Return "/PermusClient;component/Images/noimage.png"
            Else
                Dim s As Subject = value
                If s.isAuthority Then
                    Return "/PermusClient;component/Images/bank.png"
                Else
                    If s.isPublic Then
                        Return "/PermusClient;component/Images/public.png"
                    Else
                        Return "/PermusClient;component/Images/user.png"
                    End If
                End If
            End If
        Catch ex As Exception
            Return ""
        End Try
        Return parameter
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
        Return value
    End Function

End Class