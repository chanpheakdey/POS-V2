// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.



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

function CheckToken() {
    var token = readCookie("token");

    console.log(token);
    $.ajax({
        cache: false,
        async: false,
        type: "GET",
        dataType: "Json",
        contentType: "application/json; charset=utf-8",
        url: "SignInweb.aspx/Checktoken",
        data: '{"Token":"' + token + '"}',
        complete: function () {
            //$('#loading').hide();
        },
        success: function (data) {
            console.log("success data:" + data);
            var str = window.location.href;
            if (data == "Valid") {
                    window.location = "/header?now=";
                
            } else {
                eraseCookie("token");
                if (str.indexOf("/login") < 0) {
                    window.location = "/login?now=";
                }



            }



        },
        error: function (result) {
            $('#loading').hide();
        }
    });


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

function eraseCookie(name) {
    createCookie(name, "", -1);
}






function adduserlog(obj) {

        $.ajax({
            cache: false,
            async: false,
            type: "POST",
            contentType: "application/json",
            url: "https://fbuserapi.azurewebsites.net/api/AddUserLog",
            data: JSON.stringify(obj),
            success: function (data) {
                
            },
            error: function (result) {
                console.log("error");
                console.log(result);

            }
        });
 

}