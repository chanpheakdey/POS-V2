Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data
Public Class ChangePassword
    Function UpdatePassword(Content As String) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblUser1 set Password=@A1"

            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Content

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
