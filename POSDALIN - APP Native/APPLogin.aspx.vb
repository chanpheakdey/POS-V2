Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Net.Mail
Imports System.Net.NetworkCredential
Imports System.Web.Services
Imports System.Net.HttpResponseHeader
Partial Class _Default
    Inherits System.Web.UI.Page


    Private Sub UpdateUserAttempt(Username As String)
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand()
                Command.Connection = connection
                Command.CommandText = "update tblUser set Attempts=Attempts+1 Where Username=@UserName"
                Command.CommandType = CommandType.Text
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@UserName", SqlDbType.VarChar)
                sqlParameter1.Value = Username
                connection.Open()
                Command.ExecuteNonQuery()
                connection.Close()

            End Using
        End Using

    End Sub

    Private Sub UpdateUserLocked(Username As String)
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand()
                Command.Connection = connection
                Command.CommandText = "update tblUser set Locked=1 Where Username=@UserName"
                Command.CommandType = CommandType.Text
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@UserName", SqlDbType.VarChar)
                sqlParameter1.Value = Username
                connection.Open()
                Command.ExecuteNonQuery()
                connection.Close()

            End Using
        End Using

    End Sub

    Protected Sub btnSignIn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSignIn.Click

        lblErrAll.InnerText = ""
        lblErrAll.Visible = False

        If txtUserName.Text.Trim = String.Empty Then
            lblErrUserName.Visible = True
            txtUserName.BorderColor = Drawing.Color.Red
            Exit Sub
        Else
            lblErrUserName.Visible = False
            txtUserName.BorderColor = Drawing.Color.Black
        End If

        Dim BalUsr As New BalUser
        Dim ClUsr As New ClUser

        If txtPassword.Text.Trim = String.Empty Then
            lblErrPassword.Visible = True
            txtPassword.BorderColor = Drawing.Color.Red
            Exit Sub
        Else
            lblErrPassword.Visible = False
            txtPassword.BorderColor = Drawing.Color.Black
        End If

        Dim BalGlobal As New BalGlobal

        Dim ds As New Data.DataSet
        ClUsr.UserName = txtUserName.Text
        ClUsr.Password = txtPassword.Text

        'ds = BalGlobal.getDataFromTable("tblUser", "*", "UserName", "Where UserName='" & txtUserName.Text & "'", "asc")
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand()
                Command.Connection = connection
                Command.CommandText = "select * from tblUser Where UserName=@UserName And UserLevel='Customer'"
                Command.CommandType = CommandType.Text
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@UserName", SqlDbType.VarChar)
                sqlParameter1.Value = txtUserName.Text.Trim
                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using
                connection.Close()

            End Using
        End Using

        If ds.Tables(0).Rows.Count = 0 Then
            lblErrAll.InnerText = "User name is incorrect!"
            lblErrAll.Visible = True
            txtUserName.Focus()
            'HyperChangePassword.Enabled = False
            lblInfo.Text = "Login Failed!"
            Exit Sub
        Else
            If ds.Tables(0).Rows(0)("Locked") = True Then
                'lblInfo.Text = "Your user account has been locked!"
                lblInfo.Text = "Your user account has been locked!" & vbCrLf & "Please contact IT"
                'lblInfo.Text = lblInfo.Text & vbCrLf & "or Logistics office (dial 108 for SP, 103 for SM)."
                lblInfo.ForeColor = Drawing.Color.Red
                Exit Sub
            End If
            If ds.Tables(0).Rows(0)("Password") <> txtPassword.Text Then
                lblErrAll.InnerText = "Password is incorrect!"
                lblErrAll.Visible = True
                txtPassword.Focus()
                'HyperChangePassword.Enabled = False
                lblInfo.Text = "Login Failed!"
                lblInfo.ForeColor = Drawing.Color.Red

                UpdateUserAttempt(txtUserName.Text.Trim)
                If CType(ds.Tables(0).Rows(0)("Attempts"), Integer) >= 5 Then
                    ClUsr.Locked = True
                    UpdateUserLocked(txtUserName.Text.Trim)
                    lblInfo.Text = "Your account is locked!"

                End If

                Exit Sub
            End If

            lblInfo.Text = "Login Successful!"

            'BalUsr.UpdateUserAttempt(ClUsr, True)
            Dim isapp As String = Request.QueryString("app")

            isapp = "1"
            Dim token As String = Request.QueryString("token")
            If IsNothing(token) Then
                token = ""
            Else
                token = "app-" & token
            End If
            Dim tokenstatus As String = ""
            If token = "" Then
                tokenstatus = "Invalid"
            Else
                tokenstatus = appservice.CheckToken(token)
            End If
            If tokenstatus <> "Valid" Then

                token = DalGlobal.CreateTokenApp(ClUsr.UserName, token)
            End If
            Response.Cookies("token").Value = token
            Response.Cookies("token").Expires = DateAdd(DateInterval.Day, 7, Now.Date)

            Response.Cookies("apptype").Value = "new"
            Response.Cookies("apptype").Expires = DateAdd(DateInterval.Day, 7, Now.Date)


            Session(ds.Tables(0).Rows(0)("userID") & "LogIn") = True
                Session("app" + token) = True
                Session(token) = True

                Dim url2 As String = ""

            If ds.Tables(0).Rows(0)("UserLevel").ToString = "Customer" Then
                url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
            ElseIf ds.Tables(0).Rows(0)("UserLevel").ToString = "Staff" Then
                url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
            Else
                url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
            End If
                Response.Redirect(url2)

            End If
    End Sub


    Protected Sub HyperChangePassword0_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles HyperChangePassword0.Click

        Dim BalGlobal As New BalGlobal
        Dim Spam As String = BalGlobal.EnscriptSpam(txtUserName.Text)
        Response.Redirect("ChangePassword.aspx?Spam=" & Spam & "&Date=" & BalGlobal.GetServerCurrentDate())

    End Sub

    Protected Sub btnAccept_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnAccept.Click
        Dim BalGlobal As New BalGlobal
        Dim Spam As String = BalGlobal.EnscriptSpam(txtUserName.Text)
        Dim url2 As String = "SearchContact.aspx?Spam=" & Spam & "&LogIn=1&Now=" & BalGlobal.GetServerCurrentDate()
        Response.Redirect(url2)
    End Sub






    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Page.IsPostBack = False Then
            Session("app") = True
            'Dim userInfo As HttpCookie = New HttpCookie("userInfo")
            Dim BalGlobal As New BalGlobal
            Dim token As String = ""

            token = Request.QueryString("token")
            If IsNothing(token) Then
                If IsNothing(Request.Cookies("token")) = False Then
                    token = Request.Cookies("token").Value
                Else
                    token = ""
                End If
            Else
                Response.Cookies("token").Value = token

            End If


            'Exit Sub
            'If IsNothing(Request.Cookies("token")) = False Then
            '    token = Request.Cookies("token").Value
            'End If
            'userInfo("UserName") = "Annathurai"
            'userInfo("UserColor") = "Black"
            'userInfo.Expires.Add(New TimeSpan(0, 1, 0))
            'Response.Cookies.Add(userInfo)

            Dim tokenstatus As String
            If token = "" Then
                tokenstatus = "Invalid"
            Else
                tokenstatus = appservice.CheckToken(token)
            End If
            If tokenstatus = "Valid" Then
                If IsNothing(Request.Cookies("token")) = True Then
                    Response.Cookies("token").Value = token
                    Response.Cookies("token").Expires = DateAdd(DateInterval.Day, 7, Now.Date)
                End If


                Session("app" + token) = True
                Session(Request.QueryString("user") & "LogIn") = True
                Session(token) = True
                Response.Cookies("apptype").Value = "new"
                Response.Cookies("apptype").Expires = DateAdd(DateInterval.Day, 7, Now.Date)

                'Response.Redirect("Dashboard.aspx?user=1&Now=" & Now.ToString)
                Dim ds As New Data.DataSet

                'ds = BalGlobal.getDataFromTable("tblUser", "UserID,UserLevel,UserName", "UserName", "Where UserName in (select UserName from tblToken where TokenID='" & token & "')", "asc")
                Using connection As New SqlConnection(DalConnection.MainConnectionString)
                    Using Command As New SqlCommand()
                        Command.Connection = connection
                        Command.CommandType = CommandType.Text
                        Command.CommandText = "select UserID,UserLevel,UserName from tblUser Where UserName in (select UserName from tblToken where TokenID='" & token & "')"
                        Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                        sqlParameter2.Value = token

                        connection.Open()
                        Using da As New SqlDataAdapter(Command)
                            da.Fill(ds)
                        End Using

                        connection.Close()
                    End Using
                End Using

                Dim url2 As String = ""
                Dim isapp As String = Request.QueryString("app")
                If ds.Tables(0).Rows(0)("UserLevel").ToString = "Customer" Then
                    url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
                ElseIf ds.Tables(0).Rows(0)("UserLevel").ToString = "Staff" Then
                    url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
                Else
                    url2 = "userorder.aspx?user=" & ds.Tables(0).Rows(0)("UserID") & IIf(isapp = "1", "&app=1", "") & "&Now=" & BalGlobal.GetServerCurrentDate()
                End If

                Dim userid As Integer = ds.Tables(0).Rows(0)("UserID").ToString
                If isapp = "1" Then
                    Session(userid & "app") = True
                Else
                    Session(userid & "app") = False
                End If
                Response.Redirect(url2)
            End If
        End If
        txtUserName.Focus()
        MultiView1.ActiveViewIndex = 0
    End Sub

    Protected Sub HyperReport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles HyperReport.Click
        Try
            Dim BalGlobal As New BalGlobal
            Dim Subject As String = "Suspicion Notification"
            Dim Body As String = ""
            Body = Body & vbCrLf & "From: " & txtUserName.Text
            Body = Body & vbCrLf & "Pre Log in: " & txtPreviousLogIn.Text
            Body = Body & vbCrLf & "Current Log in: " & txtCurrentLogIn.Text
            Body = Body & vbCrLf & "Report Date: " & BalGlobal.GetServerCurrentDate()

            Dim Recipient As String = "sothea.korng@acecambodia.org"
            BalGlobal.MySendEmail(Subject, Body, Recipient, "")

            ''BalGlobal.MySendEmail(Subject, Body, Recipient, Server.MapPath("MidTermReport/kk.pdf"))
            lblInfoReport.Text = "Message is sent."
            lblInfoReport.Visible = True
            lblInfoReport.ForeColor = Drawing.Color.Blue
            MultiView1.ActiveViewIndex = 1
        Catch ex As Exception
            lblInfoReport.Text = "There was an error. " & ex.Message
            lblInfoReport.Visible = True
            lblInfoReport.ForeColor = Drawing.Color.Red
            MultiView1.ActiveViewIndex = 1
        End Try
    End Sub
End Class
