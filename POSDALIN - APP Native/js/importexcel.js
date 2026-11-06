var jsondata_purchase = [];
var jsondata_transfer = [];
var jsondata_adjustment = [];
var arrjsondata = [];
var uploadtype = '';
var jsondata_stock = [];

const employees = [
    {
        id: '1',
        name: 'Saitama',
        age: '32'
    },
    {
        id: '2',
        name: 'Genos',
        age: "24"
    }
]


window.onscroll = function () { myfixedheader() };



function myfixedheader() {

    var myheader = document.getElementById("rowheader");
    var sticky = myheader.offsetTop;

    if (window.pageYOffset > sticky) {
        myheader.classList.add("sticky");
    } else {
        myheader.classList.remove("sticky");
    }
}



$(document).ready(function () {
    $(function () {
        $("#txtdatefrom").datepicker(
            {
                dateFormat: 'dd-mm-yy',

            }).datepicker("setDate", 'now');
        $("#txtdateend").datepicker(
            {
                dateFormat: 'dd-mm-yy',

            }).datepicker("setDate", 'now');
        $("#txttrandate").datepicker(
            {
                dateFormat: 'dd-mm-yy',

            }).datepicker("setDate", 'now');
        
    });

    //getperiod();   
    $("#div_upload").hide();
    let username = $.urlParam('User');
    
});


$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + '=([^&#]*)')
        .exec(window.location.search);

    return (results !== null) ? results[1] || 0 : false;
}

//exportExcel.addEventListener("click", async () => {

//    const XLSX = await import("https://cdn.sheetjs.com/xlsx-0.19.2/package/xlsx.mjs");

//    const worksheet = XLSX.utils.json_to_sheet(jsondata_stock)

//    const workbook = XLSX.utils.book_new();
//    XLSX.utils.book_append_sheet(workbook, worksheet, "Employees");

//    XLSX.writeFile(workbook, "Employee Lists.xlsx", { compression: true });


//});






function exportexcel() {
    var d = new Date();

    var month = d.getMonth() + 1;
    var day = d.getDate();

    var output = d.getFullYear() + '-' + (month < 10 ? '0' : '') + month + '-' + (day < 10 ? '0' : '') + day;

    var data = $('#div_list table');
    //var data = jsondata_stock;

    var excelFile = XLSX_Export.utils.table_to_book(data, { sheet: 'Stock_On_Hand' });
    XLSX_Export.write(excelFile, { bookType: 'xlsx', bookSST: true, type: 'base64' });
    XLSX_Export.writeFile(excelFile, 'bookstock' + output + '.xlsx');


}
function checkuser(username) {


    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStock.aspx/GetUser",
        data: '{"Username":"' + username +'"}',
        success: function (data) {
            console.log("checkuser");
            console.log(data.d);
            let securitygroup = data.d.SecurityGroup;
            if (securitygroup != 'Finance' && securitygroup != 'DBAdmin') {
                $("#aupload_purchase").hide();
                $("#aupload_transfer").hide();
                $("#aupload_adjustment").hide();
                $("#div_addtran").hide();
            }


        },
        error: function (result) {
            console.log(result);
            $("#aupload_purchase").hide();
            $("#aupload_transfer").hide();
            $("#aupload_adjustment").hide();
            $("#div_addtran").hide();
            //$('#loading').hide();
        }
    });
}

function gohome() {
    window.location = window.location.href.replace("itemstock.aspx", "edb.aspx");
}

function getperiod() {
    

    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemPeriod.aspx/GetPeriod",
        data: '',
        success: function (data) {
            console.log(data);
            $("#dplperiod").find("option").remove();
            $("#dplperiod").append($('<option>', {
                value: 0,
                text: 'Please select period'
            }));
            let selectedid = 0;
            for (var i = 0; i < data.d.length; i++) {
                let id = data.d[i].PeriodID;
                let startdate = data.d[i].StartDate;
                let enddate = data.d[i].EndDate;
                let closed = data.d[i].Closed;
                let closeddate = data.d[i].ClosedDate;
                
                if (closed == false) {
                    selectedid = id;
                    $("#dplperiod").append($('<option>', {
                        value: id,
                        text: startdate + ' To ' + enddate
                    }));
                } else {
                    $("#dplperiod").append($('<option>', {
                        value: id,
                        text: startdate + ' To ' + enddate
                    }));
                }
            }
            $("#dplperiod").val(selectperiod);
           

        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}

function selectperiod() {
    let periodid = 0; //$("#dplperiod").val();
    let showcost = false;
    if ($('#dplType').val() =="Value") {
        showcost = true;
    }
    let startdate = $("#txtdatefrom").val();
    let enddate = $("#txtdateend").val();

    getItemStockBalance(periodid,showcost, startdate,enddate);
}

function editrecord(campus ,itemcode, detail, balance) {

    let html = "<div>Campus: " + campus + "</div><div>Item_code: " + itemcode + " (" + detail + ")</div><div>Last_Balance: <span id='spanbalance'>" + balance + "</span></div>";
    $("#hdid").val(itemcode);
    $("#hdcampus").val(campus);

    $("#div_itemdetail").html(html);
    $("#div_closed").hide();
    $(".div-popup").show();
    $("#btnDelete").hide();
    Gettranlist();
}

function closepopup() {
    $(".div-popup").hide();
}

function changetrantype(o) {
    let trantype = $(o).val();

    if (trantype == "Transfer Out") {
        $("#dplCampus").show();
    } else {
        $("#dplCampus").hide();
    }

    if (trantype == "Openning Stock" || trantype == "Purchases") {
        $("#txtCost").show();
    } else {
        $("#txtCost").hide();
    }


}

function submittran() {
    let TranType = $("#dplTranType").val();
    if (TranType == "Transfer Out") {
        let PeriodID = 0;
        let itemcode = $("#hdid").val();
        let campus = $("#hdcampus").val();
        let targetcampus = $("#dplCampus").val();
        let TranType = $("#dplTranType").val();
        let adjustamount = $("#txtadjustamount").val();
        let trandate = ""; //$("#txttrandate").val();
        let comment = $("#txtcomment").val();
        let cost = 0;
        let sourcetranid = 0;
        let createdby = "";
        addtransfer(trandate, PeriodID, itemcode, campus, TranType, adjustamount, cost, comment, targetcampus, campus, sourcetranid,createdby);
    } else {
        addtran();
    }
}

function addtran() {
    let PeriodID = 0; //$("#dplperiod").val();
    let itemcode = $("#hdid").val();
    let campus = $("#hdcampus").val();
    let TranType = $("#dplTranType").val();
    let adjustamount = $("#txtadjustamount").val();
    let cost = $("#txtCost").val(); 
    let trandate = ""; //$("#txttrandate").val();
    let comment = $("#txtcomment").val();
    let createdby = "";


    if (TranType == "Transfer In" || TranType == "Return") {
        alert("This option not allowed.");
    } else {
        if (TranType == "Openning Stock" || TranType == "Purchases") {
            if ($.isNumeric(cost)==false) {
                alert("Invalid Cost!")
            } else if ($.isNumeric(adjustamount)==false) {
                alert("Invalid Adjust Amount!")
            } else {
                addtrandetail(trandate, PeriodID, itemcode, campus, TranType, adjustamount, cost, comment, "", "", 0, createdby);
            }
        } else {
            if ($.isNumeric(adjustamount)==false) {
                alert("Invalid Adjust Amount!")
            } else {
                addtrandetail(trandate, PeriodID, itemcode, campus, TranType, adjustamount, cost, comment, "", "", 0, createdby);
            }
        }
    }



}




function syndata(obj) {
    let jsonstring = "";

    jsonstring = JSON.stringify(obj);
    console.log(jsonstring);
    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStock.aspx/AddTransaction_Upload",
        data: "{'Jsonstring':'" + jsonstring + "'}",
        success: function (data) {
            console.log(data);
            $("#divLoading").hide();
            $("#btnConfirm").show();
            closepopup();
            alert("uploaded!");
            selectperiod();
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}


function addtrandetail_upload(arr_records) {
    let jsonstring = "";

    let obj = {};
    obj.JSONData = arr_records;

    console.log(obj);

    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "Export.aspx/AddTransaction_Upload",
        data: JSON.stringify(obj),
        success: function (data) {
            console.log(data);
            $("#divLoading").hide();
            $("#btnConfirm").show();
            closepopup();
            alert("uploaded!");
            selectperiod();
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}


function addtrandetail(trandate, PeriodID, itemcode, campus, TranType, adjustamount, cost, comment,targetcampus, sourcecampus, sourcetranid,createdby) {
    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStock.aspx/AddTransaction",
        data: '{"TranDate":"' + trandate + '","PeriodID":' + PeriodID + ',"ItemCode":"' + itemcode + '","Campus":"' + campus + '","TranType":"' + TranType + '","AdjustAmount":' + adjustamount + ',"Cost":' + cost + ',"CreatedBy":"' + createdby + '","Comment":"' + comment + '","SourceCampus":"' + sourcecampus + '","TargetCampus":"' + targetcampus + '","SourceTranID":' + sourcetranid + '}',
        success: function (data) {
            console.log(data);

            let result = data.d;
            if (result == "Add new transaction") {
                let balance = parseInt($("#spanbalance").html());
                let newbalance = 0;
                if (TranType == "Openning Stock" || TranType == "Purchases" || TranType == "Transfer Out") {
                    newbalance = parseInt(balance) + parseInt(adjustamount);
                } else {
                    newbalance = parseInt(balance) + parseInt(adjustamount);
                }
                $("#spanbalance").html(newbalance);
                //closepopup();
                Gettranlist();
                selectperiod();
            } else {
                alert(result);
            }
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}

function addtran_purchase(PeriodID, itemcode, campus, TranType, adjustamount, trandate, comment) {
    //let PeriodID = $("#dplperiod").val();
    //let itemcode = $("#hdid").val();
    //let campus = $("#hdcampus").val();
    //let TranType = $("#dplTranType").val();
    //let adjustamount = $("#txtadjustamount").val();
    //let trandate = $("#txttrandate").val();
    //let comment = $("#txtcomment").val();

    if (TranType == "Transfer In" || TranType == "Return") {
        alert("This option not allowed.");
    } else {
        $.ajax({
            //cache: false,
            //async: false,
            type: "Post",
            dataType: "Json",
            contentType: "application/json; charset=utf-8",
            url: "ItemStock.aspx/AddTransaction",
            data: '{"TranDate":"' + trandate + '","PeriodID":' + PeriodID + ',"ItemCode":"' + itemcode + '","Campus":"' + campus + '","TranType":"' + TranType + '","AdjustAmount":' + adjustamount + ',"CreatedBy":"","Comment":"' + comment + '"}',
            success: function (data) {
                console.log(data);
                let result = data.d;
                if (result == "Add new transaction") {
                    Gettranlist();
                    selectperiod();
                } else {
                    alert(result);
                }
            },
            error: function (result) {
                console.log(result);
                //$('#loading').hide();
            }
        });
    }

   
}


function addtransfer_upload(obj) {

    console.log("addtransfer");
    let jsonstring = "";

    jsonstring = JSON.stringify(obj);
    //console.log(jsonstring);

        $.ajax({
            //cache: false,
            //async: false,
            type: "Post",
            dataType: "Json",
            contentType: "application/json; charset=utf-8",
            url: "ItemStock.aspx/AddTransfer_upload",
            data: "{'Jsonstring':'" +  jsonstring + "'}",
            success: function (data) {
                console.log(data);
                $("#divLoading").hide();
                $("#btnConfirm").show();
                closepopup();
                alert("uploaded!")
                let result = data.d;
                if (result == "Add new transaction") {
                    let balance = parseInt($("#spanbalance").html());
                    let newbalance = 0;
                    if (adjustamount < 0) {
                        newbalance = parseInt(balance) + parseInt(adjustamount);
                    } else {
                        newbalance = parseInt(balance) - parseInt(adjustamount);
                    }

                    $("#spanbalance").html(newbalance);
                    //closepopup();
                    Gettranlist();
                    selectperiod();
                } else {
                    alert(result);
                }
            },
            error: function (result) {
                console.log(result);
                //$('#loading').hide();
            }
        });
   


}


function addtransfer(trandate, PeriodID, itemcode, campus, TranType, adjustamount, cost, comment, targetcampus, sourcecampus, sourcetranid, createdby) {
    
    console.log("addtransfer");
    if (TranType == "Transfer In" || TranType == "Return") {
        alert("This option not allowed.");
    } else {
        $.ajax({
            //cache: false,
            //async: false,
            type: "Post",
            dataType: "Json",
            contentType: "application/json; charset=utf-8",
            url: "ItemStock.aspx/AddTransfer",
            data: '{"TranDate":"' + trandate + '","PeriodID":' + PeriodID + ',"ItemCode":"' + itemcode + '","Campus":"' + campus + '","TranType":"' + TranType + '","AdjustAmount":' + adjustamount + ',"Cost":' + cost + ',"CreatedBy":"' + createdby + '","Comment":"' + comment + '","SourceCampus":"' + sourcecampus + '","TargetCampus":"' + targetcampus + '","SourceTranID":' + sourcetranid + '}',
            success: function (data) {
                console.log(data);
                let result = data.d;
                if (result == "Add new transaction") {
                    let balance = parseInt($("#spanbalance").html());
                    let newbalance = 0;
                    if (adjustamount < 0) {
                        newbalance = parseInt(balance) + parseInt(adjustamount);
                    } else {
                        newbalance = parseInt(balance) - parseInt(adjustamount);
                    }
                        
                    $("#spanbalance").html(newbalance);
                    //closepopup();
                    Gettranlist();
                    selectperiod();
                } else {
                    alert(result);
                }
            },
            error: function (result) {
                console.log(result);
                //$('#loading').hide();
            }
        });
    }


}


const number_format = (number, decimals, dec_point, thousands_sep) => {
    number = number.toFixed(decimals);

    var nstr = number.toString();
    nstr += '';
    x = nstr.split('.');
    x1 = x[0];
    x2 = x.length > 1 ? dec_point + x[1] : '';
    var rgx = /(\d+)(\d{3})/;

    while (rgx.test(x1))
        x1 = x1.replace(rgx, '$1' + thousands_sep + '$2');

    return x1 + x2;
}


function getItemStockBalance(periodid,showcost,startdate,enddate) {

    $("#div_upload").hide();
    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStock.aspx/getItemStockBalance",
        data: '{"PeriodID":' + periodid + ',"StartDate":"' + startdate + '","EndDate":"' + enddate + '"}',
        success: function (data) {
            console.log(data);
            jsondata_stock = data;
            var html = "";
          
            html += "<table class='mytable' style='border:1px solid #adadad;' cellspacing='5' cellpadding='5'>";
            html += "<tr><td style='border:none;text-align:left;' colspan='11'>Stock On Hand by Campus (" + enddate + ")</td></tr>";
            html += "<tr id='rowheader'><th>Item Code</th><th style='width:200px'>Item_Name</th><th>TP(Stock)</th><th>SM</th><th>SP</th><th>TP</th><th>TK</th><th>CA</th><th>SR</th><th>SR2</th><th>Total</th></tr>";

            let cssdisplay = 'text-align:right;color:#CDDC39;';
            if (showcost == false) {
                cssdisplay = 'text-align:right;color:#CDDC39;display:none;'
            }

            let TotalSM = 0;
            let TotalSP = 0;
            let TotalTP = 0;
            let TotalTK = 0;
            let TotalCA = 0;
            let TotalSR = 0;
            let TotalSR2 = 0;
            let TotalTPStock = 0;

            let TotalSMCost = 0;
            let TotalSPCost = 0;
            let TotalTPCost = 0;
            let TotalTKCost = 0;
            let TotalCACost = 0;
            let TotalSRCost = 0;
            let TotalSR2Cost = 0;
            let TotalTPStockCost = 0;

           
            for (var i = 0; i < data.d.length; i++) {
                let id = data.d[i].PeriodID;
                let itemcode = data.d[i].ItemCode;
                let detail = data.d[i].Detail;
                let SM = data.d[i].SM;
                let SP = data.d[i].SP;
                let TP = data.d[i].TP;
                let TK = data.d[i].TK;
                let CA = data.d[i].CA;
                let SR = data.d[i].SR;
                let SR2 = data.d[i].SR2;
                let TPStock = data.d[i].TPStock;

                let SMCost = data.d[i].SMCost;
                let SPCost = data.d[i].SPCost;
                let TPCost = data.d[i].TPCost;
                let TKCost = data.d[i].TKCost;
                let CACost = data.d[i].CACost;
                let SRCost = data.d[i].SRCost;
                let SR2Cost = data.d[i].SR2Cost;
                let TPStockCost = data.d[i].TPStockCost;


                TotalSM += SM;
                TotalSP += SP;
                TotalTP += TP;
                TotalTK += TK;
                TotalCA += CA;
                TotalSR += SR;
                TotalSR2 += SR2;
                TotalTPStock += TPStock;

                TotalSMCost += SMCost;
                TotalSPCost += SPCost;
                TotalTPCost += TPCost;
                TotalTKCost += TKCost;
                TotalCACost += CACost;
                TotalSRCost += SRCost;
                TotalSR2Cost += SR2Cost;
                TotalTPStockCost += TPStockCost;

                if (showcost == true) {
                    html += "<tr><td style='text-align:left;'>" + itemcode + "</td><td style='text-align:left;width:200px;'>" + detail + "</td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(TPStockCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(SMCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(SPCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(TPCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(TKCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(CACost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(SRCost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format(SR2Cost, 2, '.', ',') + "</div></td>";
                    html += "<td><div style='" + cssdisplay + "'>$" + number_format((SMCost + SPCost + TPCost + TKCost + CACost + SRCost + SR2Cost + TPStockCost), 2, '.', ',') + "</div></td>";
                    html += "</tr> ";
                } else {
                    html += "<tr><td style='text-align:left;'>" + itemcode + "</td><td style='text-align:left;width:200px;'>" + detail + "</td>";
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('TPStock','" + itemcode + "','" + detail + "'," + TPStock + ")\" >" + TPStock + "</a></td>";
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('SM','" + itemcode + "','" + detail + "'," + SM + ")\" >" + SM + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('SP','" + itemcode + "','" + detail + "'," + SP + ")\" >" + SP + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('TP','" + itemcode + "','" + detail + "'," + TP + ")\" >" + TP + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('TK','" + itemcode + "','" + detail + "'," + TK + ")\" >" + TK + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('CA','" + itemcode + "','" + detail + "'," + CA + ")\" >" + CA + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('SR','" + itemcode + "','" + detail + "'," + SR + ")\" >" + SR + "</a>" ;
                    html += "<td><a style='color:white;' href='#' onclick=\"editrecord('SR','" + itemcode + "','" + detail + "'," + SR2 + ")\" >" + SR2 + "</a>" ;
                    html += "<td>" + (SM + SP + TP + TK + CA + SR + SR2 + TPStock) + "</td>";
                    html += "</tr> ";
                }            

            }



            if (showcost == true) {
                html += "<tr><td style='text-align:left;'></td><td style='text-align:left;'>Total</td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalTPStockCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalSMCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalSPCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalTPCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalTKCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalCACost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalSRCost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format(TotalSR2Cost, 2, '.', ',') + "</div></td>";
                html += "<td><div style='" + cssdisplay + "'>$" + number_format((TotalSMCost + TotalSPCost + TotalTPCost + TotalTKCost + TotalCACost + TotalSRCost + TotalSR2Cost + TotalTPStockCost), 2, '.', ',') + "</div></td>";
                html += "</tr> ";

            }else{
                html += "<tr><td style='text-align:left;'></td><td style='text-align:left;'>Total</td>";
                html += "<td>" + TotalTPStock +  "</td>";
                html += "<td>" + TotalSM + "</td>";
                html += "<td>" + TotalSP + "</td>";
                html += "<td>" + TotalTP + "</td>";
                html += "<td>" + TotalTK +  "</td>";
                html += "<td>" + TotalCA +  "</td>";
                html += "<td>" + TotalSR +  "</td>";
                html += "<td>" + TotalSR2 + "</td>";
                html += "<td>" + (TotalTPStock + TotalSM + TotalSP + TotalTP + TotalTK + TotalCA + TotalSR + TotalSR2) + "</td>";
                html += "</tr> ";

            }


            html += "</table>"
            $("#div_list").html(html);
            $("#div_upload").show();
        },
        error: function (result) {
            console.log(result);

            //$('#loading').hide();
        }
    });
}
function viewreport() {
    window.location = window.location.href.replace("itemstock.aspx","itemstockreport.aspx")
}
function getreport() {
    let campus = $("#dplCampus").val();
    let startdate = $("#txtdatefrom").val();
    let enddate = $("#txtdateend").val();
    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStockReport.aspx/GetReport",
        data: '{"Campus":"' + campus + '","StartDate":"' + startdate + '","EndDate":"' + enddate + '"}',
        success: function (data) {
            

            //BindTable(data.d, '#exceltable');      
            var html = "";

            html += "<table class='mytable' style='border:1px solid #adadad;width:98%' cellspacing='5' cellpadding='5'>";
            html += "<tr><th>ItemCode</th><th>Openning Stock</th><th>Purchases</th><th>Transfer Out</th><th>Transfer In</th><th>Sponsor sales</th><th>Return/ Refunds</th><th>Sales</th><th>Closing Stock</th><th>Adjustments</th><th>Adjusted Closing Stock</th></tr>";
            

            for (var i = 0; i < data.d.length; i++) {
               
                let ItemCode = data.d[i].ItemCode;
                let OpenningStock = data.d[i].OpenningStock;
                let Purchases = data.d[i].Purchases;
                let TransferOut = data.d[i].TransferOut;
                let TransferIn = data.d[i].TransferIn;
                let Sponsorsales = data.d[i].Sponsorsales;
                let Refunds = data.d[i].Refunds;
                let Sales = data.d[i].Sales;
                let ClosingStock = data.d[i].ClosingStock;
                let Adjustments = data.d[i].Adjustments;
                let AdjustedClosingStock = data.d[i].AdjustedClosingStock;
               


                html += "<tr><td>" + ItemCode + "</td><td>" + OpenningStock + "</td><td>" + Purchases + "</td><td>" + TransferOut + "</td><td>" + TransferIn + "</td><td>" + Sponsorsales + "</td><td>" + Refunds + "</td><td>" + Sales + "</td><td>" + ClosingStock + "</td><td>" + Adjustments + "</td><td>" + AdjustedClosingStock + "</td>"

              
                html += "</tr> ";
            }
            html += "</table>"
            $("#div_list").html(html);

        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}

function Gettranlist() {
    let PeriodID = 0;//$("#dplperiod").val();
    let itemcode = $("#hdid").val();
    let campus = $("#hdcampus").val();
    let startdate = $("#txtdatefrom").val();
    let enddate = $("#txtdateend").val();

    $.ajax({
        //cache: false,
        //async: false,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemStock.aspx/GetTransaction",
        data: '{"PeriodID":' + PeriodID + ',"ItemCode":"' + itemcode + '","Campus":"' + campus + '","StartDate": "' + startdate + '", "EndDate": "' + enddate + '"}',
        success: function (data) {
            console.log(data);  
            var html = "";

            html += "<table class='mytable' style='border:1px solid #adadad;width:98%' cellspacing='5' cellpadding='5'>";
            html += "<tr><th style='width:100px;'>Tran_Date</th><th>Openning Stock</th><th>Purchases</th><th>Transfer Out</th><th>Transfer In</th><th>Sponsor sales</th><th>Return/ Refunds</th><th>Sales</th><th>Closing Stock</th><th>Adjustments</th><th>Adjusted Closing Stock</th><th>Weighted Average Cost</th><th>Comment</th></tr>";

            for (var i = 0; i < data.d.length; i++) {
                let TranID = data.d[i].TranID;
                let TranDate = data.d[i].TranDate;
               
                let OpenningStock = data.d[i].OpenningStock;
                let Purchases = data.d[i].Purchases;
                let TransferOut = data.d[i].TransferOut;
                let TransferIn = data.d[i].TransferIn;
                let Sponsorsales = data.d[i].Sponsorsales;
                let Refunds = data.d[i].Refunds;
                let Sales = data.d[i].Sales;
                let ClosingStock = data.d[i].ClosingStock;
                let Adjustments = data.d[i].Adjustments;
                let AdjustedClosingStock = data.d[i].AdjustedClosingStock;
                let Cost = data.d[i].Cost;
                let Comment = data.d[i].Comment;
           
                html += "<tr><td>" + TranDate + "</td><td>" + OpenningStock + "</td><td>" + Purchases + "</td><td>" + TransferOut + "</td><td>" + TransferIn + "</td><td>" + Sponsorsales + "</td><td>" + Refunds + "</td><td>"  + Sales + "</td><td>" + ClosingStock + "</td><td>" + Adjustments + "</td><td>" + AdjustedClosingStock + "</td><td>" + Cost + "</td>"

                if (Comment == "") {
                    html += "<td></td>"
                } else {
                    html += "<td><div style='text-align:center'><i href='#' onclick=\"alert('" + Comment + "')\" class='fa fa-info-circle' style='font-size: 30px;'></i></div></td>"
                }
                //html += "<td><i href='#' onclick=\"editransaction(" + TranID + "')\" class='fa fa-edit'></i></td>";
                html += "</tr> ";
            }
            html += "</table>"
            $("#div_tranlist").html(html);
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}


function ExportToTable() {
    console.log("ExportToTable")
    $("#exceltable").html("");
    var regex = /^([a-zA-Z0-9\s_\\.\-:])+(.xlsx|.xls)$/;
    /*Checks whether the file is a valid excel file*/
    if (regex.test($("#excelfile").val().toLowerCase())) {
        var xlsxflag = false; /*Flag for checking whether excel is .xls format or .xlsx format*/
        if ($("#excelfile").val().toLowerCase().indexOf(".xlsx") > 0) {
            xlsxflag = true;
        }
        /*Checks whether the browser supports HTML5*/
        if (typeof (FileReader) != "undefined") {
            var reader = new FileReader();
            reader.onload = function (e) {
                var data = e.target.result;
                console.log(data);
                /*Converts the excel data in to object*/
                if (xlsxflag) {
                    var workbook = XLSX.read(data, { type: 'binary' });
                }
                else {
                    var workbook = XLS.read(data, { type: 'binary' });
                }
                /*Gets all the sheetnames of excel in to a variable*/
                var sheet_name_list = workbook.SheetNames;

                var cnt = 0; /*This is used for restricting the script to consider only first sheet of excel*/
                sheet_name_list.forEach(function (y) { /*Iterate through all sheets*/
                    /*Convert the cell value to Json*/
                    if (xlsxflag) {
                        var exceljson = XLSX.utils.sheet_to_json(workbook.Sheets[y]);
                    }
                    else {
                        var exceljson = XLS.utils.sheet_to_row_object_array(workbook.Sheets[y]);
                    }
                    if (exceljson.length > 0 && cnt == 0) {
                        BindTable(exceljson, '#exceltable');
                        cnt++;
                    }
                });
                $('#exceltable').show();
            }
            if (xlsxflag) {/*If excel file is .xlsx extension than creates a Array Buffer from excel*/
                reader.readAsArrayBuffer($("#excelfile")[0].files[0]);
            }
            else {
                reader.readAsBinaryString($("#excelfile")[0].files[0]);
            }
        }
        else {
            alert("Sorry! Your browser does not support HTML5!");
        }
    }
    else {
        alert("Please upload a valid Excel file!");
    }
}  


function BindTable(jsondata, tableid) {/*Function used to convert the JSON array to Html Table*/
    console.log(jsondata);
    if (uploadtype == "purchase") {
        jsondata_purchase = jsondata;
    } else if (uploadtype == "transfer") {
        jsondata_transfer = jsondata;
    } else if (uploadtype == "adjustment") {
        jsondata_adjustment = jsondata;
    } else if (uploadtype == "item") {
        arrjsondata = jsondata;
    }
    var columns = BindTableHeader(jsondata, tableid); /*Gets all the column headings of Excel*/
    for (var i = 0; i < jsondata.length; i++) {
        var row$ = $('<tr/>');
        for (var colIndex = 0; colIndex < columns.length; colIndex++) {
            var cellValue = jsondata[i][columns[colIndex]];
            if (cellValue == null)
                cellValue = "";
            row$.append($('<td/>').html(cellValue));
        }
        $(tableid).append(row$);
    }
}
function BindTableHeader(jsondata, tableid) {/*Function used to get all column names from JSON and bind the html table header*/
    var columnSet = [];
    var headerTr$ = $('<tr/>');
    for (var i = 0; i < jsondata.length; i++) {
        var rowHash = jsondata[i];
        for (var key in rowHash) {
            if (rowHash.hasOwnProperty(key)) {
                if ($.inArray(key, columnSet) == -1) {/*Adding each unique column names to a variable array*/
                    columnSet.push(key);
                    headerTr$.append($('<th/>').html(key));
                }
            }
        }
    }
    $(tableid).append(headerTr$);
    return columnSet;
}  




function uploaddata() {
    $("#btnConfirm").hide();
    $("#divLoading").show();

        let arrobj_purchase = [];
        
    addtrandetail_upload(arrjsondata);

   
}

function uploaddataold() {
    $("#btnConfirm").hide();
    $("#divLoading").show();

    if (uploadtype == "purchase") {
        let arrobj_purchase = [];
        for (var i = 0; i < jsondata_purchase.length; i++) {
            let myobj_purchase = {};


            let Amount = jsondata_purchase[i].Amount;
            let Comment = jsondata_purchase[i].Comment;
            let CreatedBy = jsondata_purchase[i]["Created by"];
            let CreatedDate = jsondata_purchase[i]["Created Date"];
            let itemcode = jsondata_purchase[i]["Item Code"];
            let ItemName = jsondata_purchase[i]["Item Name"];
            let TargetCampus = jsondata_purchase[i]["Target Campus"];
            let TotalCost = jsondata_purchase[i]["Total Cost"].replace("$", "");
            let PeriodID = 0;
            myobj_purchase.TranDate = CreatedDate;
            myobj_purchase.TranType = "Purchases";
            myobj_purchase.AdjustAmount = Amount;
            myobj_purchase.PeriodID = 0;
            myobj_purchase.Comment = Comment;
            myobj_purchase.CreatedBy = CreatedBy;
            myobj_purchase.CreatedDate = CreatedDate;
            myobj_purchase.ItemCode = itemcode;
            myobj_purchase.ItemName = ItemName;
            myobj_purchase.Campus = TargetCampus;
            myobj_purchase.Cost = TotalCost;
         
            arrobj_purchase.push(myobj_purchase); 
           
        }
        addtrandetail_upload(arrobj_purchase);

    } else if (uploadtype == "transfer") {
        console.log(uploadtype);

        let arrobj = [];
        for (var i = 0; i < jsondata_transfer.length; i++) {
            let myobj = {};
            let Amount = jsondata_transfer[i].Amount;
            let Comment = jsondata_transfer[i].Comment;
            let Createdby = jsondata_transfer[i]["Approval By"];
            let CreatedDate = jsondata_transfer[i]["Created Date"];
            let itemcode = jsondata_transfer[i]["Item Code"];
            let FromCampus = jsondata_transfer[i]["From Campus"];
            let TargetCampus = jsondata_transfer[i]["Target Campus"];
            let TotalCost = 0;
            let PeriodID = 0;
            //console.log(Amount);
            myobj.TranDate = CreatedDate;
            myobj.PeriodID = 0;
            myobj.ItemCode = itemcode;
            myobj.Campus = FromCampus;
            myobj.TranType = "Transfer Out";
            myobj.AdjustAmount = Amount;
            myobj.Cost = TotalCost;
            myobj.CreatedBy = Createdby;
            myobj.Comment = Comment;
            myobj.SourceCampus = FromCampus;
            myobj.TargetCampus = TargetCampus;
            myobj.SourceTranID = 0;
            
            arrobj.push(myobj); 
        }

        addtransfer_upload(arrobj);

    } else if (uploadtype == "adjustment") {
        for (var i = 0; i < jsondata_adjustment.length; i++) {
            let Amount = jsondata_adjustment[i]["Adjusted Amount"];
            let Comment = jsondata_adjustment[i].Comment;
            let Createdby = jsondata_adjustment[i]["Created by"];
            let CreatedDate = "";
            let itemcode = jsondata_adjustment[i]["Item Code"];
            let ItemName = jsondata_adjustment[i]["Item Name"];
            let TargetCampus = jsondata_adjustment[i]["Target Campus"];
            let TotalCost = 0;
            let PeriodID = 0;
            addtrandetail(CreatedDate, PeriodID, itemcode, TargetCampus, "Adjustments", Amount, TotalCost, Comment, "", "", 0, Createdby);
            
        }
    }
}

function upload(type) {
    uploadtype = type;
    currentstep = 1;
    $("#div_popup_upload").show();

    if (uploadtype == "purchase") {
        $("#div_template").html('<a style="color:white;" href="Upload_s/Book Purchase Template.xlsx" target="_blank"> Download Excel Template</a>');
    } else if (uploadtype == "transfer") {
        $("#div_template").html('<a style="color:white;" href="Upload_s/Book Transfer Template.xlsx" target="_blank"> Download Excel Template</a>');
    } else if (uploadtype == "adjustment"){
        $("#div_template").html('<a style="color:white;" href="Upload_s/Adjustment Template.xlsx" target="_blank"> Download Excel Template</a>');
    }

    //if (uploadtype == "purchase") {
    //    $("#div_purchase_template").show();
    //    $("#div_transfer_template").hide();
    //} else {
    //    $("#div_purchase_template").hide();
    //    $("#div_transfer_template").show();
    //}
    gotostep(1);
}

var currentstep = 1;
function nextstep() {
    if (currentstep == 1) {
        ExportToTable();
    }
    if (currentstep >= 3) {
        currentstep = 3;
       
    } else {
        currentstep += 1;
    }   
   
    gotostep(currentstep);
}

function backstep() {
    if (currentstep <= 1) {
        currentstep = 1;
    } else {
        currentstep -= 1;
    }
    gotostep(currentstep);
}


function gotostep(step) {
    $("#div_step1").hide();
    $("#div_step2").hide();
    $("#div_step3").hide();
    $("#btnBack").show();
    $("#btnNext").show();
    if (step==1) {
        $("#div_step1").show();
        $("#btnBack").hide();
    } else if (step == 2) {
        $("#div_step2").show();
    } else if (step == 3) {
        $("#div_step3").show();
        $("#btnNext").hide();
    }
}