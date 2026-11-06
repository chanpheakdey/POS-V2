Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalExchangeRate
    Function UpdateRate(ID As Integer, Rate As String) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblExchangeRate1 set DollarToReil=@A1 " &
                                  " where ExchangeRateID=@A2"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Rate
            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = ID


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function


    Function AddRate(Rate As String, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblExchangeRate1 (DollarToReil)" &
                " Values(@A1)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Rate


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
