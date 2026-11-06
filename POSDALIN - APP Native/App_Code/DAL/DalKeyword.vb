Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalKeyword

    Function UpdateKeyword(clKeyword As ClKeyword) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblKeyword set Keyword=@A1, LanguageID=@A2 " & _
                                                                    " where KeywordID=@A3"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clKeyword.Keyword
            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = clKeyword.LanguageID
            cmd.Parameters.Add("@A3", SqlDbType.Int).Value = clKeyword.KeywordID

            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function



    Function AddKeyword(clKeyword As ClKeyword, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblKeyword (Keyword, LanguageID)" & _
                " Values(@A1, @A2)"


            cmd.Connection = cn


            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clKeyword.Keyword
            cmd.Parameters.Add("@A2", SqlDbType.Int).Value = clKeyword.LanguageID

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
