Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalExpense

    Function AddExpense(clExpense As ClExpense, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblExpense1 (Item, ExpenseDate, Amount, Currency, CreatedBy, CreatedDate, Comment)" &
                " Values(@A1, @A2, @A3, @A4, @A5, @A6, @A7)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clExpense.Item
            cmd.Parameters.Add("@A2", SqlDbType.DateTime).Value = clExpense.ExpenseDate
            cmd.Parameters.Add("@A3", SqlDbType.Float).Value = clExpense.Amount
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clExpense.Currency
            cmd.Parameters.Add("@A5", SqlDbType.Int).Value = clExpense.CreatedBy
            cmd.Parameters.Add("@A6", SqlDbType.DateTime).Value = Now
            cmd.Parameters.Add("@A7", SqlDbType.NVarChar).Value = clExpense.Comment

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

    Function UpdateExpense(clExpense As ClExpense) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblExpense1 set Item=@A1, ExpenseDate=@A2, " &
                                  " Amount=@A3, Currency=@A4, Comment=@A5 " &
                                  " where ID=@A6"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clExpense.Item
            cmd.Parameters.Add("@A2", SqlDbType.DateTime).Value = clExpense.ExpenseDate
            cmd.Parameters.Add("@A3", SqlDbType.Float).Value = clExpense.Amount
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clExpense.Currency
            cmd.Parameters.Add("@A5", SqlDbType.NVarChar).Value = clExpense.Comment
            cmd.Parameters.Add("@A6", SqlDbType.Int).Value = clExpense.Id


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function

End Class
