Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalGroup
    Function UpdateGroup(ID As Integer, Name As String, CatID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblGroup set GroupName=@A1, CategoryID=@A2 " &
                                  " where GroupID=@A3"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Name
            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = CatID
            cmd.Parameters.Add("@A3", SqlDbType.Int).Value = ID


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()

            UpdateItemCategoryinGroup(ID, CatID)
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try



        Return False
    End Function

    Function UpdateItemCategoryinGroup(ID As Integer, CatID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblItem set CategoryID=@A2 " &
                                  " where GroupID=@A3"
            cmd.Connection = cn

            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = CatID
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





    Function AddGroup(Name As String, CatID As String, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblGroup (GroupName, CategoryID, Deleted)" &
                " Values(@A1, @A2, @A3)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = Name
            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = CatID
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
