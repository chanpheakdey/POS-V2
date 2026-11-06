var cacellist = false;
$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + '=([^&#]*)')
        .exec(window.location.search);

    return (results !== null) ? results[1] || 0 : false;
}
$(document).ready(function () {
    let history = $.urlParam('history');
    
    if (history == 'yes') {
        show_orderlist();
    }
   
});

function show_orderlist() {
    cacellist = false;
    loadorderlist();
}
function show_cancellist() {
    cacellist = true;
    loadorderlist();
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
        url: "order.aspx/GetOrderDetail",
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
            $("#div_action").hide();
            $("#div_list").show();
            $("#div_list").html(data.d);
            $("#div_bag").hide();
            $("#span_logout").hide();
            $("#span_home").show();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}
function cancelorder(orderid) {
    let obj = {};
    obj.OrderID = orderid;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "order.aspx/CancelOrder",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            show_orderlist();            
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}
function gohome() {
    $("#div_action").show();
    $("#div_list").hide();
    $("#div_list").html("");
    $("#div_bag").show();
    $("#span_logout").show();
    $("#span_home").hide();
}

function logout() {
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "order.aspx/LogOut",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            window.location = "applogin.aspx"
        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function showorderitem() {
    
    window.location = "userorder.aspx?vieworder=1";
}