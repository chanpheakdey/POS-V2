var pubcredit_added = 0;
var currenctcredit = 0;
var currenctcreditReil = 0;
function show_formcredit(staffid, credit, creditReil) {
    currenctcredit = credit;
    currenctcreditReil = creditReil;
    let html = "<div class='modal-body' style='padding:10px'>";
    html += "<div>";
    html += "<div>";


    html += "<table style = 'width:100%' >";
    html += "<tbody><tr>";
    html += "<td colspan='2'>";
   
    //let img_url = "https://slpos-app.azurewebsites.net/" + obj.ImageUrl.replace("^", "\/");
    //let img_url = "https://slpos-app.azurewebsites.net/" + obj.ImageUrl;

    html += "</td>"
    html += "</tr>"
    html += "<tr style='height:50px'>"
    html += "<td><input id='chkDeduct' type='checkbox' onchange='checkdeductchange()'><label for='chkDeduct'>ដកក្រេឌីតចេញវិញ</label>"
    html += "</td>"
    html += "</tr>"

    html += "<tr style='height:50px'>"
    html += "<td><span style='padding-right:5px;'><input id='rdodollar' type='radio' onchange='changecurrency()' checked='checked' name='currency'><label for='rdodollar'>ទឹកប្រាក់ជាដុល្លា</label></span><span><input id='rdoreil' type='radio' onchange='changecurrency()' name='currency'><label for='rdoreil'>ទឹកប្រាក់ជារៀល</label></span>"
    html += "</td>"
    html += "</tr>"


    html += "<tr style='height:50px'>"
    html += "<td>ក្រេឌីត(Credit):"
    html += ""
    html += "<span class='item-price'>" + credit + "$ និង " + creditReil + "R</span>"
    html += "</td>"
    html += "</tr>"

    html += "<tr style='height:50px'>"
    html += "<td colspan='2'>"
    html += "ក្រេឌីតសរុប(Balance):";
    html += "<span class='item-price' id='span_credit'>" + credit + "$ និង " + creditReil + "R</span>"
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td colspan='2'><span style='float: left; margin-right: 20px;'>ក្រេឌីតបន្ថែម:</span>";
    //html += "<div style='height: 25px;float: left;margin-right:10px;' onclick='add_qty(-1)'><img src='images/logo/subtract-color-icon.png' style='height: 100%;'></div>";
    html += "<div style='min-width: 40px;height: 25px;float: left;font-size: x-large;text-align: center;'><span id='itemqty'>" + 0 + "$</span></div>";
    //html += "<div style='height: 25px;float: left;margin-left:10px;' onclick='add_qty(1)'><img src='images/logo/addition-color-icon.png' style='height: 100%;'></div>";
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td Colspan='2'>";
    html += "<div id='div_addcredit'>";
    html += "<div class='item-qty' onclick='select_qty(1)'>1</div>";
    html += "<div class='item-qty' onclick='select_qty(2)'>2</div>";
    html += "<div class='item-qty' onclick='select_qty(5)'>5</div>";
    html += "<div class='item-qty' onclick='select_qty(10)'>10</div>";
    html += "<div class='item-qty' onclick='select_qty(20)'>20</div>";
    html += "<div class='item-qty' onclick='select_qty(50)'>50</div>";
    html += "<div class='item-qty' onclick='select_qty(100)'>100</div>";
    html += "<div style='width: 60px; background-color: #a94442;' class='item-qty' onclick='clear_qty(" + credit + ")'>Clear</div>";
    html += "</div>";
    html += "</td>";
    html += "</tr>";

    html += "<tr style='height:50px'>";
    html += "<td colspan='2'>";
    html += "<div style='text-align:center'>";
    html += "<input type='button' value='កែប្រែ' class='button-add' onclick='addcredit()' />";
    html += "</div>";
    html += "<div style='text-align:center'>";
    html += "</div>";

    html += "</td>";
    html += "</tr>";


    html += "</tbody></table>";
    html += "</div>";
    html += "</div>";
    html += "</div>";

    $("#div_popup_content").html(html);
    openModal();

}

function changecurrency(credit) {
    let html = "";
    if ($('#rdodollar').is(":checked")) {
        html += "<div class='item-qty' onclick='select_qty(1)'>1</div>";
        html += "<div class='item-qty' onclick='select_qty(2)'>2</div>";
        html += "<div class='item-qty' onclick='select_qty(5)'>5</div>";
        html += "<div class='item-qty' onclick='select_qty(10)'>10</div>";
        html += "<div class='item-qty' onclick='select_qty(20)'>20</div>";
        html += "<div class='item-qty' onclick='select_qty(50)'>50</div>";
        html += "<div class='item-qty' onclick='select_qty(100)'>100</div>";
        html += "<div style='width: 60px; background-color: #a94442;' class='item-qty' onclick='clear_qty(" + credit + ")'>Clear</div>";
        $("#div_addcredit").html(html);
        $("#itemqty").html("0$");
        pubcredit_added = 0;
        select_qty(0);
    } else {
        html += "<div class='item-qty' onclick='select_qty(1000)'>1.000</div>";
        html += "<div class='item-qty' onclick='select_qty(2000)'>2.000</div>";
        html += "<div class='item-qty' onclick='select_qty(5000)'>5.000</div>";
        html += "<div class='item-qty' onclick='select_qty(10000)'>10.000</div>";
        html += "<div class='item-qty' onclick='select_qty(20000)'>20.000</div>";
        html += "<div class='item-qty' onclick='select_qty(50000)'>50.000</div>";
        html += "<div class='item-qty' onclick='select_qty(100000)'>100.000</div>";
        html += "<div style='width: 60px; background-color: #a94442;' class='item-qty' onclick='clear_qty(" + credit + ")'>Clear</div>";
        $("#div_addcredit").html(html);
        $("#itemqty").html("0R");
        pubcredit_added = 0;
        select_qty(0);
    }
}
function clear_qty(credit) {
    //pubcredit_added = 0;
    //$("#itemqty").html(0 + "$");
    //let GTotal = credit;
    //$("#span_credit").html(GTotal.toLocaleString() + "$");
    pubcredit_added = 0;
    select_qty(0);

}

function checkdeductchange() {
    pubcredit_added = 0;

    $("#itemqty").html(pubcredit_added + "$");
    //console.log(objitem);

    let GTotal = currenctcredit;
    $("#span_credit").html(GTotal.toLocaleString() + "$");

    if ($('#chkDeduct').is(":checked")) {
        //pubcredit_added -= credit_added;
    } else {
        //pubcredit_added += credit_added;
    }

}
function select_qty(credit_added) {
    //console.log(objitem);
    //objitem.Qty = qty;

    if ($('#chkDeduct').is(":checked")) {
        pubcredit_added -= credit_added;
    } else {
        pubcredit_added += credit_added;
    }
    let GTotal = 0;
    let GTotalReil = 0;
    if ($('#rdodollar').is(":checked")) {
        $("#itemqty").html(pubcredit_added.toLocaleString() + "$");
        GTotal = parseFloat(currenctcredit) + parseFloat(pubcredit_added);
        GTotalReil = parseFloat(currenctcreditReil);
    } else {
        $("#itemqty").html(pubcredit_added.toLocaleString() + "R");
        GTotal = parseFloat(currenctcredit);
        GTotalReil = parseFloat(currenctcreditReil) + parseFloat(pubcredit_added);
    }
    //console.log(objitem);

    
    $("#span_credit").html(GTotal.toLocaleString() + "$ និង " + GTotalReil.toLocaleString() + "R");

}


function addcredit() {
    let staffid = $("#hdStaffID").val();
    //let itemcode = $("#txtBarcode").val();
    let currency = "";
    if ($('#rdodollar').is(":checked")) {
        currency = "$";
    } else {
        currency = "R";
    }
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ManageUser.aspx/AddCredit",
        data: '{"StaffID":' + staffid + ',"Currency":"' + currency + '","Credit":' + pubcredit_added + ',"CreatedByID":1}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            //console.log(JSON.parse(data));
            window.location = window.location.href;

        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function showcredit() {
    let staffid = $("#hdStaffID").val();
    let credit = $("#txtCredit").val();
    let creditReil = $("#txtCreditReil").val();
    show_formcredit(staffid, credit, creditReil);
}

function showcredithistory() {
    let staffid = $("#hdStaffID").val();
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ManageUser.aspx/CreditHistory",
        data: '{"StaffID":' + staffid + '}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            $("#div_popup_content").html(data.d);
            openModal();

        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}