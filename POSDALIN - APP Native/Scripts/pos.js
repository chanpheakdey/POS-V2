function view_group(categoryid, userid) {
    $.ajax({
        cache: false,
        async: false,
        type: "POST",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "appservice.asmx/getGroup",
        data: '{"CategoryID":' + categoryid + ',"UserID":' + userid + '}',
        success: function (data) {
            var html = data.d;
            $("#div_popup_contentnew").html(html);
            $("#div_popupnew").show();
        },
        error: function (result) {
            //$('#loading').hide();
        }
    });

  

}


function viewSaleItem(categoryid, groupid, userid) {
    $.ajax({
        cache: false,
        async: false,
        type: "POST",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "appservice.asmx/getSaleItem",
        data: '{"CategoryID":' + categoryid + ', "GroupID":' + groupid + ', "UserID":' + userid +'}',
        success: function (data) {
            var html = data.d;
            $("#div_popup_contentnew").html(html);
            $("#div_popupnew").show();
        },
        error: function (result) {
            //$('#loading').hide();
        }
    });
}


function viewitem(categoryid, groupid, itmeid, userid) {
    var url = "pos.aspx?category=" + categoryid + "&item=" + itmeid + "&user=" + userid ;
    window.location = url;
}


function closepopup(elementid) {
    $("#" + elementid).hide();
}