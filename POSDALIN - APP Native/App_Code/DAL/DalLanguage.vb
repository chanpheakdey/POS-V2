Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data
Public Class DalLanguage

    Function UpdateLanguage(clLanguage As ClLanguage) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblLanguage set Language=@A1, " & _
                                  " Visible=@A2 " & _
                                  " where LanguageID=@A3"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clLanguage.Language
            cmd.Parameters.Add("@A2", SqlDbType.Bit).Value = clLanguage.Visible
            cmd.Parameters.Add("@A3", SqlDbType.Int).Value = clLanguage.LanguageID

            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function



    Function AddLanguage(clLanguage As ClLanguage, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblLanguage (Language, Visible)" & _
                " Values(@A1, @A2)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clLanguage.Language
            cmd.Parameters.Add("@A2", SqlDbType.Bit).Value = clLanguage.Visible


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
