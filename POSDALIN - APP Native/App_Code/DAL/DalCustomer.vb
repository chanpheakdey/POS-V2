Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data

Public Class DalCustomer
    Function UpdateCustomer(clCustomer As ClCustomer, CurPoint As Integer, ChangePoint As Integer, DesPoint As String) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblCustomer1 set CustomerName=@A1, " &
                                  " Phone=@A2, Email=@A3, Address=@A4, CarIn=@A5, CurrentPoint=@A6 " &
                                  " where CustomerID=@A7"
            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clCustomer.Name
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = clCustomer.Mobile
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clCustomer.Email
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clCustomer.Address
            cmd.Parameters.Add("@A5", SqlDbType.NVarChar).Value = clCustomer.CarIn
            cmd.Parameters.Add("@A6", SqlDbType.Int).Value = CurPoint
            cmd.Parameters.Add("@A7", SqlDbType.Int).Value = clCustomer.ID


            cn.Open()
            cmd.ExecuteNonQuery()
            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function



    Function AddCustomer(clCustomer As ClCustomer, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblCustomer1 (CustomerName, Phone, Email, Address, Deleted, CurrentPoint)" &
                " Values(@A1, @A2, @A3, @A4, @A5, @A6)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clCustomer.Name
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = clCustomer.Mobile
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clCustomer.Email
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clCustomer.Address
            cmd.Parameters.Add("@A5", SqlDbType.Bit).Value = False
            cmd.Parameters.Add("@A6", SqlDbType.Int).Value = 0

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

    Function ChangePoint(ChangedPoint As Integer, Description As String, CustomerID As Integer, ByRef NewID As Integer) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Insert into tblChangePoint1 (ChangePoint, Description, ChangeDate, CustomerID)" &
                " Values(@A1, @A2, @A3, @A4)"


            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.Int).Value = ChangedPoint
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = Description
            cmd.Parameters.Add("@A3", SqlDbType.DateTime).Value = Now
            cmd.Parameters.Add("@A4", SqlDbType.Int).Value = CustomerID

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
