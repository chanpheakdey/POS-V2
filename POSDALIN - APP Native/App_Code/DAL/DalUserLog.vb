Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data
Imports System.Reflection
Imports System

Public Class DalUserLog
    Private Shared UserLogInstance As DalUserLog

    Public Shared ReadOnly Property Instance As DalUserLog
        Get
            If UserLogInstance Is Nothing Then
                UserLogInstance = New DalUserLog
            End If
            Return UserLogInstance
        End Get
    End Property

    Public Sub AddUserLog(ByVal UserLog As ClUserLog)
        Dim AllField As String = ""
        Dim AllValue As String = ""

        AllField = AllField & "LogInID, "
        AllValue = AllValue & "@LogInID, "

        AllField = AllField & "UserName, "
        AllValue = AllValue & "@UserName, "

        AllField = AllField & "TimeIn, "
        AllValue = AllValue & "@TimeIn, "

        AllField = AllField & "ClientAddress "
        AllValue = AllValue & "@ClientAddress "


        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString


        cmd.CommandText = " Insert into tblUserLog(" & AllField & ") Values (" & AllValue & "); "
        cmd.Connection = cn 'assign the connection

            'add the parameters
        With UserLog
            cmd.Parameters.Add("@LogInID", SqlDbType.Int).Value = .LogInID
            cmd.Parameters.Add("@TimeIn", SqlDbType.DateTime).Value = .TimeIn
            cmd.Parameters.Add("@UserName", SqlDbType.Char).Value = .UserName
            cmd.Parameters.Add("@ClientAddress", SqlDbType.Char).Value = .ClientAddress
        End With
        'execute the command
        cn.Open()
        cmd.ExecuteNonQuery()
        cn.Close()



    End Sub

    Public Function GetUserLog(ByVal UserLog As ClUserLog) As DataSet
        Dim ds As DataSet = DalGlobal.Instance.getDataFromTable(DalConnection.MainConnectionString, "tblUserLog", "*", "UserLogID", "Where UserName='" & UserLog.UserName & "'", "asc")
        Return ds
    End Function


    Public Sub UpdateUserLog(ByVal UserLog As ClUserLog)
        Dim AllFieldNValue As String = ""

        AllFieldNValue = AllFieldNValue & "LogInID=@LogInID, "

        AllFieldNValue = AllFieldNValue & "UserName=@UserName, "

        AllFieldNValue = AllFieldNValue & "TimeIn=@TimeIn, "

        AllFieldNValue = AllFieldNValue & "TimeOut=@TimeOut, "

        AllFieldNValue = AllFieldNValue & "Failed=@Failed "


        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString

        cmd.CommandText = " UPDATE tblUserLog SET " & AllFieldNValue & " Where UserName='" & UserLog.UserName & "'"
        cmd.Connection = cn 'assign the connection

        'add the parameters
        With UserLog
            cmd.Parameters.Add("@LogInID", SqlDbType.Int).Value = .LogInID
            cmd.Parameters.Add("@TimeIn", SqlDbType.DateTime).Value = .TimeIn
            cmd.Parameters.Add("@TimeOut", SqlDbType.DateTime).Value = .TimeOut
            cmd.Parameters.Add("@UserName", SqlDbType.Char).Value = .UserName
            cmd.Parameters.Add("@Failed", SqlDbType.Bit).Value = .Failed
        End With

        'execute the command
        cn.Open()
        cmd.ExecuteNonQuery()
        cn.Close()

    End Sub
End Class
