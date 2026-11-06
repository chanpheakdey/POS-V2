Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data
Imports System.Reflection
Imports System
Imports System.Activities.Expressions

Public Class DalUser
    Private Shared UserInstance As DalUser

    Public Shared ReadOnly Property Instance As DalUser
        Get
            If UserInstance Is Nothing Then
                UserInstance = New DalUser
            End If
            Return UserInstance
        End Get
    End Property

    Function UpdateUser(clUser As ClUser) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            cn.ConnectionString = DalConnection.MainConnectionString
            cmd.CommandText = "Update tblUser1 set UserName=@A1, Password=@A2, SecurityGroup=@A3 where AutoID=@A4"

            cmd.Connection = cn

            cmd.Parameters.Add("@A1", SqlDbType.NVarChar).Value = clUser.UserName
            cmd.Parameters.Add("@A2", SqlDbType.NVarChar).Value = clUser.Password
            cmd.Parameters.Add("@A3", SqlDbType.NVarChar).Value = clUser.SecurityGroup
            cmd.Parameters.Add("@A4", SqlDbType.NVarChar).Value = clUser.UserID

            cn.Open()
            cmd.ExecuteNonQuery()



            cn.Close()
        Catch ex As Exception
            'MsgBox(ex.Message)
            Return True
        End Try

        Return False
    End Function


    Public Function AddUser(ByVal User As ClUser) As Boolean
        Try
            Dim cn As New SqlConnection
            Dim cmd As New SqlCommand
            Dim adapter As New SqlDataAdapter
            cn.ConnectionString = DalConnection.MainConnectionString

            cmd.CommandText = " Insert into tblUser1(Username,Password,SecurityGroup,CreatedDate,CreatedBy,Locked,InUse)" &
                " Values(@A1,@A2,@A3,@A4,@A5)"
            cmd.Connection = cn 'assign the connection

            'add the parameters
            With User
                cmd.Parameters.Add("@A1", SqlDbType.Char).Value = .UserName
                cmd.Parameters.Add("@A2", SqlDbType.Char).Value = .Password
                cmd.Parameters.Add("@A3", SqlDbType.Char).Value = .SecurityGroup
                cmd.Parameters.Add("@A4", SqlDbType.DateTime).Value = Today
                cmd.Parameters.Add("@A5", SqlDbType.Char).Value = .CreatedBy
                cmd.Parameters.Add("@A6", SqlDbType.Char).Value = False
                cmd.Parameters.Add("@A7", SqlDbType.Char).Value = True

            End With

            'execute the command
            cn.Open()
            cmd.ExecuteNonQuery()

            'cmd.CommandText = "SELECT @@IDENTITY"
            'Dim obj = cmd.ExecuteScalar

            'If Not IsNothing(obj) And Not IsDBNull(obj) Then
            '    NewID = CInt(obj.ToString)
            'End If

            cn.Close()
        Catch ex As Exception
            Return True
        End Try
        Return False
    End Function

    Public Function GetUser(Username As String) As DataSet
        Dim ds As DataSet = DalGlobal.Instance.getDataFromTable(DalConnection.MainConnectionString, "tblUser1", "*", "UserName", "Where UserName='" & Username & "'", "asc")
        Return ds
    End Function



    Public Sub UpdateUserInUse(ByVal User As ClUser)
        Dim InuseValue As Integer
        If User.InUser = True Then
            InuseValue = 1
        Else
            InuseValue = 0
        End If
        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString

        cmd.CommandText = "UPDATE TOP (200) tblUser1 Set InUse = " & InuseValue & " WHERE (UserName = N'" & User.UserName & "')"

        cmd.Connection = cn 'assign the connection

        'execute the command
        cn.Open()
        'Try
        cmd.ExecuteNonQuery()
        'Catch ex As Exception
        '    MsgBox(cmd.CommandText)
        'End Try
        cn.Close()

    End Sub



End Class
