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
var selecteditemupload = {};
var displayitems = [];
var pageindex = 1;
var allitem = [];
var groupdisplayed = [];
var allitemsnotyetgroup = [];
var ismobile = false;
var lastgroup = false;

// Lazy Loading Image Observer
var lazyImageObserver = null;

// Initialize Intersection Observer for lazy loading images
function initLazyImageObserver() {
    if (lazyImageObserver) return;
    
    const options = {
        root: null, // viewport
        rootMargin: '50px', // Start loading 50px before image enters viewport
        threshold: 0.01
    };
    
    lazyImageObserver = new IntersectionObserver(function(entries, observer) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) {
                const img = entry.target;
                const src = img.getAttribute('data-src');
                
                if (src) {
                    img.src = src;
                    img.removeAttribute('data-src');
                    img.classList.remove('lazy-loading');
                    img.classList.add('lazy-loaded');
                }
                
                observer.unobserve(img);
            }
        });
    }, options);
}

// Create lazy loading image element
function createLazyImage(config) {
    const img = $('<img>', {
        id: config.id,
        class: (config.class || '') + ' lazy-loading',
        'data-src': config.src,
        src: 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"%3E%3Crect fill="%23f0f0f0" width="100" height="100"/%3E%3C/svg%3E',
        alt: config.alt || '',
        css: config.css || {}
    });
    
    // Initialize observer if not already done
    if (!lazyImageObserver) {
        initLazyImageObserver();
    }
    
    // Observe the image element after it's added to DOM
    setTimeout(function() {
        const imgElement = img[0];
        if (imgElement && lazyImageObserver) {
            lazyImageObserver.observe(imgElement);
        }
    }, 0);
    
    return img;
}

$.urlParam = function (name) {
    var results = new RegExp('[\?&]' + name + '=([^&#]*)')
        .exec(window.location.search);

    return (results !== null) ? results[1] || 0 : false;
}

function closemenu() {
    $(".menu-content").hide();
}
function showmenu() {
    $(".menu-content").show();
}
function changesearchtype(e) {
    let searchtype = $(e).val();
    getallitem(searchtype);

}

function showLoadingIndicator() {
    var loadingHtml = '<div id="imageLoadingOverlay" class="image-loading-overlay">' +
        '<div class="loading-content">' +
        '<div style="font-size: 18px; margin-bottom: 15px; color: #333; font-weight: bold;">សូមរង់ចាំ...</div>' +
        '<div class="progress-bar-container">' +
        '</div>' +
        '</div>' +
        '</div>';
    $('body').append(loadingHtml);

    // Disable main content during loading
    $('.main-content').addClass('disabled');
}


function paynow() {
    let arrobj = arritems;
    let obj = {};

    obj.JsonString = JSON.stringify(arrobj);
    obj.UserID = $("#hdUserID").val();
    

    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "UserOrder.aspx/AddOrder",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data");
            console.log(data);
            //createCookie("items", "[]", 90);
            sessionStorage.setItem('items', '[]');
            arritems = [];
            loaditemcount();
            load_itembycategory(0);
            $('#myModal3').show();
            $("#pnReceiptContainer").hide();

            //window.location = "order.aspx?history=yes";
            $("#div_orderstatus").html(`
            <div class="modal-body">
            <div style="padding: 20px;text-align: center;font-size: x-large;">
            ការបញ្ជាទិញជោគជ័យ</div>
            <div style="padding: 20px;text-align: center;font-size: x-large;">
            លេខបុង:<b>${data.d.OrderID}</b></div>
                    <div style="text-align: center;">
                    <div class="btn-option" onclick="gohome()" style="width: 50%;float: none;margin: auto;padding: 5% 2%;">ទំព័រដើម</div></div></div>`);

        },
        error: function (result) {
            $('#loading').hide();
        }
    });
}

function paynowold() {
    console.log("paynow()");
    let discountrate = $("#txtPercentageDiscount").val().replace(/,/g, '');
    let DiscountPay = $("#txtDiscountPay").val().replace(/,/g, '');
    if (DiscountPay == "") {
        DiscountPay = 0;
    }
    let DiscountPayReil = $("#txtDiscountPayReil").val().replace(/,/g, '');
    let GrandTotalToPay = $("#txtGrandTotalToPay").val().replace(/,/g, '');
    let GrandTotalToPayR = $("#txtGrandTotalToPayReil").val().replace(/,/g, '');
    let PayDollar = $("#txtPayDollar").val().replace(/,/g, '');
    let PayReil = $("#txtPayReil").val().replace(/,/g, '');
    let ReturnDollar = $("#txtReturnDollar").val().replace(/,/g, '');
    let ReturnReil = $("#txtReturnReil").val().replace(/,/g, '');
    let GetPoint = $("#txtGetPoint").val().replace(/,/g, '');
    let PaymentMothod = "";

    let isCheckedBank = $('#rdoBank').is(':checked');
    if (isCheckedBank) {
        // Checkbox is checked
        PaymentMothod = "Bank"
    } else {
        // Checkbox is not checked
        PaymentMothod = "Cash"
    }

    if ($.isNumeric(discountrate) == false) {
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
    showLoadingIndicator();
    $.ajax({
        cache: false,
        async: true,
        type: "post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/PayNow",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
            $('#imageLoadingOverlay').fadeOut(500, function () {
                $(this).remove();
            });
        },
        success: function (data) {
            let groupingitem = groupitems(allitemsnotyetgroup);
            allitem = groupingitem;
            load_itembycategory(0);
            //$("#div_progress").hide();
            //$("#btnPayNow").show();
            sessionStorage.setItem('items', '[]');
            loadordertemp();
            console.log(data);
            let printurl = "PrintHtml.aspx?print=1&InvoiceNo=" + data.d.InvoiceNo + "&user=" + UserID + "&backurl=userorder.aspx";
            //window.location = printurl;
            $("#iframe").attr("src", printurl);
            $("#div_print").show();
            $("#pnReceiptContainer").hide();
            $('[id^="countitem"]').text("0").hide();

            
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

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
    goto_order();

    getallitems();
    
    
    //getallphone();

    let windowsize = $(document).width();
    let windownheight = $(window).height(); 
    //alert(windowsize);
    $("#div_searchtype_mobile").html("");
    $("#div_searchtype_desktop").html("");

    $("#pnReceiptContainer").hide();
    if (windowsize >= 800) {
        $("#div_Category_left").css("height", (windownheight - 100) + "px");
        ismobile = false;
        getallcategory("desktop");
        $("#div_group_desktop").show();
        $("#div_top_sticky").hide();

        $("#div_top_sticky_left").show();
        $("#div_searchtype_desktop").html(` <select id="selectsearchtype" class="form-control input-sm" onchange="changesearchtype()">
                        <option value="Search">ស្វែងរក</option>
                        <option value="Barcode">ស្កេនបាកូដ</option>
                    </select>`)

    } else {
        ismobile = true;
        getallcategory("mobile");
        $("#div_group_desktop").hide();
        $("#div_top_sticky").show();
        $("#div_top_sticky_left").hide();
        $("#div_searchtype_mobile").html(` <select id="selectsearchtype" class="form-control input-sm" onchange="changesearchtype()">
                        <option value="Search">ស្វែងរក</option>
                        <option value="Barcode">ស្កេនបាកូដ</option>
                    </select>`)
    }

    // 1. Target the actual scrollable element, not the window
    window.addEventListener('scroll', () => {
        // 1. Get the current scroll position, the total height, and the viewport height
        const scrollTop = window.scrollY || document.documentElement.scrollTop;
        const scrollHeight = document.documentElement.scrollHeight;
        const clientHeight = window.innerHeight;

        // 2. Check if user is within 10px of the absolute bottom
        if (scrollHeight - scrollTop - clientHeight <= 10) {
            
            // 3. Logic to check if more items exist
            //if (pageindex * 50 < displayitems.length) {
            console.log("Reached global bottom - Loading more data...");
            let itemcode = $(".textbox-barcode").val();
            if (itemcode == "") {
                pageindex += 1;
                showitem(displayitems, pageindex);
            }
                
            //}
        }
    }, { passive: true });






    $('#myModal').on('shown.bs.modal', function () {
        $(document).off('focusin.modal');
    });
    $('#myModal2').on('shown.bs.modal', function () {
        $(document).off('focusin.modal');
    });

});




function select_item_order(SaleID, ItemID, ItemName, ItemCode, ImageUrl, ItemPrice, Currency) {
    let userid = $("#hdUserID").val();

    selectaddoneitem(SaleID, ItemID, ItemName, ItemCode, ImageUrl, ItemPrice, Currency);

}
function selectaddoneitem(SaleID, ItemID, ItemName, ItemCode, ImageUrl, ItemPrice, Currency) {
    
    selecteditem = {};
    selecteditem.ID = SaleID;
    selecteditem.ItemID = ItemID;
    selecteditem.ItemName = ItemName;
    selecteditem.ItemCode = ItemCode;
    selecteditem.ImageUrl = ImageUrl;
    selecteditem.Price = ItemPrice;
    selecteditem.Currency = Currency;
    selecteditem.Qty = 1;
    //selecteditem.unit = getitemunit(ItemID);
    console.log(selecteditem);

    $("#countitem" + ItemID).show();
    let datafilter = arritems.filter(function (el) {
        return el.ItemID == ItemID;
    });
    if (datafilter.length > 0) {
        console.log("datafilter");
        console.log(datafilter);
        let objnew = jQuery.extend(true, {}, datafilter[0]);

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

    }
}




function getitemunit(itemid) {
    let lst;
    $.ajax({
        cache: false,
        async: false,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorderv2.aspx/getitemunit",
        data: `{"ItemID":${itemid}}`,
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            lst = data.d;
        },
        error: function (result) {
            
        }

    });

    return lst;
}

function getallcategory(devidetype) {
    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorderv2.aspx/getcategory",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            let lstcat = data.d;
            let html_catleft = "";
            let html = "";
            if (devidetype == "mobile") {
                html += '<ul class="menu_menu__GXosk" style="transform: translateZ(0px);">'
                html += '<li class="menu_active__SrfmU" id="li_showsearch"><a onclick="showsearch()"><i class="fa fa-search" style="font-size: 20px;color: white;"></i></a></li>'
                html += '<li class="menu_active__SrfmU"><a id="a_allcat" onclick="view_itembbycategory(this,0)">All</a></li>'
            } else {
                html_catleft += '<ul class="menu_categoryleft" style="transform: translateZ(0px);">'
                html_catleft += '<li class="menu_active__SrfmU"><a  id="a_allcat" onclick="view_itembbycategory(this,0)">All</a></li>'
            }


            for (var i = 0; i < lstcat.length; i++) {
                let categoryName = lstcat[i].CategoryName;
                let categoryId = lstcat[i].CategoryID;
                if (categoryName.length > 20) {
                    //categoryName = categoryName.substring(0, 19) + '...';
                }

             
                html += `<li><a onclick="view_itembbycategory(this,${categoryId})">${categoryName}</a></li>`
                html_catleft += `<li><a onclick="view_itembbycategory(this,${categoryId})">${categoryName}</a></li>`
               
            }
            html += "</ul>"

            $("#div_Category").html(html);
            $("#div_Category_left").html(html_catleft);


            category_html = html;
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


function getallitem(searchtype) {

    $.ajax({
        cache: false,
        async: true,
        type: "Post",
        contentType: "application/json; charset=utf-8",
        url: "userorder.aspx/getItemList",
        data: "",
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("getItemList");
            console.log(data);

   
         
            //$("#txtsearchleft").autocomplete({
            //    source: function (request, response) {
            //        // Use $.map to transform ClItem properties to label/value
            //        var formattedData = $.map(data.d, function (item) {
            //            return {
            //                label: item.ItemCode + " - " + item.ItemName, // Shown in the dropdown list
            //                value: item.ItemCode  // Placed in textbox on select
            //            };
            //        });

            //        // Filter the results locally based on what the user typed
            //        var term = $.ui.autocomplete.escapeRegex(request.term);
            //        var matcher = new RegExp(term, "i");
            //        response($.grep(formattedData, function (item) {
            //            return matcher.test(item.label) || matcher.test(item.value);
            //        }));
            //    }
            //});


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
        let totalToPay = parseFloat($("#txtTotalToPay").val().replace(/,/g, ''));
        console.log("totaltoPay:" + totalToPay);
        let totalToPayR = parseFloat($("#txtTotalToPayR").val().replace(/,/g, ''));
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
    let GTotal = $("#txtGrandTotalToPay").val().replace(/,/g, '');
    let GTotalR = $("#txtGrandTotalToPayReil").val().replace(/,/g, '');
    let PayDollar = $("#txtPayDollar").val().replace(/,/g, '');
    let PayReil = $("#txtPayReil").val().replace(/,/g, '');


    

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
    let items = sessionStorage.getItem('items');
    console.log("jsonitems");
    console.log(items);
    if (items == null) {
        items = "[]";
    }
    let data = JSON.parse(items);
    arritems = [];
    for (var i = 0; i < data.length; i++) {
        let obj = {}
        obj.SaleID = data[i].SaleID;
        obj.ItemID = data[i].ItemID;
        obj.ItemName = data[i].ItemName;
        obj.ItemCode = data[i].ItemCode;
        obj.ImageUrl = data[i].ImageUrl;
        obj.Price = data[i].Price;
        obj.Currency = data[i].Currency;
        obj.Qty = data[i].Qty;
        obj.FromQty = data[i].FromQty;
        obj.ToQty = data[i].ToQty;
        obj.unit = data[i].unit;
        arritems.push(obj);
    }
    loadinvoiceitem(arritems);
    loaditemcount();
}


function messagebox(title, detail) {
    //$("#dialog").attr("title", title);
    //$("#dialogdetail").html(detail);
    //$("#dialog").dialog();
    $('#myModal3').modal('show');
}

function loadinvoiceitem(arrobj) {
    let windowsize = $(document).width();
    if (windowsize <= 800) {
        loadinvoiceitem_mobile(arrobj);
    } else {
        loadinvoiceitem_desktop(arrobj);
    }
}

function getGTotal(arrItems) {
    let GTotal = 0;
    for (var i = 0; i < arrItems.length; i++) {
        let obj = arrItems[i];

        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Total = ItemPrice * Qty;
        GTotal += Total;
    }
    return GTotal;

}

function changeunitprice(itemid,e) {
    let selectedvalue = $(e).val();
    let arr = selectedvalue.split(",");
    let SaleID = arr[0];
    let Price = arr[1];
    unitchanged(itemid, SaleID);
    pricechanged(itemid, Price);
    $(`#txtitemprice${itemid}`).val(Price);

}

function loaditem_selected(oitem) {
    let windowsize = $(document).width();
    if (windowsize >= 800) {
        loaditem_selected_desktop(oitem);
    } else {
        loaditem_selected_mobile(oitem);
    }

}

function loaditem_selected_desktop(oitem) {

    $("#div_order_command").html(`
                                        <div style="text-align:center;">
                                            <table style="width:100%">
                                            <tr>
                                            <td>
                                            <div class="main_button-md" style="width:120px;" onclick="closereceipt()">កំណត់                                              
                                            </div>
                                            </td>
                                            </tr></table>
                                        </div>
`);


    selectaddoneitem(oitem.ID, oitem.ItemID, oitem.ItemName, oitem.ItemCode, oitem.ImageUrl, oitem.Price, oitem.Currency)


    //let html = "";
    $("#div_invoiceitemlist").empty();
    // 1. Create the outer table
    const outerTable = document.createElement('table');
    outerTable.setAttribute('width', '100%');

    // 2. Create the outer row and cell
    const outerRow = outerTable.insertRow();
    const outerCell = outerRow.insertCell();
    outerCell.style.cssText = 'padding: 5px; font-size: large; font-weight: bold;';

    // 3. Create the inner table
    const innerTableheader = document.createElement('table');
    innerTableheader.style.width = '100%';

    // 4. Create the inner row
    const innerRowheader = innerTableheader.insertRow();

    // --- Left Inner Cell (Title) ---
    const titleCell = innerRowheader.insertCell();
    const titleDiv = document.createElement('div');
    titleDiv.textContent = "ទំនិញ";
    titleCell.appendChild(titleDiv);

    // --- Right Inner Cell (Empty/Placeholder) ---
    const actionCell = innerRowheader.insertCell();
    actionCell.style.textAlign = 'right';

    // 5. Assemble the structure
    outerCell.appendChild(innerTableheader);


    // 1. Create the main list container
    const ul = document.createElement('ul');
    ul.className = 'ul-itemlist';

    // 2. Create the list item
    const li = document.createElement('li');

    // 3. Create the table
    const table = document.createElement('table');
    table.className = 'mytable';
    table.style.cssText = 'width: 100%; border-collapse: collapse;';

    // 4. Create the header row
    const headerRow = document.createElement('tr');
    //headerRow.className = 'item-row';


    li.appendChild(table);

    ul.appendChild(li);
    // To add to your page:
    // document.getElementById('container').appendChild(ul);


    let GTotal = 0;
    let GTotalR = 0;

    //console.log(arritems);
    let arrobj = arritems.filter(function (el) {
        return el.ItemID == oitem.ItemID;
    });
    //console.log(obj);
    let obj = arrobj[0];
    //console.log(obj);
    let ItemID = obj.ItemID;
    let ItemName = obj.ItemName;
    let Qty = obj.Qty;
    let ItemPrice = obj.Price;
    let Discount = 0;
    let Total = ItemPrice * Qty;
    let ItemCode = obj.ItemCode;
    let Currency = obj.Currency;
    let ImageUrl = obj.ImageUrl;
    if (Currency == "R") {
        GTotalR += Total;
    } else {
        GTotal += Total;
    }


    /////////////////
    // 1. Create the main list item container
    /////////////////
    // 1. Create the main list item container
    let li_item = document.createElement('li');


    // 2. Create the main table
    const mainTable = document.createElement('table');
    mainTable.style.width = '100%';

    const row = mainTable.insertRow();
    row.style.cssText = 'border-width:0px; border-style:None; height:25px;';

    // --- Cell 1: Image and Name ---
    const cellInfo = row.insertCell();
    cellInfo.style.width = '50%';

    const innerTable = document.createElement('table');
    const innerRow = innerTable.insertRow();

    // Image
    const imgCell = innerRow.insertCell();
    const img = document.createElement('img');
    img.src = "https://sl-pos.sldalin.org/" + ImageUrl;
    img.style.cssText = 'height:55px; width:55px;';
    imgCell.appendChild(img);

    // Name and Code
    const nameCell = innerRow.insertCell();
    nameCell.textContent = `${ItemName} (${ItemCode})`;
    cellInfo.appendChild(innerTable);

    // --- Cell 2: Quantity Controls ---
    const cellQty = row.insertCell();
    cellQty.style.fontSize = '10pt';
    cellQty.style.width = '15%';
    cellQty.style.textAlign = "center";

    const qtyContainer = document.createElement('div');
    qtyContainer.className = 'main_quantity-container';

    // Subtract Button
    const btnSub = document.createElement('span');
    btnSub.addEventListener('click', () => {
        console.log("Button clicked!"); // Debugging
        select_qty(obj, -1);
    });
    const subImg = document.createElement('img');
    subImg.src = 'images/logo/subtract-color-icon.png';
    subImg.style.width = '40px';
    btnSub.appendChild(subImg);

    // Input Field
    const qtyInput = document.createElement('input');
    qtyInput.id = `txtitemqty${ItemID}`;
    qtyInput.type = 'text';
    qtyInput.className = 'main_input_qty';
    qtyInput.value = Qty;
    qtyInput.addEventListener('change', () => {
        console.log("qtyInput");
        console.log($(qtyInput).val());
        let NewQTY = $(qtyInput).val();
        change_amount(obj, NewQTY)
    });

    // Add Button
    const btnAdd = document.createElement('span');
    btnAdd.addEventListener('click', () => {
        console.log("Button clicked!"); // Debugging
        select_qty(obj, 1);
    });
    const addImg = document.createElement('img');
    addImg.src = 'images/logo/addition-color-icon.png';
    addImg.style.width = '40px';
    btnAdd.appendChild(addImg);

    qtyContainer.append(btnSub, qtyInput, btnAdd);

    // Display current Qty
    const qtyDisplay = document.createElement('div');
    qtyDisplay.style.cssText = 'min-width: 25px; height: 20px; float: left; font-size: large; text-align: center;';
    qtyDisplay.textContent = Qty.toLocaleString();

    cellQty.append(qtyContainer);

    // --- Cell 3: Price ---
    let objitemunits = obj.unit;
    let optionhtml = "";
    objitemunits.forEach(itemunit => {
        console.log("matched " + obj.SaleID + "," + itemunit.ID)
        if (obj.SaleID == itemunit.ID) {

            optionhtml += `<option value='${itemunit.ID},${itemunit.Price}' Selected>${itemunit.FromQty}-${itemunit.ToQty}</option>`
        } else {
            optionhtml += `<option value='${itemunit.ID},${itemunit.Price}'>${itemunit.FromQty}-${itemunit.ToQty}</option>`
        }

    });

    const cellPrice = row.insertCell();
    cellPrice.style.fontSize = '10pt';
    //cellPrice.textContent = ItemPrice.toLocaleString() + Currency;
    cellPrice.innerHTML = `<div style='padding:5px;'>
                                <table>
                                
                                <tr><td>
                                    <table><tr>
                                    <td>
                                        <span id="txtitemprice${ItemID}" type="text" class="dropdown-item-unit" style="width: 50px; height: 20px;">${ItemPrice}</span>
                                    </td>
                                    <td>
                                    $
                                    </td>
                                    </tr>
                                    </table>
                                 
                                </td>
                                <td>
                                    <table><tr><td></td>                                     
                                    <td><select style="display:none;" id="dplitemunit${ItemID}" class="dropdown-item-unit" onchange="changeunitprice(${ItemID},this)">${optionhtml}</select>
                                    </td></tr></table>
                                </td>                               
                                </tr></table>
                                </div>`

    cellPrice.style.width = '15%';
    cellPrice.style.textAlign = "center";

    // --- Cell 4: Total ---
    const cellTotal = row.insertCell();
    //cellTotal.textContent = Total.toLocaleString() + Currency;
    cellTotal.innerHTML = `សរុប: <span id='total${ItemID}'>${Total.toLocaleString()} ${Currency}`;
    cellTotal.style.width = '15%';
    cellTotal.style.textAlign = "center";
    // --- Cell 5: Edit Icon ---
    const cellEdit = row.insertCell();
    const editDiv = document.createElement('div');
    editDiv.style.textAlign = 'center';


    const editIcon = document.createElement('i');
    editIcon.className = 'fa fa-trash';
    editIcon.style.cssText = 'font-size: x-large; color:#f23; cursor:pointer;';
    editIcon.addEventListener('click', () => {
        deleteitem(ItemID);

    });



    editDiv.appendChild(editIcon);
    cellEdit.appendChild(editDiv);

    // 3. Final Assembly
    li_item.appendChild(mainTable);


    ul.appendChild(li_item);





    $("#div_invoiceitemlist").append(outerTable);
    $("#div_invoiceitemlist").append(ul);
    $("#pnReceiptContainer").show();
    $("#div_invoiceitemlist").show();
    $("#div_checkout").hide();


}


function loaditem_selected_mobile(oitem) {
   
    $("#div_order_command").html(`
                                        <div style="text-align:center;">
                                            <table style="width:100%">
                                            <tr>
                                            <td>
                                            <div class="main_button-md" style="width:120px;" onclick="closereceipt()">កំណត់                                              
                                            </div>
                                            </td>
                                            </tr></table>
                                        </div>
`);

    
    selectaddoneitem(oitem.ID, oitem.ItemID, oitem.ItemName, oitem.ItemCode, oitem.ImageUrl, oitem.Price, oitem.Currency)


    //let html = "";
    $("#div_invoiceitemlist").empty();
    // 1. Create the outer table
    const outerTable = document.createElement('table');
    outerTable.setAttribute('width', '100%');

    // 2. Create the outer row and cell
    const outerRow = outerTable.insertRow();
    const outerCell = outerRow.insertCell();
    outerCell.style.cssText = 'padding: 5px; font-size: large; font-weight: bold;';

    // 3. Create the inner table
    const innerTableheader = document.createElement('table');
    innerTableheader.style.width = '100%';

    // 4. Create the inner row
    const innerRowheader = innerTableheader.insertRow();

    // --- Left Inner Cell (Title) ---
    const titleCell = innerRowheader.insertCell();
    const titleDiv = document.createElement('div');
    titleDiv.textContent = "ទំនិញ";
    titleCell.appendChild(titleDiv);

    // --- Right Inner Cell (Empty/Placeholder) ---
    const actionCell = innerRowheader.insertCell();
    actionCell.style.textAlign = 'right';

    // 5. Assemble the structure
    outerCell.appendChild(innerTableheader);


    // 1. Create the main list container
    const ul = document.createElement('ul');
    ul.className = 'ul-itemlist';

    // 2. Create the list item
    const li = document.createElement('li');

    // 3. Create the table
    const table = document.createElement('table');
    table.className = 'mytable';
    table.style.cssText = 'width: 100%; border-collapse: collapse;';

    // 4. Create the header row
    const headerRow = document.createElement('tr');
    //headerRow.className = 'item-row';


    li.appendChild(table);

    ul.appendChild(li);
    // To add to your page:
    // document.getElementById('container').appendChild(ul);


    let GTotal = 0;
    let GTotalR = 0;

    //console.log(arritems);
    let arrobj = arritems.filter(function (el) {
        return el.ItemID == oitem.ItemID;
    });
    //console.log(obj);
    let obj = arrobj[0];
        //console.log(obj);
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
    let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;
        let ImageUrl = obj.ImageUrl;
        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }


        /////////////////
        // 1. Create the main list item container
        let li_item = document.createElement('li');


        // 2. Create the main table
        const mainTable = document.createElement('table');
        mainTable.style.width = '100%';

        const row = mainTable.insertRow();
        row.style.cssText = 'border-width:0px; border-style:None; height:25px;';

        // --- Cell 2: Quantity Controls ---
        const cellQty = row.insertCell();
        cellQty.style.fontSize = '10pt';
        cellQty.style.width = '50%';
        cellQty.style.textAlign = "left";

        const qtyContainer = document.createElement('div');
        qtyContainer.className = 'main_quantity-container';

        // Subtract Button
        const btnSub = document.createElement('span');
        btnSub.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, -1);
        });
        const subImg = document.createElement('img');
        subImg.src = 'images/logo/subtract-color-icon.png';
        subImg.style.width = '30px';
        btnSub.appendChild(subImg);

        // Input Field
        const qtyInput = document.createElement('input');
        qtyInput.id = `txtitemqty${ItemID}`;
        qtyInput.type = 'text';
        qtyInput.style.width = '30px';
        qtyInput.style.height = '20px';
        qtyInput.className = 'main_input_qty';
        qtyInput.value = Qty;
        qtyInput.addEventListener('change', () => {
            console.log("qtyInput");
            console.log($(qtyInput).val());
            let NewQTY = $(qtyInput).val();
            change_amount(obj, NewQTY)
        });

        // Add Button
        const btnAdd = document.createElement('span');
        btnAdd.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, 1);
        });
        const addImg = document.createElement('img');
        addImg.src = 'images/logo/addition-color-icon.png';
        addImg.style.width = '30px';
        btnAdd.appendChild(addImg);

        qtyContainer.append(btnSub, qtyInput, btnAdd);

        // Display current Qty
        const qtyDisplay = document.createElement('div');
        qtyDisplay.style.cssText = 'min-width: 25px; height: 20px; float: left; font-size: large; text-align: center;';
        qtyDisplay.textContent = Qty.toLocaleString();

        cellQty.append(qtyContainer);

        // --- Cell 4: Total ---
        const cellTotal = row.insertCell();
        cellTotal.innerHTML = `សរុប: <span id='total${ItemID}'>${Total.toLocaleString()} ${Currency}`;
        cellTotal.colSpan = 2;
        //cellTotal.style.width = '15%';
        cellTotal.style.textAlign = "right";


        //Row 2
        const row2 = mainTable.insertRow();
        row2.style.cssText = 'border-width:0px; border-style:None; height:25px;';


        // --- Cell 1: Image and Name ---
        const cellInfo = row2.insertCell();
        cellInfo.colSpan = 3;
        cellInfo.style.width = '100%';

        const innerTable = document.createElement('table');
    const innerRow = innerTable.insertRow();

        // Image
        const imgCell = innerRow.insertCell();
        const img = document.createElement('img');
        img.src = "https://sl-pos.sldalin.org/" + ImageUrl;
        img.style.cssText = 'height:55px; width:55px;';
        imgCell.appendChild(img);

    // Name and Code

    let objitemunits = obj.unit; //getitemunit(ItemID);
    //console.log(objitemunits);
        let optionhtml = "";
        objitemunits.forEach(itemunit => {
            //console.log("matched " + obj.SaleID + "," + itemunit.ID)
            if (obj.SaleID == itemunit.ID) {

                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}' Selected>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            } else {
                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}'>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            }

        });

        const nameCell = innerRow.insertCell();
        //nameCell.textContent = `${ItemName} (${ItemCode})`;
        nameCell.innerHTML = `<div style='padding:5px;'>${ItemName} (${ItemCode})</div>
                                <div style='padding:5px;'>
                                <table>
                                
                                <tr><td>
                                    <table><tr><td>តម្លៃ:</td>
                                    <td>
                                        <span id="txtitemprice${ItemID}" type="text" class="dropdown-item-unit" style="width: 50px; height: 20px;">${ItemPrice}</span>
                                    </td>
                                    <td>
                                    $
                                    </td>
                                    </tr>
                                    </table>
                                 
                                </td>
                                <td>
                                    <table><tr><td></td>
                                     
                                    <td><select style="display:none;" id="dplitemunit${ItemID}" class="dropdown-item-unit" onchange="changeunitprice(${ItemID},this)">${optionhtml}</select>
                                    </td></tr></table>
                                </td>
                                <td>
                                <i onclick="deleteitem(${ItemID})" class="fa fa-trash" style="font-size: x-large; color: rgb(255, 34, 51); cursor: pointer;"></i>
                                </td>
                                </tr></table>
                                </div>`



        cellInfo.appendChild(innerTable);

        // 3. Final Assembly
        li_item.appendChild(mainTable);


        ul.appendChild(li_item);

    

    
    $("#div_invoiceitemlist").append(outerTable);
    $("#div_invoiceitemlist").append(ul);
    $("#pnReceiptContainer").show();
    $("#div_invoiceitemlist").show();
    $("#div_checkout").hide();
    
    
}

function loadinvoiceitem_mobile(arrobj) {
    //let html = "";
    $("#div_invoiceitemlist").empty();
    // 1. Create the outer table
    const outerTable = document.createElement('table');
    outerTable.setAttribute('width', '100%');

    // 2. Create the outer row and cell
    const outerRow = outerTable.insertRow();
    const outerCell = outerRow.insertCell();
    outerCell.style.cssText = 'padding: 5px; font-size: large; font-weight: bold;';

    // 3. Create the inner table
    const innerTable = document.createElement('table');
    innerTable.style.width = '100%';

    // 4. Create the inner row
    const innerRow = innerTable.insertRow();

    // --- Left Inner Cell (Title) ---
    const titleCell = innerRow.insertCell();
    const titleDiv = document.createElement('div');
    titleDiv.textContent = 'បញ្ជីទំនិញ';
    titleCell.appendChild(titleDiv);

    // --- Right Inner Cell (Empty/Placeholder) ---
    const actionCell = innerRow.insertCell();
    actionCell.style.textAlign = 'right';

    // 5. Assemble the structure
    outerCell.appendChild(innerTable);


    // 1. Create the main list container
    const ul = document.createElement('ul');
    ul.className = 'ul-itemlist';

    // 2. Create the list item
    const li = document.createElement('li');

    // 3. Create the table
    const table = document.createElement('table');
    table.className = 'mytable';
    table.style.cssText = 'width: 100%; border-collapse: collapse;';

    // 4. Create the header row
    const headerRow = document.createElement('tr');
    //headerRow.className = 'item-row';


    li.appendChild(table);

    ul.appendChild(li);
    // To add to your page:
    // document.getElementById('container').appendChild(ul);


    let GTotal = 0;
    let GTotalR = 0;
    for (var i = 0; i < arrobj.length; i++) {
        let obj = arrobj[i];
        //console.log(obj);
        let ItemID = obj.ItemID;
        let ItemName = obj.ItemName;
        let Qty = obj.Qty;
        let ItemPrice = obj.Price;
        let Discount = 0;
        let Total = ItemPrice * Qty;
        let ItemCode = obj.ItemCode;
        let Currency = obj.Currency;
        let ImageUrl = obj.ImageUrl;
        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }


        /////////////////
        // 1. Create the main list item container
        let li_item = document.createElement('li');


        // 2. Create the main table
        const mainTable = document.createElement('table');
        mainTable.style.width = '100%';

        const row = mainTable.insertRow();
        row.style.cssText = 'border-width:0px; border-style:None; height:25px;';

        // --- Cell 2: Quantity Controls ---
        const cellQty = row.insertCell();
        cellQty.style.fontSize = '10pt';
        cellQty.style.width = '50%';
        cellQty.style.textAlign = "left";

        const qtyContainer = document.createElement('div');
        qtyContainer.className = 'main_quantity-container';

        // Subtract Button
        const btnSub = document.createElement('span');
        btnSub.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, -1);
        });
        const subImg = document.createElement('img');
        subImg.src = 'images/logo/subtract-color-icon.png';
        subImg.style.width = '30px';
        btnSub.appendChild(subImg);

        // Input Field
        const qtyInput = document.createElement('input');
        qtyInput.id = `txtitemqty${ItemID}`;
        qtyInput.type = 'text';
        qtyInput.style.width = '30px';
        qtyInput.style.height = '20px';
        qtyInput.className = 'main_input_qty';
        qtyInput.value = Qty;
        qtyInput.addEventListener('change', () => {
            console.log("qtyInput");
            console.log($(qtyInput).val());
            let NewQTY = $(qtyInput).val();
            change_amount(obj, NewQTY)
        });

        // Add Button
        const btnAdd = document.createElement('span');
        btnAdd.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, 1);
        });
        const addImg = document.createElement('img');
        addImg.src = 'images/logo/addition-color-icon.png';
        addImg.style.width = '30px';
        btnAdd.appendChild(addImg);

        qtyContainer.append(btnSub, qtyInput, btnAdd);

        // Display current Qty
        const qtyDisplay = document.createElement('div');
        qtyDisplay.style.cssText = 'min-width: 25px; height: 20px; float: left; font-size: large; text-align: center;';
        qtyDisplay.textContent = Qty.toLocaleString();

        cellQty.append(qtyContainer);

        // --- Cell 4: Total ---
        const cellTotal = row.insertCell();
        cellTotal.innerHTML = `សរុប: <span id='total${ItemID}'>${Total.toLocaleString()} ${Currency}`;
        cellTotal.colSpan = 2;
        //cellTotal.style.width = '15%';
        cellTotal.style.textAlign = "right";

       
        //Row 2
        const row2 = mainTable.insertRow();
        row2.style.cssText = 'border-width:0px; border-style:None; height:25px;';


        // --- Cell 1: Image and Name ---
        const cellInfo = row2.insertCell();
        cellInfo.colSpan = 3;
        cellInfo.style.width = '100%';

        const innerTable = document.createElement('table');
        const innerRow = innerTable.insertRow();

        // Image
        const imgCell = innerRow.insertCell();
        const img = document.createElement('img');
        img.src = "https://sl-pos.sldalin.org/" + ImageUrl;
        img.style.cssText = 'height:55px; width:55px;';
        imgCell.appendChild(img);

        // Name and Code
        console.log(obj);
        let objitemunits = obj.unit;
        let optionhtml = "";
        objitemunits.forEach(itemunit => {
            console.log("matched " + obj.SaleID + "," + itemunit.ID)
            if (obj.SaleID == itemunit.ID) {
                
                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}' Selected>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            } else {
                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}'>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            }
            
        });

        const nameCell = innerRow.insertCell();
        //nameCell.textContent = `${ItemName} (${ItemCode})`;
        nameCell.innerHTML = `<div style='padding:5px;'>${ItemName} (${ItemCode})</div>
                                <div style='padding:5px;'>
                                <table>
                                
                                <tr><td>
                                    <table><tr><td>តម្លៃ:</td>
                                    <td>
                                        <span id="txtitemprice${ItemID}" type="text" class="dropdown-item-unit" style="width: 50px; height: 20px;">${ItemPrice}</span>
                                    </td>
                                    <td>
                                    $
                                    </td>
                                    </tr>
                                    </table>
                                 
                                </td>
                                <td>
                                    <table><tr><td></td>
                                     
                                    <td><select style="display:none;" id="dplitemunit${ItemID}" class="dropdown-item-unit" onchange="changeunitprice(${ItemID},this)">${optionhtml}</select>
                                    </td></tr></table>
                                </td>
                                <td>
                                <i onclick="deleteitem(${ItemID})" class="fa fa-trash" style="font-size: x-large; color: rgb(255, 34, 51); cursor: pointer;"></i>
                                </td>
                                </tr></table>
                                </div>`


        
        cellInfo.appendChild(innerTable);

        

        

        // --- Cell 5: Edit Icon ---
        //const cellEdit = row2.insertCell();
        //const editDiv = document.createElement('div');
        //editDiv.style.textAlign = 'right';


        //const editIcon = document.createElement('i');
        //editIcon.className = 'fa fa-trash';
        //editIcon.style.cssText = 'font-size: x-large; color:#f23; cursor:pointer;';
        //editIcon.addEventListener('click', () => {
        //    deleteitem(ItemID);

        //});
        //editDiv.appendChild(editIcon);
        //cellEdit.appendChild(editDiv);


       

        // 3. Final Assembly
        li_item.appendChild(mainTable);


        ul.appendChild(li_item);

    }

    //html += "</ul>";

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

    datestring = dd + "/"
        + mm + "/"
        + yyyy + " "
        + hh + ":"
        + minutes;

    $("#div_invoiceitemlist").append(outerTable);
    $("#div_invoiceitemlist").append(ul);
    $("#lblReceiptDate").html(datestring);

    // 5. Define header configurations
    // This array maps text to optional widths for cleaner code
    const headers = [


        { text: `សរុប = $ <span id='spanGTotal'>${GTotal.toLocaleString()}</span>`, width: '100%', textAlign:'right', id: 'lblTotalOrder' }
    ];

    // 6. Build and append the headers
    headers.forEach(headerData => {
        const th = document.createElement('th');
        th.scope = 'col';
        th.innerHTML = headerData.text; // \u00A0 is the unicode for &nbsp;
        if (headerData.width) {
            th.style.width = headerData.width;
            th.style.textAlign = "center";
        }
        headerRow.appendChild(th);
    });

    // 7. Assemble the components
    table.appendChild(headerRow);

    $("#lblTotalOrder").text("$" + GTotal.toLocaleString());
    $("#lblSubTotalR").html("R" + GTotalR.toLocaleString());
    $("#lblGrandTotal").html("$" + GTotal.toLocaleString());
}
function closeIframe() {
    $("#div_print").hide();
}
function loadinvoiceitem_desktop(arrobj) {
    //let html = "";
    $("#div_invoiceitemlist").empty();
    // 1. Create the outer table
    const outerTable = document.createElement('table');
    outerTable.setAttribute('width', '100%');

    // 2. Create the outer row and cell
    const outerRow = outerTable.insertRow();
    const outerCell = outerRow.insertCell();
    outerCell.style.cssText = 'padding: 5px; font-size: large; font-weight: bold;';

    // 3. Create the inner table
    const innerTable = document.createElement('table');
    innerTable.style.width = '100%';

    // 4. Create the inner row
    const innerRow = innerTable.insertRow();

    // --- Left Inner Cell (Title) ---
    const titleCell = innerRow.insertCell();
    const titleDiv = document.createElement('div');
    titleDiv.textContent = 'បញ្ជីទំនិញ';
    titleCell.appendChild(titleDiv);

    // --- Right Inner Cell (Empty/Placeholder) ---
    const actionCell = innerRow.insertCell();
    actionCell.style.textAlign = 'right';

    // 5. Assemble the structure
    outerCell.appendChild(innerTable);

    
    // 1. Create the main list container
    const ul = document.createElement('ul');
    ul.className = 'ul-itemlist';

    // 2. Create the list item
    const li = document.createElement('li');

    // 3. Create the table
    const table = document.createElement('table');
    table.className = 'mytable';
    table.style.cssText = 'width: 100%; border-collapse: collapse;';

    // 4. Create the header row
    const headerRow = document.createElement('tr');
    //headerRow.className = 'item-row';

    
    li.appendChild(table);

    ul.appendChild(li);
    // To add to your page:
    // document.getElementById('container').appendChild(ul);


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
        let ImageUrl = obj.ImageUrl;
        if (Currency == "R") {
            GTotalR += Total;
        } else {
            GTotal += Total;
        }
       

        /////////////////
        // 1. Create the main list item container
        let li_item = document.createElement('li');
        

        // 2. Create the main table
        const mainTable = document.createElement('table');
        mainTable.style.width = '100%';

        const row = mainTable.insertRow();
        row.style.cssText = 'border-width:0px; border-style:None; height:25px;';

        // --- Cell 1: Image and Name ---
        const cellInfo = row.insertCell();
        cellInfo.style.width = '50%';

        const innerTable = document.createElement('table');
        const innerRow = innerTable.insertRow();

        // Image
        const imgCell = innerRow.insertCell();
        const img = document.createElement('img');
        img.src = "https://sl-pos.sldalin.org/" + ImageUrl;
        img.style.cssText = 'height:55px; width:55px;';
        imgCell.appendChild(img);

        // Name and Code
        const nameCell = innerRow.insertCell();
        nameCell.textContent = `${ItemName} (${ItemCode})`;
        cellInfo.appendChild(innerTable);

        // --- Cell 2: Quantity Controls ---
        const cellQty = row.insertCell();
        cellQty.style.fontSize = '10pt';
        cellQty.style.width = '15%';
        cellQty.style.textAlign = "center";

        const qtyContainer = document.createElement('div');
        qtyContainer.className = 'main_quantity-container';

        // Subtract Button
        const btnSub = document.createElement('span');
        btnSub.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, -1);
        });
        const subImg = document.createElement('img');
        subImg.src = 'images/logo/subtract-color-icon.png';
        subImg.style.width = '40px';
        btnSub.appendChild(subImg);

        // Input Field
        const qtyInput = document.createElement('input');
        qtyInput.id = `txtitemqty${ItemID}`;
        qtyInput.type = 'text';
        qtyInput.className = 'main_input_qty';
        qtyInput.value = Qty;
        qtyInput.addEventListener('change', () => {
            console.log("qtyInput");
            console.log($(qtyInput).val());
            let NewQTY = $(qtyInput).val();
            change_amount(obj, NewQTY)
        });

        // Add Button
        const btnAdd = document.createElement('span');
        btnAdd.addEventListener('click', () => {
            console.log("Button clicked!"); // Debugging
            select_qty(obj, 1);
        });
        const addImg = document.createElement('img');
        addImg.src = 'images/logo/addition-color-icon.png';
        addImg.style.width = '40px';
        btnAdd.appendChild(addImg);

        qtyContainer.append(btnSub, qtyInput, btnAdd);

        // Display current Qty
        const qtyDisplay = document.createElement('div');
        qtyDisplay.style.cssText = 'min-width: 25px; height: 20px; float: left; font-size: large; text-align: center;';
        qtyDisplay.textContent = Qty.toLocaleString();

        cellQty.append(qtyContainer);

        // --- Cell 3: Price ---
        let objitemunits = obj.unit;
        let optionhtml = "";
        objitemunits.forEach(itemunit => {
            console.log("matched " + obj.SaleID + "," + itemunit.ID)
            if (obj.SaleID == itemunit.ID) {

                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}' Selected>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            } else {
                optionhtml += `<option value='${itemunit.ID},${itemunit.Price}'>${itemunit.FromQty}-${itemunit.ToQty}</option>`
            }

        });

        const cellPrice = row.insertCell();
        cellPrice.style.fontSize = '10pt';
        //cellPrice.textContent = ItemPrice.toLocaleString() + Currency;
        cellPrice.innerHTML = `<div style='padding:5px;'>
                                <table>
                                
                                <tr><td>
                                    <table><tr>
                                    <td>
                                        <span id="txtitemprice${ItemID}" type="text" class="dropdown-item-unit" style="width: 50px; height: 20px;">${ItemPrice}</span>
                                    </td>
                                    <td>
                                    $
                                    </td>
                                    </tr>
                                    </table>
                                 
                                </td>
                                <td>
                                    <table><tr><td>ឯកតា:</td>                                     
                                    <td><select id="dplitemunit${ItemID}" class="dropdown-item-unit" onchange="changeunitprice(${ItemID},this)">${optionhtml}</select>
                                    </td></tr></table>
                                </td>                               
                                </tr></table>
                                </div>`

        cellPrice.style.width = '15%';
        cellPrice.style.textAlign = "center";

        // --- Cell 4: Total ---
        const cellTotal = row.insertCell();
        //cellTotal.textContent = Total.toLocaleString() + Currency;
        cellTotal.innerHTML = `សរុប: <span id='total${ItemID}'>${Total.toLocaleString()} ${Currency}`;
        cellTotal.style.width = '15%';
        cellTotal.style.textAlign = "center";
        // --- Cell 5: Edit Icon ---
        const cellEdit = row.insertCell();
        const editDiv = document.createElement('div');
        editDiv.style.textAlign = 'center';


        const editIcon = document.createElement('i');
        editIcon.className = 'fa fa-trash';
        editIcon.style.cssText = 'font-size: x-large; color:#f23; cursor:pointer;';
        editIcon.addEventListener('click', () => {
            deleteitem(ItemID);    
            
        });

        

        editDiv.appendChild(editIcon);
        cellEdit.appendChild(editDiv);

        // 3. Final Assembly
        li_item.appendChild(mainTable);


        ul.appendChild(li_item);

    }

    //html += "</ul>";
   
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
    
    $("#div_invoiceitemlist").append(outerTable);
    $("#div_invoiceitemlist").append(ul);
    $("#lblReceiptDate").html(datestring);

    // 5. Define header configurations
    // This array maps text to optional widths for cleaner code
    const headers = [

        { text: 'ទំនិញ\u00A0\u00A0', width: '50%' },
        { text: 'ចំនួន\u00A0\u00A0', width: '15%' },
        { text: 'តម្លៃ\u00A0\u00A0', width: '15%' },
        { text: `សរុប = $ <span id='spanGTotal'>${GTotal.toLocaleString()}</span>`, width: '100%', textAlign: 'right', id: 'lblTotalOrder' },
        { text: '\u00A0\u00A0', width: 'auto' }
    ];

    // 6. Build and append the headers
    headers.forEach(headerData => {
        const th = document.createElement('th');
        th.scope = 'col';
        th.innerHTML = headerData.text; // \u00A0 is the unicode for &nbsp;
        if (headerData.width) {
            th.style.width = headerData.width;
            th.style.textAlign = "center";
        }
        headerRow.appendChild(th);
    });

    // 7. Assemble the components
    table.appendChild(headerRow);

    $("#lblTotalOrder").text("$" + GTotal.toLocaleString());
    $("#lblSubTotalR").html("R" + GTotalR.toLocaleString());
    $("#lblGrandTotal").html("$" + GTotal.toLocaleString());
}

function logout() {
    sessionStorage.setItem('items', '[]');
    let userid = 0
    userid = $("#hdUserID").val();
    if (userid == 0) {
        window.location = "applogin.aspx";
    } else {

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
            //addorder_client();
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
    $("#txtGetPoint").val("0");
    $("#spanTotal").text("$" + GTotal.toLocaleString());
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

    sessionStorage.setItem('items', '[]');

    arritems = [];
    loaditemcount();
    gohome();


}




function addorder_temp() {
    let arrobj = arritems;
    let JsonString = JSON.stringify(arrobj);
    //createCookie("items", JsonString, 90);

    // Store in sessionStorage for current session use
    try {
        sessionStorage.setItem('items', JsonString);

    } catch (e) { console.log(e); }

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

    let img_url = "https://sl-pos.sldalin.org/" + obj.ImageUrl.replace("^", "\/");
    //let img_url = "https://sl-pos.sldalin.org/" + obj.ImageUrl;

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
    console.log(arritems);
    const totalScope = arritems.reduce((sum, item) => sum + item.Price * item.Qty, 0);


    if (arritems.length == 0) {
        $("#span_itemcount").text("");
        $("#div_itemcount").hide();

    } else {
        $("#span_itemcount").text(arritems.length + " items = $" + totalScope.toFixed(2));
        $("#div_itemcount").show();

    }
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

function showorderitem() {
    let windowsize = $(document).width();
    if (arritems.length > 0) {
        let userlevel = $("#hdUserLevel").val();
        if (userlevel == "Admin" || userlevel == "Staff") {
            
            //alert(windowsize);
            if (windowsize <= 800) {
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
            if (windowsize <= 800) {
                stepname = "receipt";
                gotopage(stepname);
            } else {
                stepname = "category list";
                gotopage(stepname);
            }

        }

    }
  
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

function showorderlist() {
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
        url: "userorder.aspx/LoadDetail",
        data: JSON.stringify(obj),
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log(data);
            stepname = "ordered list";
            $("#pnOrderList").show();
            $("#div_orderlist").show();
            $("#div_orderdetail").hide();
            showorderlisthtml(data.d);
           
            //$("#div_logo").hide();
            //$("#div_list").show();
            //$("#div_list").html(data.d);
            //$("#div_bag").hide();
            //$("#span_logout").hide();
            //$("#span_home").show();
            //$("#btngoback").show();
        },
        error: function (result) {
            $('#loading').hide();
        }
    });

}

function showorderlisthtml(data) {
    let Menustring = "";
    Menustring += `<div style='padding: 10px 10px'><span style='float: left; text-decoration: underline; color: blue; ' onclick='showorderlist()'​>បញ្ជីការបញ្ជាទិញ</span><span onclick='show_cancellist()' style='float: right; text-decoration: underline; color: #6b0402; '>បញ្ជីនៃការលុប</span></div>`
    Menustring += `<div style='padding-left: 10px; padding-top: 20px; padding-bottom: 20px'>`

    Menustring += "<table style='border: 1px solid #adadad; width: 94% ' cellspacing='5' cellpadding='5'>";

    Menustring +=  "<tr>"
    Menustring += "<td align='center' style='padding: 4px; border-right: 1px solid #dddddd; border-bottom: 1px solid #adadad; background-color: #e9cb19'>ល.រ</td>"
    Menustring += "<td align='center' style='padding: 4px; border-right: 1px solid #dddddd; border-bottom: 1px solid #adadad; background-color: #e9cb19'>លេខបញ្ជាទិញ</td>"
    Menustring += "<td align='center' style='padding: 4px; border-right: 1px solid #dddddd; border-bottom: 1px solid #adadad; background-color: #e9cb19'>សរុប</td>"
    Menustring += "<td style='padding: 4px; padding-left: 10px; border-bottom: 1px solid #adadad; background-color: #e9cb19'>ផ្សេងៗ</td>"
    Menustring += "</tr>"

    for (var i = 0; i < data.length; i++) {
        let oderid = data[i].OrderID;
        Menustring += "<tr style='border: 1px solid #adadad'>"
        Menustring += "<td align='center' style='padding: 4px; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; '>" + (i + 1) + "</td>"
        Menustring += "<td style='padding: 4px; padding-left: 10px; border-bottom: 1px solid #dddddd; '>"
        Menustring += `<div><a onclick='get_order(${oderid}, "${data[i].CreatedDate}")' class='a-popup-detail'>${oderid}</a></div>`
        Menustring += `<div style='font-size: 12px; '>${data[i].CreatedDate}</div>`
            Menustring += "</td>"
        //Menustring += "<td style='padding:4px;border-right:1px solid #dddddd;padding-left:10px;border-bottom:1px solid #dddddd;'>" + data[i].CustomerName + "</td>"
        Menustring += "<td align='center' style='padding: 4px; border-right: 1px solid #dddddd; border-bottom: 1px solid #dddddd; '>" + data[i].Total + "</td>"
        if (data[i].Deleted == true) {
            Menustring += "<td style='padding: 4px; padding-left: 10px; border-bottom: 1px solid #dddddd; color: red; '>មោឃៈការទិញ</td>"
        }
        else {
            let paymentdetail = data[i].InvoiceNo;
            Menustring += "<td style='padding: 4px; padding-left: 10px; border-bottom: 1px solid #dddddd; '>" + paymentdetail + "</td>"
        }
        Menustring += "</tr>"
    }

    Menustring += "</table></div>"

    $("#div_orderlist").html(Menustring);
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
            stepname = "order detail";
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

    html += "<div style='padding:5px;'>សរុប: <span style='font-size:large;font-weight: bold;'>" + GTotal.toLocaleString() + "$</span> </div>";


    $("#div_orderdetail").html(html);
    $("#div_orderlist").hide();
    $("#div_orderdetail").show();

}


function nextstep() {
    console.log("step:" + stepname);
    if (stepname == "category list") {
        stepname = "checkout";

    } else if (stepname == "checkout") {
        //stepname = "category list";
        console.log(arritems);
        let userid = 0
        userid = $("#hdUserID").val();
        if (userid == 0) {
            showlogin();
        } else {
            paynow();
        }
        
    }

    if (stepname == "order list") {
        showreceipt();
    } else if (stepname == "checkout") {
        showcheckout();
    }
}
function showlogin() {
    $('#myModal3').show();
    $("#pnReceiptContainer").hide();
    $("#div_orderstatus").html(`
            <div class="modal-body">
            <div style="padding: 20px;text-align: center;font-size: x-large;">
            សូម log in ដើម្បីធ្វើការបញ្ជាទិញ</div>
                    <div style="text-align: center;">
                    <div class="btn-option" onclick="gotologin()" style="width: 50%;float: none;margin: auto;padding: 5% 2%;">Log in</div></div></div>`);
}

function gotologin() {
    window.location = "applogin.aspx"
}
function showcheckout() {
    $("#div_checkout").show();
    $("#div_invoiceitemlist").hide();
    document.querySelectorAll('.main_action-bg').forEach(el => {
        el.classList.remove('cart-desktop_active');
    });
    $("#span_step2").addClass("cart-desktop_active");
    $("#div_nextstep").html(`បញ្ជាទិញ`)
    
    loadcheckout();
}
function gotopage(step) {
    let windowsize = $(document).width();
    let userlevel = $("#hdUserLevel").val();
    if (step == "home") {
        $("#div_item").hide();
        $("#div_Category").hide();
        $("#div_Category_left").hide();

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
        $("#div_Category_left").show();
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
            $("#pnReceiptContainer").hide();
        }
        closeModal();
    } else if (step == "receipt") {
        $("#div_item").hide();
        $("#div_Category").show();
        $("#div_Category_left").show();
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
        $("#div_Category_left").hide();

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
        $("#div_Category_left").hide();

        $("#div_home").show();
        $("#btngoback").hide();
        $("#div_list").hide();
        $("#div_logo").show();
        $("#span_logout").show();
    } else if (step == "Checkout") {
        $("#div_checkout").show();
        $("#div_item").hide();
        $("#div_Category").hide();
        $("#div_Category_left").hide();

        $("#div_home").hide();
        $("#btngoback").show();
        $("#div_list").hide();
        $("#div_logo").hide();
        $("#span_logout").hide();
        $("#pnReceipt").hide();
    }
}

function gohome() {
    $("#myModal3").hide();
    
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

function select_qty(obj, increasingqty) {
    //$("#div_itemnotice").html("");
    console.log(obj);

    let newqty = 0;
    newqty = obj.Qty + increasingqty;
    if (newqty <= 0) {
        newqty = 1;
    }
    $(`#txtitemqty${obj.ItemID}`).val(newqty);
    change_amount(obj, newqty);

}

function change_amount(obj, NewQTY) {    
    obj.Qty = NewQTY;
    let total = obj.Qty * obj.Price;
    $(`#total${obj.ItemID}`).text(total.toLocaleString() + " $");
    addorder_temp();

    let GTotal = getGTotal(arritems);
    $(`#spanGTotal`).text(GTotal.toLocaleString());
}

function change_price(itemid, e) {
    let newprice = $(e).val();
    pricechanged(itemid, newprice);

}

function unitchanged(itemid, saleid) {
    const item = arritems.find(el => el.ItemID == itemid);
    if (item) {
        item.SaleID =parseInt(saleid); // Updates the object directly inside arritems
    }
    addorder_temp();
}
function pricechanged(itemid, newprice) {
    const item = arritems.find(el => el.ItemID == itemid);
    if (item) {
        item.Price = newprice; // Updates the object directly inside arritems
    }

    addorder_temp();

    $(`#total${itemid}`).text(item.Qty * newprice);
    let GTotal = getGTotal(arritems);
    $(`#spanGTotal`).text(GTotal.toLocaleString());
}

function deleteitem(itemid) {
    //createCookie("items", JsonString, 90);
    arritems = arritems.filter(function (el) {
        return el.ItemID != itemid;
    });
    addorder_temp();
    loadinvoiceitem(arritems);
    loaditemcount();
    $(`#countitem${itemid}`).text("0");
    $(`#countitem${itemid}`).hide();

}


function select_qtyold(qty) {
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

function groupitems(data) {
    const rawData = data;

    // 1. Group the data as before
    const nestedGrouping = rawData.reduce((acc, item) => {
        const catID = item.CategoryID;
        const catName = "Category " + catID;
        const grpName = item.GroupName || "Other";
        const grpID = item.GroupID;

        if (!acc[catID]) {
            acc[catID] = { CategoryID: catID, CategoryName: catName, Groups: {} };
        }

        if (!acc[catID].Groups[grpName]) {
            acc[catID].Groups[grpName] = { GroupID: grpID, GroupName: grpName, Products: [] };
        }

        acc[catID].Groups[grpName].Products.push(item);
        return acc;
    }, {});

    // Helper: Fisher-Yates Shuffle Algorithm
    const shuffle = (array) => {
        for (let i = array.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [array[i], array[j]] = [array[j], array[i]];
        }
        return array;
    };

    // 2. Transform into arrays and shuffle at every level
    const finalTree = Object.values(nestedGrouping).map(cat => {
        let groupsArray = Object.values(cat.Groups);

        // Shuffle Products inside each Group
        groupsArray.forEach(group => {
            shuffle(group.Products);
        });

        // Shuffle Groups inside Category
        return {
            ...cat,
            Groups: shuffle(groupsArray)
        };
    });

    // 3. Shuffle the Categories themselves before returning
    return shuffle(finalTree);
}

function groupitemsold(data) {
    const rawData = data; // Your array of ClProduct

    const nestedGrouping = rawData.reduce((acc, item) => {
        const catID = item.CategoryID;
        const catName = "Category " + catID;
        const grpName = item.GroupName || "Other";
        const grpID = item.GroupID; // Get GroupID from the product item

        // 1. Initialize Category level
        if (!acc[catID]) {
            acc[catID] = {
                CategoryID: catID,
                CategoryName: catName,
                Groups: {}
            };
        }

        // 2. Initialize Group level within Category (Include GroupID here)
        if (!acc[catID].Groups[grpName]) {
            acc[catID].Groups[grpName] = {
                GroupID: grpID, // Added GroupID
                GroupName: grpName,
                Products: []
            };
        }

        // 3. Add the product
        acc[catID].Groups[grpName].Products.push(item);

        return acc;
    }, {});

    // Convert objects to arrays for easier looping
    const finalTree = Object.values(nestedGrouping).map(cat => ({
        ...cat,
        Groups: Object.values(cat.Groups)
    }));

    return finalTree;
}


function select_qtyold(qty) {
    console.log(objitem);
    objitem.Qty = qty;
    $("#itemqty").html(qty);
    console.log(objitem);

    let GTotal = objitem.Qty * objitem.ItemPrice;
    $("#itemtotal").html(GTotal.toLocaleString());
}

function changesearchtype() {
    let filtertype = $("#selectsearchtype").val();
    if (filtertype == "Barcode") {
        $(".textbox-barcode").focus().select();
    }

}
function changesearch(e) {
    let filtertype = $("#selectsearchtype").val();

    if (filtertype == "Barcode") {
        $(e).focus().select();

        let filteritem = allitem
            .flatMap(cat => cat.Groups)
            .flatMap(group => group.Products)
            .filter(item =>
                item.ItemCode?.toLowerCase()==$(e).val().toLowerCase()
            );

        if (filteritem.length > 0) {
            loaditem_selected(filteritem[0]);
        }

    }

}
function filter_result(e) {

    let searchtype = $("#selectsearchtype").val();

    if (searchtype == "Search") {
        let itemcode = $(e).val();


        if (itemcode == "") {
            let e = $("#a_allcat");
            if (ismobile) {
                $("#div_group_desktop").hide();
                $("#div_group_mobile").show();
            } else {
                $("#div_group_desktop").show();
                $("#div_group_mobile").hide();
            }
            view_itembbycategory(e, 0)

        } else {

            $("#div_group_desktop").hide();
            $("#div_group_mobile").hide();
            const copyitems = allitem.slice();
            

            //let filteritem = copyitems.filter(item =>
            //    item.ItemCode.toLowerCase().includes(itemcode.toLowerCase()) || item.ItemName.toLowerCase().includes(itemcode.toLowerCase())
            //);

            let filteritem = copyitems
                .flatMap(cat => cat.Groups || [])
                .flatMap(group => group.Products || [])
                .filter(item =>
                    item.ItemCode?.toLowerCase().includes(itemcode.toLowerCase()) ||
                    item.ItemName?.toLowerCase().includes(itemcode.toLowerCase())
                );


            //console.log("filteritem");
            //console.log(filteritem);

            displayitems = filteritem;
            pageindex = 1;
            showsearchitem(filteritem);

        }

    }

}
function search() {
    let itemcode = '';
    let windowsize = $(document).width();
    if (windowsize >= 800) {
        itemcode = $("#txtsearchleft").val();
    } else {
        itemcode = $("#txtbarcodenew").val();
    }
    searchbycode(itemcode);
}
function searchbycode(itemcode) {
    //let itemcode = $("#txtbarcodenew").val();
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
            
            loaditem_selected(oitem);
            //$("#txtbarcodenew").select();
            //$("#txtitemamount").select();
            //$("#txtitemamount").focus();

            
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


function view_itembbycategory(element,categoryid) {
    // Show waiting dialog
    //showWaitingDialog();
    // 1. Remove 'active' class from all siblings <li>
    $(element).closest('ul').find('li').removeClass('menu_active__SrfmU');

    // 2. Add 'active' class to the parent <li> of the clicked <a>
    $(element).parent('li').addClass('menu_active__SrfmU');

    $("#txtsearchleft").val("");
    $("#txtbarcodenew").val("");
    groupdisplayed = [];
    if (ismobile) {
        $("#div_group_mobile").show(); 
    } else {
        $("#div_group_desktop").show();
    }
    load_itembycategory(categoryid);
   
    
}

function getallitems() {
    let userlevel = $("#hdUserLevel").val();
    let obj = {};
    obj.UserLevel = userlevel;
    obj.CategoryID = 0;
    showWaitingDialog();
    $.ajax({
        cache: false,
        async: true, // Changed to async for better UX
        type: "Post",
        //dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "userorderv2.aspx/getitemsbycategory",
        data: JSON.stringify(obj),
        complete: function () {
            // Hide waiting dialog when complete
            hideWaitingDialog();
            //$('#imageLoadingOverlay').fadeOut(500, function () {
            //    $(this).remove();
            //});
        },
        success: function (data) {
            
            // Normalize incoming payload to an array of items
            //allitem = data.d;
            allitemsnotyetgroup = data.d;
            let groupingitem = groupitems(allitemsnotyetgroup);
            console.log("groupingitem");
            console.log(groupingitem);
            allitem = groupingitem;
            load_itembycategory(0);

        },
        error: function (result) {
            console.log("Error loading items by category:", result);
            // Show error message
            showErrorDialog("មានបញ្ហាក្នុងការផ្ទុកទំនិញ។ សូមព្យាយាមម្តងទៀត។");
        }
    });
}

function load_itembycategory(categoryid) {
    console.log(`load_itembycategory(${categoryid})`)
    let filterdata = [];
    if (categoryid == 0) {
        filterdata = allitem;
    } else {
        filterdata = allitem.filter(item =>
            item.CategoryID === categoryid
        );
    }

    pub_showitem = [];
    
    displayitems = filterdata;
    //loadgroupbox(displayitems);
    loadgroupbox(categoryid);   
    
    
}

function load_itembygroup(categoryid,groupid) {
    console.log(`load_itembygroup(${groupid})`)
    let filterdata = [];

    if (categoryid == 0) {
        if (groupid == 0) {
            filterdata = allitem
            .flatMap(cat => cat.Groups || []);
        } else {
            filterdata = allitem
                .flatMap(cat => cat.Groups || [])             // Get groups only from THAT category
                .filter(group => group.GroupID === groupid);  // Find the specific group

        }
    } else {
        if (groupid == 0) {
            filterdata = allitem
                .filter(cat => cat.CategoryID === categoryid) // First, find the right category
                .flatMap(cat => cat.Groups || [])             // Get groups only from THAT category
 
        } else {
            filterdata = allitem
                .filter(cat => cat.CategoryID === categoryid) // First, find the right category
                .flatMap(cat => cat.Groups || [])             // Get groups only from THAT category
                .filter(group => group.GroupID === groupid);  // Find the specific group

        }
    }

    pub_showitem = [];

    displayitems = filterdata;

    $('#div_itembycategory').empty();
    pageindex = 1;
    $(window).scrollTop($('#div_itembycategory').offset().top);
    showitem(displayitems, pageindex);
    

}


function getgrouplist(items, excludegroupid) {
  
    const groups = [...new Map(items.map(item => [
        // 1. Create the unique key (ID + Name)
        `${item.GroupID}|${item.GroupName}`,
        // 2. Define what the cloned object should look like
        { GroupID: item.GroupID, GroupName: item.GroupName }
    ])).values()];

    return groups;
}

function flytocart(itemid) {
    var cart = $('#span_itemcount');
    var imgtodrag = $("#img_item" + itemid);
    if (imgtodrag) {
        var imgclone = imgtodrag.clone()
            .offset({
                top: imgtodrag.offset().top,
                left: imgtodrag.offset().left
            })
            .css({
                'opacity': '0.5',
                'position': 'absolute',
                'height': imgtodrag.height() + 'px',
                'width': imgtodrag.width() + 'px',
                'z-index': '99999'
            })
            .appendTo($('body'))
            .animate({
                'top': $(window).scrollTop() + $(window).height() - 50,
                'left': ($(window).width() / 2) - 25,
                'width': 75,
                'height': 75
            }, 300, 'easeInOutExpo');

        //setTimeout(function () {
        //    cart.effect("shake", {
        //        times: 2
        //    }, 200);
        //}, 1500);

        imgclone.animate({
            'width': 0,
            'height': 0
        }, function () {
            $(this).detach()
        });
    }
}

function closereceipt() {
    if (stepname == "checkout") {
        showreceipt()
    } else {
        let searchtype = $("#selectsearchtype").val();
        if (searchtype=="Barcode") {
            $(".textbox-barcode").focus().select();
        }
        $("#pnReceiptContainer").hide();
    }
    
}

function closeorder() {
    if (stepname == "order detail") {
        showorderlist()
    } else {        
        $("#pnOrderList").hide();
    }
}

function showreceipt() {
    stepname = "category list"
    $("#div_order_command").html(`<div>
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
`);
    $("#pnReceiptContainer").show();
    $("#div_invoiceitemlist").show();
    $("#div_checkout").hide();
    $("#div_nextstep").html(`បន្ទាប់<i class="fa fa-angle-right icon-font_font icons_icon-arrow-cart main_button-icon"></i>`)
    document.querySelectorAll('.main_action-bg').forEach(el => {
        el.classList.remove('cart-desktop_active');
    });
    $("#span_step1").addClass("cart-desktop_active");
    loadinvoiceitem(arritems);
}

function loadgroupbox(categoryid) {
    let itemsall = displayitems;
    console.log(itemsall);
    if (!Array.isArray(itemsall)) {
        itemsall = [];
    }

    const items = itemsall.slice();
    //load group///////////////
    let windowsize = $(document).width();
    let html_group = "";
    
    html_group += '<ul class="menu_group" style="transform: translateZ(0px);height: auto;">'
        html_group += `<li class="menu_active__SrfmU"><a id="a_allgroup" onclick="view_itembygroup(this,${categoryid},0)">All</a></li>`
   
    //////////////////////


    // We use a label "mainLoop" so we can break out of all nested loops at once
    mainLoop: for (const category of items) {
        //console.log(`--- Category: ${category.CategoryName} ---`);

        for (const group of category.Groups) {

            //console.log(`--- Group: ${group.GroupName} ---`);
            let groupid = group.GroupID;
            let groupname = group.GroupName;
            ////load each group/////////////
            html_group += `<li><a onclick="view_itembygroup(this,${categoryid},${groupid})">${groupname}</a></li>`
            ////////////////////////////////
        }
    }
    $("#div_group_desktop").html(html_group);
    $("#div_group_mobile").html(html_group);

    view_itembygroup(this,categoryid, 0)

    //stepname = "item list";
}

function view_itembygroup(element,categoryid, groupid) {
    $(element).closest('ul').find('li').removeClass('menu_active__SrfmU');

    // 2. Add 'active' class to the parent <li> of the clicked <a>
    $(element).parent('li').addClass('menu_active__SrfmU');

    $("#txtsearchleft").val("");
    $("#txtbarcodenew").val("");
    groupdisplayed = [];
    load_itembygroup(categoryid,groupid);

    
}

function showitem(itemdata, pageindex) {
    console.log("showitem page:" + pageindex);
    let itemsall = itemdata;
    console.log(itemsall);
    if (!Array.isArray(itemsall)) {
        itemsall = [];
    }
    console.log("groupdisplayed");
    console.log(groupdisplayed);

    const items = itemsall.filter(
        group => !groupdisplayed.includes(group.GroupID)
    )
    console.log(items);

    let countitems = 0;

    if (items.length == 0 && lastgroup == false) {
        lastgroup = true;
        const $bottomdiv = $('<div>', {
            class: 'item-groupname',
            style: 'background-color: transparent;height:100px;'
        });
        $('#div_itembycategory').append($bottomdiv);

    } else if (items.length > 0) {
        lastgroup = false;
    }

    // We use a label "mainLoop" so we can break out of all nested loops at once

    mainLoop: for (const group of items) {
        //console.log(group);
        //console.log(`--- Group: ${group.GroupName} ---`);
        let groupid = group.GroupID;
        let groupname = group.GroupName;
        groupdisplayed.push(groupid);
        pub_showitem.push(groupid);

        const $itemGroup = $('<div>', {
            class: 'item-groupname',
            click: function () { showitemingroup(groupid); }
        });

        const $table = $(`
            <table style='width:100%'>
                <tr>
                    <td>${groupname}</td>
                    <td style='text-align:right;'>
                        <span id='span_arrow_${groupid}'>
                            <i class='fa fa-arrow-up' style='font-size: 28px;'></i>
                        </span>
                    </td>
                </tr>
            </table>
        `);



        const $contentDiv = $('<div>', {
            id: `divgroup_${groupid}`,
            css: { display: 'block' }
        });

        $itemGroup.append($table);
        $('#div_itembycategory').append($itemGroup);

        for (const product of group.Products) {
            countitems += 1;


            let anchor = getitemanchor(product);
            $contentDiv.append(anchor);
        }

        $('#div_itembycategory').append($contentDiv);

        // 1. Check the limit
        if (countitems > 50) {
            console.log("Reached 50 items. Stopping...");
            break mainLoop; // This exits all loops immediately
        }

    }
}




function showitem_old(itemdata, pageindex) {
    console.log("showitem page:" + pageindex);
    let items = itemdata;
    console.log(items);
    if (!Array.isArray(items)) {
        items = [];
    }
    let fromindex = (pageindex - 1) * 50;
    let toindex = pageindex * 50;
    if (pageindex * 50 > items.length) {
        toindex = items.length;
    }
    let exclude_groupid = 0;
    if (pageindex > 1) {
        let lastitem = items.slice(fromindex - 1, fromindex);
        console.log("lastitem");
        console.log(lastitem);
        exclude_groupid = lastitem[0].GroupID;
    } else {
        exclude_groupid = 0;
    }

    console.log("exclude groupid");
    console.log(exclude_groupid);
    console.log("page");
    console.log(pageindex);

    var showitems = items.slice(fromindex, toindex);
    //var showitems = items;
    console.log(showitems);
    let lstgroup = getgrouplist(showitems);
    console.log("lstgroup");
    console.log(lstgroup);
    //displayitems = items;
    // Small helper to escape HTML text

    let html = "";

    lstgroup.forEach(function (group) {
        let groupid = group.GroupID;
        let groupname = group.GroupName;
        let itemsingroup = showitems.filter(item =>
            item.GroupID === groupid
        );
        //console.log("itemsingroup id:" + groupid + "," + groupname);
        //console.log(itemsingroup);
        pub_showitem.push(groupid);
        // 1. Create the header container
        const $itemGroup = $('<div>', {
            class: 'item-groupname',
            click: function () { showitemingroup(groupid); }
        });

        // 2. Build the internal table structure
        const $table = $(`
    <table style='width:100%'>
        <tr>
            <td>${groupname}</td>
            <td style='text-align:right;'>
                <span id='span_arrow_${groupid}'>
                    <i class='fa fa-arrow-up' style='font-size: 28px;'></i>
                </span>
            </td>
        </tr>
    </table>
`);

        // 3. Create the content div
        const $contentDiv = $('<div>', {
            id: `divgroup_${groupid}`,
            css: { display: 'block' }
        });

        // 4. Assemble and Append
        $itemGroup.append($table);
        //console.log($itemGroup);
        if (groupid != exclude_groupid) {
            $('#div_itembycategory').append($itemGroup);
        }

        itemsingroup.forEach(function (row) {
            let anchor = getitemanchor(row);
            // 4. Finally, append it to your list container
            $('#div_itembycategory').append($contentDiv).append(anchor);


        });


    });



    //stepname = "item list";
}


function getitemanchor(row) {
   
    const ItemID = row.ItemID;
    const ItemName = row.ItemName;
    const ItemCode = row.ItemCode;
    const img_url = row.ImageUrl;
    const ItemPrice = row.Price;
    const Currency = row.Currency;
    const SaleID = row.SaleID;

    const ItemNameEsc = (ItemName).replace(/'/g, "\\'");
    const ItemCodeEsc = (ItemCode).replace(/'/g, "\\'");
    const ImageUrlEsc = (img_url).replace(/'/g, "\\'");
    const CurrencyEsc = (Currency).replace(/'/g, "\\'");

    // 1. Create the anchor tag and attach the click event directly
    const $anchor = $('<a>');



    // 2. Build the inner structure
    const $itemDetail = $('<div>', { class: 'item-detail', id: `div_itemdetail${ItemID}` });
    const $shadowBox = $('<div>').css({
        'box-shadow': '0 0 4px 0 rgba(0,0,0,.0)',
        '-webkit-box-shadow': '0 0 4px 0 rgba(0,0,0,.0)',
        '-moz-box-shadow': '0 0 4px 0 rgba(0,0,0,.0)'
    });

    //console.log("itemsingroup");
    //console.log(itemsingroup);

    let item_selected = arritems.filter(function (el) {
        return el.ItemID == ItemID;
    });
    //console.log(item_selected);
    if (item_selected.length > 0) {
        if (item_selected[0].Qty == 0) {
            let $div_selecteditem = $('<div>', { class: 'count-selected-item', id: 'countitem' + ItemID, text: item_selected[0].Qty });
            $shadowBox.append($div_selecteditem);
            $div_selecteditem.hide();
            //html += `<div class='count-selected-item' id='countitem${ItemID}' style='display:none;'></div>`;
        } else {
            //html += `<div class='count-selected-item' id='countitem${ItemID}'>${item_selected[0].Qty}</div>`
            let $div_selecteditem = $('<div>', { class: 'count-selected-item', id: 'countitem' + ItemID, text: item_selected[0].Qty });
            $shadowBox.append($div_selecteditem);
            $div_selecteditem.show();
        }


    } else {
        //html += `<div class='count-selected-item' id='countitem${ItemID}' style='display:none;'></div>`;
        let $div_selecteditem = $('<div>', { class: 'count-selected-item', id: 'countitem' + ItemID, text: 0 });
        $shadowBox.append($div_selecteditem);

        $div_selecteditem.hide();
    }


    const $imgContainer = $('<div style="Position:Relative;">', { align: 'center' });
    $imgContainer.on('click', function (event) {
        // Prevent the link from navigating to the URL in the href attribute
        event.preventDefault();

        // Your custom function goes here
        flytocart(ItemID);
        selectaddoneitem(SaleID, ItemID, ItemNameEsc, ItemCodeEsc, ImageUrlEsc, ItemPrice, CurrencyEsc)
    });
    const $div_zoom = $('<div>', { style: 'position:relative;' });
    const $span_zoom = $('<span>', { class: 'span-zoom' });
    $div_zoom.append($span_zoom);

    $span_zoom.html(`<i class="fa fa-expand"></i>`);
    $span_zoom.on('click', function (event) {
        // Prevent the link from navigating to the URL in the href attribute
        event.preventDefault();

        // Your custom function goes here
        cloneAndZoom(ItemID);
    });

    const $img = createLazyImage({
        id: "img_item" + ItemID,
        class: 'img_item_thumnail img-round',
        src: 'https://sl-pos.sldalin.org/' + img_url,
        alt: '',
        css: { width: '100%', 'object-fit': 'fill' }
    });

    const $textBox = $('<div>', { class: 'item-detail-textbox' });

    const $nameDiv = $('<div>', {
        class: 'item-detail-text',
        id: `divitemname${ItemID}`,
        text: `${ItemCode} ${ItemName}` // .text() is safer than .html()
    });

    let userid = 0
    userid = $("#hdUserID").val();
    let displayprice = `${ItemPrice}${Currency}`
    if (userid == 0) {
        displayprice = '';
    }
    const $priceDiv = $('<div>', {
        class: 'box-buy',
        text: displayprice
    });

    const $itemcode = $('<div>', {
        class: 'item-code',
        text: `${ItemCode}`
    });
    // 3. Assemble the pieces (Bottom-up)
    $textBox.append($nameDiv);
    $imgContainer.append($img, $priceDiv, $div_zoom);
    $shadowBox.append($imgContainer, $textBox);
    $itemDetail.append($shadowBox);
    $anchor.append($itemDetail);

    return $anchor;

}

function cloneAndZoom(ItemID) {
    // Wrap in $() to select it, then use [0] to get the raw DOM element
    let originalDiv = $(`#div_itemdetail${ItemID}`)[0];

    // Safety check: if the ID doesn't exist, stop the function
    if (!originalDiv) {
        console.error(`Element #div_itemdetail${ItemID} not found`);
        return;
    }

    // FIX 1: Use querySelector for native elements
    let directImg = originalDiv.querySelector('img');
    
    directImg.setAttribute("style","width: 100%; object-fit: fill; Height: auto !important;")
    
    // 1. Create Overlay
    const overlay = document.createElement('div');
    overlay.className = 'zoom-overlay';

    // 2. Clone the Div (Now originalDiv is a raw element, so this works!)
    const clone = originalDiv.cloneNode(true);
    clone.classList.add('cloned-content');
    clone.style = "width:100% !important;max-width:400px;";

    
    // 3. Calculate Scale Factor
    const rect = originalDiv.getBoundingClientRect();
    const scaleX = (window.innerWidth * 0.9) / rect.width;
    const scaleY = (window.innerHeight * 0.9) / rect.height;
    const finalScale = Math.min(scaleX, scaleY);

    // 4. Position and append
    document.body.appendChild(overlay);
    overlay.appendChild(clone);

    // Trigger animation after a tiny delay
    requestAnimationFrame(() => {
        overlay.style.opacity = '1';
        //clone.style.transform = `scale(${finalScale})`;
    });

    // 5. Cleanup on click
    overlay.onclick = () => {
        overlay.style.opacity = '0';
        //clone.style.transform = 'scale(1)';
        directImg.setAttribute("style", "width: 100%; object-fit: fill;")

        setTimeout(() => overlay.remove(), 300);

    };
}

function showitembydata(items) {
    $('#div_itembycategory').html("");
    if (!Array.isArray(items)) {
        items = [];
    }
    var showitems = items;
    let lstgroup = getgrouplist(showitems);

    lstgroup.forEach(function (group) {
        let groupid = group.GroupID;
        let groupname = group.GroupName;
        let itemsingroup = showitems.filter(item =>
            item.GroupID === groupid
        );
        pub_showitem.push(groupid);
        // 1. Create the header container
        const $itemGroup = $('<div>', {
            class: 'item-groupname',
            click: function () { showitemingroup(groupid); }
        });

        // 2. Build the internal table structure
        const $table = $(`
    <table style='width:100%'>
        <tr>
            <td>${groupname}</td>
            <td style='text-align:right;'>
                <span id='span_arrow_${groupid}'>
                    <i class='fa fa-arrow-up' style='font-size: 28px;'></i>
                </span>
            </td>
        </tr>
    </table>
`);

        // 3. Create the content div
        const $contentDiv = $('<div>', {
            id: `divgroup_${groupid}`,
            css: { display: 'block' }
        });

        // 4. Assemble and Append
        $itemGroup.append($table);
        //console.log($itemGroup);
            $('#div_itembycategory').append($itemGroup);
        



        itemsingroup.forEach(function (row) {
            const ItemID = row.ItemID;
            const ItemName = row.ItemName;
            const ItemCode = row.ItemCode;
            const img_url = row.ImageUrl;
            const ItemPrice = row.Price;
            const Currency = row.Currency;
            const SaleID = row.ID;

            const ItemNameEsc = (ItemName).replace(/'/g, "\\'");
            const ItemCodeEsc = (ItemCode).replace(/'/g, "\\'");
            const ImageUrlEsc = (img_url).replace(/'/g, "\\'");
            const CurrencyEsc = (Currency).replace(/'/g, "\\'");

            // 1. Create the anchor tag and attach the click event directly
            const $anchor = $('<a>');

            $anchor.on('click', function (event) {
                // Prevent the link from navigating to the URL in the href attribute
                event.preventDefault();

                // Your custom function goes here
                selectaddoneitem(SaleID, ItemID, ItemNameEsc, ItemCodeEsc, ImageUrlEsc, ItemPrice, CurrencyEsc)
            });

            // 2. Build the inner structure
            const $itemDetail = $('<div>', { class: 'item-detail' });
            const $shadowBox = $('<div>').css({
                'box-shadow': '0 0 4px 0 rgba(0,0,0,.2)',
                '-webkit-box-shadow': '0 0 4px 0 rgba(0,0,0,.2)',
                '-moz-box-shadow': '0 0 4px 0 rgba(0,0,0,.2)'
            });

            const $imgContainer = $('<div>', { align: 'center' });
            const $img = createLazyImage({
                id: "img_item" + ItemID,
                class: 'img_item_thumnail img-round',
                src: 'https://sl-pos.sldalin.org/' + img_url,
                alt: '',
                css: { width: '100%', 'object-fit': 'fill' }
            });

            const $textBox = $('<div>', { class: 'item-detail-textbox' });

            const $nameDiv = $('<div>', {
                class: 'item-detail-text',
                id: `divitemname${ItemID}`,
                text: `${ItemName}` // .text() is safer than .html()
            });

            const $priceDiv = $('<div>', {
                class: 'box-buy',
                text: `${ItemPrice}${Currency}`
            });


            const $itemcode = $('<div>', {
                class: 'item-code',
                text: `${ItemCode}`
            });

            // 3. Assemble the pieces (Bottom-up)
            $textBox.append($nameDiv, $priceDiv);
            $imgContainer.append($itemcode, $img);
            $shadowBox.append($imgContainer, $textBox);
            $itemDetail.append($shadowBox);
            $anchor.append($itemDetail);

            // 4. Finally, append it to your list container
            
            $('#div_itembycategory').append($contentDiv).append($anchor);


        });


    });



    //stepname = "item list";
}


function showsearchitem(items) {
    $('#div_itembycategory').html("");
    if (!Array.isArray(items)) {
        items = [];
    }
   
      




        items.forEach(function (row) {
            
            // 1. Create the anchor tag and attach the click event directly
            let anchor = getitemanchor(row);

            // 4. Finally, append it to your list container

            $('#div_itembycategory').append(anchor);


        });

}

function showsearch() {
    $("#div_search").show();
    $("#div_banner").hide();
    $("#btngohome").hide();
    $("#li_showsearch").hide();
    $("#btnclosesearch").show();
  


}
function closesearch() {
    $("#div_search").hide();
    $("#div_banner").show();
    $("#btngohome").show();
    $("#li_showsearch").show();
    $("#btnclosesearch").hide();

}

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
        '<div style="font-size: 36px; color: #e74c3c;">⚠️</div>' +
        '</div>' +
        '<div style="font-size: 16px; color: #333; font-weight: bold;">' + message + '</div>' +
        '<div style="margin-top: 20px;">' +
        '<button onclick="$(\'#errorDialog\').fadeOut(300, function() { $(this).remove(); });" style="background: #3498db; color: white; padding: 10px 25px; border: none; border-radius: 5px; cursor: pointer; font-size: 14px;">យល់ព្រម</button>' +
        '</div>' +
        '</div>' +
        '</div>';
    
    $('body').append(errorHtml);
}
