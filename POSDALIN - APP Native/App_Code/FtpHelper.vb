Imports System.Net
Imports System.IO
Imports System.Drawing
Imports System.Configuration
'Imports CAMSOURCE.Cryption
Imports System

Public Class FtpHelper
    ''' <summary>
    ''' Upload file to ftp
    ''' </summary>
    ''' <param name="filePath">Local path, Ex: C:\My\Directory\filename.jpg</param>
    ''' <param name="ftpPath">Ftp path, Ex: ftp://localhost/abc/</param>
    ''' <param name="fileName">Local file name</param>
    ''' <param name="newFileName">New file name to upload to Ftp</param>
    ''' <remarks></remarks>
    Public Shared Sub UploadFile(ByVal filePath As String, ByVal ftpPath As String, ByVal fileName As String, ByVal newFileName As String)
        'If IsFileExists(String.Concat(ftpPath, fileName)) Then
        '    MessageBox.Show(fileName & " is already exist.")
        '    Return
        'End If
        'Upload file if not exist in ftp
        My.Computer.Network.UploadFile(filePath, ftpPath & newFileName, "slpos\$slpos", "Ssdbj4rpLqBf64iWj86soWMtlhsjG8xCEHh8vdFrx5uKX2yiFRrglisY2BBW")
        'Messages.ShowMessage("Saved")
    End Sub



    Public Shared Sub Upload222(ByVal filePath As String, ByVal ftpPath As String, ByVal fileName As String, ByVal newFileName As String)
        ' '' // Get the object used to communicate with the server.
        ' ''Dim request As FtpWebRequest = WebRequest.Create("ftp://www.contoso.com/test.htm")
        ''Dim request As FtpWebRequest = WebRequest.Create(ftpPath & "/" & newFileName)
        ''request.Method = WebRequestMethods.Ftp.UploadFile

        ' ''// This example assumes the FTP site uses anonymous logon.
        ''request.Credentials = New NetworkCredential("StudentCardUser", "pass$$$$")

        ' ''// Copy the contents of the file to the request stream.
        ' ''Dim sourceStream As StreamReader = New StreamReader("testfile.txt")

        ''Dim sourceStream As StreamReader = New StreamReader(filePath)
        ''Dim fileContents As Byte() = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd())
        ''sourceStream.Close()
        ''request.ContentLength = fileContents.Length

        ''Dim requestStream As Stream = request.GetRequestStream()
        ''requestStream.Write(fileContents, 0, fileContents.Length)
        ''requestStream.Close()


    End Sub

    Public Shared Function UploadFileFTP(ByVal FileByte() As Byte, ByVal FileName As String, ByVal ftpUserID As String, ByVal ftpPassword As String, ByVal ftpURL As String) As Boolean
        Dim retValue As Boolean = False
        Try
            Dim ftpFullPath As String = ftpURL + "/" + FileName
            Dim ftp As FtpWebRequest = FtpWebRequest.Create(New Uri(ftpFullPath))
            ftp.Credentials = New NetworkCredential(ftpUserID, ftpPassword)
            ftp.EnableSsl = True
            ftp.KeepAlive = True
            ftp.UseBinary = True
            ftp.Proxy = Nothing
            ftp.Method = WebRequestMethods.Ftp.UploadFile

            Dim ftpStream As Stream = ftp.GetRequestStream()
            ftpStream.Write(FileByte, 0, FileByte.Length)
            ftpStream.Close()
            ftpStream.Dispose()
            retValue = True
        Catch ex As Exception
            Throw ex
        End Try
        Return retValue
    End Function

    ''' <summary>
    ''' Check file exist in ftp or not
    ''' </summary>
    ''' <param name="fileUri">full path, Ex: ftp://localhost/abc/test.jpg</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function IsFileExists(ByVal fileUri As String) As Boolean
        Try
            Dim request As FtpWebRequest = GetFtpRequest(fileUri)
            request.Method = WebRequestMethods.Ftp.GetFileSize
            Dim response As FtpWebResponse = request.GetResponse()
            ' THE FILE EXISTS
        Catch ex As WebException
            Dim response As FtpWebResponse = ex.Response
            If FtpStatusCode.ActionNotTakenFileUnavailable = response.StatusCode Then
                ' THE FILE DOES NOT EXIST
                Return False
            End If
        End Try
        Return True
    End Function


    ''' <summary>
    ''' Constructs an FTP web request with the given filename
    ''' </summary>
    ''' <param name="strFtpFilePath"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Protected Shared Function GetFtpRequest(ByVal strFtpFilePath As String) As FtpWebRequest
        Try
            Dim url As String = strFtpFilePath
            Dim request As FtpWebRequest = TryCast(WebRequest.Create(url), FtpWebRequest)
            request.Credentials = New NetworkCredential("slpos\$slpos", "Ssdbj4rpLqBf64iWj86soWMtlhsjG8xCEHh8vdFrx5uKX2yiFRrglisY2BBW")
            request.Proxy = Nothing
            request.KeepAlive = False
            Return request
        Catch ex As Exception
            'Messages.ShowMessage("GetFtpRequest: " & ex.Message)
        End Try
        Return Nothing
    End Function

    Public Shared Function GetImageStreamFromFtp(ByVal strFtpFilePath As String) As Stream
        If Not strFtpFilePath = "" Then
            If IsFileExists(strFtpFilePath) Then
                Try
                    Dim request As FtpWebRequest = GetFtpRequest(strFtpFilePath)
                    request.Method = WebRequestMethods.Ftp.DownloadFile
                    request.UseBinary = True

                    Dim response As FtpWebResponse = request.GetResponse()
                    Dim imgStream As System.IO.Stream = response.GetResponseStream()
                    ' convert webstream to image
                    Return imgStream
                Catch ex As Exception
                    'Messages.ShowMessage("LoadImageFromFtp: " & ex.Message)
                End Try
            End If
        End If
        Return Nothing
    End Function

    Public Shared Function GetImageFromFtp(ByVal strFtpFilePath As String, ByVal TargetPath As FileInfo) As Image
        If Not strFtpFilePath = "" Then
            If IsFileExists(strFtpFilePath) Then
                Try
                    Dim request As FtpWebRequest = GetFtpRequest(strFtpFilePath)
                    request.Method = WebRequestMethods.Ftp.DownloadFile
                    request.UseBinary = True



                    Dim response As FtpWebResponse = request.GetResponse()
                    Dim imgStream As System.IO.Stream = response.GetResponseStream()


                   

                    ' convert webstream to image
                    Return Image.FromStream(imgStream)
                Catch ex As Exception
                    'Messages.ShowMessage("LoadImageFromFtp: " & ex.Message)
                End Try
            End If
        End If
        Return Nothing
    End Function


    '/// <summary>
    '  /// Amend an FTP path so that it always starts with /
    '  /// </summary>
    '  /// <param name="path">Path to adjust</param>
    '  /// <returns></returns>
    '  /// <remarks></remarks>
    Public Shared Function AdjustDir(ByVal path As String) As String
        'return ((path.StartsWith( "/")) ? "" : "/").ToString() &  path
        Return ((path.StartsWith("/"))).ToString() & path
    End Function
End Class
