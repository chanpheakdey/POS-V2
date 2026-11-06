var objitem = {};
var arritems = [];
var category_html = "";
var group_html = "";
var item_html = "";
var selectedstage = "";
var selecteditem = {};
var stepname = "";
var pub_editindex = 0;
var pub_itemunit = [];

$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + '=([^&#]*)')
        .exec(window.location.search);

    return (results !== null) ? results[1] || 0 : false;
}





// Cookies
function createCookie(name, value, days) {
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        var expires = "; expires=" + date.toGMTString();
    }
    else var expires = "";

    document.cookie = name + "=" + value + expires + "; path=/";
}


function getdatetime() {
    var currentdate = new Date();
    var datetime = currentdate.getDate() + ""
        + (currentdate.getMonth() + 1) + ""
        + currentdate.getFullYear() + ""
        + currentdate.getHours() + ""
        + currentdate.getMinutes() + ""
        + currentdate.getSeconds();
    return datetime;
}

function eraseCookie(name) {
    createCookie(name, "", -1);
}



function show_cancellist() {
    cacellist = true;
    loadorderlist();
}
function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}


$(document).ready(function () {
    let vieworder = $.urlParam("vieworder");
    if (vieworder == "1" && arritems.length>0) {
        showorderitem();
    }
    
    getallcategory();
    getallitem();
    getallphone();

    let windowsize = $(document).width();
    //alert(windowsize);
    if (windowsize <= 900) {
        $("#pnReceiptContainer").hide();
    }

    $('#myModal').on('shown.bs.modal', function () {
        $(document).off('focusin.modal');
    });
    $('#myModal2').on('shown.bs.modal', function () {
        $(document).off('focusin.modal');
    });

});

function getallcategory() {
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getcategory",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            //console.log("success data:" + data);
            $("#div_Category").html(data.d);
            category_html = data.d;
            selectedstage = "category";
            loadordertemp();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });


}
function getallphone() {

    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/GetAllPhone",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            $("#txtSearchPhone_Pay").autocomplete({
                source: data.d
            });

        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function getallitem() {

    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getItemList",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            $("#txtbarcodenew").autocomplete({
                source: data.d
            });

        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}


var pub_customerinfo = {};
function loadcustomerinfo(e) {
    let phone = $(e).val();
    let obj = {};
    obj.Phone = phone;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/GetCustomerInfo",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            $("#lblSearchCustomer_Pay").html(data.d.CustomerName);
            $("#txtGetPoint").val(data.d.CurrentPoint);
            pub_customerinfo = data.d;
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function discountchange(e) {
    let discountrate = $(e).val();
    if (discountrate>100) {
        alert("តម្លៃធំបំផុត 100");
        $(e).va("0");
    } else {
        let TotalDiscount = 0;
        let TotalDiscountR = 0;
        let discount = discountrate / 100
        let totalToPay = $("#txtTotalToPay").val();
        let totalToPayR = $("#txtTotalToPayR").val();
        TotalDiscount = totalToPay * discount;
        $("#txtDiscountPay").val(TotalDiscount.toLocaleString());
        //Call txtDiscountPay_TextChanged(sender, e)
        TotalDiscountR = totalToPayR * discount;
        $("#txtDiscountPayReil").val(TotalDiscountR.toLocaleString());
        //Call txtDiscountPayReil_TextChanged(sender, e)
        let GrandTotalToPay = (totalToPay - TotalDiscount);
        $("#txtGrandTotalToPay").val(GrandTotalToPay.toLocaleString());
        let GrandTotalToPayR = (totalToPayR - TotalDiscountR);
        $("#txtGrandTotalToPayReil").val(GrandTotalToPayR.toLocaleString());

    }

}

var pub_exchangerate = 4000;
function getexchangerate() {

    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/GetExchangeRate",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            pub_exchangerate = data.d;
            $("#lblExchangeRate").html("1$=" + data.d + "R");
            loadcheckout();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}
function paidamountchange() {
    let GTotal = $("#txtGrandTotalToPay").val();
    let GTotalR = $("#txtGrandTotalToPayReil").val();
    let PayDollar = $("#txtPayDollar").val();
    let PayReil = $("#txtPayReil").val();


    

    let obj = {};
    obj.GTotal = GTotal;
    obj.GTotalR = GTotalR;
    obj.ExchangeRate = pub_exchangerate;
    if (PayDollar == "") {
        obj.PayDollar = 0;
    } else {
        obj.PayDollar = PayDollar;
    }
    if (PayReil == "") {
        obj.PayReil = 0;
    } else {
        obj.PayReil = PayReil;
    }
    

    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/PaymentCalculation",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            $("#txtReturnDollar").val(data.d.ReturnDollar.toLocaleString());
            $("#txtReturnReil").val(data.d.ReturnReil.toLocaleString());
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}
function loadcontols() {
    let userlevel = 'Admin';
    
    if (userlevel == 'Staff' || userlevel == 'Admin') {
        console.log(userlevel);
        //alert('aaa');
        $("#btngoback").show();
        $("#btngoback").html(`<i class="fa fa-home" style="font-size: 40px;color: white;line-height: 50px;"></i>`);
    } else {
        $("#btnPay").prop('value',"បញ្ជាទិញ");
        $("#btngoback").html(`<i class="fa fa-arrow-left" style="font-size: 40px;color: white;line-height: 50px;"></i>`);
    }
}

function loadordertemp() {
    let items = readCookie("items");
    console.log("jsonitems");
    console.log(items);
    if (items == null) {
        items = [];
    }
    let data = JSON.parse(items);
    arritems = [];
    for (var i = 0; i < data.length; i++) {
        let obj = {}
        obj.ID = data[i].SaleID;
        obj.ItemID = data[i].ItemID;
        obj.ItemName = data[i].ItemName;
        obj.ItemCode = data[i].ItemCode;
        obj.ImageUrl = data[i].ImageUrl;
        obj.Price = data[i].Price;
        obj.Currency = data[i].Currency;
        obj.Qty = data[i].Qty;
        obj.FromQty = data[i].FromQty;
        obj.ToQty = data[i].ToQty;
        arritems.push(obj);
    }
    loadinvoiceitem(arritems);
    loaditemcount();
}
function loadordertemp_old() {
    let UserID = $("#hdUserID").val();
    let token = readCookie("token");
    let obj = {};
    obj.UserID = UserID;
    obj.Token = token;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/GetOrderItemTemp",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            
            arritems = [];
            for (var i = 0; i < data.d.length; i++) {
                let obj = {}
                obj.ID = data.d[i].SaleID;
                obj.ItemID = data.d[i].ItemID;
                obj.ItemName = data.d[i].ItemName;
                obj.ItemCode = data.d[i].ItemCode;
                obj.ImageUrl = data.d[i].ImageUrl;
                obj.Price = data.d[i].Price;
                obj.Currency = data.d[i].Currency;
                obj.Qty = data.d[i].Qty;
                obj.FromQty = data.d[i].FromQty;
                obj.ToQty = data.d[i].ToQty;
                arritems.push(obj);
            }
            loadinvoiceitem(arritems);
            loaditemcount();

        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function messagebox(title, detail) {
    //$("#dialog").attr("title", title);
    //$("#dialogdetail").html(detail);
    //$("#dialog").dialog();
    $('#myModal3').modal('show');
}


function loadinvoiceitem(arrobj) {
    let html = "";
    html += `<table width="100%">
                                <tr>
                                    <td style="padding: 5px; font-size: large; font-weight: bold;">
                                        <table style="width:100%">
                                            <td>
                                                
                                               <div> បញ្ជីទំនិញ</div>
                                            </td>
                                            <td style="text-align:right;">
                                                
                                            </td>
                                        </table>
                                                                            
                                    </td>
                                 </tr>                                
                            </table>`;
    html += "<table style='width:100%' class='mytable'>";
    html += "<tr><th scope='col'>ទំនិញ&nbsp;&nbsp;</th><th scope='col' style='width:15%'>ចំនួន&nbsp;&nbsp;</th><th scope='col' style='width:15%'>តម្លៃ&nbsp;&nbsp;</th><th scope='col'  style='width:15%'>សរុប&nbsp;&nbsp;</th><th scope='col' style='width:10%'>&nbsp;&nbsp;</th>	</tr>";
    let GTotal = 0;
    let GTotalR = 0;
    for (var i = 0; i < arrobj.length; i++) {
        let obj = arrobj[i];
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;

        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }


        html += "<tr style='border-width:0px;border-style:None;height:25px;'>";

        html += "<td>" + ItemName + " (" + ItemCode + ")</td>";
        html += "<td style='font-size:10pt'>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/subtract-color-icon.png' style='width: 100%;'></div>";
        html += "<div style='min-width: 25px;height: 20px;float: left;font-size: large;text-align: center;'>" + Qty.toLocaleString() + "</div>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/addition-color-icon.png' style='width: 100%;'></div>";
        html += "</td>";
        html += "<td style = 'font-size:10pt;'>" + ItemPrice.toLocaleString() + Currency + "</td>";
        html += "<td>" + Total.toLocaleString() + Currency + "</td>";
        html += "<td><div style='text-align:center;'><i class='fa fa-edit' style='font-size: x-large;color:#1b67a9;' onclick='edititem(" + i + ")'></i></div></td>";
        html += "</tr>";

    }
    html += "</table>";
   
    let currentdate = new Date();
    let datestring = "";
    let dd = "";
    let mm = "";
    let yyyy = currentdate.getFullYear();
    let hh = "";
    let minutes = "";
    dd = currentdate.getDate();
    if (dd < 10) { dd = "0" + dd; } 
    mm = (currentdate.getMonth() + 1);
    if (mm < 10) { mm = "0" + mm; } 
    hh = (currentdate.getHours() + 1);
    if (hh < 10) { hh = "0" + hh; } 
    minutes = (currentdate.getMinutes() + 1);
    if (minutes < 10) { minutes = "0" + minutes; }

     datestring= dd + "/"
        + mm + "/"
        + yyyy + " "
        + hh + ":"
        + minutes;

    $("#div_invoiceitemlist").html(html);
    $("#lblReceiptDate").html(datestring);
    $("#lblSubTotal").html("$" + GTotal.toLocaleString());
    $("#lblSubTotalR").html("R" + GTotalR.toLocaleString());
    $("#lblGrandTotal").html("$" + GTotal.toLocaleString());
}

function logout() {
    createCookie("items", "[]", 90);
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/LogOut",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            let token = readCookie("token");
            window.location = "applogin.aspx?token=" + token;
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function addorder() {
    if (arritems.length > 0) {
        let userlevel = $("#hdUserLevel").val();
        if (userlevel == "Admin" || userlevel == "Staff") {
            console.log(userlevel);
            stepname = "Checkout";
            gotopage(stepname);
            getexchangerate();
            $("#txtGetPoint").val("0");
        } else {
            addorder_client();
        }
    }
    
}

function loadcheckout() {
    let GTotal = 0;
    let GTotalR = 0;
    let arrobj = arritems;
    for (var i = 0; i < arrobj.length; i++) {
        let obj = arrobj[i];
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;

        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }
    }

    $("#txtTotalToPay").val(GTotal.toLocaleString());
    $("#txtTotalToPayR").val(GTotalR.toLocaleString());
    let txtdiscount = $("#txtPercentageDiscount");
    txtdiscount.val("0");
    discountchange(txtdiscount);
    let txtPayDollar = $("#txtPayDollar");
    txtPayDollar.val("0");
    let txtPayReil = $("#txtPayReil");
    txtPayReil.val("0");
    paidamountchange();

}

function clearordertemp() {
    let token = readCookie("token");
    let obj = {};
    obj.UserID = $("#hdUserID").val();
    obj.Token = token;
    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/ClearOrderTemp",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data");
            arritems = [];
            loadinvoiceitem(arritems);
            loaditemcount();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}
function addorder_client() {
    let arrobj = arritems;
    let obj = {};

    obj.JsonString = JSON.stringify(arrobj);
    obj.UserID = $("#hdUserID").val();
    $('#myModal3').modal('show');

    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/AddOrder",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data");
            console.log(data);
            createCookie("items", "[]", 90);
            arritems=[];
            loaditemcount();
            //window.location = "order.aspx?history=yes";
            $("#div_orderdetail").html(`
            <div id="div_orderdetail" class="modal-body">
            <div style="padding: 20px;text-align: center;font-size: x-large;">
            ការបញ្ជាទិញជោគជ័យ</div>
            <div style="padding: 20px;text-align: center;font-size: x-large;">
            លេខបុង:<b>${data.d}</b></div>
                    <div style="text-align: center;">
                    <div class="btn-option" onclick="gohome()" style="width: 50%;float: none;margin: auto;padding: 5% 2%;">ទំព័រដើម</div></div></div>`);
          
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}


function addorder_temp() {
    let arrobj = arritems;
    let JsonString = JSON.stringify(arrobj);
    createCookie("items", JsonString, 90);

}
function addorder_temp_old() {
    let arrobj = arritems;
    let obj = {};
    let token = readCookie("token");
    obj.JsonString = JSON.stringify(arrobj);
    obj.UserID = $("#hdUserID").val();
    obj.Token = token;

    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/AddOrderTemp",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data");
            console.log(data);
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function saveedititemold() {
    //arritems[index] = objitem;
    closeModal();
    load_itemlist(arritems);
}
function saveedititem() {
    if (selecteditem.Qty == 0) {
        $("#div_itemnotice").html("មិនត្រឹមត្រូវ");
    } else {
        if (selecteditem.Qty < 0) {           
            let userlevel = $("#hdUserLevel").val();
            if (userlevel == "Admin" || userlevel == "Staff") {

            } else {
                $("#div_itemnotice").html("មិនត្រឹមត្រូវ");
                return;
            }
        }
        let index = pub_editindex;
        arritems[index] = selecteditem;

        addorder_temp();
        loadinvoiceitem(arritems);
        showorderitem();
        loaditemcount();
        closepopup();


    }
}
function removeitem(index) {
    arritems.splice(index, 1);
    closeModal();
    load_itemlist(arritems);
}
function show_edititem_old(index) {
    let obj = arritems[index];
    objitem = obj;
    let html = "<div class='modal-body' style='padding:10px'>";
    html += "<div>";
    html += "<div>";


    html += "<table style = 'width:100%' >";
    html += "<tbody><tr>";
    html += "<td colspan='2'>";
    html += "<div>";

    let img_url = "https://slpos-app.azurewebsites.net/" + obj.ImageUrl.replace(/\^/g, "/");
    //let img_url = "https://slpos-app.azurewebsites.net/" + obj.ImageUrl;

    html += "<div style='text-align:center'>"
    html += "<img id='img_item' class='img-item' src='" + img_url + "'>"
    html += "</div>"
    html += "</td>"
    html += "</tr>"
    html += "<tr style='height:50px'>"
    html += "<td>ទំនិញ:</td>"
    html += "<td>"
    html += "<span>" + obj.ItemName + "</span>"
    html += "</td>"
    html += "</tr>"

    html += "<tr style='height:50px'>"
    html += "<td>"
    html += "តម្លៃ:</td>"
    html += "<td>"
    html += "<span class='item-price'>" + obj.ItemPrice + obj.Currency + "</span>"
    html += "<span class='item-price' style='margin-left:20px;'> សរុប: <span id='itemtotal'>" + (obj.ItemPrice * obj.Qty) + "</span><span>" + obj.Currency + "</span></span>";
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td>ចំនួន:</td><td>";
    html += "<div style='height: 25px;float: left;margin-right:10px;' onclick='add_qty(-1)'><img src='images/logo/subtract-color-icon.png' style='height: 100%;'></div>";
    html += "<div style='min-width: 40px;height: 25px;float: left;font-size: x-large;text-align: center;'><span id='itemqty'>" + obj.Qty + "</span></div>";
    html += "<div style='height: 25px;float: left;margin-left:10px;' onclick='add_qty(1)'><img src='images/logo/addition-color-icon.png' style='height: 100%;'></div>";
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td></td><td>";
    html += "<div class='item-qty' onclick='select_qty(1)'>1</div>";
    html += "<div class='item-qty' onclick='select_qty(2)'>2</div>";
    html += "<div class='item-qty' onclick='select_qty(5)'>5</div>";
    html += "<div class='item-qty' onclick='select_qty(10)'>10</div>";
    html += "<div class='item-qty' onclick='select_qty(20)'>20</div>";
    html += "<div class='item-qty' onclick='select_qty(50)'>50</div>";
    html += "<div class='item-qty' onclick='select_qty(100)'>100</div>";
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td colspan='2'>";
    html += "<div style='text-align:center'>";
    html += "<input type='button' value='កែប្រែ' class='button-add' onclick='saveedititem(" + index + ")' />";
    html += "</div>";
    html += "<div style='text-align:center'>";
    html += "<input type='button' value='លុប' class='button-add' onclick='removeitem(" + index + ")' />";
    html += "</div>";

    html += "</td>";
    html += "</tr>";


    html += "</tbody></table>";
    html += "</div>";
    html += "</div>";
    html += "</div>";

    $("#div_popup_content").html(html);
    openModal();
    selectedstage = "edit item";

}

function loaditemcount() {

    console.log("arritems");
    console.log(arritems);
    
    // Sum all quantities and totals in arritems
    let totalQty = sumItemQuantities();
    let totalAmount = sumItemTotals();
    
    if (arritems.length == 0) {
        $("#span_itemcount").text("");
        $("#span_itemcount").hide();

    } else {
        // Show total quantity and total amount
        $("#span_itemcount").text(totalQty + " items = $" + totalAmount.toFixed(2));
        $("#span_itemcount").show();

    }
}

function sumItemQuantities() {
    let totalQty = 0;
    for (let i = 0; i < arritems.length; i++) {
        if (arritems[i].Qty && !isNaN(arritems[i].Qty)) {
            totalQty += parseInt(arritems[i].Qty);
        }
    }
    return totalQty;
}

function sumItemTotals() {
    let totalAmount = 0;
    for (let i = 0; i < arritems.length; i++) {
        if (arritems[i].Price && !isNaN(arritems[i].Price)) {
            totalAmount += parseFloat(arritems[i].Price) * parseInt(arritems[i].Qty);
        }
    }
    return totalAmount;
}
function goto_order() {
    stepname = "category list";
    gotopage(stepname);
    //$("#div_item").show();
    //$("#div_Category").show();
    //$("#div_home").hide();
    //$("#btngoback").show();
    //$("#div_header").show();

}
function additem() {
    $("#div_itemnotice").html("");
    if (selecteditem.Qty == 0 || selecteditem.Price == 0) {
        $("#div_itemnotice").html("មិនត្រឹមត្រូវ");
    } else {
        if (selecteditem.Qty < 0) {
            let userlevel = $("#hdUserLevel").val();
            if (userlevel == "Admin" || userlevel == "Staff") {

            } else {
                $("#div_itemnotice").html("មិនត្រឹមត្រូវ");
                return;
            }
        }
        let datafilter = arritems.filter(function (el) {
            return el.ItemID==selecteditem.ItemID;
        });
        if (datafilter.length>0) {
            $("#div_itemnotice").html("ទំនិញដដែរ");
        } else {
            let obj = jQuery.extend(true, {}, selecteditem);
            arritems.push(obj);
            loaditemcount();
            loadinvoiceitem(arritems);
            addorder_temp();
            //load_itemlist(arritems);
            //$("#div_Category").html(category_html);
            closeModal();
            closepopup();
            //selectedstage = "category";
        }
    }


}

function selectaddoneitem(SaleID, ItemID, ItemName, ItemCode, ImageUrl, ItemPrice, Currency){
    selecteditem = {};
    selecteditem.ID = SaleID;
    selecteditem.ItemID = ItemID;
    selecteditem.ItemName = ItemName;
    selecteditem.ItemCode = ItemCode;
    selecteditem.ImageUrl = ImageUrl;
    selecteditem.Price = ItemPrice;
    selecteditem.Currency = Currency;
    selecteditem.Qty = 1;

     $("#countitem" + ItemID).show();
    let datafilter = arritems.filter(function (el) {
        return el.ItemID == ItemID;
    });
    if (datafilter.length > 0) {
        console.log("datafilter");
        console.log(datafilter);
        let objnew = jQuery.extend(true, {}, datafilter[0]);
        //$("#div_itemnotice").html("ទំនិញដដែរ");
        objnew.Qty += 1;
        $("#countitem" + ItemID).text(objnew.Qty);
        let newdata = arritems.filter(function (el) {
            return el.ItemID != ItemID;
        });
        
        newdata.push(objnew);
        arritems = newdata;
        loaditemcount();
        loadinvoiceitem(arritems);
        addorder_temp();
    } else {
        let obj = jQuery.extend(true, {}, selecteditem);
        $("#countitem" + ItemID).text(obj.Qty);
        arritems.push(obj);
        loaditemcount();
        loadinvoiceitem(arritems);
        addorder_temp();
        //load_itemlist(arritems);
        //$("#div_Category").html(category_html);
        //closeModal();
        //closepopup();
        //selectedstage = "category";
    }
}
function showorderitem() {
    let windowsize = $(document).width();
    if (arritems.length > 0) {
        let userlevel = $("#hdUserLevel").val();
        if (userlevel == "Admin" || userlevel == "Staff") {
            
            //alert(windowsize);
            if (windowsize <= 900) {
                stepname = "receipt";
                gotopage(stepname);
            } else {
                stepname = "category list";
                gotopage(stepname);
            }
        } else {
            //stepname = "order list";
            //gotopage(stepname);
            //load_itemlist(arritems);
            //alert(windowsize);
            $("#btnPay").prop('value', 'បញ្ជាទិញ');
            if (windowsize <= 900) {
                stepname = "receipt";
                gotopage(stepname);
            } else {
                stepname = "category list";
                gotopage(stepname);
            }

        }

    }
  
}
    
function gohome() {
    stepname = "home";
    gotopage(stepname);
}
function edititem(index) {
   
    //console.log(obj);
    show_edititem(index);
}
function load_itemlist(arrobj) {
    let html = "";
    html += `<table width="100%">
                                <tr>
                                    <td style="padding: 5px; font-size: large; font-weight: bold;">
                                        <table style="width:100%">
                                            <td>
                                                
                                               <div> បញ្ជីទំនិញ</div>
                                            </td>
                                            <td style="text-align:right;">
                                                
                                            </td>
                                        </table>
                                                                            
                                    </td>
                                 </tr>                                
                            </table>`;
    html += "<table style='width:100%' class='mytable'>";
    html += "<tr><th scope='col'>ទំនិញ&nbsp;&nbsp;</th><th scope='col' style='width:22%'>ចំនួន&nbsp;&nbsp;</th><th scope='col' style='width:15%'>តម្លៃ&nbsp;&nbsp;</th><th scope='col'  style='width:15%'>សរុប&nbsp;&nbsp;</th><th scope='col' style='width:10%'>&nbsp;&nbsp;</th>	</tr>";
    let GTotal = 0;
    let GTotalR = 0;
    for (var i = 0; i < arrobj.length; i++) {
        let obj = arrobj[i];
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;

        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }
        

        html += "<tr style='border-width:0px;border-style:None;height:25px;'>";

        html += "<td>" + ItemName + " (" + ItemCode + ")</td>";
        html += "<td style='font-size:10pt'>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/subtract-color-icon.png' style='width: 100%;'></div>";
        html += "<div style='min-width: 25px;height: 20px;float: left;font-size: large;text-align: center;'>" + Qty.toLocaleString() + "</div>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/addition-color-icon.png' style='width: 100%;'></div>";
        html += "</td>";
        html += "<td style = 'font-size:10pt;'>" + ItemPrice.toLocaleString() + Currency + "</td>";
        html += "<td>" + Total.toLocaleString() + Currency + "</td>";
        html += "<td><div style='text-align:center;'><i class='fa fa-edit' style='font-size: x-large;color:#1b67a9;' onclick='edititem(" + i +")'></i></div></td>";
        html += "</tr>";

    }
    html += "</table>";

    html += "<div style='padding:5px;'>សរុប: <span style='font-size:large;font-weight: bold;'>" + GTotal.toFixed(2) + "$</span> និង <span style='font-size:large;font-weight: bold;'>" + GTotalR.toLocaleString() + "R</span></div>";

    let footer_action = "<div  class='bg-option'>";

    //footer_action += "<div class='btn-option' onclick='goto_item()'>បន្ថែមទំនិញ</div>";
    footer_action += "<div class='btn-option' onclick='addorder()'>បញ្ជាទិញ</div>";
    footer_action += "</div>";
    $("#div_items").html(html + footer_action);

   
    stepname = "order list";

}

function show_orderlist() {
    $("#btngoback").html("<i class='fa fa-arrow-left' style='font-size: 40px;    color: white;    line-height: 50px;'></i>");
    cacellist = false;
    loadorderlist();
}

function loadorderlist() {
    let obj = {};
    obj.UserID = $("#hdUserID").val();
    obj.Page = 1;
    obj.RecordsPerPage = 100;
    obj.CancelList = cacellist;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "order.aspx/LoadDetail",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            stepname = "ordered list";

            $("#div_logo").hide();
            $("#div_list").show();
            $("#div_list").html(data.d);
            $("#div_bag").hide();
            $("#span_logout").hide();
            $("#span_home").show();
            $("#btngoback").show();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function gotopage(step) {
    let windowsize = $(document).width();
    let userlevel = $("#hdUserLevel").val();
    if (step == "home") {
        $("#div_item").hide();
        $("#div_Category").hide();
        $("#div_list").hide();
        $("#div_home").show();
        $("#div_logo").show();
        if (userlevel == "Admin" || userlevel == "Staff") {
            $("#btngoback").show();
            $("#btngoback").html("<i class='fa fa-home' style='font-size: 40px;    color: white;    line-height: 50px;'></i>");
        } else {
            $("#btngoback").hide();
        }
        $("#span_logout").show();
        $('#myModal3').modal('hide');
    } else if (step == "category list") {
        $("#div_item").show();
        $("#div_Category").show();
        $("#div_home").hide();
        $("#div_items").hide();
        $("#div_header").show();
        $("#div_list").hide();
        $("#div_logo").show();
        $("#span_logout").hide();
        $("#btngoback").show();
        $("#btngoback").html("<i class='fa fa-arrow-left' style='font-size: 40px;    color: white;    line-height: 50px;'></i>");
        $("#pnReceipt").show();
        
        //alert(windowsize);
        if (windowsize <= 900) {
            $("#pnReceiptContainer").hide();
        } else {
            $("#pnReceiptContainer").show();
        }
        closeModal();
    } else if (step == "receipt") {
        $("#div_item").hide();
        $("#div_Category").show();
        $("#div_home").hide();
        $("#div_items").hide();
        $("#div_header").show();
        $("#div_list").hide();
        $("#div_logo").show();
        $("#span_logout").hide();
        $("#btngoback").show();
        $("#btngoback").html("<i class='fa fa-arrow-left' style='font-size: 40px;    color: white;    line-height: 50px;'></i>");
        $("#pnReceipt").show();
        $("#pnReceiptContainer").show();
        closeModal();
    } else if (step == "item list") {

        //$("#div_items").show();
        //$("#div_item").show();
        //$("#div_Category").hide();
        //$("#div_home").hide();
        //$("#btngoback").show();
        //$("#div_header").hide();
    } else if (step == "order list") {
        $("#btngoback").html("<i class='fa fa-arrow-left' style='font-size: 40px;    color: white;    line-height: 50px;'></i>");
        $("#div_item").show();
        $("#div_Category").hide();
        $("#div_home").hide();
        $("#div_items").show();
        $("#div_header").hide();
        $("#div_list").hide();
        $("#div_logo").hide();
        $("#span_logout").hide();
        $("#btngoback").show();
        
        closeModal();
    } else if (step == "selected item") {

        //$("#div_item").hide();
        //$("#div_Category").show();
        //$("#div_home").hide();
        //$("#div_header").show();
        //$("#span_logout").hide();

        closeModal();
    } else if (step == "ordered list") {
        $("#div_item").hide();
        $("#div_Category").hide();
        $("#div_home").show();
        $("#btngoback").hide();
        $("#div_list").hide();
        $("#div_logo").show();
        $("#span_logout").show();
    } else if (step == "Checkout") {
        $("#div_checkout").show();
        $("#div_item").hide();
        $("#div_Category").hide();
        $("#div_home").hide();
        $("#btngoback").show();
        $("#div_list").hide();
        $("#div_logo").hide();
        $("#span_logout").hide();
        $("#pnReceipt").hide();
    }
}
function goto_item() {
    let userlevel = $("#hdUserLevel").val();
    //go back
    if (stepname == "order list") {
        stepname = "category list";
        gotopage(stepname);
    } else if (stepname == "category list") {
        stepname = "home";
        gotopage(stepname);
    } else if (stepname == "item list") {
        stepname = "category list";
        gotopage(stepname);
        //$("#div_item").hide();showorderitem();
        //$("#div_Category").hide();
        //$("#div_home").show();        
        //$("#span_logout").show();
        //back();
    } else if (stepname == "selected item") {
        stepname = "item list";
        closepopup();

    } else if (stepname == "ordered list") {
        stepname = "home";
        gotopage(stepname);
    } else if (stepname == "home" || stepname == "") {
        window.location = "dashboard.aspx";
    } else if (stepname == "Checkout") {
        $("#div_checkout").hide();
        //stepname = "order list";
        stepname = "category list";
        gotopage(stepname);


    } else if (stepname == "receipt") {
        stepname = "category list";
        gotopage(stepname);

    }
}


function back() {
    closeModal();
    //let html = ''
    //if (selectedstage == 'item') {
    //    html = group_html;
    //    $("#div_popup_content").html(html);
    //    openModal();
    //    selectedstage = "group";
    //} else if (selectedstage == 'group') {
    //    html = category_html;
    //    $("#div_Category").html(html);
    //    closeModal();
    //    selectedstage = "category";
    //} else if (selectedstage == 'edit item') {
    //    closeModal();
    //    load_itemlist();
    //    selectedstage = "";
    //} else {
    //    html = category_html;
    //    $("#div_Category").html(html);
    //    closeModal();
    //    selectedstage = "category";
    //}

}

function select_qty(qty) {
    $("#div_itemnotice").html("");
    let oldqty = $("#txtitemamount").val();
    if (oldqty == "") {
        oldqty = 0;
    }
    
    newqty = parseInt(oldqty) + qty;
    //if (newqty < 0) {
    //    newqty = 0;
    //}
    $("#txtitemamount").val(newqty);
    //let GTotal = newqty * selecteditem.Price;
    //selecteditem.Total = GTotal;
    //selecteditem.Qty = newqty;
    
    //$("#div_itemtotal").html(GTotal.toLocaleString() + "$");

    let txtamount = $("#txtitemamount");
    changeamount(txtamount);
   
}

function select_qtyold(qty) {
    console.log(objitem);
    objitem.Qty = qty;
    $("#itemqty").html(qty);
    console.log(objitem);

    let GTotal = objitem.Qty * objitem.ItemPrice;
    $("#itemtotal").html(GTotal.toLocaleString());
}
function search() {
    let itemcode = $("#txtbarcodenew").val();
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/SearchItem",
        data: '{"ItemCode":"' + itemcode + '"}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(JSON.parse(data.d));
            let result = JSON.parse(data.d);
            //view_itemdetail(oitem[0]);
            let oitem = result[0];
            orderitem(oitem.ID, oitem.ItemID, oitem.ItemName, oitem.ItemCode, oitem.ImageUrl, oitem.Price,oitem.Currency);

            
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function select_item(SaleID,ItemID,ItemName,ItemCode,ImageUrl,ItemPrice,Currency) {
    let obj = {};
    obj.ItemID = ItemID;
    obj.ItemName = ItemName;
    obj.ItemCode = ItemCode;
    obj.ImageUrl = ImageUrl;
    obj.Price = ItemPrice;
    obj.Currency = Currency;
    obj.Qty = 1;
    obj.ID = SaleID;

    view_itemdetail(obj);
}
function add_qty(qty) {
    let oldqty = objitem.Qty;
    let newqty = oldqty + qty;
    if (qty < 0) {
        if (newqty < 1) {
            newqty = 1;
        }
    } 
    objitem.Qty = newqty;
    $("#itemqty").html(newqty);
    let GTotal = objitem.Qty * objitem.ItemPrice;
    $("#itemtotal").html(GTotal.toLocaleString());
}
function view_group(categoryid) {
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getgroup",
        data: '{"CategoryID":' + categoryid + '}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data:" + data);
            $("#div_popup_content").html(data.d);
            group_html = data.d;
            selectedstage = "group";
            
            openModal();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

var pub_showitem = [];
function showitemingroup(groupid) {

    if (jQuery.inArray(groupid, pub_showitem) != -1) {
        //console.log("is in array");
        $("#divgroup_" + groupid).hide();
        $("#span_arrow_" + groupid).html('<i class="fa fa-arrow-down" style="font-size: 28px;"></i>');
        pub_showitem = jQuery.grep(pub_showitem, function (value) {
            return value != groupid;
        });
        

    } else {
        $("#divgroup_" + groupid).show();
        $("#span_arrow_" + groupid).html('<i class="fa fa-arrow-up" style="font-size: 28px;"></i>');
        pub_showitem.push(groupid);
    }
    
    
}

function view_itembbycategory(categoryid) {
    // Show waiting dialog
    showWaitingDialog();
    
    let userlevel = $("#hdUserLevel").val();
    let obj = {};
    obj.UserLevel = userlevel;
    obj.CategoryID = categoryid;
    $.ajax({
        cache: false,
        async: true, // Changed to async for better UX
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "customer.aspx/getitemsbycategory",
        data: JSON.stringify(obj),
        complete: function () {
            // Hide waiting dialog when complete
            hideWaitingDialog();
        },
        success: function (data) {
            pub_showitem = [];
            console.log("success data items:");
            console.log(data);
            load_itemlist_bydata(data.d);
            //$("#div_popup_content").html(data.d);
            //group_html = data.d;
            stepname = "item list";
            openModal();
        },
        error: function (result) {
            console.log("Error loading items by category:", result);
            // Show error message
            showErrorDialog("មានបញ្ហាក្នុងការផ្ទុកទំនិញ។ សូមព្យាយាមម្តងទៀត។");
        }
    });

}

function load_itemlist_bydata(data) {
    console.log("load_itemlist_bydata");
    let html = "";
    let running_groupname = "";
    for (let i = 0; i < data.length; i++) {
        console.log("Item " + i + ":");
        console.log(data[i]);
        //load_single_item_html(data[i]);
    
        let ItemID = data[i].ItemID;
        let ItemName = data[i].ItemName;
        let ItemCode = data[i].ItemCode;
        let ImageUrl = data[i].ImageUrl;
        let ItemPrice = data[i].Price;
        let Currency = data[i].Currency;
        let SaleID = data[i].ID;
        let GroupID = data[i].GroupID;
        let GroupName = data[i].GroupName;

        let img_url = ImageUrl;

        let item_selected = arritems.filter(function (el) {
            return el.ItemID==ItemID;
        });
        
        console.log("GroupName: " + GroupName + ", running_groupname: " + running_groupname);
        if (running_groupname !== GroupName) {
                //html += "</div>"               
                html += "<div class='item-groupname'><table style='width:100%'><tr><td>" + GroupName + "</td><td style='text-align:right;'></td></tr></table></div>"                
                running_groupname = GroupName
            
        }

            html += '<a onclick="selectaddoneitem(' + SaleID + "," + ItemID + ",'" + ItemName + "','" + ItemCode + "','" + ImageUrl + "'," + ItemPrice + ",'" + Currency + "')" + '">'       
            html += "<div class='item-detail'>"
            html += "<div style='-webkit-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);-moz-box-shadow: 0 0 4px 0 rgba(0,0,0,.2);box-shadow: 0 0 4px 0 rgba(0,0,0,.2);'>"
        if (item_selected.length > 0) {
            if (item_selected[0].Qty==0){
                html += `<div class='count-selected-item' id='countitem${ItemID}' style='display:none;'></div>`;
            }else{
                html += `<div class='count-selected-item' id='countitem${ItemID}'>${item_selected[0].Qty}</div>`
            }
                

        }else{
            html += `<div class='count-selected-item' id='countitem${ItemID}' style='display:none;'></div>`;                
        }

            html += "<div align='center'>"

                html += "<img class='img_item_thumnail' alt='' class='img-round' src='" + img_url + "' style='width:100%;' />"
                //Dim index As Integer = j - 20 * Int(j / 20)

                html += "<div class='item-detail-textbox'><div class='item-detail-text'  id='divitemname" + ItemID + "'>" + ItemCode + " " + ItemName + "</div>"
                html += "<div class='box-buy'>" + ItemPrice + Currency
                html += "</div>"
                html += "</div>"

                html += "</div>"
                html += "</div>"
                html += "</div>"
                html += "</a>"
        
    }
    $("#div_popup_content").html(html);
}

function view_item(categoryid,groupid) {
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getitems",
        data: '{"CategoryID":' + categoryid + ',"GroupID":' + groupid + '}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data:" + data);
            $("#div_popup_content").html(data.d);
            item_html = data.d;
            selectedstage = "item";
            openModal();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function view_itemdetail(objdata) {
    console.log("view_itemdetail");
    console.log(objdata);
    let obj = {};
    obj.ItemID = objdata.ItemID;
    obj.ItemName = objdata.ItemName;
    obj.ItemCode = objdata.ItemCode;
    obj.ImageUrl = objdata.ImageUrl;
    obj.ItemPrice = objdata.Price;
    obj.Currency = objdata.Currency;
    obj.Qty = 1;
    obj.SaleID = objdata.ID;
    console.log("view_itemdetail");
    console.log(obj);
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getitemdetail",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data:" + data);
            $("#div_popup_content").html(data.d);
            openModal();
            objitem = obj;
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function loaditemunit(ItemID) {
    $.ajax({
        cache: false,
        async: false,
        type: "post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getitemunit",
        data: "{ItemID:" + ItemID + "}",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            let result = data.d;
            pub_itemunit = result;
            $('#dplitemunit').empty();
            for (var i = 0; i < result.length; i++) {
                let unitType = result[i].FromQty + '-' + result[i].ToQty
                $('#dplitemunit').append($('<option>', {
                    value: result[i].ID,
                    text: unitType
                }));

            }
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}
function deleteitem() {
    arritems.splice(pub_editindex, 1);
    let JsonString = JSON.stringify(arritems);
    createCookie("items", JsonString, 90);
    loadinvoiceitem(arritems);
    loaditemcount();
    closepopup();
}
function deleteitem_old() {
    let selecteditem = arritems[pub_editindex];
    console.log(selecteditem);
    let UserID = $("#hdUserID").val();
    let token = readCookie("token");
    let obj = {};
    obj.UserID = UserID;
    obj.Token = token;
    obj.SaleID = selecteditem.ID;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/DeleteItemTemp",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            arritems.splice(pub_editindex, 1);
            loadinvoiceitem(arritems);
            loaditemcount();

        },
        error: function (result) {
            $('#loading').hide();
        }
    });


    addorder_temp();
    showorderitem();
    loaditemcount();
    closepopup();
}
function gotoupload(itemid) {

    let uploadurl = "/customer.aspx?item=" + itemid + "&proid=" + itemid + "&type=proid&upload=1&photoid=" + itemid;
    window.location = uploadurl;

}
function orderitem(SaleID, ItemID, ItemName, ItemCode, ImageUrl, ItemPrice, Currency) {
    $("#div_edititem").hide();
    $("#div_additem").show();
    selecteditem = {};
    selecteditem.ID = SaleID;
    selecteditem.ItemID = ItemID;
    selecteditem.ItemName= ItemName;
    selecteditem.ItemCode = ItemCode;
    selecteditem.ImageUrl = ImageUrl;
    selecteditem.Price = ItemPrice;
    selecteditem.Currency = Currency;
    selecteditem.Qty = 0;

    //$("#txtitemprice").val(ItemPrice);
    let userlevel = $("#hdUserLevel").val();
    if (userlevel == "Admin" || userlevel == "Staff") {
        $("#dplitemunit").prop("disabled", false);
        $("#txtitemprice").prop("disabled", false);
    } else {
        $("#dplitemunit").prop("disabled", true);
        $("#txtitemprice").prop("disabled", true);
    }
    $("#txtitemprice").val(ItemPrice.toLocaleString());

    loaditemunit(ItemID);
    stepname = "selected item";
    let itemname = $("#divitemname" + ItemID).html();
    //console.log("itemname" + itemname);
    let infohtml = `<div style="text-align:center;padding: 10px 5px;">
    <table style="width:100%">
    <tr><td style="width:33%">
    <span onclick="select_qty(-1)"><img src="images/logo/subtract-color-icon.png" style="width: 40px;"></span>
    </td>
    <td  style="text-align:center;">`;
    let token = readCookie("token");
    if (token.substring(0, 3) == "PC_") {

    } else {
        infohtml += `
    <div class="div_upload"><i onclick="gotoupload(${ItemID})" class="fa fa-picture-o" aria-hidden="true" style="background-color: white;"></i></div>`;
    }
    infohtml += `
    <img alt="" src="${ImageUrl}?${getdatetime()}" class="item-image">
    </td>
    <td style="width:33%">
        <span onclick="select_qty(1)"><img src="images/logo/addition-color-icon.png" style="width: 40px;"></span>
    </td>
    </tr></table>
    </div>
    
    <div style="text-align: center;padding:2px;height: 30px;overflow: hidden;">${ItemName}</div>`;

    $("#div_iteminfo").html(infohtml);
    $("#txtitemamount").val(0);
    $("#div_itemtotal").html("0.00$");
    //select_qty(0);
    $("#div_popupitem").show();

}


function show_edititem(index) {

    let userlevel = $("#hdUserLevel").val();
    if (userlevel == "Admin" || userlevel == "Staff") {
        $("#dplitemunit").prop("disabled", false);
        $("#txtitemprice").prop("disabled", false);
    } else {
        $("#dplitemunit").prop("disabled", true);
        $("#txtitemprice").prop("disabled", true);
    }


    $("#div_edititem").show();
    $("#div_additem").hide();
    pub_editindex = index;
    let obj = arritems[index];
    console.log(obj);
    selecteditem = {};
    selecteditem.ID = obj.ID;
    selecteditem.ItemID = obj.ItemID;
    selecteditem.ItemName = obj.ItemName;
    selecteditem.ItemCode = obj.ItemCode;
    selecteditem.ImageUrl = obj.ImageUrl;
    selecteditem.Price = obj.Price;
    selecteditem.Currency = obj.Currency;
    selecteditem.Qty = 0;

    $("#txtitemprice").val(obj.Price);
    loaditemunit(obj.ItemID);
    stepname = "selected item";
    let itemname = $("#divitemname" + obj.ItemID).html();
    //console.log("itemname" + itemname);
    let infohtml = `<div style="text-align:center;">
    <table style="width:100%">
    <tr><td style="width:33%">
    <span onclick="select_qty(-1)"><img src="images/logo/subtract-color-icon.png" style="width: 50px;"></span>
    </td>
    <td  style="text-align:center;">
    <img class="" alt="" src="${obj.ImageUrl.replace(/\^/g, "/")}" style="width: 200px;height: 200px;object-fit: cover;">
    </td>
    <td style="width:33%">
        <span onclick="select_qty(1)"><img src="images/logo/addition-color-icon.png" style="width: 50px;"></span>
    </td>
    </tr></table>
    </div>
    
    <div style="text-align: center;padding:2px;height: 30px;">${obj.ItemName}</div>`;


    $("#div_iteminfo").html(infohtml);
    $("#txtitemamount").val(obj.Qty);
    let total = obj.Qty * obj.Price;
    $("#div_itemtotal").html(total.toLocaleString());
    //select_qty(0);
    $("#div_popupitem").show();

}
function closepopup() {
    $("#div_popupitem").hide();
}

function changeamount(e) {
    let ctrl = $(e)
    let val = $(ctrl).val();
    if ($.isNumeric(val)==true || val=="-") {

    } else {
        if (/\D/g.test($(ctrl).val())) {
            console.log("tt");
            $(ctrl).val($(ctrl).val().replace(/\D/g, ''));
        } 
    }

    if (val == "") {
        selectunitprice(0);
    } else {
        let userlevel = $("#hdUserLevel").val();
        console.log(userlevel);
        if (userlevel == "Admin" || userlevel == "Staff") {
            selectunitprice(val);
        }
        //select_qty(0);

    }

    let newqty = $("#txtitemamount").val();
    let price = $("#txtitemprice").val();
    selecteditem.Price = price;
    let GTotal = newqty * selecteditem.Price;
    selecteditem.Total = GTotal;
    selecteditem.Qty = newqty;

    $("#div_itemtotal").html(GTotal.toLocaleString() + "$");

    
}
function changeitemprice() {
    let newqty = $("#txtitemamount").val();
    let price = $("#txtitemprice").val();
    selecteditem.Price = price;
    let GTotal = newqty * selecteditem.Price;
    selecteditem.Total = GTotal;
    selecteditem.Qty = newqty;

    $("#div_itemtotal").html(GTotal.toLocaleString() + "$");

}
function selectunitprice(qty) {
    let price = 0;
    let saleid = 0;
    for (var i = 0; i < pub_itemunit.length; i++) {
        if (qty >= pub_itemunit[i].FromQty && qty <= pub_itemunit[i].ToQty) {
            price = pub_itemunit[i].Price;
            saleid = pub_itemunit[i].ID;
        }
    }
    $("#txtitemprice").val(price);
    $("#dplitemunit").val(saleid);

}
function changeunitprice() {

    let price = 0;
    let saleid = $("#dplitemunit").val();
    for (var i = 0; i < pub_itemunit.length; i++) {
        if (saleid == pub_itemunit[i].ID) {
            price = pub_itemunit[i].Price;           
        }
    }
    $("#txtitemprice").val(price);
    changeitemprice();
}
function paynow() {
    let discountrate = $("#txtPercentageDiscount").val();
    let DiscountPay = $("#txtDiscountPay").val();
    if (DiscountPay == "") {
        DiscountPay = 0;
    }
    let DiscountPayReil = $("#txtDiscountPayReil").val();
    let GrandTotalToPay = $("#txtGrandTotalToPay").val();
    let GrandTotalToPayR = $("#txtGrandTotalToPayReil").val();
    let PayDollar = $("#txtPayDollar").val();
    let PayReil = $("#txtPayReil").val();
    let ReturnDollar = $("#txtReturnDollar").val();
    let ReturnReil = $("#txtReturnReil").val();
    let GetPoint = $("#txtGetPoint").val();
    let PaymentMothod = "";

    let isCheckedBank = $('#rdoBank').is(':checked');
    if (isCheckedBank) {
        // Checkbox is checked
        PaymentMothod = "Bank"
    } else {
        // Checkbox is not checked
        PaymentMothod = "Cash"
    }

    if ($.isNumeric(discountrate)==false) {
        // The value is a number
        $("#div_paynow_info").html("ភាគរយបញ្ចុះតម្លៃមិនត្រឹមត្រូវ");
        return;  
    } 
    if ($.isNumeric(DiscountPay) == false || $.isNumeric(DiscountPayReil) == false) {
        // The value is a number
        $("#div_paynow_info").html("បញ្ចុះតម្លៃជាទឹកប្រាក់មិនត្រឹមត្រូវ");
        return;
    } 
    if ($.isNumeric(PayDollar) == false && $.isNumeric(PayReil) == false) {
        // The value is a number
        $("#div_paynow_info").html("ទឹកប្រាក់ទទួលមិនត្រឹមត្រូវ");
        return;
    } else {
        if ($.isNumeric(PayDollar) == false) {
            PayDollar = 0;
        }
        if ($.isNumeric(PayReil) == false) {
            PayReil = 0;
        }

    }

    if ($.isNumeric(GetPoint) == false) {
        // The value is a number
        $("#div_paynow_info").html("ពិន្ទុអតិថិជនមិនត្រឹមត្រូវ");
        return;
    }

    let token = readCookie("token");
    let UserID = $("#hdUserID").val();
    let obj = {};
    obj.SalerID = parseInt(UserID);
    obj.GrandTotalToPay = parseFloat(GrandTotalToPay);
    obj.GrandTotalToPayR = parseInt(GrandTotalToPayR);
    obj.PaymentMothod = PaymentMothod;
    obj.Discount = parseInt(DiscountPay);
    obj.DiscountReil = parseInt(DiscountPayReil);
    obj.PaymentAmountDollar = parseFloat(PayDollar);
    obj.PaymentAmountReil = parseInt(PayReil);
    obj.ReturnDollar = parseFloat(ReturnDollar);
    obj.ReturnReil = parseInt(ReturnReil);
    obj.Token = token;
    obj.Items = JSON.stringify(arritems);
    console.log(pub_customerinfo.CustomerID);
    if (pub_customerinfo.CustomerID == undefined) {
        obj.CustomerID = 0;
        obj.CustomerName = "";
    } else {
        obj.CustomerID = pub_customerinfo.CustomerID;
        obj.CustomerName = pub_customerinfo.CustomerName;
    }
    obj.NewPoint = parseInt(GetPoint);
    obj.ExchangeRate = pub_exchangerate;

    $("#div_progress").show();
    $("#btnPayNow").hide();
    $.ajax({
        cache: false,
        async: true,
        type: "post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/PayNow",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();

        },
        success: function (data) {
            //$("#div_progress").hide();
            //$("#btnPayNow").show();
            createCookie("items", "[]", 90);
            console.log(data);
            let printurl = "PrintHtml.aspx?print=1&InvoiceNo=" + data.d.InvoiceNo + "&user=" + UserID + "&backurl=userorder.aspx";
            window.location = printurl;
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}


function get_order(orderid, createddate) {

    let obj = {};
    obj.OrderID = orderid;


    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/GetOrderDetail",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(JSON.parse(data.d));
            load_itemlist(JSON.parse(data.d), orderid, createddate);
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}
function load_itemlist(arrobj, orderid, createddate) {

    let headerstyle = 'padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#e9cb19';
    let rowstyle = 'padding:4px;border-right:1px solid #dddddd;;border-bottom:1px solid #dddddd;';

    let html = "";
    html += "<div><span style='color:black;font-size:large;float:left;'>លេខបញ្ជាទិញ: " + orderid + "</span><span style='color:black;font-size:large;float:right;'> " + createddate + "</span></div>"
    html += "<table style='width:100%' class='mytable'>";
    html += "<tr><th scope='col' style='" + headerstyle + "'>ទំនិញ&nbsp;&nbsp;</th><th scope='col' style='width:22%;" + headerstyle + "'>ចំនួន&nbsp;&nbsp;</th><th scope='col' style='width:15%;" + headerstyle + "'>តម្លៃ&nbsp;&nbsp;</th><th scope='col'  style='width:15%;" + headerstyle + "'>សរុប&nbsp;&nbsp;</th>	</tr>";
    let GTotal = 0;
    let GTotalR = 0;
    for (var i = 0; i < arrobj.length; i++) {
        let obj = arrobj[i];
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;

        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }


        html += "<tr style='border-width:0px;border-style:None;height:25px;'>";

        html += "<td style='" + rowstyle + "'>" + ItemName + " (" + ItemCode + ")</td>";
        html += "<td style='font-size:10pt;" + rowstyle + "'>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/subtract-color-icon.png' style='width: 100%;'></div>";
        html += "<div style='min-width: 25px;height: 20px;float: left;font-size: large;text-align: center;" + rowstyle + "'>" + Qty.toLocaleString() + "</div>";
        //html += "<div style='width: 20px;height: 20px;float: left;'><img src='images/logo/addition-color-icon.png' style='width: 100%;'></div>";
        html += "</td>";
        html += "<td style = 'font-size:10pt;" + rowstyle + "'>" + ItemPrice.toLocaleString() + Currency + "</td>";
        html += "<td style='" + rowstyle + "'>" + Total.toLocaleString() + Currency + "</td>";

        html += "</tr>";

    }
    html += "</table>";

    html += "<div style='padding:5px;'>សរុប: <span style='font-size:large;font-weight: bold;'>" + GTotal + "$</span> និង <span style='font-size:large;font-weight: bold;'>" + GTotalR.toLocaleString() + "R</span></div>";


    $("#div_popup_content").html(html);
    $('#myModal').modal('show');

}

// Waiting dialog functions
function showWaitingDialog() {
    // Remove existing waiting dialog if any
    $("#waitingDialog").remove();
    
    var waitingHtml = '<div id="waitingDialog" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">' +
        '<div style="background: white; padding: 30px; border-radius: 15px; text-align: center; max-width: 300px; font-family: \'Koh Santepheap\', Arial;">' +
        '<div style="margin-bottom: 20px;">' +
        '<div class="spinner" style="border: 4px solid #f3f3f3; border-top: 4px solid #3498db; border-radius: 50%; width: 40px; height: 40px; animation: spin 1s linear infinite; margin: 0 auto;"></div>' +
        '</div>' +
        '<div style="font-size: 18px; color: #333; font-weight: bold;">កំពុងដំណើរការ...</div>' +
        '<div style="font-size: 14px; color: #666; margin-top: 10px;">សូមរង់ចាំ</div>' +
        '</div>' +
        '</div>';
    
    // Add spinner animation CSS if not exists
    if (!$('#spinnerStyle').length) {
        $('<style id="spinnerStyle">@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }</style>').appendTo('head');
    }
    
    $('body').append(waitingHtml);
}

function hideWaitingDialog() {
    $("#waitingDialog").fadeOut(300, function() {
        $(this).remove();
    });
}

function showErrorDialog(message) {
    // Hide waiting dialog first
    hideWaitingDialog();
    
    var errorHtml = '<div id="errorDialog" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); z-index: 9999; display: flex; align-items: center; justify-content: center;">' +
        '<div style="background: white; padding: 30px; border-radius: 15px; text-align: center; max-width: 350px; font-family: \'Koh Santepheap\', Arial;">' +
        '<div style="margin-bottom: 20px;">' +
        '<i class="fa fa-exclamation-triangle" style="font-size: 48px; color: #e74c3c;"></i>' +
        '</div>' +
        '<div style="font-size: 16px; color: #333; margin-bottom: 20px;">' + message + '</div>' +
        '<button onclick="closeErrorDialog()" style="background: #3498db; color: white; border: none; padding: 10px 20px; border-radius: 5px; font-size: 14px; cursor: pointer; font-family: \'Koh Santepheap\', Arial;">យល់ព្រម</button>' +
        '</div>' +
        '</div>';
    
    $('body').append(errorHtml);
}

function closeErrorDialog() {
    $("#errorDialog").fadeOut(300, function() {
        $(this).remove();
    });
}
