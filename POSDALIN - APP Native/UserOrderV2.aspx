<%@ Page Language="VB" AutoEventWireup="false" CodeFile="UserOrderV2.aspx.vb" Inherits="POS" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
			  
<meta http-equiv="content-type" content="text/html; charset=UTF-8">
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <!-- The above 3 meta tags *must* come first in the head; any other head content must come *after* these tags -->
    <link rel="canonical" href="http://www.smarterasp.net/">
	<title>POS</title>

   <link rel="stylesheet" href="css/jquery-ui-1.14.1.css">

  <script  src="js/jquery-3.7.1.min.js" ></script>
  <script src="js/jquery-ui.js"></script>

    

            <link href="https://fonts.googleapis.com/css2?family=Koh+Santepheap:wght@300&amp;display=swap" rel="stylesheet">

	<link rel="icon" type="image/png" href="images/logo/sl.png">

    <!-- Bootstrap -->
    <!-- Latest compiled and minified CSS -->
    
<link rel="stylesheet" href="css/mainclient.css?v26">

<!-- Optional theme -->
    
<%--<link rel="stylesheet" href="POS_files/style.css">--%>
<link rel="stylesheet" href="POS_files/font-awesome.css">

        <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.7.0/css/font-awesome.min.css">

        <link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Moul&family=Noto+Serif+Khmer:wght@600&display=swap" rel="stylesheet">

<!-- jQuery (necessary for Bootstrap's JavaScript plugins) -->
   


    <!-- Include all compiled plugins (below), or include individual files as needed -->
    <!-- Latest compiled and minified JavaScript -->
   <%-- <script src="POS_files/bootstrap.js"></script>--%>
<script src="POS_files/bootstrap-toggle.js"></script>
<script src="POS_files/custom.js"></script>
<script src="Scripts/userorder.js?85"></script>

  <script src="js/bootstrap.min.js"></script>

    <link href="css/Enrol_popup.css?v2" rel="stylesheet" type="text/css" />

    <link href="css/cropper.min.css" rel="stylesheet" />

    <script type="text/javascript">
        function openModal() {
            $('#myModal').modal('show');
        }

        function closeModal() {
            $('#myModal').modal('hide');
        }
        function openModal2() {
            $('#myModal2').modal('show');
        }

        function closeModal2() {
            $('#myModal2').modal('hide');
        }

        $(document).ready(function() {
            // Auto focus on barcode input when page loads
            //$('#txtbarcodenew').focus().select();

            // Keep focus on barcode input after search or other actions
            //$(document).on('click', function(e) {
            //    // Don't refocus if clicking inside input fields or buttons
            //    if (!$(e.target).is('input, button, select, textarea')) {
            //        setTimeout(function() {
            //            $('#txtbarcodenew').focus().select();
            //        }, 100);
            //    }
            //});

            let windowsize = $(document).width();
            console.log(windowsize);
            $("#pnReceiptContainer").hide();
            if (windowsize >= 800) {
                
                $("#btnhome").show();
                // Refocus after modal closes
                $('.modal').on('hidden.bs.modal', function () {
                    setTimeout(function () {
                        $('#txtsearchleft').focus().select();
                    }, 100);
                });


                setTimeout(function () {
                    $('#txtsearchleft').focus().select();
                }, 100);

            }

           
        });
       
    </script>

<script type = "text/javascript">
    function ClientItemSelected(sender, e) {
        $get("<%=hfCustomerId.ClientID %>").value = e.get_value();

    }

   

</script>

    <style type="text/css">
        .mytable{

        }
        .mytable tr th{            
            padding:5px;
        }
        .mytable tr td{
            border:solid 1px gray;
            padding:2px;
            font-size:12px;
        }
        .button-add{
            padding: 15px 30px;
    font-family: 'Koh Santepheap', Arial;
    border-radius: 30px;
    font-size: large;
    background-color: lightgreen;
    border: solid 1px greenyellow;
    font-weight: bold;
    margin-top: 20px;
        }
    
  
.btn-option{
     padding: 5% 4%;
    text-decoration: none;
    background-color: #c8ab01;
    border-radius: 15px;
    font-weight: bold;
    width: 92%;
    float: left;
    text-align: center;
    margin-bottom: 4%;
    font-size: large;
    margin-top: 0%;
    color: white;
            }

    .title_h1
    {
        padding: 5px; font-size: large; font-weight: bold; background-color: #e9cb19;
    }
    .gridview td
    {
        padding:5px;
    }
    .gridview th
    {
        padding:2px;
    }

    
    .dialog_popup2
    {
        width:300px; 
        
    }
        .autocomplete_completionListElement
{
    padding: 0px;
    margin: 0px !important;
    background-color: #C6EAE8;
    color: windowtext;
    border: buttonshadow;
    border-width: 1px;
    border-style: solid;
    cursor: 'default';
    overflow: auto;
    height: 200px;
    font-family: Tahoma;
    font-size: 16px;
    text-align: left;
    list-style-type: none;
    width: 100%;
}
/* AutoComplete highlighted item */
.autocomplete_highlightedListItem
{
    margin: 0px;
    background-color: #32B6AE;
    color: black;
    padding: 5px;
    
    font-size: 16px;
    width: 100%;
}

    /* AutoComplete item */
.autocomplete_listItem
{
    background-color: #C6EAE8;
    color: windowtext;
    padding: 5px;
    line-height:1em;
    font-size: 16px;
}



    

    .top-tap{
        position:absolute;
        top:0;
        width:100%;
        z-index:9999;
    }
    .tap{
        width:100px;
        float:right;
    }
    .img-cart{
        height:50px;
    }

    </style>





  </head>

<body class="body-order">
    <form id="form1" runat="server" onsubmit="return false;">
        <asp:HiddenField ID="hdUserID" runat="server" />
        <asp:HiddenField ID="hdUserLevel" runat="server" />
        <asp:HiddenField ID="hdItemID" runat="server" />


   <div id="div_print" style="display:none;">
     <div id="iframe-container" class="fullscreen-overlay">
         <div class="header-bar">
             <div class="header-bar-title">
            <table style="width:100%">
                <tr>
                    <td><span class="title">Print</span></td>
                    <td style="text-align:right">
                        <!-- Close button triggers the hide function -->
                        <input type="button" class="close-btn" onclick="closeIframe()" value="X">
                    </td>
                </tr>
            </table>
             
             
            </div>
         </div>
         <iframe id="iframe" src="" frameborder="0" allowfullscreen></iframe>
     </div>
 </div>


        <div id="div_upload" class="popup-box" style="background: white;z-index: 999998;display: none;">
            <div style="background-color: #958003;color: white;padding: 5px;max-width: 400px;margin: auto;">
            <table style="width: 100%;font-size: 24px;">
                <tbody><tr><td><span id="spantitle">រូបថត</span></td><td style="text-align: right;">
                <span style="background-color: red;  padding: 3px 5px;color: white; border-radius: 5px;" onclick="closeupload()">X</span>

                                             </td></tr>
            </tbody></table>
            </div>

            <asp:FileUpload ID="fuFile" runat="server" style="position: fixed;top:-100vh;" />
           
            
            <div style="max-width: 400px;margin: auto;">
                <img id="previewImage" style="max-width:100%;display:none;" />
            </div>
            <div style="margin-top:8px;text-align: center;">
                <button type="button" id="btnCropUpload" class="button-buy" style="
    font-size: 14px;
">Upload</button>
                <span id="cropStatus" style="margin-left:10px;color:#333"></span>
            </div>
            <br /><br />
            <asp:Label ID="lblStatus" runat="server" Text="" EnableViewState="false"></asp:Label>

          
        </div>


        <div class="container">
        
        </div>
    <div id="div_itemcount" class="div-buttom"  onclick="showreceipt();">
        
            <table style="width:100%">
            <tbody><tr>
            <td style="width:33%">
                <i style="    font-size: 40px;    color: white;    line-height: 50px;" class="fa fa-shopping-cart" aria-hidden="true" ></i>
                
            </td>
            <td>
            <div class="">
                
                
                <div class="itemcount-box"><span><span id="span_itemcount">0</span></span></div>
            </td>
            
    </tr>
</tbody></table>

</div>
    <div id="div_home" class="container">
        <div id="div_list" class="order-list" style="display:none;">

        </div>
        <div id="div_logo" style="max-width: 600px;margin: auto;">
    <div class="col-md-12 caching-box polish crypto-ssl" style="margin-top:5px;padding-bottom: 20px;height: 100vh;">
<div style="padding: 5px;">
    <div id="container_header">
       
                    <div>
                        <div class="small-devices">
                            <div class="">
                                <div class="header-top-left" >
                                    <table style="width:100%">
                                        <tr style="width:100%">
                                            <td align="left">
                                                <div class="menu-dashboard" style="display:none;">
                                                    <asp:LinkButton ID="LinkButton8" runat="server" ForeColor="White">Go to Dashboard</asp:LinkButton>
                                                </div>
                                                
                                              
                                            </td>
                                            <td align="right" style="width:100px;">
                                                <table>
                                                    <tr>

                                                        <td align="right" style="padding-right:25px">
                                                            
                                                            <span id="span_orderlist" class="shopping-home" style="right:5px;top:5px;" onclick="show_orderlist()" runat="server">
                                                                <i class="fa fa-bell" style="    font-size: 40px;    color: white;    line-height: 50px;"></i>
                                                            </span>
                                                            
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                            
                                        </tr>
                                    </table>
                                    
                                </div>
                                <div class="col-md-6 col-xs-6 col-sm-6 header-top-right" 
                                    style="padding-right: 45px">
                                    <div class="row">
                                        <div class="col-md-2 col-xs-6 col-sm-6 logo"></div>
                                        <div class="col-xs-6 toggle-menu pull-right">
                                            <a id="toggle-menu" class="menu-trigger" href="javascript:void(0)">
                                                <div>
                                                    <span class="top"></span>
                                                    <span class="middle"></span>
                                                    <span class="bottom"></span>
                                                </div>
                                            </a>
                                        </div>
                                        <div class="col-md-16 col-sm-16 top-menu-right">
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
    
        <asp:HiddenField ID="HiddenField2" runat="server" />  

        
    </div>
</div>
<div id="div_popup_order" style="display:none;">

</div>
<div id="nad" runat="server" style="display:none;">

    <div>
        <table width="100%" cellpadding="5" cellspacing="5">
            <tr>
                <td colspan="2" style="">
                    <div class="title-product">
                        <table style="width:100%">
                            <tr>
                                <td>
                                    បញ្ជីការបញ្ជាទិញ
                                </td>
                                <td style="text-align:right;">
                                    
                                </td>
                            </tr>
                        </table>
                        
                    </div>
                </td>
            </tr>

            <tr>
                <td colspan="2">
                    <div id="div_new_Expense" runat="server" style="margin-left:10px;margin-top:10px">
                        <asp:Button ID="cmdN_Category" runat="server" Text="បង្កើតថ្មី" Width="80px" CssClass="btn btn-default" ForeColor="#009999" />
                    </div>
                </td>
            </tr>

            <tr>
                <td colspan="2">
                      
                     <div class="div-add-newinvoice">
                         <asp:LinkButton ID="lnkNewOrder" runat="server" CssClass="btn-new-order">ត្រឡប់ទៅទំព័រដើម</asp:LinkButton>
                    </div>
                    <div id="div_expense" runat="server" style="padding-left:10px;padding-right:10px">

                    </div>

                    <div id="div_expense_detail" runat="server" style="padding-left:10px;padding-right:10px">
                        <table style="width:100%">
                        <tr>
                            <td align="left" style="padding-top:20px">
                            <table style="width:92%;margin-right:8%">
                                <tr>
                                    <td style="width:20%;padding: 10px">ចំណាយលើ:</td>
                                    <td style="width:80%;padding: 10px" align="left">
                                        <asp:TextBox ID="TextBox1" runat="server" Width="100%" 
                                                CssClass="form-control input-sm" Height="40px">
                                        </asp:TextBox>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ErrorMessage="Required" ControlToValidate="txtItem" ForeColor="Red"></asp:RequiredFieldValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px">ចំនួន:</td>
                                    <td style="padding: 10px">
                                        <asp:TextBox ID="txtAmount" runat="server" Width="100%" 
                                                CssClass="form-control input-sm" Height="40px"></asp:TextBox>
                                        <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender12" runat="server" Enabled="True" TargetControlID="txtAmount" FilterType="Numbers, Custom" ValidChars=".">
                                        </cc1:FilteredTextBoxExtender>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px">ប្រភេទរូបីយប័ណ្ឌ</td>
                                    <td style="padding: 10px">
                                        <%--<asp:TextBox ID="TextBox4" runat="server" AutoPostBack="True" Width="100%" 
                                                CssClass="dropdown-header"></asp:TextBox>--%>                                                   <%-- <asp:Button ID="Button2" runat="server" Text="Show" />--%>
                                        <table style="width:100%">
                                            <tr style="width:100%">
                                                <td>
                                                    <asp:DropDownList ID="cboCurrency" runat="server" Width="100%" CssClass="form-control input-sm" Height="40px">
                                                   
                                                             <asp:ListItem>$</asp:ListItem>
                                                             <asp:ListItem>៛</asp:ListItem>
                                
                                                    </asp:DropDownList>
                                                </td>
                                        
                                            </tr>
                                        </table>
                                
                                
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px">ថ្ងៃខែឆ្នាំៈ</td>
                                    <td style="padding: 10px">
                                        <table style="width:100%">
                                            <tr style="width:100%">
                                                <td>
                                                    <table>
                                                        <tr>
                                                            <td>ថ្ងៃៈ</td>
                                                            <td>
                                                                <asp:DropDownList ID="cboDay" runat="server" Width="100%" CssClass="form-control input-sm" Height="40px"></asp:DropDownList>
                                                            </td>
                                                            <td>ខែៈ</td>
                                                            <td>
                                                                <asp:DropDownList ID="cboMonth" runat="server" Width="100%" CssClass="form-control input-sm" Height="40px"></asp:DropDownList>
                                                            </td>
                                                            <td>ឆ្នាំៈ</td>
                                                            <td>
                                                                <asp:DropDownList ID="cboYear" runat="server" Width="100%" CssClass="form-control input-sm" Height="40px"></asp:DropDownList>
                                                            </td>
                                                        </tr>
                                                    </table>
                                                </td>
                                            </tr>
                                        </table>
                                
                                    </td>
                                </tr>
                            </table>
                    
                            </td>
                        </tr>

                        <tr>
                            <td style="padding-bottom:20px;padding-top:10px">
                                <table style="width:100%">
                                    <tr style="width:100%">
                                        <td style="width:19%;padding-left:10px">
                                            អត្ថាធិប្យាយៈ
                                        </td>
                                        <td style="width:79%;padding-right:10px;padding-left:0px">
                                            <asp:TextBox ID="txtComment" runat="server" Width="100%" CssClass="form-control input-sm" TextMode="MultiLine"></asp:TextBox>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>

                        <tr>
                            <td colspan="2">
                    <table style="width:100%">
                        <tr>
                            <td align="left">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:Button ID="cmdNew" runat="server" Text="បង្កើតថ្មី" Width="80px" CssClass="btn btn-default" ForeColor="#009999" CausesValidation="False" />
                                        </td>
                                        <td style="padding-left:5px">
                                            <asp:Button ID="cmdSave" runat="server" Text="រក្សាទុក" Width="80px" CssClass="btn btn-default" ForeColor="#339966" />
                                        </td>
                                        
                                    </tr>
                                </table>
                            </td>
                            <td align="right">
                                <table>
                                    <tr>
                                        <td style="padding-left:5px">
                                            <asp:Button ID="cmdDelete_Product" runat="server" Text="លុប" Width="80px" CssClass="btn btn-default" BackColor="#ff8d71" ForeColor="White" CausesValidation="False" />
                                        </td>
                                        <td style="padding-left:5px">
                                            <asp:Button ID="cmdCancel" runat="server" Text="ត្រឡប់" Width="80px" CssClass="btn btn-default" ForeColor="#ff3300" CausesValidation="False" />
                                            <asp:HiddenField ID="hdExpenseID" runat="server" />
                                            
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        
                    </table>
                </td>
                        </tr>

                            <tr>
                <td colspan="2" style="padding-top:10px">
                    <asp:Label ID="lblResult" runat="server" Text=""></asp:Label>
                </td>
            </tr>
                    </table>
                    </div>
                </td>
                
            </tr>

        </table>
    </div>
</div>

<div id="div_option" runat="server" class="bg-option">
    <div>
    <img src="images/logo/SL POWER TOOLS-New.png">
    <div style="text-align: center;"> Tel: 070354588 / 0968255525 / 0976255525</div>
</div>
        <table width="100%" cellpadding="5" cellspacing="5">
            <tbody>
                
            <tr>
                <td colspan="2">
                     

                     <div id="div_action">
                         <div class="btn-option" onclick="goto_order()">ចាប់ផ្តើមបញ្ជាទិញ</div>
                         <div style="display:none;" class="btn-option" onclick="show_orderlist()">ប្រវត្តិទិញ</div>
                    </div >
                    

                    
                </td>
                
            </tr>

        </tbody></table>
    </div>
</div>
        </div>
    </div>

    <div class="popup-box" id="div_popupitem" style="display:none;">
        <div class="popup-content">
        <div style="    background-color: #958003;color: white;">
            <table style="width: 100%;font-size: 24px;">
                <tr><td><span id="spantitle">ចំនួនដែលត្រូវទិញ</span></td><td style="text-align: right;">
                <span style="background-color: red;  padding: 3px 5px;color: white; border-radius: 5px;" onclick="closepopup()">X</span>

                                             </td></tr>
            </table>
        </div>
        <div id="div_iteminfo" class="item-info">

        </div>
        <div class="div-item-amount">
        <table style="width: 100%;"><tbody>
            
            <tr><td>
                <div style="margin: 10px 0px;height: 70px;">
                <span class="item-qty" onclick="select_qty(1)">ទិញ+1</span>
                <span class="item-qty" onclick="select_qty(5)">ទិញ+5</span>
                <span class="item-qty" onclick="select_qty(10)">ទិញ+10</span>
                <span class="item-qty" onclick="select_qty(50)">ទិញ+50</span>
                 </div>
                <div>
                    <table style="width: 100%;">
                        <tr>
                            <td>ចំនួន:</td>
                            <td>
                                <input type="text" id="txtitemamount"  class="dropdown-item-unit" value="0"  onkeyup="changeamount(this)"/>
                            </td>
                            <td>
                                <div style="text-align:right;">
                                    ឯកតា
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <td>តម្លៃរាយ:</td>
                            <td>
                                <input type="text" id="txtitemprice" class="dropdown-item-unit" value="0" disabled="disabled" onchange="changeitemprice()" />$
                            </td>
                            <td>
                                <div style="text-align:right;">
                                 <select id="dplitemunit" class="dropdown-item-unit" disabled="disabled" style="width:110px;" onchange="changeunitprice()">

                                 </select>
                                </div>
                            </td>
                        </tr>
                    </table>
                     
                    
                </div>
                
               </td><td></td>

            </tr>
            <tr><td></td><td></td></tr></tbody></table>
         <div id="div_itemnotice" class="" style="color:#b23;">

         </div>
        <div style="height:50px;">
        <table style="width:100%">
            <tr>
                <td>
                    សរុប: 
                     <span id="div_itemtotal" class="item-info-total" style="    font-size: 24px;    font-weight: bold;">

                    </span>
                </td>
                <td>
                    <div style="text-align:right;padding-bottom: 15px;" id="div_additem">
                    <span class="button-buy" onclick="additem()" style="background-color: green;font-weight: bold;">ទិញ</span>
                   </div>
                    <div style="text-align:right;padding-bottom: 15px;" id="div_edititem">
                        <span class="button-buy" onclick="deleteitem()" style="background-color: #b23;font-weight: bold;">លុប</span>
                        <span class="button-buy" onclick="saveedititem()" style="background-color: green;font-weight: bold;">កែប្រែ</span>
                    </div>
                </td>
            </tr>
        </table>
        </div>
        </div>

            
        </div>
    </div>
    <div class="site_wrapper" id="div_orderpage" style="display:none;">
          <div id="dialog" title="Basic dialog">
              <p id="dialogdetail"></p>
            </div>
    </div>

            <header id="header">

                <div class="container-fluid" style="display:none;">
                    <div class="header-top navbar-fixed-top">
                        <div class="row">
                            <div class="container">
                                <div>
                                    <div class="small-devices" style="display:none;">
                                        <div class="">
                                            <div class="header-top-left" >
                                                <table style="width:100%">
                                                    <tr style="width:100%">
                                                        <td align="left">
                                                            <div class="menu-dashboard">
                                                                <asp:LinkButton ID="lnkDashboard" runat="server" ForeColor="White">Go to Dashboard</asp:LinkButton>
                                                            </div>
                                                            
                                                            <div class="welcome-username">
                                                                <span>Welcome:</span>
                                                                        <asp:Label ID="lblUser_Login" runat="server" Text="David" Font-Bold="true"></asp:Label>
                                                                <div id="div_credit" runat="server">

                                                                </div>
                                                            </div>
                                                        </td>
                                                        <td align="right" style="width:100px;">
                                                            <table>
                                                                <tr>

                                                                    <td align="right" style="padding-right:25px">
                                                                        <a href="SignInweb.aspx" 
                                                                            style="color: #FFFFFF; font-size: large; font-weight: bold;">
												                        Log Out
												                        
												                        </a>
                                                                        <asp:ScriptManager ID="ScriptManager1" runat="server">
                                                                        </asp:ScriptManager>
                                                                    </td>
                                                                </tr>
                                                            </table>
                                                        </td>
                                                        
                                                    </tr>
                                                </table>
                                                
                                            </div>
                                            <div class="col-md-6 col-xs-6 col-sm-6 header-top-right" 
                                                style="padding-right: 45px">
                                                <div class="row">
                                                    <div class="col-md-2 col-xs-6 col-sm-6 logo"></div>
                                                    <div class="col-xs-6 toggle-menu pull-right">
                                                        <a id="toggle-menu" class="menu-trigger" href="javascript:void(0)">
                                                            <div>
                                                                <span class="top"></span>
                                                                <span class="middle"></span>
                                                                <span class="bottom"></span>
                                                            </div>
                                                        </a>
                                                    </div>
                                                    <div class="col-md-16 col-sm-16 top-menu-right">
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <div style="background-color:#04a9d9">

                <div class="container" style="margin-top: 0px" runat="server" visible="false">
                    <div class="row">
                        <!--header top-->
                        <div class="header-box">
	<ul id="navbar" class="nav navbar-nav navbar-collapse">

						<li class="col-md-1 col-xs-6 active">
							
							
							
								<table>
                                <tr>
                                <td>

                                <asp:ImageButton ID="ImageButton1" runat="server" 
                                ImageUrl="~/Images/Logo/sell.png" Height="30px"></asp:ImageButton>
                                </td>
                                <td><span class="box-text"><asp:LinkButton ID="LinkButton1" runat="server">ការលក់</asp:LinkButton></span></td>
                                </tr></table>
								                 							

						</li>
     

						<li class=" active col-md-1 col-xs-6">
							<table>
                            <tr>
                            <td><img src="Images/Logo/item.png" id="loadingweb" height="30px" ></td>
                            <td><span class="box-text"><asp:LinkButton ID="LinkButton2" runat="server">ទំនិញ</asp:LinkButton></span></td></tr></table>
                         	
						</li>
     

						<li class="col-md-1 col-xs-6">
                        <table>
                        <tr>
                        <td>
                        <span class="icon">
								<img src="Images/Logo/stock.png" id="loadingdb" height="30px">
								</span>
                        </td>
                        <td>
                        <span class="box-text"><asp:LinkButton ID="LinkButton3" runat="server">ស្តុក</asp:LinkButton></span>
                        </td></tr></table>
								
							

						</li>
     
		
						<li class="col-md-1 col-xs-6">
                        <table>
                        <tr>
                        <td>
                        <span class="icon">
								<img src="Images/Logo/supplier.png" id="loadingemail" height="30px">
								</span>
                        </td>
                        <td>
                        <span class="box-text"><asp:LinkButton ID="LinkButton4" runat="server">អ្នកផ្គត់ផ្គង</asp:LinkButton></span>
                        </td></tr></table>
								
						</li>
			
     
		
						<li class="col-md-1 col-xs-6">
							<table>
                            <tr>
                            <td>
                                        <asp:ImageButton ID="imgInvoice" runat="server" 
                                ImageUrl="~/Images/Logo/history.png" Height="30px"></asp:ImageButton>
				
                            </td>
                            <td>
                            <span class="box-text"><asp:LinkButton ID="LinkButton5" runat="server">វិកយបត្រ</asp:LinkButton></span>
                            </td></tr></table>
                        	
						</li>
     

						<li class="col-md-1 col-xs-6">
							<table>
                            <tr>
                            <td>
                            	<span class="icon">
                           
                                <asp:ImageButton ID="imgReport" runat="server" 
                                ImageUrl="~/Images/Logo/report.png" height="30px"></asp:ImageButton>
								</span>
                            </td>
                            <td>
                            <span class="box-text"><asp:LinkButton ID="LinkButton6" runat="server">របាយការណ៍</asp:LinkButton></span>
                            </td></tr></table>
                            
                        	
						</li>

						<li class="col-md-1 col-xs-6">
                        <table>
                        <tr>
                        <td>
                        	<span class="icon">
                                <asp:ImageButton ID="imgUser" runat="server" 
                                ImageUrl="~/Images/Logo/user.png" height="30px"></asp:ImageButton>
								</span>
                        	
                        </td>
                        <td>
                                                    <span class="box-text"><asp:LinkButton ID="LinkButton7" runat="server">អ្នកប្រើប្រាស់</asp:LinkButton></span>

                        </td></tr></table>
							
						</li>
     

					</ul>


                        </div>
                    </div>
                </div>	
                
                </div>
            </header>
            <!-- end header-->
 
         <asp:HiddenField ID="hfCustomerId" runat="server" />
<!-- content area-->
    <div>

	<div class="main-content caching crypto-main speed-main">

    <div class="container" >
      <div class="row" id="pnOrderList" style="display:none;">
        <div class="receipt" style="padding:0px;">
     	  <div class="receipt-content" style="padding:0px;">
              <div class="div-back">
                  <span onclick="closeorder()">< Back </span>
              </div>

               <div class="receipt-paper" style="padding: 5px;background-color: white;">
	
                                <div style="padding: 5px; background-color: white;">
                                <table width="100%">
                                <tbody>
                                    
                                <tr>
                                    <td valign="top" style="padding: 5px;">
                                        <div>
                                        <div style="display:none;">
                                            <table style="width:100%;">
                                                <tr>
                                                    <td style="width:50%"​><span class="Shopping-Title">ការបញ្ជាទិញ</span></td>
                                                    <td>
                                                       
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                        </div>

                                        <div style="min-height:50px">
                                        <div id="div_orderlist">

	                                    </div>
                                        <div id="div_orderdetail">

                                        </div>
                                                
                                               
                                        </div>
                                    </td>
                                </tr>
                               
                            </tbody></table>
                                </div>    
                                    
</div>
     	  </div>
        </div>
		       
                        
</div>

        <div class="row" id="pnReceipt" style="display:none;">
        <div class="receipt" style="padding:0px;"  id="pnReceiptContainer">
     	  <div class="receipt-content" style="padding:0px;">
              <div class="div-back" style="padding-top:20px;">
                  <span onclick="closereceipt()">< Back </span>
              </div>

               <div class="receipt-paper" style="padding: 5px;background-color: white;">
	
                                <div style="padding: 5px; background-color: white;">
                                <table width="100%">
                                <tbody>
                                    <tr style="display:none;">
                                    <td>
                                        <table width="100%" style="color: #000000">
                                            <tbody><tr>
                                                <td colspan="2" style="padding: 5px; ">
                                                    <table width="100%">
                                                        <tbody><tr>
                                                            <td width="70">
                                                                <img id="Img1" src="Images/Logo/SL POWER TOOLS-New.png" height="50px"></td>
                                                            <td style="font-size: large; font-weight: bold" width="130">
                                                                &nbsp;អ៊ឹង សាលីន</td>
                                                            <td align="right">
                                                                <table>
                                                                    <tbody><tr>
                                                                        <td width="100px">
                                                                            <input type="button" value="បង់ប្រាក់" id="btnPay" class="btn btn-default" style="background-color:#CCFFCC;font-weight:bold;width:80px;"​ onclick="addorder()" />
                                                                        </td>
                                                                    </tr>
                                                                    <tr>
                                                                        <td>
                                                                            <input onclick="clearordertemp()" type="button" value="លះបង់" id="btnCancelReceipt" class="btn btn-default" style="background-color:#FF9966;font-weight:bold;width:80px;display:none;" />
                                                                        </td>
                                                                    </tr>
                                                                </tbody></table>
                                                                
                                                            </td>
                                                        </tr>
                                                    </tbody></table>
                                                </td>
                                            </tr>
                                            <tr style="border-top-style: dashed; border-width: thin; border-color: #333333">
                                                <td style="font-size: small;" valign="top" colspan="2">
                                                    អាសយដ្ឋាន៖ ផ្ទះលេខ 394 ផ្លូវម៉ៃដា 2004 សង្កាត់កាកាប ខណ្ឌដង្កោ ភ្នំពេញ<br> Tel: 070354588 / 0968255525 / 0976255525</td>
                                            </tr>
                                            <tr style="border-top-style: dashed; border-width: thin; border-color: #333333">
                                                <td style="padding: 15px 3px 3px 3px" valign="top">
                                                    <span id="lblReceiptNo" style="color:Black;font-weight:bold;">វិកយបត្រលេខ: 000000</span>
                                                    <br>
                                                    <span id="lblCustomer" style="color:Black;font-weight:normal;">អតិថិជនៈ</span>
                                                </td>
                                                <td align="right" style="padding: 15px 3px 3px 3px" valign="top">
                                                    ថ្ងៃទី:<span id="lblReceiptDate" style="color:Black;">01/02/2003 00:00</span>
                                                </td>
                                            </tr>
                                        </tbody></table>
                                    </td>
                                </tr>
                                <tr>
                                    <td valign="top" style="padding: 5px;">
                                        <div id="div_order_command">
                                        <div>
                                            <table style="width:100%;">
                                                <tr>
                                                    <td style="width:50%"​><span class="Shopping-Title">ការបញ្ជាទិញ</span></td>
                                                    <td>
                                                        <ul class="cart-desktop_cart-steps">
                                                            <li style="list-style:none;list-style-type:none;"><span id="span_step1" class="cart-desktop_active main_action-bg">1</span></li>
                                                            <li ><span  id="span_step2" class="main_action-bg">2</span></li>
                                                        </ul>
                                                    </td>
                                                </tr>
                                            </table>
                                        </div>
                                        <div style="text-align:center;">
                                            <div id="div_nextstep" class="main_button-md" onclick="nextstep()">បង់ប្រាក់
                                                <i class="fa fa-angle-right icon-font_font icons_icon-arrow-cart main_button-icon"></i>
                                            </div>
                                        </div>
                                        </div>

                                        <div style="min-height:50px">
                                        <div id="div_invoiceitemlist">

	                                    </div>
                                                <div id="div_checkout" class="div-checkout" style="display:none;">
                                                    <div class="radio-button_item-container-mobile">
                                                        <div class="radio-button_info-container">
                                                            <div class="radio-button_item-info">
                                                                <div class="radio-button_body-text">
                                                                    <div>ម៉ោងដឹកជញ្ជូន៖</div>
                                                                    <input type="checkbox" id="chktransport"​ checked="checked" />
                                                                    <label for="chktransport">ម៉ោង ៩ព្រឹក-៦ល្ងាច</label>

                                                                </div>
                                                                <div class="radio-button_body-text">
                                                                    <div>ទូទាត់ជា៖</div>
                                                                    <input type="checkbox" id="rdoCash"  checked="checked"/>
                                                                    <label for="chktransport">សាច់ប្រាក់</label>

                                                                </div>
                                                                <div class="radio-button_body-text" style="text-align:center;">
                                                                    <div>សរុប៖ <span id="spanTotal" class="total-cost"></span></div>
                                                                    

                                                                </div>

                                                                  </div>
                                                        </div>
                                                    </div>
                                                    <div style="padding: 10px 10px 50px 10px;display:none;">
                                                         <div style="color:red;text-align: center;">
                                                             <span id="div_paynow_info" style="color:red;">

                                                             </span>
                                                         </div>
                                                        <div>           
                                                                            <table width="100%">
                                                                                <tbody>
                                                                                <tr>
                                                                                    <td width="100px">
                                                                                        &nbsp;</td>
                                                                                    <td align="right" colspan="2">
                                                                                        <span id="lblExchangeRate" title="4050">1$=4050R</span>
                                                                                    </td>
                                                                                </tr>

 
                                                                                <tr>
                                                                                    <td>
                                                                                        &nbsp;អតិថិជនៈ&nbsp;</td>
                                                                                    <td colspan="2" style="padding: 5px" width="100%">
                                                
                                                                                        <table>
                                                                                            <tbody><tr>
                                                                                                <td>
                                                                                                     ទូរស័ព្ទ:
                                                                                                </td>
                                                                                                <td style="padding-left:10px">
                                                                                                    <input type="text" onchange="loadcustomerinfo(this)" id="txtSearchPhone_Pay" class="form-control input-sm" style="width:100%;" autocomplete="off">
                                                        
                                                            
                                                                                                </td>
                                                                                            </tr>
                                                                                        </tbody></table>
                                                                       
                                                                                        </td>
                                            
                                                                                </tr>
                                                                                <tr>
                                                                                    <td>
                                                                                        &nbsp;&nbsp;</td>
                                            
                                                                                    <td colspan="2" style="padding: 5px" width="40%">
                                                                                       <table>
                                                                                            <tbody><tr>
                                                                                                <td>
                                                                                                     ឈ្មោះ:
                                                                                                </td>
                                                                                                <td style="padding-left:10px">
                                                                                                    <span id="lblSearchCustomer_Pay" style="color:Black;font-weight:normal;"></span>
                                                            
                                                            
                                                                                                </td>
                                                                                            </tr>
                                                                                        </tbody></table>

                                                                                    </td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td>
                                                                                        &nbsp;</td>
                                                                                    <td style="padding: 5px" width="33%">
                                                                                        ដុល្លា($)</td>
                                                                                    <td style="padding: 5px" width="33%">
                                                                                        រៀល(R)</td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        សរុប:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" value="$0.25" readonly="readonly" id="txtTotalToPay" class="form-control input-sm" style="color:Black;width:100%;" />
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" value="R0" readonly="readonly" id="txtTotalToPayR" class="form-control input-sm" style="color:Black;width:100%;display:none" />
                                                                                    </td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        ភាគរយបញ្ចុះតម្លៃ:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <table>
                                                                                            <tbody><tr>
                                                                                                <td>
                                                                                                    <input type="text" onchange="discountchange(this)" id="txtPercentageDiscount" class="form-control input-sm" style="width:50px;" />
                                                                                                </td>
                                                                                                <td>
                                                                                                    %</td>
                                                                                            </tr>
                                                                                        </tbody></table>
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        &nbsp;</td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        បញ្ចុះតម្លៃជាទឹកប្រាក់:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" id="txtDiscountPay" class="form-control input-sm" style="width:100%;" />
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" id="txtDiscountPayReil" class="form-control input-sm" style="width:100%;display:none;" />
                                                                                    </td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        សរុបចុងក្រោយ:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input name="txtGrandTotalToPay" type="text" readonly="readonly" id="txtGrandTotalToPay" class="form-control input-sm" style="color:Black;width:100%;" />
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        <input name="txtGrandTotalToPayReil" type="text" readonly="readonly" id="txtGrandTotalToPayReil" class="form-control input-sm" style="color:Black;width:100%;display:none;" />
                                                                                    </td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        ទទួលប្រាក់:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input name="txtPayDollar" type="text" onchange="paidamountchange()"  id="txtPayDollar" class="form-control input-sm" style="width:100%;" />
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        <input name="txtPayReil" type="text" onchange="paidamountchange()" id="txtPayReil" class="form-control input-sm" style="width:100%;" />
                                                                                    </td>
                                                                                </tr>
                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        ប្រាក់អាប់:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" readonly="readonly" id="txtReturnDollar" class="form-control input-sm" style="color:Black;width:100%;" />
                                                                                    </td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" readonly="readonly" id="txtReturnReil" class="form-control input-sm" style="width:100%;display:none;" />
                                                                                    </td>
                                                                                </tr>

                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        ពិន្ទុ:</td>
                                                                                    <td style="padding: 5px">
                                                                                        <input type="text" id="txtGetPoint" class="form-control input-sm" style="color:Black;width:100%;" />
                                                                                    </td>
                                                                                    <td>
                                                                                        <div style="text-align:center;">
                                                                                        <span id="div_progress" style="display:none;">
                                                                                            <img src="Images/progress.gif" style="height:30px;" />
                                                                                        </span>
                                                                                        </div>
                                                                                    </td>
                                                                                </tr>

                                                                                <tr>
                                                                                    <td style="padding: 5px">
                                                                                        </td>
                                                                                    <td colspan="2">
                                                                                        
                                                                                        <input id="rdoBank" type="radio" name="PaymentType" value="rdoBank" /><label for="rdoBank">Bank</label>
                                                                                    </td>
                                                                                </tr>
                                       
                                                                                <tr>
                                                                                    <td>
                                                                                        &nbsp;</td>
                                                                                    <td>
                                                                                        &nbsp;</td>
                                                                                    <td align="right">
                                                                                        &nbsp;</td>
                                                                                </tr>
                                                                            </tbody></table>
                                
                                
                                
                                        </div>
                                                                        <div style="height: 50px">
                                                                            <table width="100%" align="right">
                                                                            <tbody><tr>
                                                                           <td>
                                                                                        &nbsp;</td>
                                                                                    <td align="right">
                                                
                                               
                                                                                </td>
                                                                            </tr></tbody></table>
                                   
                                                                        </div>
                                                                        </div>
                                                </div>
                                               
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <table width="100%">
                                            <tbody><tr>
                                                <td style="padding: 3px">
                                                    <table width="100%">
                                                        <tbody><tr>
                                                            <td width="50%">
                                                                <div style="padding: 5px; margin: 3px;display:none;">
                                                                    <table align="center" cellspacing="5" width="95%">
                                                                        <tbody><tr>
                                                                            <td width="120px" height="20">
                                                                                <span id="Label4" style="color:Black;font-weight:bold;">សរុប</span>
                                                                            </td>
                                                                            <td style="">
                                                                                <span id="lblSubTotal" style="color:Black;font-size:14pt;font-weight:bold;border-bottom-style: double; border-bottom-width: thin; border-bottom-color: #000000">$0.00</span>
                                                                            </td>
                                                                            <td style="border-bottom-style: double; border-bottom-width: thin; border-bottom-color: #000000">
                                                                                <span id="lblSubTotalR" style="color:Black;font-size:11pt;font-weight:bold;display:none;"></span>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td height="20">
                                                                                <span id="Label8" style="color:Black;">បញ្ចុះតម្លៃ:</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblGrandDiscount" autopostback="True" style="display:inline-block;color:Black;border-style:None;">$0.00</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblGrandDiscountReil" autopostback="True" style="display:inline-block;color:Black;border-style:None;"></span>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td height="20">
                                                                                <span id="Label9" style="color:Black;font-weight:bold;">សរុបចុងក្រោយ</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblGrandTotal" style="color:Black;font-weight:bold;">$0.00</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblGrandTotalReil" autopostback="True" style="display:inline-block;color:Black;border-style:None;font-weight:bold;"></span>
                                                                            </td>
                                                                        </tr>
                                                                        <tr style="display:none;">
                                                                            <td height="20">
                                                                                <span id="Label6" style="color:Black;">ប្រាក់បានទទួល:</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblReceivedDollar" style="color:Black;">$0.00</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblReceivedReil" style="color:Black;">R0</span>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td height="20">
                                                                                <span id="Label7" style="color:Black;font-weight:bold;">&ZeroWidthSpace;ប្រាក់អាប់:</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblReturnDollar" autopostback="True" style="display:inline-block;color:Black;border-style:None;font-weight:bold;">$0.00</span>
                                                                            </td>
                                                                            <td>
                                                                                <span id="lblReturnReil" style="color:Black;font-weight:bold;"></span>
                                                                            </td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td colspan="2" height="20">
                                                                                &nbsp;</td>
                                                                            <td height="20">
                                                                                
                                                                            </td>
                                                                        </tr>
                                                                    </tbody></table>
                                                                </div>
                                                            </td>
                                                        </tr>
                                                    </tbody></table>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td>
                                                   
                                                </td>
                                            </tr>
                                        </tbody></table>
                                    </td>
                                </tr>
                            </tbody></table>
                                </div>    
                                    
</div>
     	  </div>
        </div>
		  <div class="col-md-12 caching-box polish crypto-ssl" style="margin-top:5px;padding:0px;">
                	<div class="row">
         </div>       	
                         <div class="div-back" style="background-color: transparent;box-shadow: none;border: none;right: 0px;width: 100px;float: right;display:none;padding-top:20px;">
                             <table style="width:100%">
                                 <tr>
                                     <td>
                                           <div class="welcome-username" style="display:none;">
                                              <img src="images/logo/userpro2.png" style="height: 50px;">
                                                      <asp:Label ID="lblUsername" runat="server" Text="David" Font-Bold="true"></asp:Label>
                                               <div id="div1" runat="server"></div>
                                          </div>
                                     </td>
                                     <td style="text-align:right;">
                                         <span id="btnhome" class="shopping-home" onclick="gohome()" style="right: 10px;display:none;"><i class="fa fa-home" style="font-size: 40px;    color: white;    line-height: 50px;"></i></span>
                                     </td>
                                 </tr>
                             </table>
                           

                         </div>
                        <div class="panel-item status-box-left crypto-ssl-left" id="div_item" style="width:100%;display:none;">
                           
                            <div style="padding: 5px;">
                            <div 
                                    class="frame_item">
                            <div id="div_top_sticky" class="top-sticky">
                            <div style="position: absolute;width: 100%;padding-top:20px;">
                                <table style="width:100%;">
                                    <tr>
                                        <td>
                                             <div class="dropdown shopping-home-mobile">
                                              <input type="checkbox" id="menu-toggle" onclick="showmenu()">
                                              <label for="menu-toggle" class="">
                                                  <i class="fa fa-bars" style="font-size: 20px;color: white;line-height: 30px;"></i>  
                                              </label>
  
                                              <div class="menu-content">
                                                <div class="div-back" style="padding-top:20px;">
                                                    <div>
                                                        
                                                        <table style="width:100%;">
                                                            <tr>
                                                                <td style="text-align:left;">DALIN</td>
                                                                <td style="text-align:right;">
                                                                    <span onclick="closemenu()" class="shopping-home-mobile" style="float: right;  color: white;    font-size: 24px;    line-height: 30px;    font-family: cursive;    background-color: red;">X</span>

                                                                </td>
                                                            </tr>
                                                        </table> 
                                                    </div>
                                                </div>
                                                <div class="menu-list">
                                                <div onclick="showorderlist()" style="display:block; padding:10px;">Order History</div>
                                                <div id="divlogout" onclick="logout()" style="display:block; padding:10px;" runat="server">Log out</div>
                                                </div>
                                              </div>
                                            </div>
                                        </td>
                                        <td style="text-align:right;">
                                            <span class="shopping-home-mobile" onclick="closesearch()" id="btnclosesearch" style="float: right;margin-right: 10px;background-color: red;display:none;" runat="server">
                                                <i class="fa fa-close" style="font-size: 20px;color: white;line-height: 30px;"></i>      
                                            </span>
                                             
                                        </td>
                                        
                                        
                                    </tr>
                                </table>
                            </div>

                            <div id="div_header">
                            <div id="div_banner">
                                <img src="images/logo/banner 02.png?v2" style="width:100%"/>
                            </div>
                            <div id="div_search" style="display:none;">
                        	<table width="100%">
                                <tr>
                                    <td style="padding: 5px; font-size: large; font-weight: bold;">
                                        <table style="width:100%">
                                            <tr>
                                            <td colspan="3">
                                              ជ្រើសរើសប្រភេទទំនិញ

                                            </td>
                                            </tr>
                                            <tr>
                                            <td>
                                                 
                                            </td>
                                            <td>
                                                <div id="div_searchtype_mobile">

                                                </div>
                                            </td>
                                            <td style="text-align:right;width:200px;">
                                                <table>                                                   
                                                    <tr>
                                                        <td>
                                                            <input type="text" id="txtbarcodenew" style="width:150px;" class="textbox-barcode form-control input-sm"  onkeyup="filter_result(this)" onchange="changesearch(this)" />
                                                        </td>
                                                        <td>
                                                            <span class="button-search" style="display:none;" ><i style="line-height:30px;display:none;" class="fa fa-search" aria-hidden="true" onclick="search()"></i></span>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </td>
                                            </tr>
                                        </table>
                                                                            
                                    </td>
                                 </tr>
                                <tr style="display:none;">   
                                    <td>
                                        <div class="row" style="padding:5px;">
                                            <div class="col-lg-2" style="padding: 6px 15px;">
                                                ស្វែងរកតាមលេខកូដ:
                                            </div>
                                            <div class="col-lg-5"​ style="display:none;">
                                                <asp:DropDownList ID="dplField" runat="server" 
                                                                    Width="100%" CssClass="form-control input-sm" AutoPostBack="True">
                                                                    <asp:ListItem Value="code">លេខកូដ</asp:ListItem>
                                                                    <asp:ListItem>ឈ្មោះទំនិញ</asp:ListItem>
                                                                </asp:DropDownList>
                                            </div>
                                            <div class="col-lg-5">
                                                <table>
                                                    <tr>
                                                        <td>
                                                            
                                                            
                                                        </td>
                                                        <td>
                                                            
                                                        </td>
                                                    </tr>
                                                </table>
                                                 
                                                        

                                            </div>
                                        </div>
                                        
                                    </td>
                                </tr>
                            </table>
                            </div>
                            </div>
                            <div id="div_Category" runat="server" style="display:none;" class="top-category">
                            <div class="col-md-3 col-sm-6 status-box-left crypto-ssl-left">
                                <div style="background-color: #999999; color: #FFFFFF; font-weight: bold">&nbsp;</div>
                                <div style="text-align: center">title</div>

                            </div>
                            <div class="col-md-3 col-sm-6 status-box-left crypto-ssl-left">
                                </div>
                            <div class="col-md-3 col-sm-6 status-box-left crypto-ssl-left">
                            </div>
                            <div class="col-md-3 col-sm-6 status-box-left crypto-ssl-left">
                            </div>
                            </div>
                            <div id="div_group_mobile" class="group-box-mobile">

                            </div>
                            </div>
                          
 <div id="div_top_sticky_left" class="top-sticky-left">
           <table style="width:100%">
               <tr>
                <td>
                     <span class="shopping-home-mobile" onclick="gohome()" id="Span1" style="" runat="server">
                         <i class="fa fa-home" style="font-size: 20px;color: white;line-height: 30px;"></i>      
                     </span>
                </td>
                <td>
                    <div id="div_searchtype_desktop">

                    </div>
                   
                </td>
                <td style="text-align:right;">
                    
                    <input id="txtsearchleft" type="text" style="width:90%;" class="textbox-barcode form-control input-sm" onkeyup="filter_result(this)" onchange="changesearch(this)" />
                    
                </td>
                <td style="text-align:right;">
                    <span class="button-search" style="display:none;"><i style="line-height:30px;display:none;" class="fa fa-search" aria-hidden="true" onclick="search()"></i></span>
                </td>
                </tr>
            </table>
                  <div>
                      ប្រភេទទំនិញ
                  </div>                            
 
<div id="div_Category_left" runat="server" style="display:none;" class="top-category-left">

</div>
</div>
                                     <div id="div_items" runat="server">

                                    </div>
                                    <div id="div_group_desktop" class="group-box-desktop"> </div>
                                        <div id="div_itembycategory" class="item-bycategory"></div>
                            </div>
                            </div>			

<!---code for myFormModal --->			

<%--<script src="POS_files/bootstrap.js"></script>--%>


</div>
</div>
</div>

            
    </div>
    </div>
    </div>

    

    <div>


<div class="modal fade" id="myModal" role="dialog">
    <div class="modal-dialog dialog_popup">
    
      <!-- Modal content-->
      <div class="modal-content">
                        <div class="modal-header">
                            
                            <button type="button" class="close" onclick="back()">
                                X</button>
                            <h4 class="modal-title">
                                ជ្រើសរើសទំនិញ</h4>
                        </div>
        <div class="modal-body" id="div_popup" runat="server">
            
                <div id="div_popup_content" runat="server">

                </div>
        
                
        </div>
        <div style="padding: 10px;overflow:hidden;display:none;" align="right">
                            <div style="padding: 60px">
                            &nbsp;
                            </div>
                            <button type="button" class="btn btn-default" data-dismiss="modal">
                                Close</button>
                            
                        </div>


      </div>
      
    </div>
  </div>


  <div class="modal fade" id="myModal2" role="dialog">
    <div class="modal-dialog dialog_popup2">
    
      <!-- Modal content-->
      <div class="modal-content">
                        <div class="modal-header">
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span></button>
                            <h4 class="modal-title">
                                ជ្រើសរើសទំនិញ</h4>
                        </div>
        <div class="modal-body" id="div_popup2" runat="server">
                    <asp:UpdatePanel ID="UpdatePanel2" runat="server" style="padding:10px">
                    <ContentTemplate>
                    
                     <asp:Panel ID="UserPanel" runat="server" DefaultButton="btnAdd">
                         
                                           
                
                    <table style="width:100%">
                         <tr >
                    <td colspan="2">
                        <div style="text-align:center">                          
                            <asp:Image ID="img_item" CssClass="img-item" runat="server" />
                        </div>
                    </td>
                    </tr>
                        <tr style="height:50px">
                    <td>ទំនិញ:</td><td>
                        <asp:TextBox ID="txtItem" runat="server" 
                                    CssClass="form-control input-sm" Enabled="False"></asp:TextBox>
                     </td>
                    </tr>
                    
                        <tr style="height:50px">
                    <td>ចំនួន:</td><td>
                             <asp:TextBox ID="txtQty_Select" runat="server" 
                                    CssClass="form-control input-sm" AutoPostBack="False" ></asp:TextBox>
                                </td>
                    </tr>

                        <tr style="height:50px;display:none;">
                            <td>
                                ឯកតា:</td>
                            <td>
                                <asp:DropDownList ID="dplUnit" runat="server" AutoPostBack="True" 
                                    CssClass="form-control input-sm" Enabled="False">
                                </asp:DropDownList>

                            </td>
                        </tr>

                        <tr style="height:50px">
                            <td>
                                តម្លៃ:</td>
                            <td>
                                <asp:TextBox ID="txtprice" runat="server" 
                                    CssClass="form-control input-sm" Enabled="False"></asp:TextBox>
                            </td>
                        </tr>
                       
                        <tr style="height:50px">
                            <td>
                                រូបិយបណ្ណ:</td>
                            <td>
                                <asp:DropDownList ID="dplCurrencyUnit" runat="server" 
                                    CssClass="form-control input-sm" Enabled="False">
                                    <asp:ListItem>$</asp:ListItem>
                                    <asp:ListItem>R</asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>

                        <tr style="height:50px">
                            <td>
                            </td>
                            <td>
                                <asp:Button ID="btnAdd" runat="server" Text="បញ្ចូល" CssClass="btn btn-default"
                                                                Width="80px" BackColor="#CCFFCC" />
                            </td>
                        </tr>

                        <tr >
                            <td colspan="2">

                                <div style="margin-top:20px;margin-bottom:20px">
                                                        <%--<asp:FileUpload ID="FileUpload_Item_POS" runat="server" />
                                                        <asp:Button ID="cmd_Upload_Item_POS" runat="server" Text="Upload" OnClick="UploadFile_Item_POS" />
                                                        </ br>
                                                        <asp:Label ID="lblInfo_Image_Upload_Item_POS" runat="server"></asp:Label>--%>
                                </div>

                            </td>
                            <td>
                                <asp:Label ID="lblInfo" runat="server" Text=""></asp:Label>
                            </td>
                        </tr>

                    </table>
                        
                         </asp:Panel>
                    </ContentTemplate>
                    </asp:UpdatePanel>            

                
        </div>
        
      </div>
      
    </div>
  </div>

    <div class="modal" id="myModal3" role="dialog">
  <div class="modal-dialog dialog_popup2">
  
    <!-- Modal content-->
    <div class="modal-content">
                      <div class="modal-header">
                          <button type="button" class="close" data-dismiss="modal" aria-label="Close" style="display:none;">
                              <span aria-hidden="true">&times;</span></button>
                          <h4 class="modal-title">
                              ការបញ្ជាទិញ</h4>
                      </div>
      <div class="modal-body" id="div_orderstatus" runat="server">
                         កំពុងដំណើរការ...
          
              
      </div>
      
    </div>
    
  </div>
</div>

  </div>
    </form>

<script src="https://unpkg.com/cropperjs@1.5.13/dist/cropper.min.js"></script>
    <script>
        (function(){
            var fileInput = document.getElementById('<%= fuFile.ClientID %>');
            var img = document.getElementById('previewImage');
            var cropper = null;
            var originalName = '';

            fileInput.addEventListener('change', function(e){
                var f = this.files && this.files[0];
                if(!f) return;
                originalName = f.name;
                var url = URL.createObjectURL(f);
                img.src = url;
                img.style.display = 'block';
                if(cropper){ cropper.destroy(); cropper = null; }

                // Auto-init cropper once the image has loaded
                img.onload = function(){
                    initCrop();
                };
            });

            function initCrop(){
                if(!img.src || img.style.display==='none') { return; }
                if(cropper){ cropper.destroy(); }
                cropper = new Cropper(img, { aspectRatio: NaN, viewMode: 1, autoCropArea: 0.8 });
                document.getElementById('cropStatus').textContent = '';
                document.getElementById('btnCropUpload').disabled = false;
            }
 
          
            document.getElementById('btnCropUpload').addEventListener('click', function(){
                if(!cropper){ alert('Initialize cropper first'); return; }
                document.getElementById('cropStatus').textContent = 'Processing...';
                var canvas = cropper.getCroppedCanvas({ maxWidth: 1200, maxHeight: 1200, imageSmoothingQuality: 'high' });
                if(!canvas){ alert('Failed to get cropped canvas'); return; }
                var dataUrl = canvas.toDataURL('image/jpeg', 0.9);

                var fd = new FormData();
                fd.append('image', dataUrl);
                let itemid = selecteditemupload.ItemID;
                fd.append('itemid', itemid);

                fetch('UploadCrop.ashx', { method: 'POST', body: fd }).then(function(res){
                    return res.json();
                }).then(function(j){
                    if(j && j.success){
                        document.getElementById('cropStatus').textContent = 'Uploaded: ' + j.name;
                        closeupload();
                        $("#txtbarcodenew").val(selecteditemupload.ItemCode);
                        search();
                    } else {
                        document.getElementById('cropStatus').textContent = 'Error: ' + (j && j.message ? j.message : 'unknown');
                    }
                }).catch(function(err){
                    document.getElementById('cropStatus').textContent = 'Upload failed';
                });
            });
        })();
    </script>

</body>
</html>

