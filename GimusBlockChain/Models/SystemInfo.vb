Public Class SystemInfo
    Public Sub New()
    End Sub
    Public Function Clone() As SystemInfo
        Dim si As New SystemInfo
        si.blockMasterReady = Me.blockMasterReady
        si.blockChainVersion = Me.blockChainVersion
        si.certificationAuthorities = Me.certificationAuthorities
        si.currentBlockSerial = Me.currentBlockSerial
        si.currentTransactionSerial = Me.currentTransactionSerial
        si.maxTransactionsPerBlock = Me.maxTransactionsPerBlock
        Return si
    End Function

    Public Property blockMasterReady As Boolean
    Public Property blockChainVersion As String
    Public Property currentBlockSerial As Long
    Public Property currentTransactionSerial As Integer
    Public Property maxTransactionsPerBlock As Integer
    Public Property certificationAuthorities As String
    Public Property requesterInfo As UserInfo
    Public Property otherUserInfo As UserInfo
    Public Property currentTimeStamp As Long
End Class

Public Class UserInfo
    Public Property userId As String
    Public Property isOnline As Boolean
    Public Property userOutgoingPendingTransfersCount As Integer
    Public Property userIncomingPendingTransfersCount As Integer
    Public Property token As String
    Public Property coinBalance As Double
End Class
