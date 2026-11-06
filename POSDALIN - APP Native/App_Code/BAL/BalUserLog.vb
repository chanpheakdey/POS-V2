Imports Microsoft.VisualBasic
Imports System

Public Class BalUserLog
    Public Function GetUserLog(ByVal UserLog As ClUserLog) As Data.DataSet
        Return DalUserLog.Instance.GetUserLog(UserLog)
    End Function



    Public Sub UpdateUserLog(ByVal UserLog As ClUserLog)
        DalUserLog.Instance.UpdateUserLog(UserLog)
    End Sub

    Public Sub AddUserLog(ByVal UserLog As ClUserLog)
        DalUserLog.Instance.AddUserLog(UserLog)
    End Sub
End Class
