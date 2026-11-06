var SubTaskID = 0;
var arrFormularFields = [];
var objhidden = [];
var objmodel = {};
var searchdata = [];

var tableToExcel = (function () {
    var uri = 'data:application/vnd.ms-excel;base64,'
        , template = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--><meta http-equiv="content-type" content="text/plain; charset=UTF-8"/></head><body><table>{table}</table></body></html>'
        , base64 = function (s) { return window.btoa(unescape(encodeURIComponent(s))) }
        , format = function (s, c) { return s.replace(/{(\w+)}/g, function (m, p) { return c[p]; }) }
    return function (html, name) {
        var ctx = { worksheet: name || 'Worksheet', table: html }
        window.location.href = uri + base64(format(template, ctx))
    }
})()

// Read a page's GET URL variables and return them as an associative array.
function getUrlVars() {
    var vars = [], hash;
    var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}


$(document).ready(function () {

    
    $("#div_headertitle").html("Item List ");

    //SubTaskID = subtaskid;
    
    objhidden = ["", "ItemID","__type"];
    //getsale(SubTaskID);

    // Toggle barcode column visibility
    $('#chkShowBarcode').on('change', function() {
        toggleBarcodeColumn(this.checked);
    });

});

function search() {
    let searchby = $("#select_searchby").val();
    let searchvalue = $("#txt_searchvalue").val();
    let lowstock = 'No';
    if ($("#chkLowStock").is(":checked")) {
        lowstock='Yes'
    }
    if (searchby == "ItemType") {
        let categoryid = $("#select_category").val();
        let groupid = $("#select_group").val();
        getitemlistbygroup(categoryid, groupid, lowstock);
    } else {
        getitemlist(searchby, searchvalue, lowstock);
    }
}

function toggleBarcodeColumn(show) {
    // Toggle header and data cells for barcode column
    if (show) {
        $('th, td').filter(function() {
            return $(this).text() === 'Barcode' || $(this).find('svg.barcode').length > 0;
        }).show();
    } else {
        $('th, td').filter(function() {
            return $(this).text() === 'Barcode' || $(this).find('svg.barcode').length > 0;
        }).hide();
    }
}

function toggleSelectAll(checkbox) {
    $('.chkRow').prop('checked', checkbox.checked);
}

function getSelectedItemIds() {
    let selectedIds = [];
    $('.chkRow:checked').each(function() {
        selectedIds.push($(this).data('itemid'));
    });
    return selectedIds;
}

function exportexcel() {
    //let selectedIds = getSelectedItemIds();
    
    
    let searchby = $("#select_searchby").val();
    let searchvalue = $("#txt_searchvalue").val();
    let lowstock = 'No';
    if ($("#chkLowStock").is(":checked")) {
        lowstock = 'Yes'
    }
    
    // Check if barcode should be included
    let includeBarcode = $("#chkShowBarcode").is(":checked");
    
    // Store selected IDs for backend to filter
    //console.log('Exporting selected items:', selectedIds);
    
    if (searchby == "ItemType") {
        let categoryid = $("#select_category").val();
        let groupid = $("#select_group").val();
        exportlistbygroup(categoryid, groupid, lowstock, includeBarcode);
    } else {
        exportlistbysearchvalue(searchby, searchvalue, lowstock, includeBarcode);
    }
}

function DownloadFile(fileName) {
    //Set the File URL.
    var url = "Export/" + fileName;

    $.ajax({
        url: url,
        cache: false,
        xhr: function () {
            var xhr = new XMLHttpRequest();
            xhr.onreadystatechange = function () {
                if (xhr.readyState == 2) {
                    if (xhr.status == 200) {
                        xhr.responseType = "blob";
                    } else {
                        xhr.responseType = "text";
                    }
                }
            };
            return xhr;
        },
        success: function (data) {
            //Convert the Byte Data to BLOB object.
            var blob = new Blob([data], { type: "application/octetstream" });

            //Check the Browser type and download the File.
            var isIE = false || !!document.documentMode;
            if (isIE) {
                window.navigator.msSaveBlob(blob, fileName);
            } else {
                var url = window.URL || window.webkitURL;
                link = url.createObjectURL(blob);
                var a = $("<a />");
                a.attr("download", fileName);
                a.attr("href", link);
                $("body").append(a);
                a[0].click();
                $("body").remove(a);
            }
        }
    });
};
function exportexcelold() {

    
            let html = exporthtml(searchdata);
            console.log(html);

            let today = new Date();
            let dd = String(today.getDate()).padStart(2, '0');
            let mm = String(today.getMonth() + 1).padStart(2, '0'); //January is 0!
            let yyyy = today.getFullYear();

            today = dd + '-' + mm + '-' + yyyy;


            tableToExcel(html, 'Stock-' + today);

       
   
}

function changevatrate(e) {
    let rate = $(e).val();
    let obj = {};
    obj.SubTaskID = SubTaskID; 
    obj.CreatedByID = 0;
    obj.VAT = rate;

    $.ajax({
        //cache: false,
        async: false,
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "api/updatesalevatrate",
        data: JSON.stringify(obj),
        success: function (data) {
            //console.log(data);  
            if (data.result == "success") {
                getsale(SubTaskID);
            } else {
                alert("something went wrong!")
            }
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });

}

function exportlistbygroup(categoryid, groupid, lowstock, includeBarcode) {
    let obj = {};
    obj.GroupID = groupid;
    obj.LowStock = lowstock;
    obj.CategoryID = categoryid;
    obj.IncludeBarcode = includeBarcode !== false; // Default to true

    // Show loading dialog
    showLoadingDialog('Processing export... Downloading images and generating Excel file. Please wait.');

    $.ajax({
        //cache: false,
        async: true, // Changed to true for better UX
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/ExportItemListByGroup",
        data: JSON.stringify(obj),
        success: function (data) {
            console.log(data.d);
            hideLoadingDialog();
            DownloadFile(data.d);
        },
        error: function (result) {
            console.log(result);
            hideLoadingDialog();
            alert('Export failed. Please try again.');
        }
    });
}

function exportlistbysearchvalue(searchby, searchvalue, lowstock, includeBarcode) {
    let obj = {};
    obj.SearchBy = searchby;
    obj.SearchValue = searchvalue;
    obj.LowStock = lowstock;
    obj.IncludeBarcode = includeBarcode !== false; // Default to true

    // Show loading dialog
    showLoadingDialog('Processing export... Downloading images and generating Excel file. Please wait.');

    $.ajax({
        //cache: false,
        async: true, // Changed to true for better UX
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/ExportItemListBySearchValue",
        data: JSON.stringify(obj),
        success: function (data) {
            console.log(data.d);
            hideLoadingDialog();
            DownloadFile(data.d);
        },
        error: function (result) {
            console.log(result);
            hideLoadingDialog();
            alert('Export failed. Please try again.');
        }
    });
}


function getitemlistbygroup(categoryid,groupid, lowstock) {
    let obj = {};
    obj.GroupID = groupid;
    obj.LowStock = lowstock;
    obj.CategoryID = categoryid;

    $.ajax({
        //cache: false,
        async: false,
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/GetItemListByGroup",
        data: JSON.stringify(obj),
        success: function (data) {

            //$("#div_customerlist").html(data.result);
            let obj = data.d;
            console.log(obj);
            searchdata = obj;
            loadresult(obj);

          

        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}


function getitemlist(searchby,searchvalue,lowstock) {
    let obj = {};
    obj.SearchBy = searchby;
    obj.SearchValue = searchvalue;
    obj.LowStock = lowstock;


    $.ajax({
        //cache: false,
        async: false,
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/GetItemList" ,
        data: JSON.stringify(obj),
        success: function (data) {
            
            //$("#div_customerlist").html(data.result);
            let obj = data.d;
            console.log(obj);
            searchdata = obj;
            loadresult(obj);

            $(".textbox-date").datepicker(
                {
                    dateFormat: 'dd-mm-yy',

                })

        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}

function change_category(e) {
    let categoryid = $(e).val();
    loadgroup(categoryid);
}
function getsale(subtaskid) {


    $.ajax({
        //cache: false,
        async: false,
        type: "Get",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "api/getsale/" + subtaskid,
        data: '',
        success: function (data) {
            console.log(data);
            //$("#div_customerlist").html(data.result);
            loadresult(data);

            $(".textbox-date").datepicker(
                {
                    dateFormat: 'dd-mm-yy',

                })

        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}
function change_searchby(e) {
    let searchby = $(e).val();
    $("#div_select_category").hide();
    $("#div_select_group").hide();

    if (searchby == "ItemCode" || searchby == "ItemName") {
        $("#div_searchvalue").show();
    } else if (searchby == "ItemType") {
        $("#div_searchvalue").hide();
        $("#div_select_category").show();
        $("#div_select_group").show();
        loadcategory();
    } else if (searchby == "Supplier") {
        $("#div_searchvalue").show();
    } else {
        $("#div_searchvalue").show();
    }
}

function loadcategory() {
    
    $.ajax({
        //cache: false,
        async: false,
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/GetItemCategory",
        data: '',
        success: function (data) {
            //console.log(data);
            let obj = JSON.parse(data.d);
            console.log(obj);
            let length = obj.length;

            
            for (var i = 0; i < length; i++) {
                $('#select_category').append($('<option>', {
                    value: obj[i].CategoryID,
                    text: obj[i].CategoryName
                }));
            }

            loadgroup(obj[0].CategoryID);

        },
        error: function (result) {
            console.log(result);
        }
    });
}

function loadgroup(CategoryID) {

    $.ajax({
        //cache: false,
        async: false,
        type: "POST",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/GetItemGroup",
        data: '{"CategoryID":' + CategoryID + '}',
        success: function (data) {
            //console.log(data);
            let obj = JSON.parse(data.d);
            console.log(obj);
            let length = obj.length;

            $('#select_group').empty();
            $('#select_group').append($('<option>', {
                value: 0,
                text: 'ទាំងអស់'
            }));


            for (var i = 0; i < length; i++) {
                $('#select_group').append($('<option>', {
                    value: obj[i].GroupID,
                    text: obj[i].GroupName
                }));
            }

        },
        error: function (result) {
            console.log(result);
        }
    });
}

function loadresult(data) {

    resultdata = data;
    console.log(resultdata);
    let Menustring = "";

    Menustring = "<div style='padding-left:2px;padding-top:20px;padding-bottom:20px'>";
    Menustring += "<table style='border:1px solid #adadad;width:98%' cellspacing='5' cellpadding='5'>";
    Menustring += "<tr>";
    
    // Add checkbox column header with select all
    //Menustring += "<th align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:3%'><input type='checkbox' id='chkSelectAll' onchange='toggleSelectAll(this)' /></th>";

   // Menustring += "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70'><i onclick='newlead()' class=\"fa fa-plus\" style=\"color: #04ff04;border: solid #17ff00;border-radius: 15px;padding: 5px;\"></i></td>";
   
    let objheader = {};
    objheader.ItemCode = "代码";
    objheader.ImageUrl = "图片";
    objheader.ItemName = "ឈ្មោះខ្មែរ";
    objheader.ItemChineseName = "产品名称"; 
    objheader.OrderAmount = "数量";
    objheader.CurrentQty = "Stock";
    objheader.SetLowStock = "Low Stock";
    objheader.Barcode = "Barcode";
    console.log("objheader");
    console.log(objheader);
    $("#div_excel").html("");
    let obj = data[0];
    objmodel = obj;
    console.log(obj);
    let objstyle = {};
    objstyle.ItemCode = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"
    objstyle.ImageUrl = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"
    objstyle.ItemName = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:28%"
    objstyle.ItemChineseName = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:22%;"
    objstyle.OrderAmount = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"
    objstyle.CurrentQty = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"
    objstyle.SetLowStock = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"
    objstyle.Barcode = "padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;width:10%;"

    //obj.Barcode = obj.ItemCode;

    for (var key in obj) {
        
        //console.log(key);
        if (objhidden.find(x => x === key)) {
            
        } else {
            let headername = "";
            
            headername = objheader[key];
           
            Menustring += "<th align='center' style='" + objstyle[key] + "'>" + headername + "</th>";
            
            
        }
     
        //console.log(obj[key]);
    }
    
    //Menustring += "<th align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70'>Action</th>";  
    Menustring += "</tr>";


    for (var i = 0; i < data.length; i++) {
        obj = data[i];
        obj.Barcode = obj.ItemCode;
        if (obj.ItemID > 0) {
            
            Menustring += newrowhtml(obj,i);
            
        }
    }
    //Menustring += addnewrow(objmodel, arrFormularFields, objhidden);

    Menustring += "</table>";

    
    $("#div_excel").html(Menustring);
    
    // Generate barcodes after HTML is rendered
    setTimeout(function() {
        for (var i = 0; i < data.length; i++) {
            obj = data[i];
            if (obj.ItemID > 0 && obj.ItemCode) {
                try {
                    var barcodeElement = document.getElementById('barcode' + obj.ItemID);
                    if (barcodeElement) {
                        JsBarcode(barcodeElement, obj.ItemCode, {
                            format: "CODE128",
                            width: 1.5,
                            height: 35,
                            displayValue: true,
                            fontSize: 12,
                            margin: 2
                        });
                    }
                } catch (e) {
                    console.log("Barcode generation error for ItemID " + obj.ItemID + ":", e);
                }
            }
        }
    }, 100);
}


function exporthtml(data) {

    resultdata = data;

    resultdata = data;
    let Menustring = "";

    Menustring = "<div style='padding-left:2px;padding-top:20px;padding-bottom:20px'>";
    Menustring += "<table style='border:1px solid #adadad;width:98%' cellspacing='5' cellpadding='5'>";
    Menustring += "<tr>";


    // Menustring += "<td align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70'><i onclick='newlead()' class=\"fa fa-plus\" style=\"color: #04ff04;border: solid #17ff00;border-radius: 15px;padding: 5px;\"></i></td>";

    let objheader = {};
    objheader.saleDate = "ថ្ងៃខែឆ្នាំ<br />Date";
    objheader.invoiceNo = "លេខវិក្កយបត្រ<br />Rec/Inv.No.";
    objheader.client = "អ្នកទិញ<br />Clients";
    objheader.vatNo = "លេខ អតប<br />VAT no";
    objheader.description = "សេចក្តីបរិយាយ<br />Description";
    objheader.qty = "បរិមាណ<br />Qty";
    objheader.vatExemptSalesUSD = "ការផ្គត់ផ្គង់មិនជាប់អាករ(USD)<br />VAT Exempt Sales(USD)";
    objheader.vatExemptSalesKH = "ការផ្គត់ផ្គង់មិនជាប់អាករ(KHR)<br />VAT Exempt Sales(KHR)";
    objheader.saleRegisteredUSD = "ការលក់ជាប់អាករ(USD)<br />Sales(USD)";
    objheader.vatRegisteredUSD = "អាករ(USD)<br />VAT(USD)";
    objheader.saleRegisteredKH = "ការលក់ជាប់អាករ(KHR)<br />Sales(KHR)";
    objheader.vatRegisteredKH = "អាករ(KH)<br />VATKH)";

    objheader.saleFinalUserUSD = "ការលក់អោយអ្នកប្រើប្រាស់(USD)<br />Sales(USD)";
    objheader.vatFinalUserUSD = "អាករ(USD)<br />VAT(USD)";
    objheader.saleFinalUserKH = "ការលក់អោយអ្នកប្រើប្រាស់(KHR)<br />Sales(KHR)";
    objheader.vatFinalUserKH = "អាករ(KH)<br />VATKH)";

    objheader.totalUSD = "សរុបថ្លៃលក់រួមទាំងអាករ(USD)<br />Total USD(VAT inclusive)"
    objheader.totalKH = "សរុបថ្លៃលក់រួមទាំងអាករ(KHR)<br />Total KHR(VAT inclusive)"

    objheader.exchangeRate = "អត្រាប្ដូរប្រាក់<br />Exchange Rate"
    objheader.taxImplication = "Tax Implication"
    console.log(data);
    if (data == null) {
        return "";
    }
    let obj = data[0];
    objmodel = obj;

    //arrFormularFields = JSON.parse(obj.formularFields);

    console.log("formularfields");
    console.log(arrFormularFields);

    for (var key in obj) {
        //console.log(key);
        if (objhidden.find(x => x === key)) {

        } else {
            let headername = "";

            headername = objheader[key];
            Menustring += "<th align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70;color:white;'>" + headername + "</th>";
        }

        //console.log(obj[key]);
    }
    Menustring += "<th align='center' style='padding:4px;border-right:1px solid #dddddd;border-bottom:1px solid #adadad;background-color:#144d70'>Action</th>";
    Menustring += "</tr>";

    for (var i = 0; i < data.length; i++) {
        obj = data[i];
        if (obj.ItemID > 0) {
            //Menustring += newrowhtml(obj, i);
            Menustring += "<tr>";
            for (var key in obj) {
                if (objhidden.find(x => x === key)) {

                } else {
                    //console.log(key + ":" + obj[key]);

                    if (arrFormularFields.find(y => y === key)) {
                        Menustring += `<td><span>` + obj[key] + "</span></td>";
                    } else {
                        //if (key == "saleDate") {

                        //    Menustring += `<td>` + obj[key] + "</td>";
                        //} else {
                            Menustring += `<td>` + obj[key] + "</td>";
                        //}

                    }


                }
            }
            //Menustring += "<td><span class='trash' style='font-size:12px;' onclick='deleterow(" + i + ")'>X</span></td>";
            Menustring += "</tr>";
        }
    }

    //Menustring += addnewrow(objmodel, arrFormularFields, objhidden);

    Menustring += "</table>";

    return Menustring;
}


function newrowhtml(obj,i) {
    let Menustring = "";
    Menustring += "<tr id='row" + obj['ItemID'] + "'>";
    
    // Add checkbox for row selection
   // Menustring += "<td align='center'><input type='checkbox' class='chkRow' data-itemid='" + obj['ItemID'] + "' /></td>";

    for (var key in obj) {
        if (objhidden.find(x => x === key)) {

        } else {
            //console.log(key + ":" + obj[key]);

           //if (arrFormularFields.find(y => y === key)) {
            //    Menustring += `<td><span id="txt${key}${obj['saleID']}">` + obj[key] + "</span></td>";
            //} else {
            if (key == "ImageUrl") {
                    Menustring += `<td><img src="${obj.ImageUrl}" style="height:35px;width:35px"/></td>`;
            } else if (key == "Barcode") {
                    Menustring += `<td style="display:none;"><svg class='barcode' id='barcode${obj.ItemID}' data-barcode='${obj.ItemCode}' data-itemname='${obj.ItemName.replace("'", "&apos;")}'></svg></td>`;
            } else {
                Menustring += `<td><input id="txt${key}${obj['ItemID']}" onchange="changedata(${obj.ItemID},'${key}',this)" class='inputbox' type='text' value='` + obj[key] + "' /></td>";
            }


            //}


        }
    }
    //Menustring += "<td><span class='trash' style='font-size:12px;' onclick='deleterow(" + obj['ItemID'] + ")'>X</span></td>";
    Menustring += "</tr>";

    return Menustring;
}

function addnewrow(obj, arrFormularFields, objhidden) {
    let Menustring = "";
    Menustring += "<tr id='row0'>";
        //obj = data[0];
        for (var key in obj) {
            if (objhidden.find(x => x === key)) {

            } else {
                //console.log(key + ":" + obj[key]);

                if (arrFormularFields.find(y => y === key)) {
                    Menustring += `<td><span id="txt${key}0">` + "</span></td>";
                } else {
                    if (key == "saleDate") {
                        Menustring += `<td><input style="width:80px;" id="txt${key}0" onchange="changedata(0,'${key}',this,0)" class='inputbox textbox-date' type='text' value='` + "' /></td>";
                    } else {
                        Menustring += `<td><input id="txt${key}0" onchange="changedata(0,'${key}',this,0)" class='inputbox' type='text' value='` + "' /></td>";
                    }
                    
                }


            }
        }
        Menustring += "<td></td>";
        Menustring += "</tr>";

    return Menustring;
}

function deleterow(saleid) {
    let obj = {};
    obj.SaleID = saleid;

    $.ajax({
        //cache: false,
        async: false,
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "api/deletesaledata",
        data: JSON.stringify(obj),
        success: function (data) {
            //console.log(data);  
            if (data.result == "success") {
                $("#row" + saleid).remove();

            } else {
                alert("something went wrong!")
            }
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}
function changedata(itemid,fieldname, e) {
    let fieldvalue = $(e).val();
    let obj = {};
    obj.ItemID = itemid;
    obj.FieldName = fieldname;
    obj.FieldValue = fieldvalue;
    obj.CreatedByID = 0;

    $.ajax({
        //cache: false,
        async: false,
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "ItemList.aspx/UpdateData",
        data: JSON.stringify(obj),
        success: function (data) {
            //console.log(data);  
            if (data.d == "success") {
                //loadupdatedfieldvalue(data, saleid);
               
            } else {
                search();
                alert("something went wrong!")
            }
        },
        error: function (result) {
            console.log(result);
            //$('#loading').hide();
        }
    });
}


function loadupdatedfieldvalue(data,SaleID) {
    let arrdata = JSON.parse(data.updatedFileds);
    console.log("loadupdatedfieldvalue");
    console.log(arrdata);

    let obj = arrdata;
        for (var key in obj) {
            $("#txt" + key + SaleID).html(obj[key]);
        }

    if (SaleID == 0) {

        $('#row0').remove();

        let tblhtml = $("#div_excel").html();
        tblhtml = tblhtml.replace("</table>", "");
        tblhtml += newrowhtml(data, 0);
        tblhtml += addnewrow(objmodel, arrFormularFields, objhidden);
        tblhtml += "</table>";
        $("#div_excel").html(tblhtml);
    }


}

// Loading Dialog Functions
function showLoadingDialog(message) {
    message = message || 'Processing... Please wait.';
    
    // Create loading overlay if it doesn't exist
    if ($('#loadingOverlay').length === 0) {
        const overlayHtml = `
            <div id="loadingOverlay" style="
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.7);
                z-index: 9999;
                display: flex;
                justify-content: center;
                align-items: center;
            ">
                <div style="
                    background: white;
                    padding: 30px 40px;
                    border-radius: 10px;
                    text-align: center;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.3);
                    max-width: 400px;
                ">
                    <div class="spinner" style="
                        border: 4px solid #f3f3f3;
                        border-top: 4px solid #3498db;
                        border-radius: 50%;
                        width: 50px;
                        height: 50px;
                        animation: spin 1s linear infinite;
                        margin: 0 auto 20px;
                    "></div>
                    <div id="loadingMessage" style="
                        font-size: 16px;
                        color: #333;
                        font-family: 'Koh Santepheap', Arial;
                        font-weight: bold;
                    ">${message}</div>
                </div>
            </div>
            <style>
                @keyframes spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
            </style>
        `;
        $('body').append(overlayHtml);
    } else {
        $('#loadingMessage').text(message);
        $('#loadingOverlay').show();
    }
}

function hideLoadingDialog() {
    $('#loadingOverlay').fadeOut(300);
}

function updateLoadingMessage(message) {
    $('#loadingMessage').text(message);
}