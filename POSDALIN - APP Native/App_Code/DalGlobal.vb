Imports Microsoft.VisualBasic
Imports System.Data.SqlClient
Imports System.Data
Imports System.Web.UI.StateBag
Imports System.Net.Mail
Imports System.Net.NetworkCredential
Imports System.IO
Imports System.Security.Cryptography.X509Certificates
Imports System.Net.Security
Imports System.Drawing


Public Class DalGlobal
    Private Shared GlobalInstance As DalGlobal


    Public Shared ReadOnly Property Instance As DalGlobal
        Get
            If GlobalInstance Is Nothing Then
                GlobalInstance = New DalGlobal
            End If
            Return GlobalInstance
        End Get
    End Property


    Public Function ChangeToDate(ByVal DD_MM_YYYY As String) As Date
        Dim FormatDate As Date = DateSerial(Mid(DD_MM_YYYY, 7, 4), Mid(DD_MM_YYYY, 4, 2), Mid(DD_MM_YYYY, 1, 2))
        Return FormatDate
    End Function




    Public Function Seach(ByVal TableName As String, ByVal Field As String, ByVal Keyword As String, ByVal Choice As Integer, ByVal AdditionalCondition As String, ByVal DisplayFields As String) As DataSet
        Dim cn As New SqlConnection
        cn.ConnectionString = DalConnection.MainConnectionString
        cn.Open()
        Dim ds As New DataSet
        Dim condition As String = ""
        If Choice = 0 Then
            If Keyword = "*" Then
                condition = " where " & Field & " is not null " & AdditionalCondition
            Else
                condition = " where " & Field & " like N'%" & Keyword & "%' " & AdditionalCondition
            End If

        ElseIf Choice = 1 Then
            If Keyword = "*" Then
                condition = " where " & Field & " is not null " & AdditionalCondition
            Else
                condition = " where " & Field & " like N'" & Keyword & "%' " & AdditionalCondition
            End If

        ElseIf Choice = 2 Then
            If Keyword = "*" Then
                condition = " where " & Field & " is not null " & AdditionalCondition
            Else
                condition = " where " & Field & " like N'%" & Keyword & "' " & AdditionalCondition
            End If

        Else
            If Field = "ContactID" Or Field = "ClassID" Or Field = "SponsorID" Then
                If IsNumeric(Keyword) = False Then
                    Return Nothing
                    Exit Function
                End If

                condition = " where " & Field & " = " & Keyword & " " & AdditionalCondition
            Else
                If Keyword = "*" Then
                    condition = " where " & Field & " is not null " & AdditionalCondition
                Else
                    condition = " where " & Field & " = N'" & Keyword & "' " & AdditionalCondition
                End If

            End If

        End If

        Dim daa As New SqlDataAdapter("select " & DisplayFields & " from " & TableName & condition, cn)

        daa.Fill(ds)

        cn.Close()
        Return ds
    End Function

    Public Function GenerateID(ByVal TableName As String, ByVal Field As String) As Integer
        Dim cn As New SqlConnection
        cn.ConnectionString = DalConnection.MainConnectionString
        cn.Open()
        Dim ds As New DataSet

        Dim daa As New SqlDataAdapter("select Max(" & Field & ") as ExistingID from " & TableName, cn)
        daa.Fill(ds)
        cn.Close()
        Dim ID As Integer
        If ds.Tables(0).Rows.Count = 0 Then
            ID = 1
        Else
            If Not IsNumeric(ds.Tables(0).Rows(0)(0)) Then
                ID = 1
            Else
                ID = CInt(ds.Tables(0).Rows(0)(0)) + 1
            End If

        End If
        Return ID
    End Function

    Public Sub UpdateID(ByVal Field As String, ByVal NewID As Integer)

        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString
        '' ''... reset testid if it exceed 8999
        ''If NewID >= 9000 Then
        ''    NewID = 1000
        ''End If

        cmd.CommandText = " UPDATE tblReference SET " & Field & "=" & NewID

        cmd.Connection = cn 'assign the connection

        'execute the command
        cn.Open()
        cmd.ExecuteNonQuery()
        cn.Close()

    End Sub




    Public Function getBoundData(ByVal TableName As String, ByVal DisplayField As String, ByVal ValueField As String, ByVal NumberofRecords As Integer, ByVal Condition As String, ByVal SortOrder As String) As DataSet

        Dim ds As New DataSet
        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString

        Dim RecordNumber As String
        If NumberofRecords = 0 Then
            RecordNumber = " "
        Else
            RecordNumber = "Top(" & NumberofRecords & ") "
        End If

        cmd.CommandText = "SELECT Distinct " & RecordNumber & DisplayField & ", " & ValueField & " FROM " & TableName & " " & Condition & " ORDER BY " & DisplayField & " " & SortOrder


        cmd.CommandType = CommandType.Text
        cmd.Connection = cn
        adapter.SelectCommand = cmd
        'fill the dataset
        adapter.Fill(ds)
        'Add blank record to the datatable
        Dim dr As DataRow
        dr = ds.Tables(0).NewRow
        dr(DisplayField) = ""
        dr(ValueField) = 0
        ds.Tables(0).Rows.InsertAt(dr, 0)

        'return the dataset
        Return ds
    End Function



    Public Function getDataFromTable(ByVal ConnectionString As String, ByVal TableName As String, ByVal DisplayFields As String, ByVal SortField As String, ByVal Condition As String, ByVal SortOrder As String) As DataSet

        Dim ds As New DataSet
        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = ConnectionString

        cmd.CommandText = "SELECT " & DisplayFields & " FROM " & TableName & " " & Condition & " ORDER BY " & SortField & " " & SortOrder

        cmd.CommandType = CommandType.Text
        cmd.Connection = cn
        cn.Open()
        adapter.SelectCommand = cmd

        'fill the dataset
        adapter.Fill(ds)
        cn.Close()
        'return the dataset
        Return ds
    End Function

    Public Shared Function FixedSize(ByVal image As Image, ByVal Width As Integer, ByVal Height As Integer, ByVal needToFill As Boolean) As System.Drawing.Image
        '#Region "много арифметики"
        Dim sourceWidth As Integer = image.Width
        Dim sourceHeight As Integer = image.Height
        Dim sourceX As Integer = 0
        Dim sourceY As Integer = 0
        Dim destX As Double = 0
        Dim destY As Double = 0

        Dim nScale As Double = 0
        Dim nScaleW As Double = 0
        Dim nScaleH As Double = 0

        nScaleW = (CDbl(Width) / CDbl(sourceWidth))
        nScaleH = (CDbl(Height) / CDbl(sourceHeight))
        If Not needToFill Then
            nScale = Math.Min(nScaleH, nScaleW)
        Else
            nScale = Math.Max(nScaleH, nScaleW)
            destY = (Height - sourceHeight * nScale) / 2
            destX = (Width - sourceWidth * nScale) / 2
        End If

        'If nScale > 1 Then
        '    nScale = 1
        'End If

        Dim destWidth As Integer = CInt(Math.Round(sourceWidth * nScale))
        Dim destHeight As Integer = CInt(Math.Round(sourceHeight * nScale))
        '#End Region

        Dim bmPhoto As System.Drawing.Bitmap = Nothing
        Try
            bmPhoto = New System.Drawing.Bitmap(destWidth + CInt(Math.Round(2 * destX)), destHeight + CInt(Math.Round(2 * destY)))
        Catch ex As Exception
            Throw New ApplicationException(String.Format("destWidth:{0}, destX:{1}, destHeight:{2}, desxtY:{3}, Width:{4}, Height:{5}", destWidth, destX, destHeight, destY, Width,
                Height), ex)
        End Try
        Using grPhoto As System.Drawing.Graphics = System.Drawing.Graphics.FromImage(bmPhoto)
            'grPhoto.InterpolationMode = _interpolationMode
            'grPhoto.CompositingQuality = _compositingQuality
            'grPhoto.SmoothingMode = _smoothingMode

            grPhoto.InterpolationMode = Drawing2D.InterpolationMode.Default
            grPhoto.CompositingQuality = Drawing2D.CompositingQuality.Default
            grPhoto.SmoothingMode = Drawing2D.SmoothingMode.Default


            Dim [to] As Rectangle = New System.Drawing.Rectangle(CInt(Math.Round(destX)), CInt(Math.Round(destY)), destWidth, destHeight)
            Dim from As Rectangle = New System.Drawing.Rectangle(sourceX, sourceY, sourceWidth, sourceHeight)
            'Console.WriteLine("From: " + from.ToString());
            'Console.WriteLine("To: " + to.ToString());
            grPhoto.DrawImage(image, [to], from, System.Drawing.GraphicsUnit.Pixel)

            Return bmPhoto
        End Using
    End Function


    Public Sub UpdateValueInTable(ByVal TableName As String, ByVal UpdatingFieldNValue As String, ByVal Condition As String)

        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString

        cmd.CommandText = " UPDATE " & TableName & " SET " & UpdatingFieldNValue & " " & Condition

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

    Public Function InsertToTable(ByVal TableName As String, ByVal FieldsName As String, ByVal Values As String) As Integer

        Dim cn As New SqlConnection
        Dim cmd As New SqlCommand
        Dim adapter As New SqlDataAdapter
        cn.ConnectionString = DalConnection.MainConnectionString

        cmd.CommandText = " INSERT INTO " & TableName & " ( " & FieldsName & " ) Values (" & Values & ") SELECT SCOPE_IDENTITY();"

        cmd.Connection = cn 'assign the connection

        'execute the command
        cn.Open()
        Dim ID As Integer = 0
        Dim obj = cmd.ExecuteScalar()
        If Not IsNothing(obj) And Not IsDBNull(obj) Then
            ID = CInt(obj.ToString)
        End If
        cn.Close()
        Return ID
    End Function

    Public Function ConvertStringToNumber(ByVal obj As Object) As String
        If Not IsNumeric(obj) Then
            Return 0
        Else
            Return obj
        End If
    End Function
    ''.. this function is for breaking a string into fragments based on a defined separator, and then put them into an array
    Public Function Cut_String_into_Array(ByVal MainString As String, ByVal Separator As String) As Array
        Dim StrArray(0) As String
        Dim ind As Integer
        Dim i As Integer = 1
        Dim j As Integer = 0
        Do While i = 1
            ind = InStr(MainString, Separator, CompareMethod.Text)
            ReDim Preserve StrArray(j)
            If ind = 0 Then
                StrArray(j) = MainString
                i = 0 ''.. to leave the loop
            Else
                StrArray(j) = MainString.Substring(0, ind - 1)
                MainString = MainString.Substring(ind, MainString.Length - ind)
            End If
            j = j + 1
        Loop

        Return StrArray
    End Function
    ''.. this function is to sort datatable based on the assinged columnkey and its sort order
    Public Function AlphabeticSort(ByVal dtTable As DataTable, ByVal columnkey As String, ByVal sortOrder As Integer) As DataTable
        Dim dsSorted As New DataSet
        Dim sortDirection As String = ""
        Dim sortFormat As String = "{0} {1}"
        Select Case sortOrder
            Case 0
                sortDirection = "ASC"
            Case 1
                sortDirection = "DESC"
            Case Else
                sortDirection = "ASC"
        End Select
        dtTable.DefaultView.Sort = String.Format(sortFormat, columnkey, sortDirection)
        Return dtTable.DefaultView.Table
    End Function



    Public Function ValidPhone(ByVal PhoneNumber As String) As Boolean
        Try

            Dim dsPrefix As DataSet = getDataFromTable(DalConnection.MainConnectionString, "tlkpEmailNPhone", "Prefix", "Prefix", "Where Type='Phone'", "ASC")


            Dim isValid As Boolean = False
            For i = 0 To dsPrefix.Tables(0).Rows.Count - 1
                Dim Suffix As String = dsPrefix.Tables(0).Rows(i)("Prefix")
                Dim LenString As Integer = Len(Suffix)
                If Left(PhoneNumber, LenString) = Suffix Then
                    If PhoneNumber.ToString.Length = 6 + 3 OrElse PhoneNumber.ToString.Length = 7 + 3 Then
                        isValid = True
                        Return isValid
                        Exit Function
                    End If
                End If
            Next

            Return isValid
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Function ValidEmail(ByVal Email As String) As Boolean
        Try
            Dim dsSuffix As DataSet = getDataFromTable(DalConnection.MainConnectionString, "tlkpEmailNPhone", "Suffix", "Suffix", "Where Type='Email'", "ASC")


            Dim isValid As Boolean = False
            For i = 0 To dsSuffix.Tables(0).Rows.Count - 1
                Dim Suffix As String = dsSuffix.Tables(0).Rows(i)("Suffix")
                Dim LenString As Integer = Len(Suffix)
                If Right(Email, LenString) = Suffix Then
                    If Email.ToString.Contains("@") = True Then
                        isValid = True
                        Return isValid
                        Exit Function
                    End If
                End If
            Next

            Return isValid
        Catch ex As Exception
            Return False
        End Try

    End Function

    Public Function EnscriptSpam(ByVal Spam As String) As String
        Try
            Spam = Spam.Replace("a", "XXXkstXXX")
            Spam = Spam.Replace("u", "YYYmnsYYY")
            Spam = Spam.Replace("o", "||||||||||")
            Spam = Spam.Replace("i", "$$$$$$$$$$")
            Spam = Spam.Replace("e", "__________")
            Spam = Spam.Replace(".", "%%%%%Kg*****")

            Return Spam
        Catch ex As Exception
            Return ""
        End Try

    End Function


    Public Shared Function CreateTokenApp(Username As String, Token As String) As String
        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_CreateTokenApp", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@UserName", SqlDbType.VarChar)
                sqlParameter0.Value = Username
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                sqlParameter1.Value = Token

                connection.Open()
                Using da As New SqlDataAdapter(Command)

                    da.Fill(ds)
                End Using
                connection.Close()

                Return ds.Tables(0).Rows(0)("TokenID").ToString()

            End Using
        End Using

    End Function


    Public Shared Function CreateToken(Username As String, isapp As String) As String
        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_CreateTokenID", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@UserName", SqlDbType.VarChar)
                sqlParameter0.Value = Username
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@isapp", SqlDbType.VarChar)
                sqlParameter1.Value = isapp

                connection.Open()
                Using da As New SqlDataAdapter(Command)

                    da.Fill(ds)
                End Using
                connection.Close()

                Return ds.Tables(0).Rows(0)("TokenID").ToString()

            End Using
        End Using

    End Function

    Public Shared Function getSaleItem(GroupID As Integer, CategoryID As Integer) As DataSet
        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_getSaleItem_Client2", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@GroupID", SqlDbType.Int)
                sqlParameter0.Value = GroupID
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@CategoryID", SqlDbType.Int)
                sqlParameter1.Value = CategoryID

                connection.Open()
                Using da As New SqlDataAdapter(Command)

                    da.Fill(ds)
                End Using
                connection.Close()

                Return ds

            End Using
        End Using

    End Function


    Public Shared Function getOrderList(PaymentType As String, StartDate As String, EndDate As String, OrderID As Integer) As DataSet
        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_getOrderList", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@PaymentType", SqlDbType.VarChar)
                sqlParameter0.Value = PaymentType
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@StartDate", SqlDbType.VarChar)
                sqlParameter1.Value = StartDate
                Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@EndDate", SqlDbType.VarChar)
                sqlParameter2.Value = EndDate
                Dim sqlParameter3 As SqlParameter = Command.Parameters.Add("@OrderID", SqlDbType.Int)
                sqlParameter3.Value = OrderID

                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using
                connection.Close()

                Return ds

            End Using
        End Using

    End Function
    Public Shared Function getOrder(MemberID As Integer) As DataSet
        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_getOrder_v2", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@MemberID", SqlDbType.Int)
                sqlParameter0.Value = MemberID

                connection.Open()
                Using da As New SqlDataAdapter(Command)

                    da.Fill(ds)
                End Using
                connection.Close()

                Return ds

            End Using
        End Using

    End Function

    Public Function DescriptSpam(ByVal Spam As String) As String
        Try
            Spam = Spam.Replace("XXXkstXXX", "a")
            Spam = Spam.Replace("YYYmnsYYY", "u")
            Spam = Spam.Replace("||||||||||", "o")
            Spam = Spam.Replace("$$$$$$$$$$", "i")
            Spam = Spam.Replace("__________", "e")
            Spam = Spam.Replace("%%%%%Kg*****", ".")
            Return Spam
        Catch ex As Exception
            Return ""
        End Try

    End Function

    Public Sub MySendEmail(ByVal Subject As String, ByVal Body As String, ByVal Recipient As String, ByVal AttachmentPath As String)
        ''Dim mail As MailMessage = New MailMessage()
        ''mail.To.Add(Recipient)
        ''mail.From = New MailAddress("ace.notification@gmail.com")
        ''mail.Subject = Subject
        ''mail.Body = Body
        ''If AttachmentPath <> "" Then
        ''    Dim att As New Attachment(AttachmentPath)
        ''    mail.Attachments.Add(att)
        ''End If

        ''mail.IsBodyHtml = True
        ''Dim smtp As SmtpClient = New SmtpClient()
        ''smtp.Host = "smtp.gmail.com"
        ''smtp.Credentials = New System.Net.NetworkCredential("ace.notification@gmail.com", "pass$1234")
        ''smtp.EnableSsl = True
        ''smtp.Port = 587
        ''smtp.EnableSsl = True
        ''smtp.Send(mail)



        Dim mail As MailMessage = New MailMessage()
        Dim cc As New MailAddress("sopheak.nuon@acecambodia.org")
        Dim cc1 As New MailAddress("chanpheakdey.vong@acecambodia.org")
        mail.To.Add(Recipient)
        mail.From = New MailAddress("ace.edb@acecambodia.org")
        mail.CC.Add(cc)
        'mail.CC.Add("sopheak.nuon@acecambodia.org")
        mail.CC.Add(cc1)
        mail.Subject = Subject
        mail.Body = Body
        mail.IsBodyHtml = True
        Dim smtp As SmtpClient = New SmtpClient()
        'smtp.Host = "203.217.170.20"
        smtp.Host = "192.168.0.253"
        smtp.Credentials = New System.Net.NetworkCredential("ace.edb@acecambodia.org", "pass$1234")
        smtp.EnableSsl = False
        smtp.Port = 25
        ''smtp.EnableSsl = True
        smtp.Send(mail)



        ''Dim mail As MailMessage = New MailMessage()
        ''mail.To.Add(Recipient)
        ''mail.From = New MailAddress("sothea.korng@acecambodia.org")
        ''mail.Subject = Subject
        ''mail.Body = Body
        ''mail.IsBodyHtml = True
        ''Dim smtp As SmtpClient = New SmtpClient()
        ''smtp.Host = "192.168.0.253"
        ''smtp.Credentials = New System.Net.NetworkCredential("sothea.korng@acecambodia.org", "pass$1234")
        ''smtp.EnableSsl = True
        ''smtp.Port = 25
        ''smtp.EnableSsl = True
        ''smtp.Send(mail)
    End Sub

    Public Sub RemoveSubMenu(ByVal TargetMenu As Menu, ByVal MainMenu As String, ByVal SubMenu As String)
        Dim valuePath1 As String = MainMenu
        Dim musicMenuItem As MenuItem = TargetMenu.FindItem(valuePath1)
        If musicMenuItem IsNot Nothing Then
            Dim valuePath2 As String = SubMenu
            Dim item As MenuItem = TargetMenu.FindItem(valuePath2)
            If item IsNot Nothing Then
                musicMenuItem.ChildItems.Remove(item)
            End If
        End If
    End Sub

    Public Function DefaultPass(ByVal input As String) As String

        Dim TempPassword As String
        Dim CurrentMinute As String = Minute(Now())
        Dim CurrentHour As String = Hour(Now())
        TempPassword = LCase(Left(input, 1) & Right(input, 1) & CurrentMinute & Left(input, 2) & "." & CurrentHour)

        Return TempPassword


    End Function

#Region "Get DateTime from sql server"
    ''' <summary>
    ''' Get Current date and time from server
    ''' </summary>
    ''' <param name="sqlTran"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function GetCurrentDate(ByVal sqlTran As SqlTransaction) As DateTime
        Dim datReturn As New DateTime()
        Dim sqlConn As New SqlConnection
        Try
            Dim strSQL As String = "SELECT GETDATE()" & vbCr
            sqlConn.ConnectionString = DalConnection.MainConnectionString
            Dim sqlcom As SqlCommand = sqlConn.CreateCommand()
            sqlcom.CommandText = strSQL
            sqlcom.Transaction = sqlTran
            Dim obj As Object = sqlcom.ExecuteScalar()
            If obj IsNot Nothing AndAlso Not IsDBNull(obj) Then
                datReturn = DateTime.Parse(obj.ToString())
            End If
        Catch exp As Exception
            Return Now()
        End Try
        Return datReturn
    End Function
#End Region

    ''' <summary>
    ''' Execute a stored procedure with parameters and return the results as a DataSet.
    ''' Usage:
    '''   Dim ds = Instance.GetDataSet("Sp_GetSomething", New SqlParameter() { New SqlParameter("@Id", 5) })
    ''' </summary>
    Public Function GetDataSet(ByVal ConnectionString As String, ByVal storedProcName As String, Optional ByVal parameters As SqlParameter() = Nothing) As DataSet
        Dim ds As New DataSet()
        Using connection As New SqlConnection(ConnectionString)
            Using cmd As New SqlCommand(storedProcName, connection)
                cmd.CommandType = CommandType.StoredProcedure

                If parameters IsNot Nothing Then
                    For Each p As SqlParameter In parameters
                        If p IsNot Nothing Then
                            cmd.Parameters.Add(p)
                        End If
                    Next
                End If

                connection.Open()
                Using da As New SqlDataAdapter(cmd)
                    da.Fill(ds)
                End Using
            End Using
        End Using

        Return ds
    End Function

End Class