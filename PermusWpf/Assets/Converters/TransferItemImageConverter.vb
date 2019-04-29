Imports Permus
Friend Class TransferItemImageConverter

    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements IValueConverter.Convert
        Try
            If value Is Nothing Then
                Return "/PermusWpf;component/Images/noimage.png"
            Else
                Dim ti As TransferTransaction = value
                Select Case ti.GetType()
                    Case GetType(CoinCreation)
                        Return "/PermusWpf;component/Images/TransferTransactions/CoinTransfer.png"
                    Case GetType(CoinTransfer)
                        Return "/PermusWpf;component/Images/TransferTransactions/CoinTransfer.png"
                    Case GetType(PrivateCompensation)
                        Return "/PermusWpf;component/Images/TransferTransactions/Compensation.png"
                    Case GetType(PrivateSale)
                        Return "/PermusWpf;component/Images/TransferTransactions/PrivateSale.png"
                    Case GetType(PublicSale)
                        Return "/PermusWpf;component/Images/TransferTransactions/PublicSale.png"
                    Case GetType(PrivateTransfer)
                        Return "/PermusWpf;component/Images/TransferTransactions/PrivateTransfer.png"
                    Case Else
                        Return "/PermusWpf;component/Images/transferElement.png"
                End Select
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