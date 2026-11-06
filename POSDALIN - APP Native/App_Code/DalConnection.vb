Imports Microsoft.VisualBasic
Imports System.Data.SqlClient

Public Class DalConnection

    Public Shared DBUser As String = "sladmin"
    Public Shared DBPassword As String = "Gamevb$92"
    Public Shared DBDatabase As String = "sl"
    Public Shared DBServer As String = "slpos.database.windows.net"

    'Public Shared DBUser As String = "db_ac9193_iosapp_admin"
    'Public Shared DBPassword As String = "Gamevb$92"
    'Public Shared DBDatabase As String = "db_ac9193_iosapp"
    'Public Shared DBServer As String = "sql9001.site4now.net"


    'Public Shared EDBConnectionString As String = "Data Source=" & DBServer & ";Initial Catalog=" & DBDatabase & ";User Id=" & DBUser & ";Password=" & DBPassword & ";"

    Public Shared MainConnectionString As String = "Data Source=" & DBServer & ";Initial Catalog=" & DBDatabase & ";User Id=" & DBUser & ";Password=" & DBPassword & ";"


    'Public Shared EDBConnectionString As String = "Data Source=LAPTOP-13PNEPPE\MSSQL;Initial Catalog=SL;User Id=sa;Password=Pass$1234;"
    'Public Shared EDBConnectionString As String = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SL;User Id=sa;Password=pass$$1234;"

    ' ''Public Shared EDBConnectionString As String = "Data Source=192.168.0.6\EDB;Initial Catalog=EDB;User Id=sa;Password=pass$1234;"




End Class


