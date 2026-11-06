Imports Microsoft.VisualBasic

Public Class ClStock

    Public StockID As Integer
    Public ItemID As Integer
    Public Qty As Double
    Public TranType As String
    Public TranDate As DateTime
    Public CreatedUserID As Integer
    Public CreatedDate As DateTime
    Public Comment As String
    Public CurrentStock As Double
    Public NewCurrentStock As Double
    'Add New Field
    Public ExpiredStock As Boolean

End Class