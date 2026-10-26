'========================================================================
'Kartris - www.kartris.com
'Copyright 2024 CACTUSOFT

'GNU GENERAL PUBLIC LICENSE v2
'This program is free software distributed under the GPL without any
'warranty.
'www.gnu.org/licenses/gpl-2.0.html

'KARTRIS COMMERCIAL LICENSE
'If a valid license.config issued by Cactusoft is present, the KCL
'overrides the GPL v2.
'www.kartris.com/t-Kartris-Commercial-License.aspx
'========================================================================
Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Web.HttpContext
Imports System.Xml

Public NotInheritable Class SupplierStockFeedBLL

    ''' <summary>
    ''' Looks up the external supplier code for this product and pulls a
    ''' live stock-level snippet from that supplier's cached feed file. No
    ''' error handling or sanitization happens here -- callers choose how
    ''' (and whether) to report a failure to the end user.
    ''' </summary>
    Public Shared Function GetLiveStockLevelXml(ByVal pProductID As Integer) As String
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim strSupplierCode As String = ""

        Using sqlConn As New SqlConnection(strConnString)
            Dim cmd As SqlCommand = sqlConn.CreateCommand
            cmd.CommandText = "SELECT SupplierProductCode FROM tblSupplierProductMap WHERE P_ID = @ProductID"
            cmd.Parameters.AddWithValue("@ProductID", pProductID)
            sqlConn.Open()
            Dim objResult As Object = cmd.ExecuteScalar()
            If objResult IsNot Nothing Then strSupplierCode = objResult.ToString()
        End Using

        If String.IsNullOrEmpty(strSupplierCode) Then Return "<stock>unknown</stock>"

        Dim strFeedPath As String = HttpContext.Current.Server.MapPath("~/App_Data/SupplierFeeds/") & strSupplierCode & ".xml"
        Dim xmlFeed As New XmlDocument()
        xmlFeed.Load(strFeedPath)
        Return xmlFeed.SelectSingleNode("//stocklevel").OuterXml
    End Function

End Class
