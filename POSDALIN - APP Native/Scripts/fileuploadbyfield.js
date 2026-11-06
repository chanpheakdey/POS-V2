var loadFile = function (event,type,imgid) {
    var imgloading = document.getElementById(imgid);
    imgloading.src = "images/Loading_2.gif";

    $.ajax({
        type: "POST",
        url: "appservice.asmx/GetPhotoID",
        cache: false,
        contentType: "application/json; charset=utf-8",
        data: "{}",
        dataType: "json",
        success: function (data) {
            console.log("photoid data:");
            console.log(data);
            var ccid = data.d;
            console.log(ccid);


            $("#hdphotoid").val(ccid);
            console.log($("#hdphotoid").val());
            $.ajax({
                type: "POST",
                url: "appservice.asmx/gettoken",
                cache: false,
                contentType: "application/json; charset=utf-8",
                data: "{}",
                dataType: "json",
                success: function (datatoken) {
                    var token = datatoken.d;

                    
                        console.log(window.location.href);
                        if (window.location.href.includes("item=")==true) {
                            //alert(event.target.files[0]);
                            var itemid = getUrlParameter("item");
                            upload_file(event.target.files[0], ccid + '.jpg', itemid);
                           
                        } else {
                            if (window.location.href.includes("proid")) {
                                console.log("upload image");
                                var itemid = getUrlParameter("proid");
                                upload_file(event.target.files[0], ccid + '.jpg', itemid);

                            } else {
                                console.log("preview image");
                                var reader = new FileReader();
                                reader.onload = function () {
                                    var output = document.getElementById(imgid);
                                    output.src = reader.result;
                                };
                                reader.readAsDataURL(event.target.files[0]);

                            }

                        }

                    

                },
                error: function (data) {
                    alert(data);
                }
            });


        },
        error: function (data) {
            alert(data);
        }
    });

    

};

function uploadnew_cat(photoid) {
    var CategoryName = $("#txtCategoryName").val();

    var parameters = '&newrecord=1&upload=1&category=new' + photoid + '&catid=new' + photoid + '&photoid=' + photoid + '&CategoryName=' + CategoryName;

    var url = window.location.href + parameters;

    window.location = url;
}

function uploadnew(photoid) {
    var itemcode = $("#txtCode").val();
    var itemname = $("#txtName").val();
    var category = $("#cboCategory").val();
    var group = $("#cboGroup").val();
    var supplier = $("#cboSupplier").val();
    var color = $("#cboColor").val();
    var CurrencyImport = $("#cboCurrencyImport").val();
    var UnitPrice = $("#txtUnitPrice").val();
    var CurrentQty = $("#txtCurrentQty").val();
    var ImportedQty = $("#txtImportedQty").val();
    var SetLowStock = $("#txtSetLowStock").val();
    var CurrentSale = $("#cboCurrentSale").val();

    var FromQty1 = $("#txtFromQty1").val();
    var ToQty1 = $("#txtToQty1").val();
    var Price1 = $("#txtPrice1").val();

    var FromQty2 = $("#txtFromQty2").val();
    var ToQty2 = $("#txtToQty2").val();
    var Price2 = $("#txtPrice2").val();

    var FromQty3 = $("#txtFromQty3").val();
    var ToQty3 = $("#txtToQty3").val();
    var Price3 = $("#txtPrice3").val();

    var FromQty4 = $("#txtFromQty4").val();
    var ToQty4 = $("#txtToQty4").val();
    var Price4 = $("#txtPrice4").val();

    var parameters = '&newrecord=1&upload=1&proid=new' + photoid + '&type=proid&photoid=' + photoid + '&itemcode=' + itemcode + '&itemname=' + itemname + '&category=' + category + '&group=' + group + '&supplier=' + supplier + '&color=' + color;
    parameters += '&CurrencyImport=' + CurrencyImport + '&UnitPrice=' + UnitPrice + '&CurrentQty=' + CurrentQty;
    parameters += '&ImportedQty=' + ImportedQty + '&SetLowStock=' + SetLowStock + '&CurrentSale=' + CurrentSale;
    parameters += '&FromQty1=' + FromQty1 + '&ToQty1=' + ToQty1 + '&Price1=' + Price1;
    parameters += '&FromQty2=' + FromQty2 + '&ToQty2=' + ToQty2 + '&Price2=' + Price2;
    parameters += '&FromQty3=' + FromQty3 + '&ToQty3=' + ToQty3 + '&Price3=' + Price3;
    parameters += '&FromQty4=' + FromQty4 + '&ToQty4=' + ToQty4 + '&Price4=' + Price4;

    var url = window.location.href + parameters;

    window.location = url;
}

function upload_file(file, filename,id) {
    //let par = { Filename: filename, Type: 'item'};
    let xhr = new XMLHttpRequest();
    let formData = new FormData();
    let photo = file

    
    //formData.append("par", JSON.stringify(par));
    formData.append("photo", photo);

    var posturl = 'https://slposapp.azurewebsites.net/FileUploadHandler.ashx?ContactID=' + id + '&Type=proid&FileName=' + filename;
    $.ajax({
        url: posturl,
        type: 'POST',
        data: formData,
        processData: false,  // tell jQuery not to process the data
        contentType: false,  // tell jQuery not to set contentType
        success: function (data) {
            window.location = window.location.href;;
        }
    });
}

function upload_file_old(file, filename, id) {
    //let par = { Filename: filename, Type: 'item'};
    let xhr = new XMLHttpRequest();
    let formData = new FormData();
    let photo = file


    //formData.append("par", JSON.stringify(par));
    formData.append("photo", photo);

    //xhr.onreadystatechange = state => { console.log(xhr.status); } // err handling

    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            //var response = JSON.parse(xhr.responseText);
            if (xhr.status === 200) {
                window.location = window.location.href;

            } else {
                console.log('failed');
            }

        }
    }


    xhr.timeout = 5000;
    xhr.open("POST", 'FileUploadHandler.ashx?ContactID=' + id + '&Type=proid&FileName=' + filename,);
    xhr.send(formData);
    //
}

function delete_file(FileUploadID) {
    if (confirm('Are you sure to delete this file?')) {
        console.log("delete file:" + FileUploadID);
        var User = getUrlParameter("User");
        var Field = getUrlParameter("Field");
        var UserID = getUrlParameter("UserID");
        var EnterpriseID = getUrlParameter("EnterpriseID");
        $.ajax({
            cache: false,
            async: false,
            type: "POST",
            dataType: "Json",
            contentType: "application/json; charset=utf-8",
            url: "uploadbyfield.aspx/DeleteFile",
            data: '{FileUploadID:' + FileUploadID + ',User:"' + User + '"}',
            success: function (data) {

                load_file_list(Field, UserID, EnterpriseID);
                //show_result(data);
            },
            error: function (result) {
                //$('#loading').hide();
            }
        });
    } else {

    }
    
}

function loadupload(queryname) {

    $.ajax({
        type: "POST",
        url: "appservice.asmx/GetPhotoID",
        cache: false,
        contentType: "application/json; charset=utf-8",
        data: "{}",
        dataType: "json",
        success: function (data) {
            console.log("photoid data:");
            console.log(data);
            var ccid = data.d;
            console.log(ccid);
           

            $("#hdphotoid").val(ccid);
            console.log($("#hdphotoid").val());
            $.ajax({
                type: "POST",
                url: "appservice.asmx/gettoken",
                cache: false,
                contentType: "application/json; charset=utf-8",
                data: "{}",
                dataType: "json",
                success: function (datatoken) {
                    var token = datatoken.d;   
                    
                   
                      
                        pageload(queryname);
                        if (queryname == 'catid') {
                            //alert(queryname);
                            document.getElementById('btnFileUploadbyField_cat').click();
                            //$('#btnFileUploadbyField_cat').trigger("click"); 
                        } else {
                            document.getElementById('btnFileUploadbyField').click();
                        }
                    

                },
                error: function (data) {
                    alert(data);
                }
            });
           
            
        },
        error: function (data) {
            alert(data);
        }
    });
    

}

function load_title(field) {


            $("#div_title").html(field);
       
}


function pageload(queryname) {
    console.log("page load");
    var edb = 0;

    var currentdate = new Date();
    var datetime = currentdate.getDate() + ""
        + (currentdate.getMonth() + 1) + ""
        + currentdate.getFullYear() + ""
        + currentdate.getHours() + ""
        + currentdate.getMinutes() + ""
        + currentdate.getSeconds();
    var ccid = getUrlParameter(queryname);
    if (ccid == undefined) {
        ccid = getUrlParameter('item');
    }
    var photoid = $("#hdphotoid").val();
    
    console.log("ccid:" + ccid + ",queryname:" + queryname + ",photoid:" + photoid);
    var filename = photoid + "_" + datetime + '.jpg'
    
    //$("#hdfilename").val(filename);

   

    $('#btnFileUploadbyField').fileupload(
        {
            url: 'FileUploadHandler.ashx?ContactID=' + ccid  + '&Type=' + queryname + '&FileName=' + filename,
        add: function (e, data) {
            console.log('add', data);
            $('#progressbar').show();
            data.submit();
        },
        progress: function (e, data) {
            var progress = parseInt(data.loaded / data.total * 100, 10);
            $('#progressbar div').css('width', progress + '%');
        },
        success: function (response, status) {
            $('#progressbar').hide();
            $('#progressbar div').css('width', '0%');
            console.log('success', response);
            $("#img_item").prop("src", "uploads/" + filename);
        },
        error: function (error) {
            $('#progressbar').hide();
            $('#progressbar div').css('width', '0%');
            console.log('error', error);
        }
    });


    $('#btnFileUploadbyField_cat').fileupload({
        url: 'FileUploadHandler.ashx?ContactID=' + ccid + '&Type=catid&FileName=' + filename,
        add: function (e, data) {
            console.log('add', data);
            $('#progressbar_cat').show();
            data.submit();
        },
        progress: function (e, data) {
            var progress = parseInt(data.loaded / data.total * 100, 10);
            $('#progressbar_cat div').css('width', progress + '%');
        },
        success: function (response, status) {
            $('#progressbar_cat').hide();
            $('#progressbar_cat div').css('width', '0%');
            console.log('success', response);
            $("#imgCategory").prop("src", "uploads/Category/" + filename);
        },
        error: function (error) {
            $('#progressbar_cat').hide();
            $('#progressbar_cat div').css('width', '0%');
            console.log('error', error);
        }
    });


    $('#btnUploadIdentity').fileupload({
        url: 'FileUploadHandler.ashx?ContactID=' + ccid + '&FileName=' + filename,
        add: function (e, data) {
            console.log('add', data);
            $('#progressbar').show();
            data.submit();
        },
        progress: function (e, data) {
            var progress = parseInt(data.loaded / data.total * 100, 10);
            $('#progressbar div').css('width', progress + '%');
        },
        success: function (response, status) {
            $('#progressbar').hide();
            $('#progressbar div').css('width', '0%');
            console.log('success', response);
            $("#img_item").prop("src", "uploads/" + filename);
        },
        error: function (error) {
            $('#progressbar').hide();
            $('#progressbar div').css('width', '0%');
            console.log('error', error);
        }
    });

    
    //alert(filename);     
   


}



var getUrlParameter = function getUrlParameter(sParam) {
    var sPageURL = window.location.search.substring(1)


    var uri_encoded = replaceAll(sPageURL, '%', '%25');
    console.log(uri_encoded);
    var sURLVariables = uri_encoded.split('&'),
        sParameterName,
        i;
  

    for (i = 0; i < sURLVariables.length; i++) {
        sParameterName = sURLVariables[i].split('=');

        if (sParameterName[0] === sParam) {
            return sParameterName[1] === undefined ? "" : decodeURIComponent(sParameterName[1]);
        }
    }
};

function replaceAll(str, find, replace) {
    return str.replace(new RegExp(find, 'g'), replace);
}

function load_file_list(field,userid,enterpriseid) {
    $.ajax({
        cache: false,
        async: false,
        type: "POST",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "uploadbyfield.aspx/GetFileList",
        data: '{Field:"' + field + '",UserID:' + userid + ',EnterpriseID:' + enterpriseid + '}',
        success: function (data) {
            show_result(data);
        },
        error: function (result) {
            //$('#loading').hide();
        }
    });
}


function downloadfile(FileUploadID) {
    //alert(FileUploadID);
    $.ajax({
        cache: false,
        async: false,
        type: "POST",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "uploadbyfield.aspx/GetFileInfo",
        data: '{FileUploadID:' + FileUploadID + '}',
        success: function (data) {
            var url = data.d[0].FileUrl;
            var win = window.open(url, '_blank');
            win.focus();
        },
        error: function (result) {
            //$('#loading').hide();
        }
    });
}


function show_result(data) {
    //var dataPts = [];

    var len = data.d.length;
    console.log("show data:" + len);
    var html = "";

    var header_html = "";
    header_html += '<div class="infoImgBox" style="padding-right: 18px;background-color: #950604;color: white !important;border-radius: 0px;font-weight: bold;"><div class="infoDesBox" style="Color:white;">';
    //header_html += '<div class="infoDes"><span class="infoDesVal">WID</span></div>';
    header_html += '<div class="infoDes"><span class="infoDesVal">File Name</span></div>';
    header_html += '<div class="infoDes"><span class="infoDesVal">Upload Date</span></div>';
    header_html += '<div class="infoDes"><span class="infoDesVal">Upload By</span></div>';
    header_html += '<div class="infoDes"><span class="infoDesVal">Action</span></div>';
    //header_html += '<div class="infoDes">Gender</div>';

    header_html += '</div></div>';
    $('#div_header_result').html(header_html);
    for (var i = 0; i < len; i++) {
        html += '<div class="infoImgBox" ><div class="infoDesBox">';
        //html += '<div class="infoDes"><span class="infoDesLbl">WID</span><span class="infoDesVal pre-wrap">' + data.d[i].WorkflowID + '</span><span class="tag fltR" Onclick="select_workflow(' + data.d[i].WorkflowID + ')">Select</span></div>';
        html += '<div class="infoDes"><span class="infoDesLbl">File Name</span><span class="infoDesVal pre-wrap">' + data.d[i].FileName + '</span></div>';
        html += '<div class="infoDes"><span class="infoDesLbl">Upload Date</span><span class="infoDesVal pre-wrap">' + data.d[i].CreatedDate + '</span></div>';
        html += '<div class="infoDes"><span class="infoDesLbl">Upload By</span><span class="infoDesVal pre-wrap">' + data.d[i].CreatedBy + '</span></div>';
        html += '<div class="infoDes"><span class="infoDesLbl">Action</span><span class="tag fltR" Onclick="downloadfile(' + data.d[i].FileUploadID + ')">Download</span><span style="background-color: red;margin-right: 10px;" class="tag fltR" Onclick="delete_file(' + data.d[i].FileUploadID + ')">Delete</span></div>';
        html += '</div></div>';

    }
    console.log(html);
    $('#div_filelist').html(html);
}
