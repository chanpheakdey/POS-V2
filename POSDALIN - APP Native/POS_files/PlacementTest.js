
var fileslist = [];

function CreateElementInputFile(elementid) {

    var inputfile = document.createElement("input");
    inputfile.multiple = true;
    inputfile.type = "file";
    inputfile.id = "uploadfiles";
    inputfile.addEventListener("click", AddFilesToList(document.getElementById(elementid), fileslist));
    //inputfile.name = "photo[]";
    document.getElementById(elementid).appendChild(inputfile);
    console.log("CreateElementInputFile");
}

function AddFilesToList(files, fileslist) {
    for (var i = 0; i < files.length; i++)
    {
        fileslist.push(files[i]);
    }

    ShowFileList(fileslist)

}
function RemoveFileFromList(index, fileslist)
{
    fileslist.splice(index, 1);
    ShowFileList(fileslist)
}

function ShowFileList(files)
{
    var html = "<table>";

    for (var i = 0; i < files.length; i++)
    {
        html += "<tr>"
            + "<td>" + files[i].name + "</td>"
            + "<td><a href='' onclick='RemoveFileFromList(" + i + ",fileslist); return false;' style='color:red' >Remove</a></td>"
            + "</tr>";
    }
    html += "</table>";
    //document.getElementById("fileuploadinfo").innerHTML  = html;
}

function SubmitButtonOnclick(fileslist) {
    console.log(fileslist);
    var formData = new FormData();

    fileslist.forEach(file => formData.append(file.name, file))

    $.ajax({
        type: "POST",
        url: '/file/upload',
        data: formData,
        dataType: 'json',
        contentType: "multipart/form-data",
        processData: false,
        success: function (response) {
            console.log("sucess");
        },
        error: function (error) {
            console.log("error");
        }
    });
}
function assignfiles() {
    console.log(document.getElementById('filesPlacementTest').files);
    document.getElementById('FileUploadControl').files = document.getElementById('filesPlacementTest').files;
}

function UploadFile(fileUpload) {
    if (fileUpload.value != '') {
        //document.getElementById("UploadButton").click();
    }
}

function uploadComplete(sender) {
    //$get("<%=lblMesg.ClientID%>").innerHTML = "File Uploaded Successfully";
}

function uploadError(sender) {
    console.log(sender);
    //$get("<%=lblMesg.ClientID%>").innerHTML = "File upload failed.";
}

