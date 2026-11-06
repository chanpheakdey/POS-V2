Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalSupplier
    Function UpdateSupplier(clSupplier As ClSupplier) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblSupplier1 set Name=@A1, Company=@A2, " &
                                  " Mobile=@A3, Email=@A4, Address=@A5, Country=@A6 " &
                                  " where ID=@A7"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clSupplier.Name
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = clSupplier.Company
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clSupplier.Mobile
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clSupplier.Email
            cmd.Parameters.Add("@A5", SqlDbType.NVarChar).Value = clSupplier.Address
            cmd.Parameters.Add("@A6", SqlDbType.NVarChar).Value = clSupplier.Country
            cmd.Parameters.Add("@A7", SqlDbType.Int).Value = clSupplier.ID


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function



    Function AddSupplier(clSupplier As ClSupplier, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblSupplier1 (Name, Company, Mobile, Email, Address, Country, Deleted)" &
                " Values(@A1, @A2, @A3, @A4, @A5, @A6, @A7)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clSupplier.Name
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = clSupplier.Company
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clSupplier.Mobile
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clSupplier.Email
            cmd.Parameters.Add("@A5", SqlDbType.NVarChar).Value = clSupplier.Address
            cmd.Parameters.Add("@A6", SqlDbType.NVarChar).Value = clSupplier.Country
            cmd.Parameters.Add("@A7", SqlDbType.Bit).Value = False

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
