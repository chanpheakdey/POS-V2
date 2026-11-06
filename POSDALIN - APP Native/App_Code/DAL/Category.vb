Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalCategory
    Function UpdateCategory(ID As Integer, Name As String) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblCategory set CategoryName=@A1 " &
                                  " where CategoryID=@A3"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Name
            'cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = Img
            cmd.Parameters.Add("@A3", SqlDbType.Int).Value = ID


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function


    Function AddCategory(Name As String, Img As String, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblCategory (CategoryName, ImageUrl, Deleted)" &
                " Values(@A1, @A2, @A3)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Name
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = Img
            cmd.Parameters.Add("@A3", SqlDbType.Bit).Value = False

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
