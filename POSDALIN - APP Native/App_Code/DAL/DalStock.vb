Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalStock

    Function AddStock(clStock As ClStock, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString

            cmd.CommandText = "Insert into tblStock1 (ItemID, Qty, TranType, TranDate, CreatedUserID, CreatedDate, Comment, CurrentStock, NewCurrentStock, ExpiredStock)" &
                " Values(@A1, @A2, @A3, @A4, @A5, @A6, @A7, @A8, @A9, @A10)"

            'cmd.CommandType = CommandType.StoredProcedure
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.Int).Value = clStock.ItemID
            cmd.Parameters.Add("@A2", SqlDbType.Float).Value = clStock.Qty
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clStock.TranType
            cmd.Parameters.Add("@A4", SqlDbType.DateTime).Value = clStock.TranDate
            cmd.Parameters.Add("@A5", SqlDbType.Int).Value = clStock.CreatedUserID
            cmd.Parameters.Add("@A6", SqlDbType.DateTime).Value = Now
            cmd.Parameters.Add("@A7", SqlDbType.NVarChar).Value = clStock.Comment
            cmd.Parameters.Add("@A8", SqlDbType.Float).Value = clStock.CurrentStock
            cmd.Parameters.Add("@A9", SqlDbType.Float).Value = clStock.NewCurrentStock
            'Add New Field
            cmd.Parameters.Add("@A10", SqlDbType.Bit).Value = clStock.ExpiredStock

            cn.Open()
            cmd.ExecuteNonQuery()

            cmd.CommandText = "SELECT @@IDENTITY"
            Dim obj = cmd.ExecuteScalar

            If Not IsNothing(obj) And Not IsDBNull(obj) Then
                NewID = CInt(obj.ToString)
            End If

            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function

End Class