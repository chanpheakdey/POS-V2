Imports System
Imports System.Activities.Expressions
Imports System.Data
Imports System.Data.SqlClient
Imports System.Data.SqlTypes
Imports System.Drawing
Imports System.IO
Imports System.Net
Imports System.Security.Policy
Imports System.Web.Services
Imports System.Windows.Documents
Imports External.NPOI.HSSF
Imports Microsoft.Build.Framework.XamlTypes

Partial Class POS
    Inherits System.Web.UI.Page

    Private UploadDir As String




    <WebMethod()>
    Public Shared Sub LogOut()
        Dim balGlobal As New BalGlobal
        Dim token As String = ""
        If IsNothing(HttpContext.Current.Request.Cookies("token")) = False Then
            token = HttpContext.Current.Request.Cookies("token").Value
        End If

        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("SP_RemoveToken", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                sqlParameter2.Value = token

                connection.Open()
                Command.ExecuteNonQuery()
                connection.Close()



            End Using
        End Using

        'balGlobal.UpdateValueInTable("tblToken", "Expired=1,ExpiredDate=getdate()", "Where tokenid='" & token & "'")
        HttpContext.Current.Request.Cookies("token").Expires = DateTime.Now.AddDays(-1)

        'Session(token) = False

    End Sub









    Public Shared Function TelegramSendMessage(ByVal apilToken As String, ByVal Chatid As String, ByVal text As String) As String
        ServicePointManager.Expect100Continue = True
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        'https://api.telegram.org/bot7875798180:AAF2kXrRxIiLuMN7N_TxRBwnT2pqcVgCVgg/sendMessage?chat_id=891867503&parse_mode=MarkdownV2&text=test
        Dim urlString As String = "https://api.telegram.org/bot" & apilToken & "/sendMessage?chat_id=" & Chatid & "&parse_mode=MarkdownV2&text=" & text
        Dim webclient As WebClient = New WebClient()
        Return webclient.DownloadString(urlString)
    End Function


    Public Class ClCategory
        Public Property CategoryID As Integer
        Public Property CategoryName As String
    End Class

    <WebMethod()>
    Public Shared Function getcategory() As List(Of ClCategory)
        Dim BalGl As New BalGlobal
        Dim ds_category As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblCategory", "*", "CategoryID", "where isnull(Deleted, 0) = 0", "ASC")

        Dim html As String = ""
        html &= "<table>"
        html &= "<tr>"
        Dim lst As New List(Of ClCategory)
        For i = 0 To ds_category.Tables(0).Rows.Count - 1
            Dim clCat As New ClCategory

            Dim categoryName As String = ds_category.Tables(0).Rows(i)("CategoryName").ToString
            Dim categoryid As Integer = ds_category.Tables(0).Rows(i)("CategoryID").ToString
            clCat.CategoryID = categoryid
            clCat.CategoryName = categoryName
            lst.Add(clCat)


        Next
        Return lst
    End Function

    <WebMethod()>
    Public Shared Function getitemunit(ItemID As Integer) As List(Of ClSalePrice)
        Dim BalGl As New BalGlobal
        Dim lst As New List(Of ClSalePrice)
        'Dim dsSaler As DataSet = BalGl.getDataFromTable("ViewSalePrice", "ID,FromQty,ToQty, Price, Currency", "ID ", "Where isnull(Deleted,0)=0 And ItemID=" & ItemID, "asc")

        Dim dsSaler As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_getItemUnit1", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter0 As SqlParameter = Command.Parameters.Add("@ItemID", SqlDbType.Int)
                sqlParameter0.Value = ItemID

                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(dsSaler)
                End Using
                connection.Close()
            End Using
        End Using

        For i = 0 To dsSaler.Tables(0).Rows.Count - 1
            Dim clSale As New ClSalePrice
            clSale.ID = dsSaler.Tables(0).Rows(i)("ID")
            clSale.FromQty = dsSaler.Tables(0).Rows(i)("FromQty")
            clSale.ToQty = dsSaler.Tables(0).Rows(i)("ToQty")
            clSale.Price = dsSaler.Tables(0).Rows(i)("Price")
            clSale.Currency = dsSaler.Tables(0).Rows(i)("Currency")
            lst.Add(clSale)
        Next

        Return lst
    End Function


    <WebMethod()>
    Public Shared Function getitemdetail(SaleID As Integer, ItemID As Integer, ItemName As String, ItemCode As String, ImageUrl As String, ItemPrice As String, Currency As String) As String
        Dim BalGl As New BalGlobal
        'Dim dsItem As DataSet = BalGl.getDataFromTable("tblItem", "*", "ItemID", "Where ItemID=" & ItemID, "asc")

        Dim html As String = "<div class='modal-body' style='padding:10px'>"
        html &= "<div>"
        html &= "<div>"


        html &= "<table style = 'width:100%' >"
        html &= "<tbody><tr>"
        html &= "<td colspan='2'>"
        html &= "<div>"

        Dim img_url As String = "/" & ImageUrl.Replace("^", "/") & "?" & Now.ToString("ddMMyyyyHHmmss")

        html &= "<div style='text-align:center'>"
        html &= "<img id='img_item' class='img-item' src='" & img_url & "'>"
        html &= "</div>"
        html &= "</td>"
        html &= "</tr>"
        html &= "<tr style='height:50px'>"
        html &= "<td>ទំនិញ:</td>"
        html &= "<td>"
        html &= "<span>" & ItemName & "</span>"
        html &= "</td>"
        html &= "</tr>"

        html &= "<tr style='height:50px'>"
        html &= "<td>"
        html &= "តម្លៃ:</td>"
        html &= "<td>"
        html &= "<span class='item-price'>" & ItemPrice & Currency & "</span>"
        html &= "<span class='item-price' style='margin-left:20px;'> សរុប: <span id='itemtotal'>" & ItemPrice & "</span><span>" & Currency & "</span></span>"
        html &= "</td>"
        html &= "</tr>"

        html &= "<tr style='height:50px'>"
        html &= "<td>ចំនួន:</td><td>"
        html &= "<div style='height: 25px;float: left;margin-right:10px;' onclick='add_qty(-1)'><img src='images/logo/subtract-color-icon.png' style='height: 100%;'></div>"
        html &= "<div style='min-width: 40px;height: 25px;float: left;font-size: x-large;text-align: center;'><span id='itemqty'>1</span></div>"
        html &= "<div style='height: 25px;float: left;margin-left:10px;' onclick='add_qty(1)'><img src='images/logo/addition-color-icon.png' style='height: 100%;'></div>"
        html &= "</td>"
        html &= "</tr>"

        html &= "<tr style='height:50px'>"
        html &= "<td></td><td>"
        html &= "<div class='item-qty' onclick='select_qty(1)'>1</div>"
        html &= "<div class='item-qty' onclick='select_qty(2)'>2</div>"
        html &= "<div class='item-qty' onclick='select_qty(5)'>5</div>"
        html &= "<div class='item-qty' onclick='select_qty(10)'>10</div>"
        html &= "<div class='item-qty' onclick='select_qty(20)'>20</div>"
        html &= "<div class='item-qty' onclick='select_qty(50)'>50</div>"
        html &= "<div class='item-qty' onclick='select_qty(100)'>100</div>"
        html &= "</td>"
        html &= "</tr>"

        html &= "<tr style='height:50px'>"
        html &= "<td colspan='2'>"
        html &= "<div style='text-align:center'>"
        html &= "<input type='button' value='បញ្ចូល' class='button-add' onclick='additem()' />"

        html &= "</div>"



        html &= "</td>"
        html &= "</tr>"


        html &= "</tbody></table>"
        html &= "</div>"
        html &= "</div>"
        html &= "</div>"

        Return html
    End Function

    <WebMethod()>
    Public Shared Function getitems(CategoryID As Integer, GroupID As Integer) As String

        Dim ds_item As DataSet = DalGlobal.getSaleItem(GroupID, CategoryID) 'BalGl.getDataFromTable("ViewSalePrice", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And GroupID=" & groupid, "ASC")
        Dim html As String = ""
        For j = 0 To ds_item.Tables(0).Rows.Count - 1
            Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
            Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
            Dim ItemCode As String = ds_item.Tables(0).Rows(j)("ItemCode")
            Dim img_url As String = "/" & ds_item.Tables(0).Rows(j)("ImageUrl").ToString
            Dim ImageUrl As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString.Replace("/", "^")
            Dim ItemPrice As String = ds_item.Tables(0).Rows(j)("Price")
            Dim Currency As String = ds_item.Tables(0).Rows(0)("Currency").ToString
            Dim SaleID As Integer = ds_item.Tables(0).Rows(j)("ID")

            html = html & "<div class='item-detail'>"
            'html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
            html = html & "<div align='center'>"
            html = html & "<a onclick=""select_item(" & SaleID & "," & ItemID & ",'" & ItemName & "','" & ItemCode & "','" & ImageUrl & "'," & ItemPrice & ",'" & Currency & "')"">"

            html &= "<img class='img_item_thumnail' alt='' class='img-round' src='" & img_url & "' style='width:100%;' />"
            html &= "</a>"


            'html = html & "</div>"
            Dim index As Integer = j - 20 * Int(j / 20)

            html = html & "<div class='item-detail-textbox'><div class='item-detail-text' id='divitemname" & ItemID & "'>" & ItemCode & " " & ItemName & "</div>"
            html &= "<div>" & ItemPrice & Currency & "</div>"
            html &= "</div>"


            html = html & "</a>"
            html = html & "</div>"
            html = html & "</div>"
        Next

        Return html
    End Function



    Public Class ClProductClient
        Public ItemID As Integer
        Public ItemName As String
        Public ItemCode As String
        Public ImageUrl As String
        Public Price As Double
        Public Currency As String
        Public GroupName As String
        Public ProductID As Integer
        Public ProductName As String
        Public SupplierID As Integer
        Public CategoryID As Integer
        Public GroupID As Integer
        Public Type As String
        Public Color As String
        Public Image As String
        Public UnitPrice As Double
        Public CurrencyUnitPrice As String
        Public ProductCode As String
        Public CurrentQty As Integer
        Public SetLowStock As Integer
        Public PriceUp1 As Double
        Public Invisible As Boolean
        Public Invisible1 As Boolean
        Public SaleID As Integer
        Public CategoryName As String

    End Class

    <WebMethod()>
    Public Shared Function getitemsbycategory(CategoryID As Integer, UserLevel As String) As List(Of ClProductClient)

        Dim lst As New List(Of ClProductClient)

        'Return lst
        Dim ds_item As DataSet = DalGlobal.getSaleItem(0, CategoryID) 'BalGl.getDataFromTable("ViewSalePrice", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And GroupID=" & groupid, "ASC")
        Dim html As String = ""

        Dim running_groupname As String = ""
        For j = 0 To ds_item.Tables(0).Rows.Count - 1
            Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
            Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
            Dim ItemCode As String = ds_item.Tables(0).Rows(j)("ItemCode")
            'Dim img_url As String = "Uploads/resized/" & ItemID & ".jpg" '"/" & ds_item.Tables(0).Rows(j)("ImageUrl").ToString
            'Dim img_url As String = "Uploads/98.jpg" 'ds_item.Tables(0).Rows(j)("ImageUrl").ToString
            'Dim ImageUrl As String = "Uploads/98.jpg" 'ds_item.Tables(0).Rows(j)("ImageUrl").ToString.Replace("/", "^")
            Dim img_url As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString
            Dim ImageUrl As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString.Replace("/", "^")
            Dim ItemPrice As String = ds_item.Tables(0).Rows(j)("Price")
            Dim Currency As String = ds_item.Tables(0).Rows(0)("Currency").ToString
            Dim SaleID As Integer = ds_item.Tables(0).Rows(j)("ID")
            Dim GroupName As String = ds_item.Tables(0).Rows(j)("GroupName").ToString
            Dim GroupID As Integer = ds_item.Tables(0).Rows(j)("GroupID")
            Dim ItemCategoryID As Integer = ds_item.Tables(0).Rows(j)("CategoryID")
            Dim CategoryName As String = ds_item.Tables(0).Rows(j)("CategoryName").ToString

            Dim clProduct As New ClProductClient
            clProduct.ItemID = ItemID
            clProduct.ItemName = ItemName
            clProduct.ItemCode = ItemCode
            clProduct.ImageUrl = img_url
            clProduct.Price = ItemPrice

            clProduct.Currency = Currency
            clProduct.GroupName = GroupName
            clProduct.GroupID = GroupID
            clProduct.CategoryID = ItemCategoryID
            clProduct.CategoryName = CategoryName
            clProduct.SaleID = SaleID
            lst.Add(clProduct)


            'If running_groupname <> GroupName Then
            '    If running_groupname <> "" AndAlso (UserLevel = "Admin" OrElse UserLevel = "Staff") Then
            '        html = html & "</div>"
            '    End If


            '    If (UserLevel = "Admin" OrElse UserLevel = "Staff") Then
            '        html = html & "<div class='item-groupname' onclick='showitemingroup(" & GroupID & ")'><table style='width:100%'><tr><td>" & GroupName & "</td><td style='text-align:right;'><span id='span_arrow_" & GroupID & "'><i class='fa fa-arrow-down' style='font-size: 28px;'></i></span></td></tr></table></div>"
            '        html = html & "<div id='divgroup_" & GroupID & "' style='display:none;'>"
            '    Else
            '        html = html & "<div class='item-groupname'><table style='width:100%'><tr><td>" & GroupName & "</td><td style='text-align:right;'></td></tr></table></div>"
            '    End If

            '    running_groupname = GroupName
            'End If

            'html &= "<a onclick=""orderitem(" & SaleID & "," & ItemID & ",'" & ItemName & "','" & ItemCode & "','" & ImageUrl & "'," & ItemPrice & ",'" & Currency & "')"">"
            'html &= "<table style='width: 100%;'>"
            'html &= "<tr><td style='width :  20vw;'>"
            'html &= "<div class='item-detail'>"
            'html &= "<img class='img_item_thumnail' src='" & img_url.Replace("^", "/") & "' />"
            'html &= "</div></td><td><div class='item-detail-textbox'>"
            'html &= "<div class='item-detail-text' id='divitemname" & ItemID & "'>" & ItemName & "</div></div>"
            'html &= "<div class='item-detail-text'>" & ItemCode & "</div></div></td>"
            'html &= "<td style='width: 12vw;Font-weight: bold;'>" & ItemPrice & Currency & "</td></tr></table>"

            'html = html & "<div class='item-detail'>"
            'html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
            'html = html & "<div align='center'>"

            'html &= "<img class='img_item_thumnail' alt='' class='img-round' src='" & img_url & "' style='width:100%;OBJECT-FIT: fill;' />"


            Dim index As Integer = j - 20 * Int(j / 20)

            'html = html & "<div class='item-detail-textbox'><div class='item-detail-text'  id='divitemname" & ItemID & "'>" & ItemCode & " " & ItemName & "</div>"
            'html &= "<div class='box-buy'>" & ItemPrice & Currency
            'html &= "</div>"
            'html &= "</div>"


            'html = html & "</div>"
            'html = html & "</div>"
            'html = html & "</div>"
            'html = html & "</a>"

        Next
        'html = html & "</div><div class='margin-bottom'></div>"
        Return lst
    End Function

    <WebMethod()>
    Public Shared Function getgroup(CategoryID As Integer) As String
        Dim BalGl As New BalGlobal
        Dim ds_group As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblGroup", "GroupID, GroupName, ImageUrl", "GroupName", "Where isnull(Deleted,0)=0 And CategoryID=" & CategoryID, "ASC")
        If ds_group.Tables(0).Rows.Count > 0 Then
            Dim html As String = ""
            For i = 0 To ds_group.Tables(0).Rows.Count - 1
                Dim img_path As String = ds_group.Tables(0).Rows(i)("ImageUrl").ToString
                Dim GroupName As String = ds_group.Tables(0).Rows(i)("GroupName").ToString
                Dim Groupid As Integer = ds_group.Tables(0).Rows(i)("GroupID").ToString
                'html = html & "<div class='col-md-3 status-box-left crypto-ssl-left'>"
                html = html & "<div class='item-detail' >"
                html = html & "<div align='center' style='background-color: #FFFFFF;text-overflow: clip;overflow: hidden;'>"
                html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"



                img_path = "hdImage_all.ashx?ImagePath=" & HttpContext.Current.Server.MapPath(img_path) & "&width=300&height=200"

                'html = html & "<a href='userorder.aspx?category=" & categoryid & "&group=" & Groupid & "&user=" & UserID & "'><img alt='' src='" & img_path & "' style='width:100%' /></a>"
                If CategoryID = 5 Then
                    html = html & "<a onclick='view_itemdetail(" & CategoryID & "," & Groupid & ")'>"
                Else
                    html = html & "<a onclick='view_item(" & CategoryID & "," & Groupid & ")'>"
                End If


                'html = html & "</div>"
                Dim index As Integer = i - 20 * Int(i / 20)
                html = html & "<div style='border-radius:20px;text-align: center;padding:17px 0px;background-color: " & arr_color(index) & "; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                'If i Mod 2 = 0 Then
                '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #7f8183; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                'Else
                '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #3399cc; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                'End If

                html = html & "</div>"
                html = html & "</div>"
                html = html & "</div>"
            Next
            Return html
            'ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)

        Else
            If CategoryID = 20 Then
                'div_popup_content.InnerHtml = ""
                'ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal2();", True)
                'Dim itemid As Integer = Request.QueryString("item")

                'dplUnit.DataSourceID = ""
                'dplUnit.DataSource = Nothing
                'dplUnit.DataBind()

                'dplUnit.SelectedIndex = 0

                'txtprice.Text = ""
                'dplCurrencyUnit.SelectedValue = "$"
                'txtItem.Text = ""
                'txtItem.Focus()
                Return ""
            Else
                Dim ds_item As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblItem", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And CategoryID=" & CategoryID, "ASC")
                Dim html As String = ""
                For j = 0 To ds_item.Tables(0).Rows.Count - 1
                    Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
                    Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
                    Dim img_url As String = "/" & ds_item.Tables(0).Rows(j)("ImageUrl").ToString

                    html = html & "<div class='item-detail'>"
                    html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
                    'html = html & "<div align='center'>"
                    img_url = "hdImage_all.ashx?ImagePath=" & HttpContext.Current.Server.MapPath(img_url) & "&width=300&height=200"

                    'html = html & "<a href='userorder.aspx?category=" & categoryid & "&item=" & ItemID & "&user=" & UserID & "'><img alt='' src='" & img_url & "' style='width:100%' /></a>"
                    'html = html & "<a href='userorder.aspx?category=" & CategoryID & "&item=" & ItemID & "&user=" & UserID & "'>"
                    html = html & "<a onclick='view_itemdetail(" & CategoryID & "," & ItemID & ")'>"
                    'html = html & "</div>"
                    Dim index As Integer = j - 20 * Int(j / 20)
                    html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%;'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                    'If j Mod 2 = 0 Then
                    '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #7f8183; color: white; font-weight: bold;margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                    'Else
                    '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #3399cc; color: white; font-weight: bold;margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                    'End If

                    html = html & "</a>"
                    html = html & "</div>"
                    html = html & "</div>"
                Next
                Return html
                'ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)

            End If
        End If

    End Function

    Public Shared arr_color(20) As String
    Protected Sub Page_Init(sender As Object, e As System.EventArgs)
        Dim BalGl As New BalGlobal
        Dim UserID As Integer = Request.QueryString("user")
        Dim categoryid As Integer = Request.QueryString("category")



        If Request.Url.ToString.Contains("category=") = True AndAlso Request.Url.ToString.Contains("group=") = False AndAlso Request.Url.ToString.Contains("item=") = False AndAlso Request.Url.ToString.Contains("selected") = False Then

            Dim ds_group As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblGroup", "GroupID, GroupName, ImageUrl", "GroupName", "Where isnull(Deleted,0)=0 And CategoryID=" & categoryid, "ASC")
            If ds_group.Tables(0).Rows.Count > 0 Then
                Dim html As String = ""
                For i = 0 To ds_group.Tables(0).Rows.Count - 1
                    Dim img_path As String = ds_group.Tables(0).Rows(i)("ImageUrl").ToString
                    Dim GroupName As String = ds_group.Tables(0).Rows(i)("GroupName").ToString
                    Dim Groupid As Integer = ds_group.Tables(0).Rows(i)("GroupID").ToString
                    'html = html & "<div class='col-md-3 status-box-left crypto-ssl-left'>"
                    html = html & "<div class='item-detail' >"
                    html = html & "<div align='center' style='background-color: #FFFFFF;text-overflow: clip;overflow: hidden;'>"
                    html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"



                    img_path = "hdImage_all.ashx?ImagePath=" & Server.MapPath(img_path) & "&width=300&height=200"

                    'html = html & "<a href='userorder.aspx?category=" & categoryid & "&group=" & Groupid & "&user=" & UserID & "'><img alt='' src='" & img_path & "' style='width:100%' /></a>"
                    If categoryid = 5 Then
                        html = html & "<a href='userorder.aspx?category=" & categoryid & "&item=" & Groupid & "&user=" & UserID & "'>"
                    Else
                        html = html & "<a href='userorder.aspx?category=" & categoryid & "&group=" & Groupid & "&user=" & UserID & "'>"
                    End If


                    'html = html & "</div>"
                    Dim index As Integer = i - 20 * Int(i / 20)
                    html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%;'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                    'If i Mod 2 = 0 Then
                    '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #7f8183; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                    'Else
                    '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #3399cc; color: white; font-weight: bold; margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle;font-size: 13px;'>" & GroupName & "</div></div></a>"
                    'End If

                    html = html & "</div>"
                    html = html & "</div>"
                    html = html & "</div>"
                Next
                div_popup_content.InnerHtml = html
                ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)

            Else
                If categoryid = 20 Then
                    div_popup_content.InnerHtml = ""
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal2();", True)
                    Dim itemid As Integer = Request.QueryString("item")

                    dplUnit.DataSourceID = ""
                    dplUnit.DataSource = Nothing
                    dplUnit.DataBind()

                    dplUnit.SelectedIndex = 0

                    txtprice.Text = ""
                    dplCurrencyUnit.SelectedValue = "$"
                    txtItem.Text = ""
                    txtItem.Focus()

                Else
                    Dim ds_item As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblItem", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And CategoryID=" & categoryid, "ASC")
                    Dim html As String = ""
                    For j = 0 To ds_item.Tables(0).Rows.Count - 1
                        Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
                        Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
                        Dim img_url As String = "/" & ds_item.Tables(0).Rows(j)("ImageUrl").ToString

                        html = html & "<div class='item-detail'>"
                        html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
                        'html = html & "<div align='center'>"
                        img_url = "hdImage_all.ashx?ImagePath=" & Server.MapPath(img_url) & "&width=300&height=200"

                        'html = html & "<a href='userorder.aspx?category=" & categoryid & "&item=" & ItemID & "&user=" & UserID & "'><img alt='' src='" & img_url & "' style='width:100%' /></a>"
                        html = html & "<a href='userorder.aspx?category=" & categoryid & "&item=" & ItemID & "&user=" & UserID & "'>"
                        'html = html & "</div>"
                        Dim index As Integer = j - 20 * Int(j / 20)
                        html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%;'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                        'If j Mod 2 = 0 Then
                        '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #7f8183; color: white; font-weight: bold;margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                        'Else
                        '    html = html & "<div style='text-align: center;padding:17px 0px;background-color: #3399cc; color: white; font-weight: bold;margin-bottom:10px;height:80px; display:table; width:100%'><div style='display: table-cell;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                        'End If

                        html = html & "</a>"
                        html = html & "</div>"
                        html = html & "</div>"
                    Next
                    div_popup_content.InnerHtml = html
                    ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)

                End If
            End If


        ElseIf Request.Url.ToString.Contains("category=") = True AndAlso Request.Url.ToString.Contains("group=") = True AndAlso Request.Url.ToString.Contains("item=") = False Then
            Dim groupid As Integer = Request.QueryString("group")
            Dim ds_item As DataSet = DalGlobal.getSaleItem(groupid, categoryid) 'BalGl.getDataFromTable("ViewSalePrice", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And GroupID=" & groupid, "ASC")
            Dim html As String = ""
            For j = 0 To ds_item.Tables(0).Rows.Count - 1
                Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
                Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
                Dim ItemCode As String = ds_item.Tables(0).Rows(j)("ItemCode")
                Dim img_url As String = "/" & ds_item.Tables(0).Rows(j)("ImageUrl").ToString

                Dim ItemPrice As String = ds_item.Tables(0).Rows(j)("Price")
                html = html & "<div class='item-detail'>"
                'html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
                html = html & "<div align='center'>"
                'img_url = "hdImage_all.ashx?ImagePath=" & Server.MapPath(img_url) & "&width=200&height=110"
                html = html & "<a href='userorder.aspx?category=" & categoryid & "&item=" & ItemID & "&user=" & UserID & "'>"
                html &= "<img class='img_item_thumnail' alt='' class='img-round' src='" & img_url & "' style='width:100%;' />"
                html &= "</a>"


                'html = html & "</div>"
                Dim index As Integer = j - 20 * Int(j / 20)
                html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%;'><div style='vertical-align: middle; font-size: 13px;'>" & ItemCode & " " & ItemName & "</div>"
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
            div_popup_content.InnerHtml = html
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)

        ElseIf Request.Url.ToString.Contains("category=") = True AndAlso Request.Url.ToString.Contains("group=") = True AndAlso Request.Url.ToString.Contains("item=") = True Then

            Dim groupid As Integer = Request.QueryString("group")
            Dim ds_item As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblItem", "ItemID, ItemName, ImageUrl", "ItemName", "Where isnull(Deleted,0)=0 And GroupID=" & groupid, "ASC")
            Dim html As String = ""
            For j = 0 To ds_item.Tables(0).Rows.Count - 1
                Dim ItemID As Integer = ds_item.Tables(0).Rows(j)("ItemID")
                Dim ItemName As String = ds_item.Tables(0).Rows(j)("ItemName")
                Dim img_url As String = ds_item.Tables(0).Rows(j)("ImageUrl").ToString
                html = html & "<div class='item-detail'>"
                html = html & "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
                'html = html & "<div align='center'>"
                html = html & "<a href='userorder.aspx?category=" & categoryid & "&group=" & groupid & "&item=" & ItemID & "&user=" & UserID & "'>"
                'html = html & "</div>"
                If j Mod 2 = 0 Then
                    html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%'><div style='overflow:hidden;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                Else
                    html = html & "<div style='text-align: center;padding-top: 5px;background-color: #58bd58;color: white;font-weight: bold;height: 40px;display:table;width:100%'><div style='overflow:hidden;vertical-align: middle; font-size: 13px;'>" & ItemName & "</div></div>"
                End If

                html = html & "</a>"
                html = html & "</div>"
                html = html & "</div>"
            Next
            div_popup_content.InnerHtml = html
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal();", True)


        ElseIf Request.Url.ToString.Contains("category=") = True AndAlso Request.Url.ToString.Contains("item=") = True AndAlso Request.Url.ToString.Contains("selected") = False Then
            div_popup_content.InnerHtml = ""
            ScriptManager.RegisterStartupScript(Me, Me.GetType(), "Pop", "openModal2();", True)
            Dim itemid As Integer = Request.QueryString("item")

            Dim dsSaler As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "ViewSalePrice", "ID, ItemName, ItemCode, ltrim(rtrim(str(FromQty))) +'-'+ ltrim(rtrim(str(ToQty))) as UnitType, Price, Currency, isnull(imageUrl,'') as ImageUrl", "ID ", "Where isnull(Deleted,0)=0 And ItemID=" & itemid, "asc")
            dplUnit.DataSourceID = ""
            dplUnit.DataTextField = "UnitType"
            dplUnit.DataValueField = "ID"
            dplUnit.DataSource = dsSaler
            dplUnit.DataBind()

            dplUnit.SelectedIndex = 0
            txtItem.Enabled = False
            If dsSaler.Tables(0).Rows.Count > 0 Then
                txtprice.Text = dsSaler.Tables(0).Rows(0)("Price").ToString
                dplCurrencyUnit.SelectedValue = dsSaler.Tables(0).Rows(0)("Currency").ToString
                txtItem.Text = dsSaler.Tables(0).Rows(0)("ItemName").ToString
                txtItem.ToolTip = dsSaler.Tables(0).Rows(0)("ItemCode").ToString
                img_item.ImageUrl = dsSaler.Tables(0).Rows(0)("ImageUrl").ToString
            End If

            txtQty_Select.Focus()



            'Dim ItemID As Integer = Request.QueryString("item")
            'Dim ds_item As DataSet = BalGl.getDataFromTable("tblItem", "ItemID, ItemName, Unit1, UnitPrice1, Currency1", "ItemName", "Where ItemID=" & ItemID, "ASC")
            'Dim ItemName As String = ds_item.Tables(0).Rows(0)("ItemName")
            'Dim Unit As String = ds_item.Tables(0).Rows(0)("Unit1")
            'Dim Qty As Integer = 1
            'Dim Price As String = ds_item.Tables(0).Rows(0)("Currency1").ToString & ds_item.Tables(0).Rows(0)("UnitPrice1").ToString
            'Dim Discount As Double = 0
            'Add_Item(ItemID, ItemName, Unit, Qty, Price, Discount)
        ElseIf Request.Url.ToString.Contains("category=") = True AndAlso Request.Url.ToString.Contains("item=") = True AndAlso Request.Url.ToString.Contains("selected=") = True Then
            'div_invoice.Visible = True
            'div_item.Visible = False
            Dim ds_criteria As DataTable = Session("items")


            'Call LoadSummary(ds_criteria)

            'Dim ItemID As Integer = Request.QueryString("item")
            'Dim ds_item As DataSet = BalGl.getDataFromTable("tblItem", "ItemID, ItemName, Unit1, UnitPrice1, Currency1", "ItemName", "Where ItemID=" & ItemID, "ASC")
            'Dim ItemName As String = ds_item.Tables(0).Rows(0)("ItemName")
            'Dim Unit As String = ds_item.Tables(0).Rows(0)("Unit1")
            'Dim Qty As Integer = Val(txtQty_Select.Text)
            'Dim Price As String = ds_item.Tables(0).Rows(0)("Currency1").ToString & ds_item.Tables(0).Rows(0)("UnitPrice1").ToString
            'Dim Discount As Double = 0
            'Add_Item(ItemID, ItemName, Unit, Qty, Price, Discount)

        End If


    End Sub

    Public Shared Function ConvertDataTabletoString(dt As DataTable) As String

        Dim serializer As System.Web.Script.Serialization.JavaScriptSerializer = New System.Web.Script.Serialization.JavaScriptSerializer()
        Dim rows As List(Of Dictionary(Of String, Object)) = New List(Of Dictionary(Of String, Object))()
        Dim row As Dictionary(Of String, Object)

        For Each dr As DataRow In dt.Rows
            row = New Dictionary(Of String, Object)()

            For Each col As DataColumn In dt.Columns
                row.Add(col.ColumnName, dr(col))
            Next

            rows.Add(row)
        Next

        Return serializer.Serialize(rows)

    End Function
    'Protected Sub getOrder(OrderID As Integer)

    '    Dim BalGl As New BalGlobal
    '    'lblReceiptNo.Text = "លេខបញ្ជាទិញ: " & OrderID.ToString("000000")
    '    Dim tbl As String = "tbluserorderdetail d inner join ViewSalePrice s on d.saleid=s.id"
    '    Dim fields As String = "s.ItemID, d.ItemName,d.MeasuringUnit,d.Qty,d.Price,d.Discount,d.TotalAmount,d.SaleID,s.ItemCode"

    '    Dim ds As DataSet = BalGl.getDataFromTable(tbl, fields, "d.POSID", "Where d.OrderID=" & OrderID, "asc")
    '    Dim ds_criteria As New DataTable

    '    'ds_criteria = New DataTable
    '    ds_criteria.TableName = "tblItems"
    '    ds_criteria.Columns.Add("ItemID", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("ItemName", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("Unit", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("Qty", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("Price", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("Discount", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("Total", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("SaleID", Type.GetType("System.String"))
    '    ds_criteria.Columns.Add("ItemCode", Type.GetType("System.String"))

    '    Dim Existing As Boolean = False

    '    For r = 0 To ds.Tables(0).Rows.Count - 1
    '        Dim ItemID_ As Integer = ds.Tables(0).Rows(r)("ItemID")
    '        Dim ItemName_ As String = ds.Tables(0).Rows(r)("ItemName")
    '        Dim Unit_ As String = ds.Tables(0).Rows(r)("MeasuringUnit")
    '        Dim Qty_ As String = ds.Tables(0).Rows(r)("Qty")
    '        Dim Price_ As String = ds.Tables(0).Rows(r)("Price")
    '        Dim Discount_ As String = ds.Tables(0).Rows(r)("Discount")
    '        Dim Total_ As String = ds.Tables(0).Rows(r)("TotalAmount")
    '        Dim SaleID_ As String = ds.Tables(0).Rows(r)("SaleID")
    '        Dim ItemCode_ As String = ds.Tables(0).Rows(r)("ItemCode")

    '        Dim dr_ As DataRow = ds_criteria.NewRow

    '        dr_("ItemID") = ItemID_
    '        dr_("ItemName") = ItemName_
    '        dr_("Unit") = Unit_
    '        dr_("Qty") = Qty_
    '        dr_("Price") = Price_
    '        dr_("Discount") = Discount_
    '        dr_("Total") = Total_
    '        dr_("SaleID") = SaleID_
    '        dr_("ItemCode") = ItemCode_
    '        ds_criteria.Rows.Add(dr_)
    '    Next



    '    Session("items") = ds_criteria




    '    Try
    '        Dim dt As DataTable = ds_criteria
    '        Dim SubTotal As Double = 0
    '        Dim TotalR As Double = 0
    '        Dim TotalDollar As Double = 0

    '        For i = 0 To dt.Rows.Count - 1
    '            If dt.Rows(i)("Total").ToString.Contains("R") Then
    '                TotalR = TotalR + Val(dt.Rows(i)("Total").ToString.Replace("R", "").Replace(",", ""))
    '            Else
    '                TotalDollar = TotalDollar + Val(dt.Rows(i)("Total").ToString.Replace("$", "").Replace(",", ""))
    '            End If

    '        Next
    '        lblSubTotal.Text = TotalDollar.ToString("$#,##0.00")
    '        lblSubTotalR.Text = TotalR.ToString("R#,##0")
    '        lblGrandTotal.Text = (SubTotal - Val(lblGrandDiscount.Text)).ToString("$#,##0.00")
    '    Catch ex As Exception

    '    End Try
    '    If Request.QueryString("admin") = 1 Then
    '        btnOrderPay.Visible = True

    '    Else
    '        btnOrderPay.Visible = False


    '    End If
    '    btnPay.Visible = False
    '    btnCancelReceipt.Visible = False
    'End Sub

    'Protected Sub odderpay() Handles btnOrderPay.Click
    '    'txtTotalToPay.Text = lblSubTotal.Text
    '    'txtTotalToPayR.Text = lblSubTotalR.Text
    '    'txtDiscount.Text = lblGrandDiscount.Text
    '    'Dim BalGl As New BalGlobal
    '    'Dim dsExchangeRate As DataSet = BalGl.getDataFromTable("tblExchangeRate", "DollarToReil", "ExchangeRateID", "Where ExchangeRateID=1", "asc")
    '    'Dim ExchangeRate As Double = dsExchangeRate.Tables(0).Rows(0)("DollarToReil")
    '    'lblExchangeRate.ToolTip = ExchangeRate
    '    'lblExchangeRate.Text = "1$=" & ExchangeRate & "R"
    '    'Dim orderid As Integer = Request.QueryString("OrderID")
    '    'Dim ds As DataSet = BalGl.getDataFromTable("tblCustomer c inner join tbluserorder o on c.customerid = o.customerid", "c.CustomerID, c.CustomerName, c.Phone", "c.CustomerName", "Where o.OrderID=" & orderid, "asc")
    '    'If ds.Tables(0).Rows.Count > 0 Then
    '    '    lblCustomerID.Text = ds.Tables(0).Rows(0)("CustomerID").ToString
    '    '    lblSearchCustomer_Pay.Text = ds.Tables(0).Rows(0)("CustomerName").ToString
    '    '    txtSearchPhone_Pay.Text = ds.Tables(0).Rows(0)("Phone").ToString
    '    'End If


    'End Sub
    Protected Sub Add_Item(ItemID As Integer, ItemName As String, fromQty As Integer, toQty As Integer, Qty As Integer, Price As String, Discount As Double, SaleID As Integer, ItemCode As String)



        Dim Existing As Boolean = False

        Dim dt As DataTable = Session("items")

        For r = 0 To dt.Rows.Count - 1
            Dim ItemID_ As Integer = dt.Rows(r)("ItemID")
            Dim ItemName_ As String = dt.Rows(r)("ItemName")
            'Dim Unit_ As String = GdItems.Rows(r).Cells(3).Text
            Dim Qty_ As String = dt.Rows(r)("Qty")
            Dim Price_ As String = dt.Rows(r)("Price")
            Dim Discount_ As String = dt.Rows(r)("Discount")
            Dim Total_ As String = dt.Rows(r)("Total")
            Dim SaleID_ As Integer = dt.Rows(r)("SaleID")
            Dim ItemCode_ As String = dt.Rows(r)("ItemCode")
            If ItemID_ = ItemID And ItemID <> 0 Then
                Existing = True
            End If
            Dim dr_ As DataRow = dt.NewRow

            dr_("ItemID") = ItemID_
            dr_("ItemName") = ItemName_
            'dr_("Unit") = Unit_
            dr_("Qty") = Qty_
            dr_("Price") = Price_
            dr_("Discount") = Discount_
            dr_("Total") = Total_
            dr_("SaleID") = SaleID_
            dr_("ItemCode") = ItemCode_
            dt.Rows.Add(dr_)


        Next


        If (Existing = False) Then
            Dim dr As DataRow = dt.NewRow
            dr("ItemID") = ItemID
            dr("ItemName") = ItemName
            'dr("Unit") = IIf(fromQty = 0 And toQty = 0, "-", fromQty & "-" & toQty)
            dr("Qty") = Qty
            dr("Price") = Price 'Val(Price.Replace("$", "").Replace("R", "")).ToString("$#,##0.00")
            dr("Discount") = Discount

            dr("Total") = Qty * Val(Price.Replace("$", "").Replace("R", "")) - Discount

            dr("SaleID") = SaleID
            dr("ItemCode") = ItemCode
            dt.Rows.Add(dr)
        End If

        Session("items") = dt


        'GdItems.DataSource = ds_criteria
        'GdItems.DataBind()

    End Sub


    Public Function getItemlist_html() As String
        Dim dt As DataTable = Session("items")

        Dim html As String = "<table style='width:100%'>"
        html &= "<tr><th scope='col'>ទំនិញ&nbsp;&nbsp;</th><th scope='col' style='width:22%'>ចំនួន&nbsp;&nbsp;</th><th scope='col' style='width:15%'>តម្លៃ&nbsp;&nbsp;</th><th scope='col'  style='width:15%'>សរុប&nbsp;&nbsp;</th><th scope='col' style='width:17%'>លេខកូដ&nbsp;&nbsp;</th>	</tr>"
        For i = 0 To dt.Rows.Count - 1

            Dim ItemID As Integer = dt.Rows(i)("ItemID")
            Dim ItemName As String = dt.Rows(i)("ItemName")
            Dim Qty As Integer = dt.Rows(i)("Qty")
            Dim Price As String = dt.Rows(i)("Price")
            Dim Discount As String = dt.Rows(i)("Discount")
            Dim Total As String = dt.Rows(i)("Total")
            Dim SaleID As Integer = dt.Rows(i)("SaleID")
            Dim ItemCode As String = dt.Rows(i)("ItemCode")

            html &= "<tr style='border-width:0px;border-style:None;height:25px;'>"

            html &= "<td>" & ItemName & "</td>"
            html &= "<td style='font-size:10pt'>"
            html &= "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/subtract-color-icon.png' style='width: 100%;'></div>"
            html &= "<div style='min-width: 25px;height: 20px;float: left;font-size: large;text-align: center;'>" & Qty & "</div>"
            html &= "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/addition-color-icon.png' style='width: 100%;'></div>"
            html &= "</td>"
            html &= "<td style = 'font-size:10pt;'>$" & Price & "</td>"
            html &= "<td>$" & Total & "</td>"
            html &= "<td>" & ItemCode & "</td>"
            html &= "</tr>"

        Next
        html &= "</table>"

        Dim footer_action As String = "<div  class='bg-option'>"

        footer_action &= "<div class='btn-option' onclick='goto_item()'>បន្ថែមទំនិញ</div>"
        footer_action &= "<div class='btn-option' onclick='addorder()'>បញ្ជាទិញ</div>"
        footer_action &= "</div>"

        Return html & footer_action

    End Function

    Protected Sub get_Item()
        Dim ds_criteria As New DataTable

        'ds_criteria = New DataTable
        ds_criteria.TableName = "tblItems"
        ds_criteria.Columns.Add("ItemID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemName", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Unit", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Qty", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Price", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Discount", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Total", Type.GetType("System.String"))
        ds_criteria.Columns.Add("SaleID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemCode", Type.GetType("System.String"))


        'For r = 0 To GdItems.Rows.Count - 1
        '    Dim ItemID_ As Integer = GdItems.Rows(r).Cells(1).Text
        '    Dim ItemName_ As String = GdItems.Rows(r).Cells(2).Text
        '    Dim Unit_ As String = GdItems.Rows(r).Cells(3).Text
        '    Dim Qty_ As String = GdItems.Rows(r).Cells(4).Text
        '    Dim Price_ As String = GdItems.Rows(r).Cells(5).Text
        '    Dim Discount_ As String = GdItems.Rows(r).Cells(6).Text
        '    Dim Total_ As String = GdItems.Rows(r).Cells(7).Text
        '    Dim SaleID_ As String = GdItems.Rows(r).Cells(8).Text
        '    Dim ItemCode_ As String = GdItems.Rows(r).Cells(9).Text

        '    Dim dr_ As DataRow = ds_criteria.NewRow

        '    dr_("ItemID") = ItemID_
        '    dr_("ItemName") = ItemName_
        '    dr_("Unit") = Unit_
        '    dr_("Qty") = Qty_
        '    dr_("Price") = Price_
        '    dr_("Discount") = Discount_
        '    dr_("Total") = Total_
        '    dr_("SaleID") = SaleID_
        '    dr_("ItemCode") = ItemCode_
        '    ds_criteria.Rows.Add(dr_)
        'Next


        Session("items") = ds_criteria

        'LoadSummary(ds_criteria)

        'Session("ds_criteria") = ds_criteria

    End Sub

    Protected Sub remove_Item(index As Integer)
        Dim ds_criteria As New DataTable

        'ds_criteria = New DataTable

        ds_criteria.TableName = "tblItems"
        ds_criteria.Columns.Add("ItemID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemName", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Unit", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Qty", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Price", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Discount", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Total", Type.GetType("System.String"))
        ds_criteria.Columns.Add("SaleID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemCode", Type.GetType("System.String"))

        'For r = 0 To GdItems.Rows.Count - 1
        '    Dim ItemID_ As Integer = GdItems.Rows(r).Cells(1).Text
        '    Dim ItemName_ As String = GdItems.Rows(r).Cells(2).Text
        '    Dim Unit_ As String = GdItems.Rows(r).Cells(3).Text
        '    Dim Qty_ As String = GdItems.Rows(r).Cells(4).Text
        '    Dim Price_ As String = GdItems.Rows(r).Cells(5).Text
        '    Dim Discount_ As String = GdItems.Rows(r).Cells(6).Text
        '    Dim Total_ As String = GdItems.Rows(r).Cells(7).Text
        '    Dim SalerID_ As String = GdItems.Rows(r).Cells(8).Text
        '    Dim ItemCode_ As String = GdItems.Rows(r).Cells(9).Text.Replace("&nbsp;", "")
        '    If r <> index Then
        '        Dim dr_ As DataRow = ds_criteria.NewRow

        '        dr_("ItemID") = ItemID_
        '        dr_("ItemName") = ItemName_
        '        dr_("Unit") = Unit_
        '        dr_("Qty") = Qty_
        '        dr_("Price") = Price_
        '        dr_("Discount") = Discount_
        '        dr_("Total") = Total_
        '        dr_("SaleID") = SalerID_
        '        dr_("ItemCode") = ItemCode_
        '        ds_criteria.Rows.Add(dr_)
        '    End If
        'Next


        Session("items") = ds_criteria


        'Session("ds_criteria") = ds_criteria
        'LoadSummary(ds_criteria)
    End Sub



    Protected Sub Page_LoadComplete(sender As Object, e As System.EventArgs) Handles Me.LoadComplete
        Dim balGlobal As New BalGlobal
        'Dim Spam As String = balGlobal.DescriptSpam(Request.QueryString("user"))


        Dim token As String = ""

        If IsNothing(Request.Cookies("token")) = False Then
            token = Request.Cookies("token").Value
        End If

        Dim tokenstatus As String = appservice.CheckToken(token)


        If Page.IsPostBack = False Then


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



            If tokenstatus <> "Valid" Then
                Dim pageUrl As String = ""
                pageUrl = Request.Url.AbsolutePath
                Dim PageName As String = Right(Request.Url.AbsolutePath, 12)

                'Response.Redirect("~/appLogin.aspx?user=" & Request.QueryString("user") & "&Now=" & Now())
            End If

        End If

        Dim UserID As Integer = 0

        If Page.IsPostBack = False Then

            Dim BalGl As New BalGlobal
            Dim ds_user As New DataSet '= BalGl.getDataFromTable("tblToken t inner join tblUser1 u on t.UserName=u.UserName", "u.UserID,u.Username,u.UserLevel,isnull(u.Credit,0) as Credit", "u.Username", "where TokenID='" & token & "'", "ASC")
            If tokenstatus = "Valid" Then
                Using connection As New SqlConnection(DalConnection.MainConnectionString)
                    Using Command As New SqlCommand()
                        Command.Connection = connection
                        Command.CommandType = CommandType.Text
                        Command.CommandText = "select u.UserID,u.Username,u.UserLevel,isnull(u.Credit,0) as Credit from tblToken t inner join tblUser u on t.UserName=u.UserName where TokenID='" & token & "'"
                        Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                        sqlParameter2.Value = token

                        connection.Open()
                        Using da As New SqlDataAdapter(Command)
                            da.Fill(ds_user)
                        End Using

                        connection.Close()
                    End Using
                    UserID = ds_user.Tables(0).Rows(0)("UserID")
                End Using

                If IsNothing(ds_user.Tables(0)) = False Then
                    lblUsername.Text = HttpUtility.HtmlEncode(ds_user.Tables(0).Rows(0)("UserName").ToString)
                    Dim UserLevel As String = ds_user.Tables(0).Rows(0)("UserLevel").ToString
                    hdUserLevel.Value = UserLevel
                    If UserLevel = "Admin" Or UserLevel = "Staff" Then
                        span_orderlist.Style.Item("display") = "none"
                        'btngoback.Style.Item("display") = "block"
                        'btngoback.InnerHtml = "<i class='fa fa-home' style='font-size: 40px;    color: white;    line-height: 50px;'></i>"
                    Else
                        span_orderlist.Style.Item("display") = "block"
                        'btngoback.Style.Item("display") = "none"
                    End If


                End If

            End If


            hdUserID.Value = UserID

            If UserID = 0 Then
                divlogout.InnerText = "Log In"
            Else
                divlogout.InnerText = "Log Out"
            End If


            div_items.InnerHtml = ""
            If IsNothing(Session("items")) Then


                Dim ds_criteria As New DataTable
                ds_criteria.TableName = "tblItems"
                ds_criteria.Columns.Add("ItemID", Type.GetType("System.String"))
                ds_criteria.Columns.Add("ItemName", Type.GetType("System.String"))
                ds_criteria.Columns.Add("Unit", Type.GetType("System.String"))
                ds_criteria.Columns.Add("Qty", Type.GetType("System.String"))
                ds_criteria.Columns.Add("Price", Type.GetType("System.String"))
                ds_criteria.Columns.Add("Discount", Type.GetType("System.String"))
                ds_criteria.Columns.Add("Total", Type.GetType("System.String"))
                ds_criteria.Columns.Add("SaleID", Type.GetType("System.String"))
                ds_criteria.Columns.Add("ItemCode", Type.GetType("System.String"))

                Session("Items") = ds_criteria

            Else
                Dim dtitems As New DataTable
                dtitems = Session("Items")
                If dtitems.Rows.Count = 0 Then
                    'div_item.Visible = True

                Else
                    'div_item.Visible = False
                    div_items.InnerHtml = getItemlist_html()
                End If


            End If





            Dim dsExchangeRate As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblExchangeRate1", "*", "ExchangeRateID", "", "ASC")
            If dsExchangeRate.Tables(0).Rows.Count > 0 Then
                'lblExchangeRate.Text = "1$=" & dsExchangeRate.Tables(0).Rows(0)("DollarToReil").ToString
            End If

            'ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "GetOrderItemTemp", "GetOrderItemTemp()", True)

        End If


    End Sub





    'Private Sub LoadSummary(dt As DataTable)
    '    Try

    '        Dim SubTotal As Double = 0
    '        Dim TotalR As Double = 0
    '        Dim TotalDollar As Double = 0

    '        For i = 0 To dt.Rows.Count - 1
    '            If dt.Rows(i)("Total").ToString.Contains("R") Then
    '                TotalR = TotalR + Val(dt.Rows(i)("Total").ToString.Replace("R", "").Replace(",", ""))
    '            Else
    '                TotalDollar = TotalDollar + Val(dt.Rows(i)("Total").ToString.Replace("$", "").Replace(",", ""))
    '            End If

    '        Next
    '        lblSubTotal.Text = TotalDollar.ToString("$#,##0.00")
    '        lblSubTotalR.Text = TotalR.ToString("R#,##0")
    '        lblGrandTotal.Text = (SubTotal - Val(lblGrandDiscount.Text)).ToString("$#,##0.00")
    '    Catch ex As Exception

    '    End Try

    'End Sub



    <WebMethod()>
    Public Shared Function SearchItem(ItemCode As String) As String
        Dim BalGl As New BalGlobal
        Dim dsItem As New DataSet

        dsItem = BalGl.getDataFromTable(DalConnection.MainConnectionString, "ViewSalePrice", "top(1) *", "FromQty", "Where ItemCode='" & ItemCode & "' and Isnull(Deleted,0)=0", "asc")


        Dim JsonData As String = ConvertDataTabletoString(dsItem.Tables(0))

        Return JsonData
    End Function

    Private Function val_from_text(value_text As String) As Double
        Dim value_string As String = value_text.Replace("$", "").Replace(",", "").Replace("R", "")
        Return Val(value_string)
    End Function

    Public Class ClInvoice
        Public Property InvoiceNo As String
        Public Property InvoiceDate As String
        Public Property OrderID As Integer



    End Class

    <WebMethod()>
    Public Shared Function PayNow(Token As String, SalerID As Integer, GrandTotalToPay As Double, GrandTotalToPayR As Double, PaymentMothod As String, Discount As Double, DiscountReil As Double _
                                  , PaymentAmountDollar As Double, PaymentAmountReil As Double, ReturnDollar As Double, ReturnReil As Double, ExchangeRate As Double _
                                  , CustomerID As Integer, CustomerName As String, NewPoint As Double, Items As String) As ClInvoice

        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("SP_NewInvoice_Campus1", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@InvoiceAmount", SqlDbType.Float)
                sqlParameter1.Value = GrandTotalToPay
                Dim sqlParameter2 As SqlParameter = Command.Parameters.Add("@InvoiceAmountR", SqlDbType.Float)
                sqlParameter2.Value = GrandTotalToPayR
                Dim sqlParameter3 As SqlParameter = Command.Parameters.Add("@PaymentMothod", SqlDbType.VarChar)
                sqlParameter3.Value = PaymentMothod
                Dim sqlParameter4 As SqlParameter = Command.Parameters.Add("@Discount", SqlDbType.Float)
                sqlParameter4.Value = Discount
                Dim sqlParameter5 As SqlParameter = Command.Parameters.Add("@DiscountReil", SqlDbType.Float)
                sqlParameter5.Value = DiscountReil
                Dim sqlParameter6 As SqlParameter = Command.Parameters.Add("@PaymentAmountDollar", SqlDbType.Float)
                sqlParameter6.Value = PaymentAmountDollar
                Dim sqlParameter7 As SqlParameter = Command.Parameters.Add("@PaymentAmountReil", SqlDbType.Float)
                sqlParameter7.Value = PaymentAmountReil
                Dim sqlParameter8 As SqlParameter = Command.Parameters.Add("@ExchangeRate", SqlDbType.Float)
                sqlParameter8.Value = ExchangeRate
                Dim sqlParameter9 As SqlParameter = Command.Parameters.Add("@SalerID", SqlDbType.Int)
                sqlParameter9.Value = SalerID
                Dim sqlParameter10 As SqlParameter = Command.Parameters.Add("@ReturnDollar", SqlDbType.Float)
                sqlParameter10.Value = ReturnDollar
                Dim sqlParameter11 As SqlParameter = Command.Parameters.Add("@ReturnReil", SqlDbType.Float)
                sqlParameter11.Value = ReturnReil
                Dim sqlParameter12 As SqlParameter = Command.Parameters.Add("@CustomerID", SqlDbType.Int)
                sqlParameter12.Value = CustomerID
                Dim sqlParameter13 As SqlParameter = Command.Parameters.Add("@CustomerName", SqlDbType.NVarChar)
                sqlParameter13.Value = CustomerName
                Dim sqlParameter14 As SqlParameter = Command.Parameters.Add("@NewPoint", SqlDbType.Float)
                sqlParameter14.Value = NewPoint
                Dim sqlParameter15 As SqlParameter = Command.Parameters.Add("@Token", SqlDbType.VarChar)
                sqlParameter15.Value = Token
                Dim sqlParameter16 As SqlParameter = Command.Parameters.Add("@Items", SqlDbType.NVarChar)
                sqlParameter16.Value = Items
                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using
                connection.Close()

            End Using
        End Using


        '-------------------
        'Dim BalGl As New BalGlobal
        'Dim InvoiceNo As Integer = BalGl.InsertToTable("tblInvoice", "InvoiceAmount, InvoiceAmountR, PaymentMothod, Discount, DiscountReil, PaymentAmountDollar, PaymentAmountReil, ExchangeRate, SalerID, InvoiceDate, Remarks, ReturnDollar, ReturnReil",
        '                    InvoiceAmount & "," & InvoiceAmountR & ",'" & PaymentMothod & "'," & Discount & "," & DiscountReil & "," & PaymentAmountDollar & "," & PaymentAmountReil & "," & ExchangeRate & "," & SalerID & ",'" & InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss") & "','" & Remark & "'," & ReturnDollar & "," & ReturnReil)
        'lblReceiptNo.Text = "វិកយបត្រលេខ: " & InvoiceNo.ToString("000000")
        'lblReceiptDate.Text = InvoiceDate.ToString("dd/MM/yyyy H:mm")

        'Dim pacificNow = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))

        'lblReceiptDate.Text = pacificNow.ToString
        ''create order

        '====== Add Point ======
        'If txtGetPoint.Text <> "" Then
        '    BalGl.UpdateValueInTable("tblCustomer", "CurrentPoint=CurrentPoint+" & txtGetPoint.Text, "Where CustomerID=" & lblCustomerID.Text)
        'End If
        '=======================

        'Dim customerid As String = dplCustmer.SelectedValue
        'Dim customername As String = dplCustmer.SelectedItem.Text
        'If dplCustmer.Text.Trim.Contains("-") Then
        '    customerid = dplCustmer.Text.Trim.Split("-")(0)
        '    customername = dplCustmer.Text.Trim.Split("-")(1)
        'Else
        '    customerid = "0"
        '    customername = dplCustmer.Text.Trim
        'End If

        'If lblCustomerID.Text <> "" Then

        '    Dim TotalPoint As Integer = 0
        '    If txtGetPoint.Text <> "" Then
        '        TotalPoint = CInt(lblScore.Text) + CInt(txtGetPoint.Text)
        '    Else
        '        TotalPoint = CInt(lblScore.Text)
        '    End If

        '    lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)

        'End If
        Dim OrderID As Integer = ds.Tables(0).Rows(0)("OrderID")

        Dim BalGl As New BalGlobal
        Dim tbl As String = "tblOrderDetail1 o " & vbCrLf &
        " inner join tblSalePrice s on o.SaleID=s.ID " & vbCrLf &
        " inner join tblItem i on s.ItemID=i.ItemID"
        Dim dt As DataTable = BalGl.getDataFromTable(DalConnection.MainConnectionString, tbl, "i.ItemID,i.ItemCode,i.ItemName, i.CurrentQty, o.Qty, i.SetLowStock", "i.ItemID", "Where o.OrderID=" & OrderID, "asc").Tables(0)
        Dim TotalActualAmount As Double = 0
        Dim TotalActualAmountR As Double = 0

        For i = 0 To dt.Rows.Count - 1
            'Dim ItemID As Integer = dt.Rows(i)("ItemID")
            'Dim ItemName As String = dt.Rows(i)("ItemName")
            'Dim UnitPrice As Double = 0
            'Dim Currency As String = 0

            'Dim SaleID As Integer = dt.Rows(i)("SaleID").ToString
            'If dt.Rows(i)("Price").ToString.Contains("R") Then
            '    UnitPrice = dt.Rows(i)("Price").ToString.Replace("R", "")
            '    Currency = "R"
            'Else
            '    UnitPrice = dt.Rows(i)("Price").ToString.Replace("$", "")
            '    Currency = "$"
            'End If

            'Dim Qty As Integer = dt.Rows(i)("Qty")
            'Dim TotalAmount As Double = dt.Rows(i)("Total").ToString.Replace("$", "").Replace("R", "")
            'Dim Discount2 As Double = dt.Rows(i)("Discount")
            'Dim ActualAmount As Double = TotalAmount - Discount2
            'Dim MeasuringUnit As String = dt.Rows(i)("Unit")
            'If Currency = "$" Then
            '    TotalActualAmount = TotalActualAmount + ActualAmount
            'Else
            '    TotalActualAmountR = TotalActualAmountR + ActualAmount
            'End If

            'Dim BasePrice As Double = 0
            'BalGl.InsertToTable("tblOrderDetail", "OrderID, SaleID, ItemName, Qty, Price, Currency, TotalAmount, Discount, ActualAmount, MeasuringUnit, BasePrice",
            '                 OrderID & "," & SaleID & ",N'" & ItemName & "'," & Qty & "," & UnitPrice & ",N'" & Currency & "'," & TotalAmount & "," & Discount2 & "," & ActualAmount & ",N'" & MeasuringUnit & "'," & BasePrice)
            'BalGl.UpdateValueInTable("tblItem", "CurrentQty=CurrentQty-" & Qty, "Where isnull(Deleted,0)=0 And ItemID=" & ItemID)
            '----------------------------------

            '-----------------------------------
            'Dim dsItem As DataSet = BalGl.getDataFromTable("tblItem", "ItemCode, ItemName, isnull(SetLowStock,0) as SetLowStock, isnull(CurrentQty,0) as CurrentQty", "ItemID", "Where isnull(Deleted,0)=0 And ItemID=" & ItemID, "asc")
            Dim CurrentQty As Integer = dt.Rows(i)("CurrentQty")
            Dim LowStock As Integer = dt.Rows(i)("SetLowStock")
            Dim Qty As Integer = dt.Rows(i)("Qty")
            Dim ItemCode As String = dt.Rows(i)("ItemCode")
            Dim ItemName As String = dt.Rows(i)("ItemName")

            If CurrentQty - Qty < LowStock Then

                Try
                    'sendnotification(ItemCode, ItemName, CurrentQty - Qty, LowStock)
                Catch ex As Exception

                End Try

            End If

        Next


        Dim InvoiceNo As Integer = ds.Tables(0).Rows(0)("InvoiceNo")
        Dim clinvoice As New ClInvoice
        clinvoice.InvoiceNo = InvoiceNo
        clinvoice.OrderID = OrderID
        Return clinvoice
        'BalGl.UpdateValueInTable("tblInvoice", "InvoiceAmount=" & TotalActualAmount & ", InvoiceAmountR=" & TotalActualAmountR, "Where InvoiceNo=" & InvoiceNo)

        'Session(SalerID & "ctrl") = pnReceipt
        'Session("Items") = Nothing
        'Dim token As String = ""
        'If IsNothing(Request.Cookies("token")) = False Then
        '    token = Request.Cookies("token").Value
        'End If
        'Timer1.Enabled = True
        'BalGl.UpdateValueInTable("tblToken", "Expired=1,ExpiredDate=getdate()", "Where tokenid='" & token & "'")
        'Request.Cookies("token").Expires = DateTime.Now.AddDays(-1)
        'Session(token) = False
        'If token.Contains("_app_") Then
        '    Response.Redirect("PrintHtml.aspx?InvoiceNo=" & InvoiceNo & "&user=" & SalerID & "&backurl=pos.aspx")
        '    'Response.Redirect("~/Printhtml.aspx?user=" & Request.QueryString("user") & "&Now=" & Now())
        'Else
        'Dim url2 As String = "Print.aspx?Date=" & CurrentTime & "&user=" & SalerID

        'Session("Items") = Nothing
        'Session(SalerID & "paynow") = False
        'Dim url2 As String = "PrintHtml.aspx?print=1&InvoiceNo=" & InvoiceNo & "&user=" & SalerID & "&backurl=pos.aspx"
        'Response.Redirect(url2)
        'ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "openpopup", "window.open('" & url2 & "','OpenChildWindowUsingUpdatePanel','height = 500, width = 800,status=no,location=no,directories=no, resizable = 1, scrollbars=yes, left=75, top=75');", True)
        'End If

        'GdItems.DataSource = Nothing
        'GdItems.DataBind()

        'MultiView1.ActiveViewIndex = 0
    End Function


    <WebMethod()>
    Public Shared Function LoadDetail(UserID As Integer, CancelList As Boolean, Page As Integer, RecordsPerPage As Integer) As List(Of ClOrder)
        Dim BalGl As New BalGlobal

        Dim ds As DataSet = DalGlobal.getOrder(UserID)

        Dim dv As DataView = ds.Tables(0).DefaultView
        If CancelList = True Then
            dv.RowFilter = "Deleted=1"
        Else
            dv.RowFilter = "Deleted=0"
        End If
        Dim dt As DataTable
        dt = dv.ToTable()

        Dim lst As New List(Of ClOrder)
        For i = 0 To dt.Rows.Count - 1
            Dim clorder As New ClOrder
            clorder.OrderID = dt.Rows(i)("orderid").ToString()
            clorder.CreatedDate = dt.Rows(i)("CreatedDate").ToString()
            clorder.Total = dt.Rows(i)("Total").ToString()
            clorder.InvoiceNo = dt.Rows(i)("InvoiceNo").ToString()
            lst.Add(clorder)
        Next

        Return lst
        'Dim Menustring As String = ""
        'Menustring &= "<div style='padding:10px 10px'><span style='float:left;text-decoration: underline;color: blue;' onclick='show_orderlist()'​>បញ្ជីការបញ្ជាទិញ</span><span onclick='show_cancellist()' style='float:right;text-decoration: underline;color: #6b0402;'>បញ្ជីនៃការលុប</span></div>"
        'Menustring &= "<div style='padding-left:10px;padding-top:20px;padding-bottom:20px'>"

        'Menustring = Menustring & "<table style='border:1px solid #adadad;width:94%' cellspacing='5' cellpadding='5'>"

        'Menustring = Menustring & "<tr>"
        'Menustring = Menustring & "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#e9cb19'>ល.រ</td>"
        'Menustring = Menustring & "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#e9cb19'>លេខបញ្ជាទិញ</td>"
        'Menustring = Menustring & "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#e9cb19'>សរុប</td>"
        'Menustring = Menustring & "<td style='padding:4px;padding-left:10px;border-bottom:1px solid #adadad;background-color:#e9cb19'>ផ្សេងៗ</td>"
        'Menustring = Menustring & "</tr>"

        'For i = 0 To dt.Rows.Count - 1
        '    'Dim userid As Integer = ds.Tables(0).Rows(i)("UserID")
        '    Menustring = Menustring & "<tr style='border:1px solid #adadad'>"
        '    Menustring = Menustring & "<td align='center' style='padding:4px;border-right:1px solid #dddddd;;border-bottom:1px solid #dddddd;'>" & i + 1 & "</td>"
        '    Menustring = Menustring & "<td style='padding:4px;padding-left:10px;border-bottom:1px solid #dddddd;'>"
        '    Menustring &= "<div><a onclick='get_order(" & dt.Rows(i)("orderid").ToString() & ",""" & dt.Rows(i)("createddate").ToString() & """)' class='a-popup-detail'>" & dt.Rows(i)("orderid") & "</a></div>"
        '    Menustring &= "<div style='font-size:12px;'>" & dt.Rows(i)("createddate").ToString() & "</div>"
        '    Menustring &= "</td>"
        '    'Menustring = Menustring & "<td style='padding:4px;border-right:1px solid #dddddd;padding-left:10px;border-bottom:1px solid #dddddd;'>" & ds.Tables(0).Rows(i)("CustomerName").ToString() & "</td>"
        '    Menustring = Menustring & "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #dddddd;'>" & dt.Rows(i)("Total") & "</td>"
        '    If CancelList = True Then
        '        Menustring = Menustring & "<td style='padding:4px;padding-left:10px;border-bottom:1px solid #dddddd;color:red;'>មោឃៈការទិញ</td>"
        '    Else
        '        Dim paymentdetail As String = dt.Rows(i)("InvoiceNo")
        '        Menustring = Menustring & "<td style='padding:4px;padding-left:10px;border-bottom:1px solid #dddddd;'>" & paymentdetail & "</td>"

        '    End If

        '    Menustring = Menustring & "</tr>"
        'Next

        'Menustring = Menustring & "</table></div>"

        'Return Menustring




    End Function


    <WebMethod()>
    Public Shared Function GetOrderDetail(OrderID As Integer) As String

        Dim BalGl As New BalGlobal
        'lblReceiptNo.Text = "លេខបញ្ជាទិញ: " & OrderID.ToString("000000")
        'Dim tbl As String = "tbluserorderdetail d inner join ViewSalePrice s on d.saleid=s.id"
        'Dim fields As String = "s.ItemID, d.ItemName,d.MeasuringUnit,d.Qty,d.Price,d.Discount,d.TotalAmount,d.SaleID,s.ItemCode,s.Currency"

        'Dim ds As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, tbl, fields, "d.POSID", "Where d.OrderID=" & OrderID, "asc")

        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_getOrderDetail_v2", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@OrderID", SqlDbType.Int)
                sqlParameter1.Value = OrderID
                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using
                connection.Close()

            End Using
        End Using


        Dim ds_criteria As New DataTable

        'ds_criteria = New DataTable
        ds_criteria.TableName = "tblItems"
        ds_criteria.Columns.Add("ItemID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemName", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Currency", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Qty", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Price", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Discount", Type.GetType("System.String"))
        ds_criteria.Columns.Add("Total", Type.GetType("System.String"))
        ds_criteria.Columns.Add("SaleID", Type.GetType("System.String"))
        ds_criteria.Columns.Add("ItemCode", Type.GetType("System.String"))

        Dim Existing As Boolean = False

        For r = 0 To ds.Tables(0).Rows.Count - 1
            Dim ItemID_ As Integer = ds.Tables(0).Rows(r)("ItemID")
            Dim ItemName_ As String = ds.Tables(0).Rows(r)("ItemName")
            Dim Currency_ As String = ds.Tables(0).Rows(r)("Currency")
            Dim Qty_ As String = ds.Tables(0).Rows(r)("Qty")
            Dim Price_ As String = ds.Tables(0).Rows(r)("Price")
            Dim Discount_ As String = ds.Tables(0).Rows(r)("Discount")
            Dim Total_ As String = ds.Tables(0).Rows(r)("TotalAmount")
            'Dim SaleID_ As String = ds.Tables(0).Rows(r)("SaleID")
            Dim ItemCode_ As String = ds.Tables(0).Rows(r)("ItemCode")

            Dim dr_ As DataRow = ds_criteria.NewRow

            dr_("ItemID") = ItemID_
            dr_("ItemName") = ItemName_
            dr_("Currency") = Currency_
            dr_("Qty") = Qty_
            dr_("Price") = Price_
            dr_("Discount") = Discount_
            dr_("Total") = Total_
            dr_("SaleID") = 0
            dr_("ItemCode") = ItemCode_
            ds_criteria.Rows.Add(dr_)
        Next

        Return ConvertDataTabletoString(ds_criteria)


    End Function


    <WebMethod()>
    Public Shared Function AddOrder(UserID As Integer, JsonString As String) As ClInvoice

        Dim ds As New DataSet
        Using connection As New SqlConnection(DalConnection.MainConnectionString)
            Using Command As New SqlCommand("Sp_AddOrder_v2", connection)

                Command.CommandType = CommandType.StoredProcedure
                Dim sqlParameter1 As SqlParameter = Command.Parameters.Add("@UserID", SqlDbType.Int)
                sqlParameter1.Value = UserID
                Dim sqlParameter3 As SqlParameter = Command.Parameters.Add("@JsonString", SqlDbType.NVarChar)
                sqlParameter3.Value = JsonString
                connection.Open()
                Using da As New SqlDataAdapter(Command)
                    da.Fill(ds)
                End Using
                connection.Close()

            End Using
        End Using


        Dim chatid As String = ""
        'chatid = "1426975719"
        chatid = "-4796769324"
        Dim Telegram_message As String = ""
        Telegram_message &= ds.Tables(0).Rows(0)("ItemDetail").replace("@newline", "\n")

        TelegramSendMessage("7875798180:AAF2kXrRxIiLuMN7N_TxRBwnT2pqcVgCVgg", chatid, Telegram_message)

        '-------------------
        'Dim BalGl As New BalGlobal
        'Dim InvoiceNo As Integer = BalGl.InsertToTable("tblInvoice", "InvoiceAmount, InvoiceAmountR, PaymentMothod, Discount, DiscountReil, PaymentAmountDollar, PaymentAmountReil, ExchangeRate, SalerID, InvoiceDate, Remarks, ReturnDollar, ReturnReil",
        '                    InvoiceAmount & "," & InvoiceAmountR & ",'" & PaymentMothod & "'," & Discount & "," & DiscountReil & "," & PaymentAmountDollar & "," & PaymentAmountReil & "," & ExchangeRate & "," & SalerID & ",'" & InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss") & "','" & Remark & "'," & ReturnDollar & "," & ReturnReil)
        'lblReceiptNo.Text = "វិកយបត្រលេខ: " & InvoiceNo.ToString("000000")
        'lblReceiptDate.Text = InvoiceDate.ToString("dd/MM/yyyy H:mm")

        'Dim pacificNow = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))

        'lblReceiptDate.Text = pacificNow.ToString
        ''create order

        '====== Add Point ======
        'If txtGetPoint.Text <> "" Then
        '    BalGl.UpdateValueInTable("tblCustomer", "CurrentPoint=CurrentPoint+" & txtGetPoint.Text, "Where CustomerID=" & lblCustomerID.Text)
        'End If
        '=======================

        'Dim customerid As String = dplCustmer.SelectedValue
        'Dim customername As String = dplCustmer.SelectedItem.Text
        'If dplCustmer.Text.Trim.Contains("-") Then
        '    customerid = dplCustmer.Text.Trim.Split("-")(0)
        '    customername = dplCustmer.Text.Trim.Split("-")(1)
        'Else
        '    customerid = "0"
        '    customername = dplCustmer.Text.Trim
        'End If

        'If lblCustomerID.Text <> "" Then

        '    Dim TotalPoint As Integer = 0
        '    If txtGetPoint.Text <> "" Then
        '        TotalPoint = CInt(lblScore.Text) + CInt(txtGetPoint.Text)
        '    Else
        '        TotalPoint = CInt(lblScore.Text)
        '    End If

        '    lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)

        'End If
        Dim OrderID As Integer = ds.Tables(0).Rows(0)("OrderID")

        Dim BalGl As New BalGlobal
        Dim tbl As String = "tblOrderDetail o " & vbCrLf &
        " inner join tblSalePrice s on o.SaleID=s.ID " & vbCrLf &
        " inner join tblItem i on s.ItemID=i.ItemID"
        Dim dt As DataTable = BalGl.getDataFromTable(DalConnection.MainConnectionString, tbl, "i.ItemID,i.ItemCode,i.ItemName, i.CurrentQty, o.Qty, i.SetLowStock", "i.ItemID", "Where o.OrderID=" & OrderID, "asc").Tables(0)
        Dim TotalActualAmount As Double = 0
        Dim TotalActualAmountR As Double = 0

        For i = 0 To dt.Rows.Count - 1
            'Dim ItemID As Integer = dt.Rows(i)("ItemID")
            'Dim ItemName As String = dt.Rows(i)("ItemName")
            'Dim UnitPrice As Double = 0
            'Dim Currency As String = 0

            'Dim SaleID As Integer = dt.Rows(i)("SaleID").ToString
            'If dt.Rows(i)("Price").ToString.Contains("R") Then
            '    UnitPrice = dt.Rows(i)("Price").ToString.Replace("R", "")
            '    Currency = "R"
            'Else
            '    UnitPrice = dt.Rows(i)("Price").ToString.Replace("$", "")
            '    Currency = "$"
            'End If

            'Dim Qty As Integer = dt.Rows(i)("Qty")
            'Dim TotalAmount As Double = dt.Rows(i)("Total").ToString.Replace("$", "").Replace("R", "")
            'Dim Discount2 As Double = dt.Rows(i)("Discount")
            'Dim ActualAmount As Double = TotalAmount - Discount2
            'Dim MeasuringUnit As String = dt.Rows(i)("Unit")
            'If Currency = "$" Then
            '    TotalActualAmount = TotalActualAmount + ActualAmount
            'Else
            '    TotalActualAmountR = TotalActualAmountR + ActualAmount
            'End If

            'Dim BasePrice As Double = 0
            'BalGl.InsertToTable("tblOrderDetail", "OrderID, SaleID, ItemName, Qty, Price, Currency, TotalAmount, Discount, ActualAmount, MeasuringUnit, BasePrice",
            '                 OrderID & "," & SaleID & ",N'" & ItemName & "'," & Qty & "," & UnitPrice & ",N'" & Currency & "'," & TotalAmount & "," & Discount2 & "," & ActualAmount & ",N'" & MeasuringUnit & "'," & BasePrice)
            'BalGl.UpdateValueInTable("tblItem", "CurrentQty=CurrentQty-" & Qty, "Where isnull(Deleted,0)=0 And ItemID=" & ItemID)
            '----------------------------------

            '-----------------------------------
            'Dim dsItem As DataSet = BalGl.getDataFromTable("tblItem", "ItemCode, ItemName, isnull(SetLowStock,0) as SetLowStock, isnull(CurrentQty,0) as CurrentQty", "ItemID", "Where isnull(Deleted,0)=0 And ItemID=" & ItemID, "asc")
            Dim CurrentQty As Integer = dt.Rows(i)("CurrentQty")
            Dim LowStock As Integer = dt.Rows(i)("SetLowStock")
            Dim Qty As Integer = dt.Rows(i)("Qty")
            Dim ItemCode As String = dt.Rows(i)("ItemCode")
            Dim ItemName As String = dt.Rows(i)("ItemName")

            If CurrentQty - Qty < LowStock Then

                Try
                    'sendnotification(ItemCode, ItemName, CurrentQty - Qty, LowStock)
                Catch ex As Exception

                End Try

            End If

        Next


        'Dim InvoiceNo As Integer = ds.Tables(0).Rows(0)("InvoiceNo").ToString()
        Dim clinvoice As New ClInvoice
        clinvoice.InvoiceNo = ""
        clinvoice.OrderID = OrderID
        Return clinvoice
        'BalGl.UpdateValueInTable("tblInvoice", "InvoiceAmount=" & TotalActualAmount & ", InvoiceAmountR=" & TotalActualAmountR, "Where InvoiceNo=" & InvoiceNo)

        'Session(SalerID & "ctrl") = pnReceipt
        'Session("Items") = Nothing
        'Dim token As String = ""
        'If IsNothing(Request.Cookies("token")) = False Then
        '    token = Request.Cookies("token").Value
        'End If
        'Timer1.Enabled = True
        'BalGl.UpdateValueInTable("tblToken", "Expired=1,ExpiredDate=getdate()", "Where tokenid='" & token & "'")
        'Request.Cookies("token").Expires = DateTime.Now.AddDays(-1)
        'Session(token) = False
        'If token.Contains("_app_") Then
        '    Response.Redirect("PrintHtml.aspx?InvoiceNo=" & InvoiceNo & "&user=" & SalerID & "&backurl=pos.aspx")
        '    'Response.Redirect("~/Printhtml.aspx?user=" & Request.QueryString("user") & "&Now=" & Now())
        'Else
        'Dim url2 As String = "Print.aspx?Date=" & CurrentTime & "&user=" & SalerID

        'Session("Items") = Nothing
        'Session(SalerID & "paynow") = False
        'Dim url2 As String = "PrintHtml.aspx?print=1&InvoiceNo=" & InvoiceNo & "&user=" & SalerID & "&backurl=pos.aspx"
        'Response.Redirect(url2)
        'ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "openpopup", "window.open('" & url2 & "','OpenChildWindowUsingUpdatePanel','height = 500, width = 800,status=no,location=no,directories=no, resizable = 1, scrollbars=yes, left=75, top=75');", True)
        'End If

        'GdItems.DataSource = Nothing
        'GdItems.DataBind()

        'MultiView1.ActiveViewIndex = 0
    End Function

    Public Shared Sub sendnotification(ItemCode As String, ItemName As String, CurrentQty As Integer, LowStock As Integer)
        Dim Telegram_message As String = ""
        Dim BalGl As New BalGlobal
        Telegram_message &= DateAdd(DateInterval.Hour, 7, BalGl.GetServerCurrentDate).ToString("dd.MM.yyyy HH:mm").Replace(".", "\.") & Chr(10) & "*" & ItemCode & "\-" & ItemName.Replace("(", "").Replace(")", "") & " * " & Chr(10) &
            " Current Stock:" & CurrentQty & Chr(10) & " Low Stock:" & LowStock
        Dim chatid As String = ""
        'chatid = "1426975719"
        chatid = "-4771067539"
        TelegramSendMessage("7875798180:AAF2kXrRxIiLuMN7N_TxRBwnT2pqcVgCVgg", chatid, Telegram_message)
    End Sub
    'Protected Sub btnpaynoworder() Handles btnPayNow.Click
    '    ''create invoice
    '    Dim InvoiceAmount As Double = val_from_text(txtTotalToPay.Text)
    '    Dim InvoiceAmountR As Double = val_from_text(txtTotalToPayR.Text)

    '    Dim PaymentMothod As String = "Cash"
    '    Dim Discount As Double = val_from_text(txtDiscountPay.Text)
    '    Dim DiscountReil As Double = val_from_text(txtDiscountPayReil.Text)

    '    Dim PaymentAmountDollar As Double = val_from_text(txtPayDollar.Text)
    '    Dim PaymentAmountReil As Double = val_from_text(txtPayReil.Text)
    '    Dim ReturnDollar As Double = val_from_text(txtReturnDollar.Text)
    '    Dim ReturnReil As Double = val_from_text(txtReturnReil.Text)

    '    Dim ExchangeRate As Double = val_from_text(lblExchangeRate.ToolTip)
    '    Dim SalerID As Integer = Request.QueryString("user")
    '    Dim InvoiceDate As Date = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
    '    Dim Remark As String = "New Invoice"

    '    Dim CurrentTime As Date = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))

    '    '-----------------
    '    lblGrandDiscount.Text = Discount.ToString("$#,##0.00")
    '    lblGrandDiscountReil.Text = DiscountReil.ToString("R#,##0")
    '    lblSubTotal.Text = InvoiceAmount.ToString("$#,##0.00")
    '    lblSubTotalR.Text = InvoiceAmountR.ToString("R#,##0")

    '    lblGrandTotal.Text = (InvoiceAmount - Discount).ToString("$#,##0.00")
    '    lblGrandTotalReil.Text = (InvoiceAmountR - DiscountReil).ToString("R#,##0")

    '    lblReceivedDollar.Text = val_from_text(txtPayDollar.Text).ToString("$#,##0.00")
    '    lblReceivedReil.Text = val_from_text(txtPayReil.Text).ToString("R#,##0")
    '    lblReturnDollar.Text = val_from_text(txtReturnDollar.Text).ToString("$#,##0.00")
    '    lblReturnReil.Text = val_from_text(txtReturnReil.Text).ToString("R#,##0")

    '    '-------------------
    '    Dim BalGl As New BalGlobal
    '    Dim dsUser As DataSet = BalGl.getDataFromTable("tblUser", "UserName", "UserID", "Where UserID=" & SalerID, "asc")
    '    lblUser.Text = dsUser.Tables(0).Rows(0)("UserName").ToString
    '    Dim InvoiceNo As Integer = BalGl.InsertToTable("tblInvoice", "InvoiceAmount, InvoiceAmountR, PaymentMothod, Discount, DiscountReil, PaymentAmountDollar, PaymentAmountReil, ExchangeRate, SalerID, InvoiceDate, Remarks, ReturnDollar, ReturnReil",
    '                        InvoiceAmount & "," & InvoiceAmountR & ",'" & PaymentMothod & "'," & Discount & "," & DiscountReil & "," & PaymentAmountDollar & "," & PaymentAmountReil & "," & ExchangeRate & "," & SalerID & ",'" & InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss") & "','" & Remark & "'," & ReturnDollar & "," & ReturnReil)
    '    'lblReceiptNo.Text = "វិកយបត្រលេខ: " & InvoiceNo.ToString("000000")
    '    lblReceiptDate.Text = InvoiceDate.ToString("dd/MM/yyyy H:mm")

    '    'Dim pacificNow = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))

    '    'lblReceiptDate.Text = pacificNow.ToString
    '    ''create order

    '    '====== Add Point ======
    '    If txtGetPoint.Text <> "" Then
    '        BalGl.UpdateValueInTable("tblCustomer", "CurrentPoint=CurrentPoint+" & txtGetPoint.Text, "Where CustomerID=" & lblCustomerID.Text)
    '    End If
    '    '=======================

    '    Dim customerid As String = dplCustmer.SelectedValue
    '    Dim customername As String = dplCustmer.SelectedItem.Text
    '    'If dplCustmer.Text.Trim.Contains("-") Then
    '    '    customerid = dplCustmer.Text.Trim.Split("-")(0)
    '    '    customername = dplCustmer.Text.Trim.Split("-")(1)
    '    'Else
    '    '    customerid = "0"
    '    '    customername = dplCustmer.Text.Trim
    '    'End If

    '    If lblCustomerID.Text <> "" Then

    '        Dim TotalPoint As Integer = 0
    '        If txtGetPoint.Text <> "" Then
    '            TotalPoint = CInt(lblScore.Text) + CInt(txtGetPoint.Text)
    '        Else
    '            TotalPoint = CInt(lblScore.Text)
    '        End If

    '        lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)

    '    End If
    '    Dim OrderID As Integer = 0

    '    If lblCustomerID.Text = "" Then
    '        OrderID = BalGl.InsertToTable("tblOrder", "CustomerID, Customer, InvoiceNo",
    '                         0 & ",N'" & customername & "'," & InvoiceNo)
    '    Else
    '        OrderID = BalGl.InsertToTable("tblOrder", "CustomerID, Customer, InvoiceNo",
    '                             lblCustomerID.Text & ",N'" & customername & "'," & InvoiceNo)
    '    End If

    '    Dim dt As DataTable = Session("items")
    '    For i = 0 To dt.Rows.Count - 1
    '        Dim ItemID As Integer = dt.Rows(i)("ItemID")
    '        Dim ItemName As String = dt.Rows(i)("ItemName")
    '        Dim UnitPrice As Double = 0
    '        Dim Currency As String = 0

    '        Dim SaleID As Integer = dt.Rows(i)("SaleID").ToString
    '        If dt.Rows(i)("Price").ToString.Contains("R") Then
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("R", "")
    '            Currency = "R"
    '        Else
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("$", "")
    '            Currency = "$"
    '        End If

    '        Dim Qty As Integer = dt.Rows(i)("Qty")
    '        Dim TotalAmount As Double = dt.Rows(i)("Total").ToString.Replace("$", "").Replace("R", "")
    '        Dim Discount2 As Double = dt.Rows(i)("Discount")
    '        Dim ActualAmount As Double = TotalAmount - Discount2
    '        Dim MeasuringUnit As String = dt.Rows(i)("Unit")

    '        Dim BasePrice As Double = 0
    '        BalGl.InsertToTable("tblOrderDetail", "OrderID, SaleID, ItemName, Qty, Price, Currency, TotalAmount, Discount, ActualAmount, MeasuringUnit, BasePrice",
    '                         OrderID & "," & SaleID & ",N'" & ItemName & "'," & Qty & "," & UnitPrice & ",N'" & Currency & "'," & TotalAmount & "," & Discount2 & "," & ActualAmount & ",N'" & MeasuringUnit & "'," & BasePrice)
    '        BalGl.UpdateValueInTable("tblItem", "CurrentQty=CurrentQty-" & Qty, "Where isnull(Deleted,0)=0 And ItemID=" & ItemID)
    '    Next
    '    Session("Items") = Nothing
    '    Session(SalerID & "ctrl") = pnReceipt
    '    Dim url2 As String = "Print.aspx?Date=" & CurrentTime & "&user=" & SalerID
    '    ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "openpopup", "window.open('" & url2 & "','OpenChildWindowUsingUpdatePanel','height = 500, width = 800,status=no,location=no,directories=no, resizable = 1, scrollbars=yes, left=75, top=75');", True)

    '    Dim userorderid As Integer = Request.QueryString("orderid")
    '    'div_orderpaid.InnerHtml = "ការបញ្ជាទិញ " & userorderid.ToString("000000") & " " & customername & " បានបង់ប្រាក់រួច!"


    'End Sub
    'Protected Sub btnPayNow_Click(sender As Object, e As System.EventArgs)
    '    ''create invoice
    '    Dim InvoiceDate As Date = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
    '    Dim Remark As String = "New Invoice"
    '    '-------------------
    '    Dim BalGl As New BalGlobal

    '    'Dim pacificNow = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))

    '    'lblReceiptDate.Text = pacificNow.ToString
    '    ''create order

    '    '====== Add Point ======
    '    'If txtGetPoint.Text <> "" Then
    '    '    BalGl.UpdateValueInTable("tblCustomer", "CurrentPoint=CurrentPoint+" & txtGetPoint.Text, "Where CustomerID=" & lblCustomerID.Text)
    '    'End If
    '    '=======================

    '    ''Dim customerid As String = dplCustmer.SelectedValue
    '    ''Dim customername As String = dplCustmer.SelectedItem.Text
    '    'If dplCustmer.Text.Trim.Contains("-") Then
    '    '    customerid = dplCustmer.Text.Trim.Split("-")(0)
    '    '    customername = dplCustmer.Text.Trim.Split("-")(1)
    '    'Else
    '    '    customerid = "0"
    '    '    customername = dplCustmer.Text.Trim
    '    'End If

    '    'If lblCustomerID.Text <> "" Then

    '    '    Dim TotalPoint As Integer = 0
    '    '    If txtGetPoint.Text <> "" Then
    '    '        TotalPoint = CInt(lblScore.Text) + CInt(txtGetPoint.Text)
    '    '    Else
    '    '        TotalPoint = CInt(lblScore.Text)
    '    '    End If

    '    '    lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)

    '    'End If
    '    Dim OrderID As Integer = 0
    '    Dim userid As Integer = Request.QueryString("user")

    '    Dim dsUser As DataSet = BalGl.getDataFromTable("tbluser u inner join tblcustomer c on u.customerid = c.customerid",
    '                                                   "c.CustomerName,u.UserID", "u.UserID", "Where u.UserID=" & userid, "asc")
    '    lblUser.Text = dsUser.Tables(0).Rows(0)("CustomerName").ToString
    '    Dim CustomerID As Integer = dsUser.Tables(0).Rows(0)("CustomerID")
    '    'lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)


    '    OrderID = BalGl.InsertToTable("tbluserorder", "UserID,CustomerID, Customer, InvoiceNo",
    '                         userid & "," & CustomerID & ",N'',''")
    '    'lblReceiptNo.Text = "លេខបញ្ជាទិញ: " & OrderID.ToString("000000")
    '    lblReceiptDate.Text = InvoiceDate.ToString("dd/MM/yyyy H:mm")

    '    Dim dt As DataTable = Session("items")
    '    For i = 0 To dt.Rows.Count - 1
    '        Dim ItemID As Integer = dt.Rows(i)("ItemID")
    '        Dim ItemName As String = dt.Rows(i)("ItemName")
    '        Dim UnitPrice As Double = 0
    '        Dim Currency As String = 0

    '        Dim SaleID As Integer = dt.Rows(i)("SaleID").ToString
    '        If dt.Rows(i)("Price").ToString.Contains("R") Then
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("R", "")
    '            Currency = "R"
    '        Else
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("$", "")
    '            Currency = "$"
    '        End If

    '        Dim Qty As Integer = dt.Rows(i)("Qty")
    '        Dim TotalAmount As Double = dt.Rows(i)("Total").ToString.Replace("$", "").Replace("R", "")
    '        Dim Discount2 As Double = dt.Rows(i)("Discount")
    '        Dim ActualAmount As Double = TotalAmount - Discount2
    '        Dim MeasuringUnit As String = dt.Rows(i)("Unit")

    '        Dim BasePrice As Double = 0
    '        BalGl.InsertToTable("tblUserOrderDetail", "OrderID, SaleID, ItemName, Qty, Price, Currency, TotalAmount, Discount, ActualAmount, MeasuringUnit, BasePrice",
    '                         OrderID & "," & SaleID & ",N'" & ItemName & "'," & Qty & "," & UnitPrice & ",N'" & Currency & "'," & TotalAmount & "," & Discount2 & "," & ActualAmount & ",N'" & MeasuringUnit & "'," & BasePrice)
    '        'BalGl.UpdateValueInTable("tblItem", "CurrentQty=CurrentQty-" & Qty, "Where isnull(Deleted,0)=0 And ItemID=" & ItemID)
    '    Next
    '    Session("Items") = Nothing
    '    Session(userid & "ctrl") = pnReceipt
    '    Dim url2 As String = "Print.aspx?Date=" & InvoiceDate.ToString("ddmmyyyyHHmmss") & "&user=" & userid
    '    ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "openpopup", "window.open('" & url2 & "','OpenChildWindowUsingUpdatePanel','height = 500, width = 800,status=no,location=no,directories=no, resizable = 1, scrollbars=yes, left=75, top=75');", True)


    'End Sub


    <System.Web.Script.Services.ScriptMethod(),
   System.Web.Services.WebMethod()>
    Public Shared Function getItemList() As List(Of ClItem)

        Dim lst As New List(Of ClItem)
        Dim conn As SqlConnection = New SqlConnection
        conn.ConnectionString = DalConnection.MainConnectionString
        Dim cmd As SqlCommand = New SqlCommand

        cmd.CommandText = "SELECT DISTINCT ItemCode,ItemName FROM tblItem where isnull(Invisible1,0)=0 and isnull(Deleted,0)=0"
        '& " ItemCode like N'%' + @SearchText + '%'"
        'cmd.Parameters.Add("@SearchText", SqlDbType.NVarChar).Value = prefixText

        ' cmd.Parameters("@SearchText").DbType = DbType.String
        'cmd.Parameters("@SearchText").Value = prefixText
        'cmd.Parameters.AddWithValue("nrows", count)
        cmd.Connection = conn
        conn.Open()
        'Dim customers As List(Of String) = New List(Of String)

        Dim dt As New DataTable()
        Dim adapter As New SqlDataAdapter(cmd)

        ' The Fill method opens and closes the connection automatically if it's not already open
        adapter.Fill(dt)

        For Each row As DataRow In dt.Rows
            Dim clitem As New ClItem()

            ' Best practice: Handle DBNull to avoid errors
            clitem.ItemCode = If(row("ItemCode") Is DBNull.Value, "", row("ItemCode").ToString())
            clitem.ItemName = If(row("ItemName") Is DBNull.Value, "", row("ItemName").ToString())

            lst.Add(clitem)
        Next

        conn.Close()
        Return lst
    End Function





    <System.Web.Script.Services.ScriptMethod(),
  System.Web.Services.WebMethod()>
    Public Shared Function GetAllPhone() As List(Of String)

        Dim conn As SqlConnection = New SqlConnection
        conn.ConnectionString = DalConnection.MainConnectionString
        Dim cmd As SqlCommand = New SqlCommand

        cmd.CommandText = "SELECT DISTINCT Phone FROM tblCustomer1 "
        cmd.Connection = conn
        conn.Open()
        Dim customers As List(Of String) = New List(Of String)
        Dim sdr As SqlDataReader = cmd.ExecuteReader
        While sdr.Read
            Dim item As String = sdr("Phone").ToString
            customers.Add(item)
        End While
        conn.Close()
        Return customers
    End Function

    <System.Web.Script.Services.ScriptMethod(),
  System.Web.Services.WebMethod()>
    Public Shared Function GetCustomerInfo(Phone As String) As ClCustomer

        Dim conn As SqlConnection = New SqlConnection
        conn.ConnectionString = DalConnection.MainConnectionString
        Dim cmd As SqlCommand = New SqlCommand

        cmd.CommandText = "SELECT top(1)CustomerID, CustomerName,CurrentPoint  FROM tblCustomer1 Where Phone = @Phone"
        cmd.Parameters.Add("@Phone", SqlDbType.NVarChar).Value = Phone
        cmd.Connection = conn
        conn.Open()

        Dim ds As New DataSet
        Dim adapter As SqlDataAdapter = New SqlDataAdapter
        adapter.SelectCommand = cmd
        adapter.Fill(ds)
        conn.Close()
        Dim cl As New ClCustomer

        If ds.Tables(0).Rows.Count > 0 Then
            Dim CustomerID As Integer = ds.Tables(0).Rows(0)("CustomerID")
            Dim CustomerName As String = ds.Tables(0).Rows(0)("CustomerName")
            Dim CurrentPoint As Double = ds.Tables(0).Rows(0)("CurrentPoint")
            cl.CustomerID = CustomerID
            cl.CustomerName = CustomerName
            cl.CurrentPoint = CurrentPoint

        End If

        Return cl
    End Function






    <System.Web.Script.Services.ScriptMethod(),
       System.Web.Services.WebMethod()>
    Public Shared Function GetExchangeRate() As Double
        Dim BalGl As New BalGlobal
        Dim dsExchangeRate As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblExchangeRate1", "*", "ExchangeRateID", "", "ASC")
        Dim exchangerate As Double = 0
        If dsExchangeRate.Tables(0).Rows.Count > 0 Then
            exchangerate = dsExchangeRate.Tables(0).Rows(0)("DollarToReil")
        End If
        Return exchangerate
    End Function
    Public Class ClPayment
        Public Property ReturnReil As Double
        Public Property ReturnDollar As Double
    End Class
    <System.Web.Script.Services.ScriptMethod(),
       System.Web.Services.WebMethod()>
    Public Shared Function PaymentCalculation(GTotal As Double, GTotalR As Double, ExchangeRate As Double, PayDollar As Double, PayReil As Double) As ClPayment
        Dim Returnable As Double = 0
        Dim cl As New ClPayment
        If PayDollar = 0 AndAlso PayReil = 0 Then
            cl.ReturnReil = 0
            cl.ReturnDollar = 0
            Return cl
        End If


        Dim Actual_Return_Dollar As Double = 0
        If PayReil < GTotalR Then
            If PayDollar < GTotal Then

                cl.ReturnReil = (PayReil - GTotalR)
                cl.ReturnDollar = Math.Truncate(PayDollar - GTotal)
            Else
                Dim Remain_in_real As Double = GTotalR - PayReil
                Dim Remain_in_dollar As Double = Remain_in_real / ExchangeRate
                Dim Return_dollar As Double = (PayDollar - GTotal) - Remain_in_dollar

                cl.ReturnReil = ((Return_dollar - Math.Truncate(Return_dollar)) * ExchangeRate)
                cl.ReturnDollar = Math.Truncate(Return_dollar)

            End If

        Else
            If PayDollar < GTotal Then
                Dim Remain_dollar As Double = GTotal - PayDollar
                Dim Remain_in_real As Double = Remain_dollar * ExchangeRate + GTotalR
                'Actual_Return_Dollar = ((PayReil - TotalR) / ExchangeRate) - (Total - PayDollar)

                If PayReil < Remain_in_real Then
                    cl.ReturnReil = (PayReil - Remain_in_real)
                    cl.ReturnDollar = Math.Truncate(0)
                Else
                    cl.ReturnReil = (PayReil - Remain_in_real)
                    cl.ReturnDollar = Math.Truncate(0)
                End If

            Else
                Dim Remain_in_real As Double = GTotalR
                Dim Remain_in_dollar As Double = GTotalR / ExchangeRate
                Dim Return_dollar As Double = (PayDollar - GTotal) - Remain_in_dollar

                cl.ReturnReil = (PayReil - GTotalR)
                cl.ReturnDollar = (PayDollar - GTotal)

            End If
        End If
        Return cl
    End Function


    Private Function GetCustomer() As DataSet

        Dim conn As SqlConnection = New SqlConnection
        conn.ConnectionString = DalConnection.MainConnectionString
        Dim cmd As SqlCommand = New SqlCommand

        cmd.CommandText = "SELECT DISTINCT TOP(@nrows) ltrim(rtrim(str(CustomerID))) + '-' + CustomerName as CustomerDetail  FROM tblCustomer1 "
        'cmd.Parameters.AddWithValue("@SearchText", prefixText)
        cmd.Connection = conn
        conn.Open()
        Dim ds As New DataSet
        Dim adapter As SqlDataAdapter = New SqlDataAdapter
        adapter.SelectCommand = cmd
        adapter.Fill(ds)
        conn.Close()
        Return ds
    End Function

    'Protected Sub btnPay_Click(sender As Object, e As System.EventArgs) Handles btnPay.Click

    '    ''create invoice
    '    Dim InvoiceDate As Date = System.TimeZoneInfo.ConvertTime(Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))
    '    Dim Remark As String = "New Invoice"
    '    '-------------------
    '    Dim BalGl As New BalGlobal


    '    Dim OrderID As Integer = 0
    '    Dim userid As Integer = Request.QueryString("user")

    '    Dim dsUser As DataSet = BalGl.getDataFromTable("tblCustomer c inner join tblUser u on c.CustomerID=u.CustomerID", "c.CustomerName as UserName", "c.CustomerName", "Where u.UserID=" & userid, "asc")
    '    lblUser.Text = dsUser.Tables(0).Rows(0)("UserName").ToString

    '    'lblCustomer.Text = "អតិថិជន :" & lblSearchCustomer_Pay.Text & " &nbsp;&nbsp;" & txtSearchPhone_Pay.Text & "&nbsp;&nbsp;&nbsp;ពិន្ទុសរុប:" & CStr(TotalPoint)


    '    OrderID = BalGl.InsertToTable("tbluserorder", "CustomerID, Customer, InvoiceNo, CreatedDate",
    '                         userid & ",N'','','" & InvoiceDate.ToString("yyyy-MM-dd HH:mm:ss") & "'")
    '    'lblReceiptNo.Text = "លេខបញ្ជាទិញ: " & OrderID.ToString("000000")
    '    lblReceiptDate.Text = InvoiceDate.ToString("dd/MM/yyyy H:mm")

    '    Dim dt As DataTable = Session("items")
    '    For i = 0 To dt.Rows.Count - 1
    '        Dim ItemID As Integer = dt.Rows(i)("ItemID")
    '        Dim ItemName As String = dt.Rows(i)("ItemName")
    '        Dim UnitPrice As Double = 0
    '        Dim Currency As String = 0

    '        Dim SaleID As Integer = dt.Rows(i)("SaleID").ToString
    '        If dt.Rows(i)("Price").ToString.Contains("R") Then
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("R", "")
    '            Currency = "R"
    '        Else
    '            UnitPrice = dt.Rows(i)("Price").ToString.Replace("$", "")
    '            Currency = "$"
    '        End If

    '        Dim Qty As Integer = dt.Rows(i)("Qty")
    '        Dim TotalAmount As Double = dt.Rows(i)("Total").ToString.Replace("$", "").Replace("R", "")
    '        Dim Discount2 As Double = dt.Rows(i)("Discount")
    '        Dim ActualAmount As Double = TotalAmount - Discount2
    '        Dim MeasuringUnit As String = dt.Rows(i)("Unit")

    '        Dim BasePrice As Double = 0
    '        BalGl.InsertToTable("tblUserOrderDetail", "OrderID, SaleID, ItemName, Qty, Price, Currency, TotalAmount, Discount, ActualAmount, MeasuringUnit, BasePrice",
    '                         OrderID & "," & SaleID & ",N'" & ItemName & "'," & Qty & "," & UnitPrice & ",N'" & Currency & "'," & TotalAmount & "," & Discount2 & "," & ActualAmount & ",N'" & MeasuringUnit & "'," & BasePrice)
    '        'BalGl.UpdateValueInTable("tblItem", "CurrentQty=CurrentQty-" & Qty, "Where isnull(Deleted,0)=0 And ItemID=" & ItemID)
    '    Next

    '    Session("Items") = Nothing
    '    Response.Redirect("order.aspx?user=" & Request.QueryString("user"))

    '    'Session("Items") = Nothing
    '    'Session(userid & "ctrl") = pnReceipt
    '    'Dim url2 As String = "Print.aspx?Date=" & InvoiceDate.ToString("ddmmyyyyHHmmss") & "&user=" & userid
    '    'ScriptManager.RegisterClientScriptBlock(Me, Me.GetType(), "openpopup", "window.open('" & url2 & "','OpenChildWindowUsingUpdatePanel','height = 500, width = 800,status=no,location=no,directories=no, resizable = 1, scrollbars=yes, left=75, top=75');", True)

    '    'MultiView1.ActiveViewIndex = 0
    'End Sub

    'Protected Sub txtDollar_TextChanged(sender As Object, e As System.EventArgs) Handles txtPayDollar.TextChanged
    '    make_payment()
    '    txtPayDollar.Text = val_from_text(txtPayDollar.Text).ToString("$#,##0.00")
    'End Sub

    'Private Sub make_payment()
    '    Dim Returnable As Double = 0
    '    Dim Discount As Double = val_from_text(txtDiscountPay.Text.Replace("$", "").Replace(",", ""))
    '    Dim DiscountReil As Double = val_from_text(txtDiscountPayReil.Text.Replace("$", "").Replace(",", ""))

    '    Dim ExchangeRate As Double = val_from_text(lblExchangeRate.ToolTip)
    '    Dim PayDollar As Double = val_from_text(txtPayDollar.Text.Replace("$", "").Replace(",", ""))
    '    Dim PayReil As Double = val_from_text(txtPayReil.Text.Replace("$", "").Replace(",", ""))
    '    Dim Total As Double = val_from_text(lblSubTotal.Text.Replace("$", "").Replace(",", ""))

    '    Dim TotalR As Double = val_from_text(lblSubTotalR.Text.Replace("R", "").Replace(",", ""))

    '    Dim GTotal As Double = Total - Discount
    '    Dim GTotalR As Double = TotalR - DiscountReil

    '    Dim Actual_Return_Dollar As Double = 0
    '    If PayReil < GTotalR Then
    '        If PayDollar < GTotal Then

    '            txtReturnReil.Text = (PayReil - GTotalR).ToString("R#,##0")
    '            txtReturnDollar.Text = Math.Truncate(PayDollar - GTotal).ToString("$#,##0.00")


    '        Else
    '            Dim Remain_in_real As Double = GTotalR - PayReil
    '            Dim Remain_in_dollar As Double = Remain_in_real / ExchangeRate
    '            Dim Return_dollar As Double = (PayDollar - GTotal) - Remain_in_dollar

    '            txtReturnReil.Text = ((Return_dollar - Math.Truncate(Return_dollar)) * ExchangeRate).ToString("R#,##0")
    '            txtReturnDollar.Text = Math.Truncate(Return_dollar).ToString("$#,##0.00")

    '        End If

    '    Else
    '        If PayDollar < GTotal Then
    '            Dim Remain_dollar As Double = GTotal - PayDollar
    '            Dim Remain_in_real As Double = Remain_dollar * ExchangeRate + GTotalR
    '            'Actual_Return_Dollar = ((PayReil - TotalR) / ExchangeRate) - (Total - PayDollar)

    '            If PayReil < Remain_in_real Then
    '                txtReturnReil.Text = (PayReil - Remain_in_real).ToString("R#,##0")
    '                txtReturnDollar.Text = Math.Truncate(0).ToString("$#,##0.00")
    '            Else
    '                txtReturnReil.Text = (PayReil - Remain_in_real).ToString("R#,##0")
    '                txtReturnDollar.Text = Math.Truncate(0).ToString("$#,##0.00")
    '            End If

    '        Else
    '            Dim Remain_in_real As Double = GTotalR
    '            Dim Remain_in_dollar As Double = GTotalR / ExchangeRate
    '            Dim Return_dollar As Double = (PayDollar - GTotal) - Remain_in_dollar

    '            txtReturnReil.Text = (PayReil - GTotalR).ToString("R#,##0")
    '            txtReturnDollar.Text = (PayDollar - GTotal).ToString("$#,##0.00")

    '        End If
    '    End If

    'End Sub



    'Protected Sub btnBack_Click(sender As Object, e As System.EventArgs) Handles btnBack.Click

    'End Sub

    'Protected Sub txtDiscountPay_TextChanged(sender As Object, e As System.EventArgs) Handles txtDiscountPay.TextChanged
    '    make_payment()
    '    txtDiscountPay.Text = val_from_text(txtDiscountPay.Text).ToString("$#,##0.00")
    '    txtGrandTotalToPay.Text = (val_from_text(txtTotalToPay.Text) - val_from_text(txtDiscountPay.Text)).ToString("$#,##0.00")
    '    txtGrandTotalToPayReil.Text = (val_from_text(txtTotalToPayR.Text) - val_from_text(txtDiscountPayReil.Text)).ToString("$#,##0.00")
    'End Sub

    'Protected Sub txtPayReil_TextChanged(sender As Object, e As System.EventArgs) Handles txtPayReil.TextChanged
    '    make_payment()
    '    txtPayReil.Text = val_from_text(txtPayReil.Text).ToString("R#,##0")

    'End Sub



    Protected Sub imgUser_Click(sender As Object, e As System.Web.UI.ImageClickEventArgs) Handles imgUser.Click
        Dim balGlobal As New BalGlobal
        Dim Spam As String = balGlobal.DescriptSpam(Request.QueryString("user"))


        Session(Spam & "LogIn") = True

        Response.Redirect("ManageUser.aspx?user=" & Spam & "&now=" & Now())
    End Sub

    Protected Sub imgReport_Click(sender As Object, e As System.Web.UI.ImageClickEventArgs) Handles imgReport.Click

    End Sub

    Protected Sub imgInvoice_Click(sender As Object, e As System.Web.UI.ImageClickEventArgs) Handles imgInvoice.Click
        Dim balGlobal As New BalGlobal
        Dim Spam As String = balGlobal.DescriptSpam(Request.QueryString("user"))


        Session(Spam & "LogIn") = True

        Response.Redirect("Invoice.aspx?user=" & Spam & "&now=" & Now())
    End Sub

    Protected Sub btnAdd_Click(sender As Object, e As System.EventArgs) Handles btnAdd.Click
        Dim BalGl As New BalGlobal
        'GdItems.DataSource = Session("items")
        'GdItems.DataBind()
        lblInfo.Text = ""
        lblInfo.ForeColor = Drawing.Color.Red
        If IsNumeric(txtprice.Text.Trim) = False OrElse IsNumeric(txtQty_Select.Text) = False Then
            lblInfo.Text = "សូមបញ្ចូលពត៌មានគ្រប់ចន្លោះ!"
            Exit Sub
        End If



        Dim ItemID As Integer = Request.QueryString("item")

        Dim UnitType As String

        Dim From_Qty As Integer
        Dim To_Qty As Integer
        Dim Qty As Integer = Val(txtQty_Select.Text)
        Dim Price As String = dplCurrencyUnit.SelectedValue & txtprice.Text

        Dim SaleID As Integer
        If dplUnit.Items.Count = 0 Then
            SaleID = 0
            UnitType = ""
            From_Qty = 0
            To_Qty = 0
        Else
            UnitType = dplUnit.SelectedItem.Text

            From_Qty = UnitType.Split("-")(0)
            To_Qty = UnitType.Split("-")(1)
            SaleID = dplUnit.SelectedValue

        End If

        Dim ItemName As String = txtItem.Text.Trim
        Dim ItemCode As String = txtItem.ToolTip.ToString.Trim
        Dim Discount As Double = 0
        Add_Item(ItemID, ItemName, From_Qty, To_Qty, Qty, Price.Replace("$", ""), Discount, SaleID, ItemCode)
        If Request.Url.ToString.Contains("category=20") Then
            Response.Redirect(Request.Url.ToString & "&item=0&selected=1")
        Else
            Response.Redirect(Request.Url.ToString & "&selected=1")
        End If

        'Session("selected") = True
        div_items.InnerHtml = getItemlist_html()

    End Sub


    Protected Sub txtprice_TextChanged(sender As Object, e As System.EventArgs) Handles txtprice.TextChanged
        txtQty_Select.Focus()
    End Sub

    Protected Sub txtQty_Select_TextChanged(sender As Object, e As System.EventArgs)
        For i = 0 To dplUnit.Items.Count - 1
            Dim UnitRange As String = dplUnit.Items(i).Text
            Dim Unit_from As Integer = UnitRange.Split("-")(0)
            Dim Unit_to As Integer = UnitRange.Split("-")(1)
            Dim Qty As Integer = txtQty_Select.Text
            If Unit_from <= Qty AndAlso Qty <= Unit_to Then
                dplUnit.SelectedIndex = i
                Call dplUnit_SelectedIndexChanged(sender, e)
                Exit For
            End If
        Next
    End Sub

    Protected Sub dplCurrencyUnit_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles dplCurrencyUnit.SelectedIndexChanged
        btnAdd_Click(sender, e)
    End Sub

    Protected Sub LinkButton5_Click(sender As Object, e As System.EventArgs) Handles LinkButton5.Click
        Dim balGlobal As New BalGlobal
        Dim Spam As String = balGlobal.DescriptSpam(Request.QueryString("user"))


        Session(Spam & "LogIn") = True

        Response.Redirect("Invoice.aspx?user=" & Spam & "&now=" & Now())
    End Sub

    Protected Sub LinkButton7_Click(sender As Object, e As System.EventArgs) Handles LinkButton7.Click
        Dim balGlobal As New BalGlobal
        Dim Spam As String = balGlobal.DescriptSpam(Request.QueryString("user"))


        Session(Spam & "LogIn") = True

        Response.Redirect("ManageUser.aspx?user=" & Spam & "&now=" & Now())
    End Sub

    Protected Sub dplUnit_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles dplUnit.SelectedIndexChanged
        Dim SaleID As Integer = dplUnit.SelectedValue
        Dim BalGl As New BalGlobal
        Dim dsSaler As DataSet = BalGl.getDataFromTable(DalConnection.MainConnectionString, "tblSalePrice", "ID, ltrim(rtrim(str(FromQty))) +'-'+ ltrim(rtrim(str(ToQty))) as UnitType, Price, Currency", "ID ", "Where ID=" & SaleID, "asc")
        txtprice.Text = dsSaler.Tables(0).Rows(0)("Price").ToString
        dplCurrencyUnit.SelectedValue = dsSaler.Tables(0).Rows(0)("Currency").ToString

        txtQty_Select.Focus()
    End Sub

    Protected Sub DropDownList1_SelectedIndexChanged(sender As Object, e As System.EventArgs) Handles dplField.SelectedIndexChanged
        'If dplField.SelectedIndex = 1 Then
        '    AutoCompleteExtender1.TargetControlID = "txtBarcode"
        'Else
        '    AutoCompleteExtender1.TargetControlID = ""
        'End If
    End Sub

    Protected Sub lnkDashboard_Click(sender As Object, e As EventArgs) Handles lnkDashboard.Click
        Session("Items") = Nothing
        Response.Redirect("~/Dashboard.aspx?user=" & Request.QueryString("user"))

    End Sub

    'Protected Sub Button1_Click(sender As Object, e As System.EventArgs) Handles Button1.Click
    '    If Request.Url.ToString.Contains("user=") = False Then
    '        Response.Redirect(Request.Url.ToString & "&user=" & Request.QueryString("user"))
    '    End If

    'End Sub

    'Protected Sub txtDiscountPayReil_TextChanged(sender As Object, e As System.EventArgs) Handles txtDiscountPayReil.TextChanged
    '    make_payment()
    '    txtDiscountPayReil.Text = val_from_text(txtDiscountPayReil.Text).ToString("R#,##0")
    '    txtGrandTotalToPay.Text = (val_from_text(txtTotalToPay.Text) - val_from_text(txtDiscountPay.Text)).ToString("$#,##0.00")
    '    txtGrandTotalToPayReil.Text = (val_from_text(txtTotalToPayR.Text) - val_from_text(txtDiscountPayReil.Text)).ToString("$#,##0.00")

    'End Sub

    'Protected Sub txtPercentageDiscount_TextChanged(sender As Object, e As System.EventArgs) Handles txtPercentageDiscount.TextChanged
    '    If IsNumeric(txtPercentageDiscount.Text) = True Then
    '        If Val(txtPercentageDiscount.Text) <= 100 Then
    '            Dim TotalDiscount As Double = 0
    '            Dim TotalDiscountR As Double = 0
    '            Dim discount = Val(txtPercentageDiscount.Text) / 100
    '            TotalDiscount = val_from_text(txtTotalToPay.Text) * discount
    '            txtDiscountPay.Text = TotalDiscount.ToString("0.00")
    '            Call txtDiscountPay_TextChanged(sender, e)
    '            TotalDiscountR = val_from_text(txtTotalToPayR.Text) * discount
    '            txtDiscountPayReil.Text = TotalDiscountR.ToString("0")
    '            Call txtDiscountPayReil_TextChanged(sender, e)

    '        End If

    '    End If
    'End Sub

    'Protected Sub txtSearchPhone_Pay_TextChanged(sender As Object, e As EventArgs) Handles txtSearchPhone_Pay.TextChanged

    '    Dim BalGl As New BalGlobal
    '    Dim dsItem As New DataSet
    '    Dim i As Integer = 0

    '    If txtSearchPhone_Pay.Text <> "" Then
    '        dsItem = BalGl.getDataFromTable("tblCustomer", "*", "Phone", "Where Phone=" & txtSearchPhone_Pay.Text & "", "asc")
    '    Else
    '        'dsItem = BalGl.getDataFromTable("tblCustomer", "*", "Phone", "Where Phone=N'" & txtSearchPhone_Pay.Text & "'", "asc")
    '        'Response.Redirect("userorder.aspx?user=" & Request.QueryString("user"))
    '    End If

    '    If dsItem.Tables(0).Rows.Count > 0 Then
    '        lblSearchCustomer_Pay.Text = dsItem.Tables(0).Rows(i)("CustomerName").ToString
    '        If IsDBNull(dsItem.Tables(0).Rows(0)("CurrentPoint")) = True Then
    '            lblScore.Text = 0
    '        Else
    '            lblScore.Text = dsItem.Tables(0).Rows(0)("CurrentPoint")
    '        End If
    '        lblCustomerID.Text = dsItem.Tables(0).Rows(0)("CustomerID").ToString
    '    End If


    'End Sub


    'Protected Sub UploadFile_Item_POS(sender As Object, e As EventArgs) Handles cmd_Upload_Item_POS.Click
    '    Dim Test As String = ""

    '    Test = "Images\Item"

    '    UploadDir = Path.Combine(Request.PhysicalApplicationPath, Test)

    '    If FileUpload_Item_POS.PostedFile.FileName = "" Then

    '        lblInfo.Text = "You do not specify a file."

    '    Else

    '        ' Check the extension.

    '        Dim Ext As String = Path.GetExtension(FileUpload_Item_POS.PostedFile.FileName)

    '        Dim SrvFName As String = ""

    '        Dim FullUpldPath As String = ""




    '        SrvFName = Path.GetFileName(FileUpload_Item_POS.PostedFile.FileName)

    '        FullUpldPath = Path.Combine(UploadDir, SrvFName)



    '        Try

    '            FileUpload_Item_POS.PostedFile.SaveAs(FullUpldPath)

    '            lblInfo_Image_Upload_Item_POS.Text = "File " + SrvFName

    '            lblInfo_Image_Upload_Item_POS.Text += " uploaded successfully to" + FullUpldPath




    '        Catch ex As Exception

    '            lblInfo_Image_Upload_Item_POS.Text = ex.Message

    '        End Try

    '    End If


    'End Sub





    'Protected Sub btnpaynoworder(sender As Object, e As EventArgs) Handles btnPayNow.Click

    'End Sub
End Class

