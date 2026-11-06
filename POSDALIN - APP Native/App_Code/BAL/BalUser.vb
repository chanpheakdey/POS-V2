Imports Microsoft.VisualBasic
Imports System

Public Class BalUser
    Public Function GetUser(ByVal User As ClUser) As Data.DataSet
        'Return DalUser.Instance.GetUser(User)
    End Function



    Public Sub UpdateUserPassword(ByVal User As ClUser)
        'DalUser.Instance.UpdateUserPassword(User)
    End Sub

    Public Sub UpdateUserInUse(ByVal User As ClUser)
        DalUser.Instance.UpdateUserInUse(User)
    End Sub


    Public Sub AddUser(ByVal User As ClUser)
        DalUser.Instance.AddUser(User)
    End Sub
End Class
