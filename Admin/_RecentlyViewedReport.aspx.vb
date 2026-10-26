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
Imports CkartrisFormatErrors
Imports MongoDB.Bson

''' <summary>
''' Back-office "Recently Viewed Products Report" tool -- ad-hoc reporting against the new
''' MongoDB-backed RecentlyViewedBLL analytics side-store, a companion to the existing relational
''' Admin/_Stats.aspx report. Three independent actions: a free-text trending-product-name search
''' (indirect plant instance), a shopper-history lookup gated entirely by polymorphic dispatch (type/
''' polymorphism-dependent plant instance), and a multi-product compare (diversity/batch plant
''' instance).
''' </summary>
Partial Class Admin_RecentlyViewedReport
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = "Recently Viewed Products Report" & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")
    End Sub

    Private Function FormatProductList(ByVal pDocs As List(Of BsonDocument)) As String
        If pDocs Is Nothing OrElse pDocs.Count = 0 Then Return "No matching products found."

        Dim names As New List(Of String)
        For Each doc As BsonDocument In pDocs
            names.Add(Server.HtmlEncode(If(doc.Contains("ProductName"), doc("ProductName").ToString(), "(unnamed)")))
        Next
        Return "Found " & pDocs.Count.ToString() & " matching view(s): " & String.Join(", ", names.ToArray())
    End Function

    ''' <summary>
    ''' Indirect plant instance: reads txtNameSearch.Text and passes it through
    ''' RecentlyViewedBLL.GetTrendingProductsByNameFilter, whose private same-class helper
    ''' BuildProductNameFilterDocument builds the raw filter -- one hop.
    ''' </summary>
    Protected Sub btnNameSearch_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim resultDocs As List(Of BsonDocument) = RecentlyViewedBLL.GetTrendingProductsByNameFilter(txtNameSearch.Text)
            lblNameSearchResult.Text = FormatProductList(resultDocs)
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblNameSearchResult.Text = "Trending-products search failed."
        End Try
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnNameSearch_Click"/>, via GetTrendingProductsByNameFilterSafe.
    ''' </summary>
    Protected Sub btnNameSearchSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim resultDocs As List(Of BsonDocument) = RecentlyViewedBLL.GetTrendingProductsByNameFilterSafe(txtNameSearch.Text)
            lblNameSearchResult.Text = FormatProductList(resultDocs)
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblNameSearchResult.Text = "Trending-products search failed."
        End Try
    End Sub

    ''' <summary>
    ''' Type/polymorphism-dependent plant instance: the shopper key from txtShopperLookup.Text flows
    ''' into a single, non-branching call site -- RecentlyViewedFilterPolicyBase.GetCurrentPolicy()
    ''' resolves which concrete policy (escaping or not) runs, entirely from the
    ''' "general.analytics.securerecentlyviewedlookup" config value.
    ''' </summary>
    Protected Sub btnShopperLookup_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim resultDocs As List(Of BsonDocument) = RecentlyViewedFilterPolicyBase.GetCurrentPolicy().LookupShopperViews(txtShopperLookup.Text)
            lblShopperLookupResult.Text = FormatProductList(resultDocs)
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblShopperLookupResult.Text = "Shopper history lookup failed."
        End Try
    End Sub

    ''' <summary>
    ''' Diversity (batch/loop) plant instance: reads txtCompareNames.Text (one product name per line)
    ''' and passes the whole list to RecentlyViewedBLL.GetTrendingViewCountsForProductNames, which runs
    ''' one query per line and aggregates the counts.
    ''' </summary>
    Protected Sub btnCompareNames_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim counts As Dictionary(Of String, Long) = RecentlyViewedBLL.GetTrendingViewCountsForProductNames(txtCompareNames.Text)
            lblCompareResult.Text = FormatCountsSummary(counts)
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblCompareResult.Text = "Compare-products action failed."
        End Try
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnCompareNames_Click"/>, via
    ''' GetTrendingViewCountsForProductNamesSafe.
    ''' </summary>
    Protected Sub btnCompareNamesSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim counts As Dictionary(Of String, Long) = RecentlyViewedBLL.GetTrendingViewCountsForProductNamesSafe(txtCompareNames.Text)
            lblCompareResult.Text = FormatCountsSummary(counts)
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblCompareResult.Text = "Compare-products action failed."
        End Try
    End Sub

    Private Function FormatCountsSummary(ByVal pCounts As Dictionary(Of String, Long)) As String
        If pCounts Is Nothing OrElse pCounts.Count = 0 Then Return "No product names supplied."

        Dim parts As New List(Of String)
        For Each kvp As KeyValuePair(Of String, Long) In pCounts
            parts.Add(Server.HtmlEncode(kvp.Key) & ": " & kvp.Value.ToString())
        Next
        Return String.Join(", ", parts.ToArray())
    End Function

End Class
