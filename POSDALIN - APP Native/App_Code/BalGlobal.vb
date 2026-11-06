Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Data.SqlClient
Imports System
Imports System.Drawing

Public Class BalGlobal
    Public Function InsertToTable(ByVal TableName As String, ByVal FieldsName As String, ByVal Values As String) As Integer
        Return DalGlobal.Instance.InsertToTable(TableName, FieldsName, Values)
    End Function
    Public Sub UpdateValueInTable(ByVal TableName As String, ByVal UpdatingFieldNvalue As String, ByVal Condition As String)
        DalGlobal.Instance.UpdateValueInTable(TableName, UpdatingFieldNvalue, Condition)
    End Sub

    Public Function GetDataSet(ByVal ConnectionString As String, ByVal storedProcName As String, Optional ByVal parameters As SqlParameter() = Nothing) As DataSet
        Return DalGlobal.Instance.GetDataSet(ConnectionString, storedProcName, parameters)
    End Function
    Public Function ChangeToDate(ByVal dd_MM_yyyy As String) As Date
        Return DalGlobal.Instance.ChangeToDate(dd_MM_yyyy)
    End Function
    Public Function FixedSize(ByVal image As Image, ByVal Width As Integer, ByVal Height As Integer, ByVal needToFill As Boolean) As System.Drawing.Image
        Return DalGlobal.FixedSize(image, Width, Height, needToFill)
    End Function

    Public Function Seach(ByVal TableName As String, ByVal Field As String, ByVal Keyword As String, ByVal Choice As Integer, ByVal AdditionalCondition As String, ByVal DisplayFields As String) As DataSet
        Return DalGlobal.Instance.Seach(TableName, Field, Keyword, Choice, AdditionalCondition, DisplayFields)
    End Function
    Public Function GenerateID(ByVal TableName As String, ByVal Field As String) As Integer
        Return DalGlobal.Instance.GenerateID(TableName, Field)
    End Function
    Public Sub UpdateID(ByVal Field As String, ByVal NewID As Integer)
        DalGlobal.Instance.UpdateID(Field, NewID)
    End Sub

    Public Function getBoundData(ByVal TableName As String, ByVal DisplayField As String, ByVal ValueField As String, ByVal NumberofRecords As Integer, ByVal Condition As String, ByVal SortOrder As String) As DataSet
        Return DalGlobal.Instance.getBoundData(TableName, DisplayField, ValueField, NumberofRecords, Condition, SortOrder)
    End Function
    Public Function getDataFromTable(ByVal ConnectionString As String, ByVal TableName As String, ByVal DisplayFields As String, ByVal SortField As String, ByVal Condition As String, ByVal SortOrder As String) As DataSet

        Return DalGlobal.Instance.getDataFromTable(ConnectionString, TableName, DisplayFields, SortField, Condition, SortOrder)
    End Function
    Public Function ConvertStringToNumber(ByVal obj As Object) As String
        Return DalGlobal.Instance.ConvertStringToNumber(obj)
    End Function
    Public Function Cut_String_into_Array(ByVal MainString As String, ByVal Separator As String) As Array
        Return DalGlobal.Instance.Cut_String_into_Array(MainString, Separator)
    End Function
    Public Function AlphabeticSort(ByVal dtTable As DataTable, ByVal columnkey As String, ByVal sortOrder As Integer) As DataTable
        Return DalGlobal.Instance.AlphabeticSort(dtTable, columnkey, sortOrder)
    End Function

    Public Function EnscriptSpam(ByVal Spam As String) As String
        Return DalGlobal.Instance.EnscriptSpam(Spam)
        'Return Cryption.EncryptString(Spam)
    End Function
    Public Function DescriptSpam(ByVal Spam As String) As String
        Return DalGlobal.Instance.DescriptSpam(Spam)
        'Return Cryption.DecryptString(Spam)
    End Function
    Public Sub MySendEmail(ByVal Subject As String, ByVal Body As String, ByVal Recipient As String, ByVal AttachmentPath As String)
        DalGlobal.Instance.MySendEmail(Subject, Body, Recipient, AttachmentPath)
    End Sub
    Public Sub RemoveSubMenu(ByVal TargetMenu As Menu, ByVal MainMenu As String, ByVal SubMenu As String)
        DalGlobal.Instance.RemoveSubMenu(TargetMenu, MainMenu, SubMenu)
    End Sub
    Public Function DefaultPass(ByVal input As String) As String
        Return DalGlobal.Instance.DefaultPass(input)
    End Function

    Public Function GetServerCurrentDate() As DateTime
        Return DalGlobal.GetCurrentDate(Nothing)
    End Function

    Public Function ValidPhone(ByVal PhoneNumber As String) As Boolean
        Return DalGlobal.Instance.ValidPhone(PhoneNumber)
    End Function

    Public Function ValidEmail(ByVal Email As String) As Boolean
        Return DalGlobal.Instance.ValidEmail(Email)
    End Function

End Class
