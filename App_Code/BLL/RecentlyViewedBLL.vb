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
Imports MongoDB.Bson
Imports MongoDB.Driver

''' <summary>
''' "Recently viewed products" / shopper-analytics side-store, backed by the new MongoDB-based
''' MongoAnalyticsConnection. Complements the existing relational StatisticsBLL hit-tracking table
''' (which is global/aggregate, see StatisticsBLL.GetRecentlyViewedProducts) with a per-shopper,
''' flexibly-queryable event store: a personalized "recently viewed by you" widget
''' (RecentlyViewed.aspx) plus ad-hoc admin trending reports (Admin/_RecentlyViewedReport.aspx),
''' without requiring a schema migration on the main SQL Server database.
''' </summary>
Public Class RecentlyViewedBLL

    ''' <summary>
    ''' Records a product view for the current shopper, called from Product.aspx.vb's Page_Load
    ''' alongside the existing Session("RecentProducts") pipe-delimited tracking, so the same event
    ''' is also durable across sessions and query-able by the reports below. Every field is a typed
    ''' value assigned directly onto the BsonDocument -- no string here ever becomes part of a query,
    ''' so there is nothing to neutralize on this write path.
    ''' </summary>
    Public Shared Sub RecordProductView(ByVal pShopperKey As String, ByVal pProductID As Integer, ByVal pProductName As String, ByVal pCategoryID As Integer)
        If String.IsNullOrEmpty(pShopperKey) OrElse pProductID <= 0 Then Return

        Dim viewDoc As New BsonDocument From {
            {"ShopperKey", pShopperKey},
            {"ProductID", pProductID},
            {"ProductName", If(pProductName, "")},
            {"CategoryID", pCategoryID},
            {"ViewedUtc", DateTime.UtcNow}
        }
        MongoAnalyticsConnection.GetViewEventsCollection().InsertOne(viewDoc)
    End Sub

    ''' <summary>
    ''' Ordinary happy-path read: the shopper's own most recent views, newest first. Strongly-typed
    ''' filter -- the shopper key never becomes part of a query string.
    ''' </summary>
    Public Shared Function GetRecentlyViewedForShopper(ByVal pShopperKey As String) As List(Of BsonDocument)
        Dim filter As FilterDefinition(Of BsonDocument) = Builders(Of BsonDocument).Filter.Eq("ShopperKey", pShopperKey)
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(filter).SortByDescending(Function(d) d("ViewedUtc")).Limit(20).ToList()
    End Function

    ''' <summary>
    ''' Direct plant instance (PLANTED-VB-HR-691). Lets a shopper narrow their own recently-viewed
    ''' list by product-name text, reached from RecentlyViewed.aspx.vb via the new, dormant-by-default
    ''' ?filter= querystring key (confirmed new via grep: unused anywhere else in the organic app).
    ''' Straight-line, no helper, no branch -- the filter document is built and the query run in the
    ''' same statement block.
    ''' </summary>
    Public Shared Function SearchRecentlyViewedByProductName(ByVal pShopperKey As String, ByVal pNameFilter As String) As List(Of BsonDocument)
        Dim filterJson As String = "{ ShopperKey: '" & pShopperKey & "', ProductName: { $regex: '" & pNameFilter & "', $options: 'i' } }"
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(BsonDocument.Parse(filterJson)).ToList() ' SINK: PLANTED-VB-HR-691
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="SearchRecentlyViewedByProductName"/>, reached via RecentlyViewed.aspx's
    ''' ?filtersafe= key: same personalized-search shape, but both the shopper key and the name filter
    ''' are bound through Builders(Of BsonDocument).Filter instead of a hand-built JSON string, so
    ''' neither can escape its intended field/value position.
    ''' </summary>
    Public Shared Function SearchRecentlyViewedByProductNameSafe(ByVal pShopperKey As String, ByVal pNameFilter As String) As List(Of BsonDocument)
        Dim filter As FilterDefinition(Of BsonDocument) = Builders(Of BsonDocument).Filter.And(
            Builders(Of BsonDocument).Filter.Eq("ShopperKey", pShopperKey),
            Builders(Of BsonDocument).Filter.Regex("ProductName", New BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(pNameFilter), "i")))
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(filter).ToList() ' SAFE_SINK: PLANTED-VB-HR-691-safe
    End Function

    ''' <summary>
    ''' Indirect plant instance (PLANTED-VB-HR-692), one-hop same-class/same-file helper. Admin
    ''' "trending products" report (Admin/_RecentlyViewedReport.aspx) lets an admin free-text search
    ''' view-events by product name. The public entry point stays in this class and hands off to the
    ''' private BuildProductNameFilterDocument helper below before running the query -- one hop.
    ''' </summary>
    Public Shared Function GetTrendingProductsByNameFilter(ByVal pNameFilterText As String) As List(Of BsonDocument)
        Dim filterDoc As BsonDocument = BuildProductNameFilterDocument(pNameFilterText)
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(filterDoc).ToList() ' SINK: PLANTED-VB-HR-692
    End Function

    Private Shared Function BuildProductNameFilterDocument(ByVal pNameFilterText As String) As BsonDocument
        Return BsonDocument.Parse("{ ProductName: { $regex: '" & pNameFilterText & "', $options: 'i' } }")
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="GetTrendingProductsByNameFilter"/>, reached via the admin report's
    ''' "Search (Hardened)" button: the private helper below builds a FilterDefinition via
    ''' Builders(Of BsonDocument).Filter instead of raw JSON.
    ''' </summary>
    Public Shared Function GetTrendingProductsByNameFilterSafe(ByVal pNameFilterText As String) As List(Of BsonDocument)
        Dim filter As FilterDefinition(Of BsonDocument) = BuildProductNameFilterDocumentSafe(pNameFilterText)
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(filter).ToList() ' SAFE_SINK: PLANTED-VB-HR-692-safe
    End Function

    Private Shared Function BuildProductNameFilterDocumentSafe(ByVal pNameFilterText As String) As FilterDefinition(Of BsonDocument)
        Return Builders(Of BsonDocument).Filter.Regex("ProductName", New BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(pNameFilterText), "i"))
    End Function

    ''' <summary>
    ''' Interprocedural plant instance (PLANTED-VB-HR-693), first hop (cross-file: Product.aspx.vb -&gt;
    ''' this method -&gt; MongoAnalyticsConnection.ExecuteRawAnalyticsFilter, the sink -- 2 hops, 3
    ''' files total). Lets a shopper attach a free-text personal note/tag to one of their own
    ''' recently-viewed products, reached from Product.aspx.vb via the new, dormant-by-default
    ''' ?viewtag= querystring key. Combines the tag together with the existing ShopperKey/ProductID
    ''' into one filter so the tagged record can be re-read back on the same request.
    ''' </summary>
    Public Shared Function TagRecentView(ByVal pShopperKey As String, ByVal pProductID As Integer, ByVal pTagText As String) As List(Of BsonDocument)
        Dim filterJson As String = "{ ShopperKey: '" & pShopperKey & "', ProductID: " & pProductID.ToString() & ", Tag: '" & pTagText & "' }"
        Return MongoAnalyticsConnection.ExecuteRawAnalyticsFilter(filterJson)
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="TagRecentView"/>, reached via Product.aspx.vb's ?viewtagsafe= key:
    ''' hands the shopper key/product ID/tag text to
    ''' MongoAnalyticsConnection.ExecuteParameterizedTagUpdate, which binds every value through
    ''' Builders(Of BsonDocument) instead of a raw filter string.
    ''' </summary>
    Public Shared Function TagRecentViewSafe(ByVal pShopperKey As String, ByVal pProductID As Integer, ByVal pTagText As String) As List(Of BsonDocument)
        Return MongoAnalyticsConnection.ExecuteParameterizedTagUpdate(pShopperKey, pProductID, pTagText)
    End Function

    ''' <summary>
    ''' Diversity (batch/loop) plant instance (PLANTED-VB-HR-695). Lets an admin compare view counts
    ''' for several specific product names at once (Admin/_RecentlyViewedReport.aspx's "Compare
    ''' Products" box, a newline-separated list), running one query per product name and aggregating
    ''' the counts into the Dictionary the report page renders -- a genuinely consumed result, not a
    ''' dead-end loop variable.
    ''' </summary>
    Public Shared Function GetTrendingViewCountsForProductNames(ByVal pProductNamesList As String) As Dictionary(Of String, Long)
        Dim results As New Dictionary(Of String, Long)
        If String.IsNullOrEmpty(pProductNamesList) Then Return results

        Dim productNames() As String = pProductNamesList.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each strProductName As String In productNames
            Dim strTrimmedName As String = strProductName.Trim()
            If strTrimmedName.Length = 0 Then Continue For

            Dim filterJson As String = "{ ProductName: '" & strTrimmedName & "' }"
            Dim matchingViews As List(Of BsonDocument) = MongoAnalyticsConnection.GetViewEventsCollection().Find(BsonDocument.Parse(filterJson)).ToList() ' SINK: PLANTED-VB-HR-695
            results(strTrimmedName) = CLng(matchingViews.Count)
        Next

        Return results
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="GetTrendingViewCountsForProductNames"/>, reached via the admin
    ''' report's "Compare Products (Hardened)" button: same per-name loop shape, but each iteration's
    ''' filter is built via Builders(Of BsonDocument).Filter.Eq instead of raw JSON concatenation.
    ''' </summary>
    Public Shared Function GetTrendingViewCountsForProductNamesSafe(ByVal pProductNamesList As String) As Dictionary(Of String, Long)
        Dim results As New Dictionary(Of String, Long)
        If String.IsNullOrEmpty(pProductNamesList) Then Return results

        Dim productNames() As String = pProductNamesList.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For Each strProductName As String In productNames
            Dim strTrimmedName As String = strProductName.Trim()
            If strTrimmedName.Length = 0 Then Continue For

            Dim filter As FilterDefinition(Of BsonDocument) = Builders(Of BsonDocument).Filter.Eq("ProductName", strTrimmedName)
            Dim matchingViews As List(Of BsonDocument) = MongoAnalyticsConnection.GetViewEventsCollection().Find(filter).ToList() ' SAFE_SINK: PLANTED-VB-HR-695-safe
            results(strTrimmedName) = CLng(matchingViews.Count)
        Next

        Return results
    End Function

End Class
