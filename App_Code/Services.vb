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
Imports System.Web
Imports System.Web.Services
Imports System.Web.Services.Protocols

Imports Kartris
Imports System.Data

<System.Web.Script.Services.ScriptService()> _
<WebService(Namespace:="http://tempuri.org/")> _
<WebServiceBinding(ConformsTo:=WsiProfiles.BasicProfile1_1)> _
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Public Class kartrisServices
    Inherits System.Web.Services.WebService

    <WebMethod(EnableSession:=True)>
    <Script.Services.ScriptMethod()>
    Public Function GetTopLevelCategories(ByVal prefixText As String, ByVal count As Integer) As String()
        Dim tblCategories As New DataTable
        Dim objCategoriesBLL As New CategoriesBLL
        tblCategories = objCategoriesBLL._SearchTopLevelCategoryByName(prefixText, Session("_LANG"))

        Dim items() As String = New String() {""}
        Dim counter As Integer = 0
        For Each row As DataRow In tblCategories.Rows
            ReDim Preserve items(counter)
            items(counter) = row("CAT_Name") + vbTab + "(" + CStr(row("CAT_ID")) + ")"
            counter += 1
        Next

        Return items
    End Function

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function GetCategories(ByVal prefixText As String, ByVal count As Integer) As String()
        Dim tblCategories As New DataTable
        Dim objCategoriesBLL As New CategoriesBLL
        tblCategories = objCategoriesBLL._SearchCategoryByName(prefixText, Session("_LANG"))

        Dim items() As String = New String() {""}
        Dim counter As Integer = 0
        For Each row As DataRow In tblCategories.Rows
            ReDim Preserve items(counter)
            items(counter) = row("CAT_Name") + vbTab + "(" + CStr(row("CAT_ID")) + ")"
            counter += 1
        Next

        Return items
    End Function

    <WebMethod(EnableSession:=True)>
    <Script.Services.ScriptMethod()>
    Public Function GetProducts(ByVal prefixText As String, ByVal count As Integer) As String()
        Dim tblProducts As New DataTable
        Dim objProductsBLL As New ProductsBLL
        tblProducts = objProductsBLL._SearchProductByName(prefixText, Session("_LANG"))

        Dim items() As String = New String() {""}
        Dim counter As Integer = 0
        Try
            For Each row As DataRow In tblProducts.Rows
                ReDim Preserve items(counter)
                items(counter) = row("P_Name") + vbTab + "(" + CStr(row("P_ID")) + ")"
                counter += 1
            Next

        Catch ex As Exception
            'just don't do anything
        End Try
        Return items
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function GetVersions(ByVal prefixText As String, ByVal count As Integer) As String()
        Dim tblVersions As New DataTable
        Dim objVersionsBLL As New VersionsBLL
        tblVersions = objVersionsBLL._SearchVersionByCode(prefixText)

        Dim items() As String = New String() {""}
        Dim counter As Integer = 0
        For Each row As DataRow In tblVersions.Rows
            ReDim Preserve items(counter)
            items(counter) = row("V_CodeNumber") + vbTab + "(" + CStr(row("V_ID")) + ")"
            counter += 1
        Next

        Return items
    End Function


    <WebMethod()>
    <Script.Services.ScriptMethod()>
    Public Function GetVersionsExcludeBaseCombinations(ByVal prefixText As String, ByVal count As Integer) As String()
        Dim tblVersions As New DataTable
        Dim objVersionsBLL As New VersionsBLL
        tblVersions = objVersionsBLL._SearchVersionByCodeExcludeBaseCombinations(prefixText)

        Dim items() As String = New String() {""}
        Dim counter As Integer = 0
        For Each row As DataRow In tblVersions.Rows
            ReDim Preserve items(counter)
            items(counter) = row("V_CodeNumber") + vbTab + "(" + CStr(row("V_ID")) + ")"
            counter += 1
        Next

        Return items
    End Function

    'Order-status webhook / branding-preview helpers - lets the admin
    'webhooks screen verify a merchant's registered webhook URL, preview a
    'branding/logo asset, and (re)send an order-status notification, all via
    'AJAX in addition to the plain postback buttons on Admin/_Webhooks.aspx.

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function TestWebhookUrl(ByVal strWebhookUrl As String) As String
        Return WebhookNotifier.TestWebhookReachable(strWebhookUrl)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function TestWebhookUrlSafe(ByVal strWebhookUrl As String) As String
        Return WebhookNotifier.TestWebhookReachable_Safe(strWebhookUrl)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function RegisterWebhook(ByVal strWebhookUrl As String) As Boolean
        Return WebhookRegistrationBLL.RegisterAndVerifyWebhook(strWebhookUrl)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function RegisterWebhookSafe(ByVal strWebhookUrl As String) As Boolean
        Return WebhookRegistrationBLL.RegisterAndVerifyWebhook_Safe(strWebhookUrl)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewLogoFromUrl(ByVal strImageUrl As String) As Integer
        Dim objRequest As New LogoPreviewRequest()
        objRequest.ImageUrl = strImageUrl
        Dim bytImage() As Byte = LogoPreviewService.FetchLogoPreview(objRequest)
        Return If(bytImage Is Nothing, 0, bytImage.Length)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewLogoFromUrlSafe(ByVal strImageUrl As String) As Integer
        Dim objRequest As New LogoPreviewRequest()
        objRequest.ImageUrl = strImageUrl
        Dim bytImage() As Byte = LogoPreviewService.FetchLogoPreview_Safe(objRequest)
        Return If(bytImage Is Nothing, 0, bytImage.Length)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewLogoFromParts(ByVal strBaseHost As String, ByVal strImagePath As String) As Integer
        Dim objJob As New LogoFetchJob()
        objJob.SetSource(strBaseHost, strImagePath)
        Dim bytImage() As Byte = objJob.FetchWithRetry()
        Return If(bytImage Is Nothing, 0, bytImage.Length)
    End Function

    <WebMethod()> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewLogoFromPartsSafe(ByVal strBaseHost As String, ByVal strImagePath As String) As Integer
        Dim objJob As New LogoFetchJob()
        objJob.SetSource_Safe(strBaseHost, strImagePath)
        Dim bytImage() As Byte = objJob.FetchWithRetry_Safe()
        Return If(bytImage Is Nothing, 0, bytImage.Length)
    End Function

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function SendOrderStatusNotification(ByVal intMerchantId As Integer, ByVal strNotifyUrl As String, ByVal strPayload As String) As Boolean
        Dim objChannel As IOrderNotificationChannel = OrderNotificationChannelFactory.GetChannel(intMerchantId)
        Return objChannel.SendOrderUpdate(strNotifyUrl, strPayload)
    End Function

    'Supplier catalog / price feed tools: the import screens preview a pasted or
    'uploaded supplier document via AJAX before anything is written to the database.

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewSupplierCatalog(ByVal strCatalogXml As String) As Integer
        Return SupplierCatalogImportBLL.PreviewCatalog(strCatalogXml).Rows.Count
    End Function

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewSupplierCatalogStrict(ByVal strCatalogXml As String) As Integer
        Return SupplierCatalogImportBLL.PreviewCatalogStrict(strCatalogXml).Rows.Count
    End Function

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewPriceFeed(ByVal strFeed As String) As Integer
        Dim objParser As PriceFeedParser = PriceFeedParser.ForContent(strFeed)
        Return objParser.ReadRows(strFeed).Count
    End Function

    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function PreviewPriceFeedStrict(ByVal strFeed As String) As Integer
        Dim objParser As PriceFeedParser = PriceFeedParser.ForContentStrict(strFeed)
        Return objParser.ReadRows(strFeed).Count
    End Function

    ''' <summary>
    ''' Checks a batch of supplier feed documents (one per supplier) in one call and
    ''' returns how many of them are well-formed. Supplier feeds reference their
    ''' published DTD for shared unit/currency entities, so DTDs are processed.
    ''' </summary>
    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function ValidateSupplierFeedBatch(ByVal arrFeeds As String()) As Integer
        Dim intValid As Integer = 0
        If arrFeeds Is Nothing Then Return 0
        Dim objSettings As New XmlReaderSettings()
        objSettings.DtdProcessing = DtdProcessing.Parse
        objSettings.XmlResolver = New XmlUrlResolver()
        For Each strFeed As String In arrFeeds
            If String.IsNullOrEmpty(strFeed) Then Continue For
            Try
                Dim xmlFeed As New XmlDocument()
                xmlFeed.Load(XmlReader.Create(New StringReader(strFeed), objSettings)) ' SINK: PLANTED-VB-HR-490
                If xmlFeed.DocumentElement IsNot Nothing Then intValid += 1
            Catch ex As XmlException
                'not well-formed, skip it
            End Try
        Next
        Return intValid
    End Function

    ''' <summary>
    ''' Same check for feeds that must be self-contained documents: DTDs are refused.
    ''' </summary>
    <WebMethod(EnableSession:=True)> _
    <Script.Services.ScriptMethod()> _
    Public Function ValidateSupplierFeedBatchStrict(ByVal arrFeeds As String()) As Integer
        Dim intValid As Integer = 0
        If arrFeeds Is Nothing Then Return 0
        Dim objSettings As New XmlReaderSettings()
        objSettings.DtdProcessing = DtdProcessing.Prohibit
        objSettings.XmlResolver = Nothing
        For Each strFeed As String In arrFeeds
            If String.IsNullOrEmpty(strFeed) Then Continue For
            Try
                Dim xmlFeed As New XmlDocument()
                xmlFeed.Load(XmlReader.Create(New StringReader(strFeed), objSettings)) ' SAFE_SINK: PLANTED-VB-HR-490-safe
                If xmlFeed.DocumentElement IsNot Nothing Then intValid += 1
            Catch ex As XmlException
                'not well-formed (or carries a DTD), skip it
            End Try
        Next
        Return intValid
    End Function

End Class
