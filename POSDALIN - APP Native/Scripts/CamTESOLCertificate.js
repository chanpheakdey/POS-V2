

function createBlob(base64string) {
    var BASE64_MARKER = ';base64,';
    if (base64string.indexOf(BASE64_MARKER) == -1) {
        var parts = base64string.split(',');
        var contentType = parts[0].split(':')[1];
        var raw = decodeURIComponent(parts[1]);
        return new Blob([raw], { type: contentType });
    }
    var parts = base64string.split(BASE64_MARKER);
    var contentType = parts[0].split(':')[1];
    var raw = window.atob(parts[1]);
    var rawLength = raw.length;

    var uInt8Array = new Uint8Array(rawLength);

    for (var i = 0; i < rawLength; ++i) {
        uInt8Array[i] = raw.charCodeAt(i);
    }

    return new Blob([uInt8Array], { type: contentType });
}


function getParameterByName(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, "\\$&");
    var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, " "));
}

function ConvertImageToPdf(ReferenceID, DateString, FileType) {

    $.ajax({
        type: "POST",
        url: "CamTESOLCertificate.aspx/ConvertImageToPdf",
        data: '{ReferenceID:"' + ReferenceID + '",DateString:"' + DateString + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            if (FileType == "PDF") {
                $('#div_certificate').css("background-image", "");
                $('#div_png_certificate').css("display", "none");
                //window.location = "Upload_s/Courses/" + pdfFileName + '?now=' + datetime;
                load_certificate(data.d, DateString);
            } else {
                //$('#div_png_certificate').css("max-width", "21cm");
                $('#div_png_certificate').css("width", "100%");
                $('#div_png_certificate').css("height", (141.43) + "%");
                //$('#div_png_certificate').css("background-image", "url(Upload_s/Courses/" + filename + ")");

                var image_name = data.d.replace(".pdf", ".jpg");
                $('#div_png_certificate').html("<img src='" + image_name + "?now=" + DateString + "' style='width:100%' />");
                $("#div_certificate").css("width", "100%");
                $("#div_certificate").css("height", (141.43) + "%");
            }
            console.log(data.d);
        },
        failure: function (response) {
            alert(response.d);
        },
        error: function (response) {
            alert(response.d);
        }
    });
}

function load_data() {
    console.log("load data");
    //var q_ContactID = getParameterByName('ContactID');
    var ReferenceID = getParameterByName('ReferenceID');

    $.ajax({
        type: "POST",
        url: "CamTESOLCertificate.aspx/LoadData",
        data: '{ReferenceID_Encript:"' + ReferenceID + '"}',
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (data) {
            $("#div_studentname").html(data.d.StudentName);
            $("#div_coursename").html(data.d.CourseName);
            $("#div_date").html("on " + data.d.ClassStartDate);
            $("#div_referenceid").html(data.d.ReferenceID);
            $("#div_certificate").css("opacity", "1");
           
            download_certificate();
        },
        failure: function (response) {
            alert(response.d);
        },
        error: function (response) {
            alert(response.d);
        }
    });
}

function redirect_tocertificate(pdfFileName,datetime) {
    window.location.href = pdfFileName + '?now=' + datetime;
}
function load_certificate(pdfFileName, datetime) {
    setTimeout(function () { redirect_tocertificate(pdfFileName, datetime); }, 1000);
}
function filter(node) {
    return (node.tagName !== 'i');
}

function toDataUrl(url, callback) {
    var xhr = new XMLHttpRequest();
    xhr.onload = function () {
        var reader = new FileReader();
        reader.onloadend = function () {
            callback(reader.result);
        }
        reader.readAsDataURL(xhr.response);
    };
    xhr.open('GET', url);
    xhr.responseType = 'blob';
    xhr.send();
}

function svgString2Image(svgString, width, height, format, callback) {
    // set default for format parameter
    format = format ? format : 'png';
    // SVG data URL from SVG string
    var svgData = svgString; //'data:image/svg+xml;base64,' + btoa(unescape(encodeURIComponent(svgString)));
    // create canvas in memory(not in DOM)
    var canvas = document.createElement('canvas');
    // get canvas context for drawing on canvas
    var context = canvas.getContext('2d');
    // set canvas size
    canvas.width = width;
    canvas.height = height;
    // create image in memory(not in DOM)
    var image = new Image();
    // later when image loads run this
    image.onload = function () { // async (happens later)
        // clear canvas
        context.clearRect(0, 0, width, height);
        // draw image with SVG data to canvas
        context.drawImage(image, 0, 0, width, height);
        // snapshot canvas as png
        var pngData = canvas.toDataURL('image/' + format);
        // pass png data URL to callback
        callback(pngData);
    }; // end async
    // start loading SVG data into in memory image
    image.src = svgData;
}


function save_invoice(divid) {

    var ReferenceID = getParameterByName("InvoiceNo");
    var node = document.getElementById(divid);

   
            domtoimage.toBlob(node)
                .then(function (blob) {
                    save_blob(blob, ReferenceID)
                });
      



        //domtoimage.toSvg(node, { filter: filter })
        //    .then(function (dataUrl) {
        //    /* do something */

        //        svgString2Image(dataUrl, 2400, 1700, 'png', /* callback that gets png data URL passed to it */function (pngData) {
        //            // pngData is base64 png string
        //            console.log(pngData);
        //            var blob = createBlob(pngData);
        //            var currentdate = new Date();
        //            var datetime = currentdate.getDate() + ""
        //                + (currentdate.getMonth() + 1) + ""
        //                + currentdate.getFullYear() + ""
        //                + currentdate.getHours() + ""
        //                + currentdate.getMinutes() + ""
        //                + currentdate.getSeconds();
        //            save_blob(blob, ReferenceID + "_" + datetime + ".png")
        //        });

        //       //document.body.appendChild(img)         

        //    });
   


    //domtoimage.toJpeg(document.getElementById('div_certificate'), {quality: 1.00 })
    //    .then(function (dataUrl) {
    //        //save_image(dataUrl, "abc.jpg");
    //        //save_blob("13", dataUrl);
    //        var ReferenceID = getParameterByName('ReferenceID');
    //        var FileType = getParameterByName('FileType');

    //        if (FileType == "PDF") {
    //            convert(dataUrl, ReferenceID);
    //        } else {
    //            var base64ImageContent = dataUrl.replace(/^data:image\/(png|jpg);base64,/, "");
    //            console.log(base64ImageContent);
    //            var blob = createBlob(base64ImageContent);
    //            var currentdate = new Date();
    //            var datetime = currentdate.getDate() + ""
    //                + (currentdate.getMonth() + 1) + ""
    //                + currentdate.getFullYear() + ""
    //                + currentdate.getHours() + ""
    //                + currentdate.getMinutes() + ""
    //                + currentdate.getSeconds();
    //            save_blob(blob, ReferenceID + "_" + datetime + ".jpg")
    //        }


    //        //var link = document.createElement('a');

    //        //link.download = '0013.jpeg';
    //        //link.href = dataUrl;

    //    }

    //    );


}


    function download_certificate() {
            
       
        var node = document.getElementById('div_png_certificate');

        //domtoimage.toSvg(node)
        //    .then(function (dataUrl) {
        //        var img = new Image();
        //        img.src = dataUrl;

        //        var canvas = document.createElement("canvas");
        //        var ctx = canvas.getContext("2d");

        //        canvas.width = 500;
        //        canvas.height = 300;

        //        ctx.drawImage(img, 0, 0);

           
        //        //document.body.appendChild('<img src="' + canvas.toDataURL("image/png") + '" />');
        //        //document.body.appendChild(img);
        //});

        var FileType = getParameterByName('FileType');
        var ReferenceID = getParameterByName('ReferenceID');

        if (FileType == "PDF") {
            console.log("generate pdf");
            domtoimage.toBlob(node)
                .then(function (blob) {
                    save_blob(blob, ReferenceID)


                });
           
        } else {
            if (FileType == "HTML") {

            } else {
                domtoimage.toBlob(node)
                    .then(function (blob) {
                        save_blob(blob, ReferenceID)
                    });
            }

           

            //domtoimage.toSvg(node, { filter: filter })
            //    .then(function (dataUrl) {
            //    /* do something */
                 
            //        svgString2Image(dataUrl, 2400, 1700, 'png', /* callback that gets png data URL passed to it */function (pngData) {
            //            // pngData is base64 png string
            //            console.log(pngData);
            //            var blob = createBlob(pngData);
            //            var currentdate = new Date();
            //            var datetime = currentdate.getDate() + ""
            //                + (currentdate.getMonth() + 1) + ""
            //                + currentdate.getFullYear() + ""
            //                + currentdate.getHours() + ""
            //                + currentdate.getMinutes() + ""
            //                + currentdate.getSeconds();
            //            save_blob(blob, ReferenceID + "_" + datetime + ".png")
            //        });

            //       //document.body.appendChild(img)         

            //    });
        }
       


            //domtoimage.toJpeg(document.getElementById('div_certificate'), {quality: 1.00 })
            //    .then(function (dataUrl) {
            //        //save_image(dataUrl, "abc.jpg");
            //        //save_blob("13", dataUrl);
            //        var ReferenceID = getParameterByName('ReferenceID');
            //        var FileType = getParameterByName('FileType');

            //        if (FileType == "PDF") {
            //            convert(dataUrl, ReferenceID);
            //        } else {
            //            var base64ImageContent = dataUrl.replace(/^data:image\/(png|jpg);base64,/, "");
            //            console.log(base64ImageContent);
            //            var blob = createBlob(base64ImageContent);
            //            var currentdate = new Date();
            //            var datetime = currentdate.getDate() + ""
            //                + (currentdate.getMonth() + 1) + ""
            //                + currentdate.getFullYear() + ""
            //                + currentdate.getHours() + ""
            //                + currentdate.getMinutes() + ""
            //                + currentdate.getSeconds();
            //            save_blob(blob, ReferenceID + "_" + datetime + ".jpg")
            //        }


            //        //var link = document.createElement('a');
                    
            //        //link.download = '0013.jpeg';
            //        //link.href = dataUrl;
                    
            //    }
                
            //    );

         
    }

//**blob to dataURL**
function blobToDataURL(blob, callback) {
    var a = new FileReader();
    a.onload = function (e) { callback(e.target.result); }
    a.readAsDataURL(blob);
}

        ////////////////////////




function save_blob(blob, ReferenceID) {

    var currentdate = new Date();
    var datetime = currentdate.getDate() + ""
        + (currentdate.getMonth() + 1) + ""
        + currentdate.getFullYear() + ""
        + currentdate.getHours() + ""
        + currentdate.getMinutes() + ""
        + currentdate.getSeconds();

    var formData = new FormData();
    formData.append('picture', blob);

    //var image_name = ReferenceID + datetime + '.jpg';
    $.ajax({
        type: "POST",
        url: "UploadPDF.ashx?ReferenceID=" + ReferenceID ,
        data: formData,
        processData: false,
        contentType: false,
        success: function (data) {
            /*var FileType = getParameterByName('FileType');*/
            //var pdf_name = ReferenceID + datetime + '.pdf'
            var ReferenceID = getParameterByName("InvoiceNo");
            var userid = getParameterByName("user");
            var backurl = getParameterByName("backurl");
            window.location = "printed.aspx?user=" + userid + "&InvoiceNo=" + ReferenceID + "&backurl=" + backurl;
            //if (FileType == "PDF") {   
                
            //    $("#div_certificate").html('<div style="padding: 30px; font-size: large; text-align: center;">Downloaded</div>')
            //    //ConvertImageToPdf("Upload_s/Certificate/" + image_name, "Upload_s/Certificate/" + pdf_name, FileType);
            //    ConvertImageToPdf(ReferenceID, datetime, FileType);
                
            //} else {              

            //    //ConvertImageToPdf("Upload_s/Certificate/" + image_name, "Upload_s/Certificate/" + pdf_name,FileType);                
            //    ConvertImageToPdf(ReferenceID, datetime, FileType);
            //    //$("#div_certificate").html("<img src='Upload_s/Courses/" + filename + "' style='width:100%' />")
            //}
        
        },
        error: function (data) {

        }
    });
    return false;

}