Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.Data.SqlClient
Imports System.Data
Imports System.Web.Script.Services

' To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line.
' <System.Web.Script.Services.ScriptService()> _
<WebService(Namespace:="http://tempuri.org/")>
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)>
<ScriptService>
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class appservice
    Inherits System.Web.Services.WebService


    Public Shared Function getarray_color() As String()
        Dim arr_color(20) As String
        arr_color(0) = "#04478a"
        arr_color(1) = "#8a040a"
        arr_color(2) = "#8a045b"
        arr_color(3) = "#6f048a"
        arr_color(4) = "#31048a"
        arr_color(5) = "#04148a"
        arr_color(6) = "#045f8a"
        arr_color(7) = "#048a71"
        arr_color(8) = "#048a27"
        arr_color(9) = "#3e8a04"
        arr_color(10) = "#888a04"
        arr_color(11) = "#8a4804"
        arr_color(12) = "#8a1704"
        arr_color(13) = "#b32020"
        arr_color(14) = "#b32071"
        arr_color(15) = "#9620b3"
        arr_color(16) = "#3d20b3"
        arr_color(17) = "#2068b3"
        arr_color(18) = "#20b3a5"
        arr_color(19) = "#20b339"
        arr_color(20) = "#4bb320"
        Return arr_color
    End Function

    <WebMethod()>
    Public Function HelloWorld() As String
        Return "Hello World"
    End Function




    <WebMethod()>
    Public Shared Function CheckToken(Token As String) As String
        Dim BalGl As New BalGlobal
        Dim ds As New DataSet '= BalGl.getDataFromTable("tblToken", "count(tokenid) as Total", "Total", "where TokenID='" & Token & "' and isnull(Expired,0)=0", "asc")

        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand()
                Command.Connection = connection
                Command.CommandType = CommandType.Text
                Command.CommandText = "select count(tokenid) as Total from tblToken where TokenID='" & Token & "' and isnull(Expired,0)=0 "
                Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                sqlParameter2.Value = Token

                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using

                connection.Close()



            End Using
        End Using

        If ds.Tables(0).Rows.Count > 0 Then
            Dim Total As Integer = ds.Tables(0).Rows(0)("Total")
            If Total > 0 Then
                Return "Valid"
            Else
                Return "Invalid"
            End If
        Else
            Return "Invalid"
        End If

    End Function


    <WebMethod()>
    Public Function removetoken() As String
        Dim BalGlobal As New BalGlobal
        Dim token As String = ""
        If IsNothing(HttpContext.Current.Request.Cookies("token")) = False Then
            HttpContext.Current.Request.Cookies("token").Expires = DateTime.Now.AddDays(-1)

        End If
        Return ""
    End Function

    <WebMethod()>
    Public Function gettoken() As String
        Dim BalGlobal As New BalGlobal
        Dim token As String = ""
        If IsNothing(HttpContext.Current.Request.Cookies("token")) = False Then
            token = HttpContext.Current.Request.Cookies("token").Value
        End If
        Return token
    End Function

    <WebMethod()>
    Public Function getGroup(CategoryID As Integer, UserID As Integer) As String
        Dim BalGl As New BalGlobal
        Dim html As String = ""
        Dim arr_color As String() = getarray_color()
        Dim ds_group As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblGroup", "GroupID, GroupName, ImageUrl", "GroupName", "Where isnull(Deleted,0)=0 And CategoryID=" & CategoryID, "ASC")
        If ds_group.Tables(0).Rows.Count > 0 Then

            For i = 0 To ds_group.Tables(0).Rows.Count - 1
                Dim img_path As String = ds_group.Tables(0).Rows(i)("ImageUrl").ToString
                Dim GroupName As String = ds_group.Tables(0).Rows(i)("GroupName").ToString
                Dim Groupid As Integer = ds_group.Tables(0).Rows(i)("GroupID").ToString
                'html = html & "<div class='col-md-3 status-box-left crypto-ssl-left'>"
                html = html & "<div class='item-detail' >"
                html = html & "<div align='center' style='background-color: #FFFFFF;text-overflow: clip;overflow: hidden;'>"
                html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"



                img_path = "hdImage_all.ashx?ImagePath=" & HttpContext.Current.Server.MapPath(img_path) & "&width=300&height=200"

                'html = html & "<a href='pos.aspx?category=" & categoryid & "&group=" & Groupid & "&user=" & UserID & "'><img alt='' src='" & img_path & "' style='width:100%' /></a>"
                html = html & "<a onclick='viewSaleItem(" & CategoryID & "," & Groupid & "," & UserID & ")'>"
                Dim index As Integer = i - 20 * Int(i / 20)
                html = html & "<div style='border-radius:20px;text-align: center;padding:17px 0px;background-color: " & arr_color(index) & "; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div>"
                html = html & "</a>"

                html = html & "</div>"
                html = html & "</div>"
                html = html & "</div>"
            Next

        Else

            Dim ds_item As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblItem", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And CategoryID=" & CategoryID, "ASC")
            For j = 0 To ds_item.Tables(0).Rows.Count - 1
                Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
                Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
                Dim img_url As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString
                html = html & "<div class='item-detail'>"
                html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"

                img_url = "hdImage_all.ashx?ImagePath=" & HttpContext.Current.Server.MapPath(img_url) & "&width=300&height=200"

                html = html & "<a onclick='viewitem(" & CategoryID & ",0," & ItemID & "," & UserID & ")'>"
                Dim index As Integer = j - 20 * Int(j / 20)
                html = html & "<div style='text-align: center;padding:17px 0px;background-color: " & arr_color(index) & "; color: white; font-weight: bold;margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"

                html = html & "</a>"
                html = html & "</div>"
                html = html & "</div>"
            Next

        End If

        Return html
    End Function
    Public Function ConvertDataTabletoString(dt As DataTable) As String
        Dim serializer As System.Web.Script.Serialization.JavaScriptSerializer = New System.Web.Script.Serialization.JavaScriptSerializer()
        Dim rows As List(Of Dictionary(Of String, Object)) = New List(Of Dictionary(Of String, Object))()
        Dim row As Dictionary(Of String, Object)
        serializer.MaxJsonLength = Int32.MaxValue

        For Each dr As DataRow In dt.Rows
            row = New Dictionary(Of String, Object)()

            For Each col As DataColumn In dt.Columns
                row.Add(col.ColumnName, dr(col))
            Next

            rows.Add(row)
        Next

        Return serializer.Serialize(rows).Replace("<string xmlns=""http://tempuri.org/"">", "").Replace("</string>", "")

    End Function


    <WebMethod()>
    Public Function getSaleItem(CategoryID As Integer, GroupID As Integer, UserID As Integer) As String
        Dim arr_color As String() = getarray_color()
        Dim ds_item As DataSet = DalGlobal.getSaleItem(GroupID, CategoryID) 'BalGl.getDataFromTable("ViewSalePrice", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And GroupID=" & groupid, "ASC")
        Dim html As String = ""
        For j = 0 To ds_item.Tables(0).Rows.Count - 1
            Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
            Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
            Dim ItemCode As String = ds_item.Tables(0).Rows(j)("ItemCode")
            Dim img_url As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString

            Dim ItemPrice As String = ds_item.Tables(0).Rows(j)("Price")
            html = html & "<div class='item-detail'>"
            'html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
            html = html & "<div align='center'>"
            img_url = "hdImage_all.ashx?ImagePath=" & HttpContext.Current.Server.MapPath(img_url) & "&width=200&height=110"
            html = html & "<a onclick='viewitem(" & CategoryID & "," & GroupID & "," & ItemID & "," & UserID & ")'>"
            html &= "<img alt='' class='img-round' src='" & img_url & "' style='width:100%;' />"
            html &= "</a>"


            'html = html & "</div>"
            Dim index As Integer = j - 20 * Int(j / 20)
            html = html & "<div style='text-align: center;padding:17px 0px;background-color: " & arr_color(index) & "; color: white; font-weight: bold;margin-bottom:10px;height:60px;display:table; width:100%'><div style='vertical-align: middle; font-size: 13px;'>" & ItemCode & " " & ItemName & "</div>"
            html &= "<div>" & ItemPrice & "$</div>"
            html &= "</div>"
            'If j Mod 2 = 0 Then
            '    html = html & "<div style='text-align: center;padding:17px;background-color: #7f8183; color: white; font-weight: bold;margin-bottom:10px;height:80px;display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
            'Else
            '    html = html & "<div style='text-align: center;padding:17px;background-color: #3399cc; color: white; font-weight: bold;margin-bottom:10px;height:80px;display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"

            'End If

            html = html & "</a>"
            html = html & "</div>"
            html = html & "</div>"
        Next
        Return html
    End Function



End Class