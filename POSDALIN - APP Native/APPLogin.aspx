<%@ Page Language="VB" AutoEventWireup="false" CodeFile="APPLogin.aspx.vb" Inherits="_Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">

    <title>Sign In</title>
     <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <script src="Scripts/jquery-1.8.3.min.js" type="text/javascript"></script>
    <script src="scripts/site.js?v=1" type="text/javascript"></script>
        <link rel="stylesheet" href="POS_files/style.css">
<link rel="stylesheet" href="POS_files/font-awesome.css">
    <link href="css/mainclient.css?v7" rel="stylesheet" type="text/css" />
            <link href="https://fonts.googleapis.com/css2?family=Koh+Santepheap:wght@300&amp;display=swap" rel="stylesheet">

    <style>
  
    </style>
    
</head>
<body bgcolor="#D4D3C7" class="body-order">
    <form id="form1" runat="server" defaultbutton ="btnSignIn">
   
	<div class="main-content caching crypto-main speed-main">

    <div class="container" style="color:#947f02">
     	  
		  <div class="col-md-12 caching-box polish crypto-ssl" style="max-width:512px">
                	     <div style="padding: 5px;">
                                <div style="padding: 5px; ">
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            <div id="content">
                    <div class="title_h1" style="background:none;">
                         <asp:Image ID="Image2" runat="server" BorderStyle="None" Height="70px" 
                                    ImageUrl="~/Images/Logo/SL POWER TOOLS-New.png" style="text-align: right" />
                    <h1 class="green" style="margin-left:10px">Log In</h1>
                    </div>
             
                    <div class="boxbody">

                    <asp:Panel ID="Panel1" runat="server">
                        <asp:MultiView ID="MultiView1" runat="server">
                        <asp:View ID="View1" runat="server">
                    <table align="center" width="90%">
                    <tr>
                 
                    <td colspan="2" width="100%">
                    <p id="lblErrAll" runat="server" visible="false" class="error"></p></td></tr>
                    <tr>
                    <td>
                        <strong style="text-align: left">
                        <br />
                        User Name:</strong></td>
                        <td style="width: 50%" width="100%">
                            &nbsp;</td>
                        </tr>
                    <tr>
                    <td style="width: 0%" width="100%">
                        <asp:TextBox ID="txtUserName" runat="server" Width="225px" 
                            CssClass="txt-app"></asp:TextBox></td>
                    <td style="width: 50%" width="100%">
                    <p id="lblErrUserName" runat="server" visible="false" class="error">User Name is required.</p>
                        </td></tr>
                    <tr>
                    <td width="100%" style="width: 0%">
                        &nbsp;</td>
                        <td style="width: 50%" width="100%">
                            &nbsp;</td>
                        </tr>
                    <tr>
                    <td width="100%" style="width: 0%">
                        <strong>Password:</strong></td>
                        <td style="width: 50%" width="100%">
                            &nbsp;</td>
                        </tr>
                    <tr>
                    <td style="width: 0%" width="100%">
                        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" Width="225px" 
                            CssClass="txt-app"></asp:TextBox></td>
                    <td style="width: 50%" width="100%">
                    <p id="lblErrPassword" runat="server" visible="false" class="error">Password is required.</p>
                        </td></tr>
                    <tr>
                    <td style="padding-top: 10px; padding-right: 10px; padding-bottom: 10px;text-align:center" >
                        <div>
                        <asp:Button ID="btnSignIn" runat="server" CssClass="btn-sign" 
                            Text="Log in" />
                        </div>
                        <div>
                            <a href="UserOrder.aspx" style="color:#947f02;">ត្រឡប់ដើម</a>
                        </div>

                    </td>
                    <td style="width: 50%" width="100%">
                        &nbsp;</td></tr>
                    <tr>
                    <td width="60" colspan="2">
                        &nbsp;</td>
                        </tr>
                    <tr>
                    <td width="60" colspan="2" class="style1">
                        <asp:Label ID="lblInfo" runat="server" ForeColor="#000099"></asp:Label>
                        </td>
                        </tr>
                        <tr>
                            <td align="right" colspan="2" width="60%">
                               
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" width="60%" align="right">
                                </td>
                        </tr>
                        </table>
                        </asp:View> 
                        <asp:View ID="View2" runat="server">
                <table align="center" width="90%">
                    <tr>
                 
                    <td colspan="2" width="100%" class="style1">
                        </td></tr>
                    <tr>
                    <td width="100%" style="width: 10%">
                        <span class="style2"><strong>Security Checking!</strong></span>&nbsp;</td>
                        <td style="width: 50%" width="100%">
                            &nbsp;</td>
                        </tr>
                    <tr>
                        <td style="width: 10%" width="100%">
                            &nbsp;</td>
                        <td style="width: 50%" width="100%">
                            <p ID="P3" runat="server" class="error" visible="true">
                                Any suspicious use of your account by anyone, please contact IT office (dial 117 
                                or e-mail to: IT-Problems@acecambodia.org)!</p>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 10%" width="100%">
                            <strong>Previous Log in Time:</strong></td>
                        <td style="width: 50%" width="100%">
                            <asp:TextBox ID="txtPreviousLogIn" runat="server" CssClass="textboxstyle1" 
                                Enabled="False"></asp:TextBox>
                            <asp:LinkButton ID="HyperReport" runat="server">Click here to report suspicion...</asp:LinkButton>
                        </td>
                    </tr>
                    <tr>
                    <td style="width: 10%" width="100%">
                        <strong>Current Log in Time:</strong></td>
                    <td style="width: 50%" width="100%">
                        <asp:TextBox ID="txtCurrentLogIn" runat="server" CssClass="textboxstyle1" 
                            Enabled="False"></asp:TextBox>
                 
                        <asp:Label ID="lblInfoReport" runat="server" ForeColor="#000099" 
                            Visible="False"></asp:Label>
                 
                        </td></tr>
                    <tr>
                    <td width="100%" style="width: 0%">
                        &nbsp;</td>
                        <td style="width: 50%" width="100%">
                            &nbsp;</td>
                        </tr>
             
             
                    <tr>
                    <td style="width: 0%" width="100%" valign="top">
                        <asp:LinkButton ID="btnAccept" runat="server" CssClass="button_large">Continue to sign in</asp:LinkButton></td>
                    <td style="width: 50%" width="100%">
                        &nbsp;</td></tr>
                    <tr>
                    <td width="60" colspan="2">
                        &nbsp;</td>
                        </tr>
                    <tr>
                    <td width="60" colspan="2" class="style1">
                        <asp:LinkButton ID="HyperChangePassword0" runat="server">Change Password</asp:LinkButton>
                        </td>
                        </tr>
                        <tr>
                            <td align="right" colspan="2" width="60%">
                                <a href="http://www.cambodia.idp.com" target="_blank">
                                <asp:Image ID="Image1" runat="server" BorderStyle="None" Height="70px" 
                                    ImageUrl="~/Styles/images/IDP logo.JPG" style="text-align: right" 
                                    Width="48px" />
                                </a>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2" width="60%" align="right">
                                ©IDP Education Pty Ltd</td>
                        </tr>
                        </table>                  
                        </asp:View>
                        </asp:MultiView>                   
                        </asp:Panel>

             
                        <br />

                    <div class="boxfooter" style="text-align: center;color:white;">app</div>
                    </div>
                                </div>
                        </div>
            </div>
    </div>
    </div>
   
    </form>
</body>
</html>

