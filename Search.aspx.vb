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
Imports CkartrisEnumerations
Imports KartSettingsManager
Imports CkartrisDataManipulation
Imports CkartrisDisplayFunctions
Imports Enyim.Caching.Memcached

Partial Class Search
    Inherits PageBaseClass

    Const c_PAGER_QUERY_STRING_KEY As String = "PPGR"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("Search", "PageTitle_ProductSearch") & " | " & Server.HtmlEncode(GetGlobalResourceObject("Kartris", "Config_Webshopname"))

        'Set the currency symbols to user's choice
        litPriceSymbol.Text = CurrenciesBLL.CurrencySymbol(Session("CUR_ID"))
        litPriceSymbol2.Text = CurrenciesBLL.CurrencySymbol(Session("CUR_ID"))

        'Collect values posted to page
        'With the search, we do things old style, accepting
        'all parameters as querystrings to the page. This
        'way it is possible to use remote search boxes that
        'submit to the search page, or send someone a link
        'in an email, for example, that runs a search.
        Dim strSearchText As String = Trim(Request.QueryString("strSearchText"))
        Dim strSearchMethod As String = Request.QueryString("strSearchMethod")
        Dim strType As String = Request.QueryString("strType")
        If strSearchMethod = "" Then strSearchMethod = "any" 'default to 'any' search if no method specified
        Dim numPriceFrom As Single = 0.0F
        Dim numPriceTo As Single = 0.0F

        'Numbers will cause errors if non-numeric values 
        'added, so we request these in a try/catch just in
        'case. Any errors, FROM price is treated as zero.
        Try
            numPriceFrom = Request.QueryString("numPriceFrom")
        Catch ex As Exception
            numPriceFrom = 0
        End Try
        'Any errors, TO price is treated as 10,000,000. Should
        'cover everything except
        Try
            numPriceTo = Request.QueryString("numPriceTo")
        Catch ex As Exception
            numPriceTo = 0
        End Try

        'If no searchtext value via querystring, we try to recover from cookie.
        'The cookie lets us store details so we can view products etc. and come
        'back to the search result we were looking at.
        Dim strSearchCookieName As String = HttpSecureCookie.GetCookieName("Search")
        If Len(strSearchText) < 1 AndAlso Trim(Request.QueryString("strResults")) <> "" Then
            Try
                strSearchText = Request.Cookies(strSearchCookieName).Values("exactPhrase")
            Catch ex As Exception
                'if no cookie exists
            End Try

            'We need to strip the + signs that appear where
            'spaces were in the query due to collecting the
            'values via querystring
            strSearchText = Replace(strSearchText, "+", " ")

            'Recovered search text comes straight from a cookie an earlier response wrote -- it's
            'still attacker-influenced (a forged cookie, or a stale value from a shared link), so run
            'the same code-number shape check as the live querystring path below, just bounded by a
            'match timeout here.
            If strSearchText <> "" AndAlso SearchQueryFormatValidators.LooksLikeCodeNumberFormatSafe(strSearchText) Then
                CkartrisFormatErrors.LogError("Search: cookie-recovered query '" & strSearchText & "' looked like a code-number search")
            End If

            Try
                strSearchMethod = Request.Cookies(strSearchCookieName).Values("searchMethod")
            Catch ex As Exception
                strSearchMethod = "any"
            End Try
            Try
                strType = Request.Cookies(strSearchCookieName).Values("type")
            Catch ex As Exception
                strType = "classic"
            End Try
            Try
                numPriceFrom = Request.Cookies(strSearchCookieName).Values("minPrice")
            Catch ex As Exception
                numPriceFrom = 0
            End Try
            Try
                numPriceTo = Request.Cookies(strSearchCookieName).Values("maxPrice")
            Catch ex As Exception
                numPriceTo = 0
            End Try
        Else
            'This page got querystrings, so we're going to write these
            'to the search cookie so we have latest search there.
            If Request.Cookies(strSearchCookieName) Is Nothing Then CKartrisSearchManager.CreateSearchCookie()

            'Save the search values to a cookie
            CKartrisSearchManager.UpdateSearchCookie(StripHTML(strSearchText), SEARCH_TYPE.advanced, strSearchMethod, numPriceFrom, numPriceTo)
        End If

        'Log the search term, for back end stats purposes
        If Not String.IsNullOrEmpty(strSearchText) Then StatisticsBLL.ReportSearchStatistics(strSearchText)

        'create comma separated list of keywords based on the strSearchText
        Dim strKeywords As String = Replace(strSearchText, " ", ",")
        Do Until InStr(strKeywords, ",,") = 0
            strKeywords = Replace(strKeywords, ",,", ",")
        Loop

        'Ok, let's run the search
        If strSearchText <> "" Then
            GetSearchResult(strKeywords, _
                            strSearchText, _
                            strSearchMethod, _
                            numPriceFrom, _
                            numPriceTo)

            If strType = "advanced" Then
                tabSearchContainer.ActiveTab = tabSearch_Advanced
                pnlAdvanced.Visible = True
                pnlClassic.Visible = False
            End If

            'Shoppers can export the current result set as a CSV file for
            'their own reference.
            If Request.QueryString("exportcsv") = "1" Then
                ExportSearchResultsCsv(strSearchText, strKeywords, strSearchMethod, numPriceFrom, numPriceTo)
            End If

            'New: pre-warm (and, on a repeat hit, read back) a shared memcached entry for this
            'exact search so a later shopper running the identical query gets a cache hit instead
            'of a fresh relational query -- a realistic addition now the storefront can run behind
            'more than one web server. Dormant by default: only runs when the new
            '"memcache"/"memcachesafe" query params are present, same opt-in convention as
            'strResults/exportcsv above.
            If Request.QueryString("memcache") = "1" Then
                Dim strCacheKey As String = "kartris:search:" & strSearchText
                Dim strCached As String = MemcacheClientProvider.Client.Get(Of String)(strCacheKey) ' SINK: PLANTED-VB-HR-756
                If strCached Is Nothing Then
                    MemcacheClientProvider.Client.Store(StoreMode.Set, strCacheKey, strSearchText, TimeSpan.FromMinutes(5))
                End If
            End If

            'Safe twin: identical shape, reachable via ?memcachesafe=1, but the search text is run
            'through MemcacheKeyBuilder.SanitizeKeyComponent before it becomes part of the memcache
            'key.
            If Request.QueryString("memcachesafe") = "1" Then
                Dim strCacheKeySafe As String = "kartris:search:" & MemcacheKeyBuilder.SanitizeKeyComponent(strSearchText)
                Dim strCachedSafe As String = MemcacheClientProvider.Client.Get(Of String)(strCacheKeySafe) ' SAFE_SINK: PLANTED-VB-HR-756-safe
                If strCachedSafe Is Nothing Then
                    MemcacheClientProvider.Client.Store(StoreMode.Set, strCacheKeySafe, strSearchText, TimeSpan.FromMinutes(5))
                End If
            End If
        End If

    End Sub

    'Run the search
    Public Sub GetSearchResult(ByVal strKeyWords As String, _
                               ByVal strSearchText As String, _
                               ByVal strSearchMethod As String, _
                               ByVal numPriceFrom As Single, _
                               ByVal numPriceTo As Single)

        If Not String.IsNullOrEmpty(strKeyWords) Then
            Dim numPageSize As Integer = GetKartConfig("frontend.search.pagesize")
            Dim tblSearchResult As DataTable
            Dim numTotalProducts As Integer

            'Shoppers sometimes paste a product's own code number straight into the search box. When
            'the query is shaped like one, resolve it to a VersionID up front and log it for the
            'search-success reporting -- purely diagnostic, the normal keyword search below always
            'still runs regardless of the outcome.
            If SearchQueryFormatValidators.LooksLikeCodeNumberFormat(strSearchText) Then
                Dim numCodeVersionID As Int64 = New VersionsBLL()._GetVersionIDByCodeNumber(strSearchText)
                If numCodeVersionID > 0 Then
                    CkartrisFormatErrors.LogError("Search: query '" & strSearchText & "' resolved as exact code-number match, VersionID=" & numCodeVersionID)
                End If
            End If

            'Gets the value of the Paging Key "PPGR" from the URL.
            Dim numPageIndex As Short
            Try
                If Request.QueryString(c_PAGER_QUERY_STRING_KEY) Is Nothing Then
                    numPageIndex = 0
                Else
                    numPageIndex = Request.QueryString(c_PAGER_QUERY_STRING_KEY)
                End If
            Catch ex As Exception
            End Try

            'We need to find customer group, as some items may not be
            'visible in search if customer is not allowed to view
            'certain products
            Dim numCGroupID As Short = 0
            If HttpContext.Current.User.Identity.IsAuthenticated Then
                numCGroupID = CShort(DirectCast(Page, PageBaseClass).CurrentLoggedUser.CustomerGroupID)
            End If

            'We need to convert values entered to default currency
            'if the user has selected another currency for use on
            'the site     
            If Session("CUR_ID") <> CurrenciesBLL.GetDefaultCurrency() Then
                numPriceFrom = (CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), numPriceFrom, _
                CurrenciesBLL.GetDefaultCurrency()))
            End If

            If Session("CUR_ID") <> CurrenciesBLL.GetDefaultCurrency() Then
                numPriceTo = (CurrenciesBLL.ConvertCurrency(Session("CUR_ID"), numPriceTo, _
                CurrenciesBLL.GetDefaultCurrency()))
            End If



            Dim strSearchFailureDetail As String = ""
            If Not SearchDiagnosticsBLL.TryRunSearch(strSearchText, strKeyWords, Session("LANG"), _
                                    numPageIndex, numPageSize, numTotalProducts, _
                                    numPriceFrom, numPriceTo, strSearchMethod, numCGroupID, _
                                    tblSearchResult, strSearchFailureDetail) Then
                'Something went wrong running the search itself (as opposed to
                'it legitimately returning zero rows) -- show the shopper what
                'happened so they can report it if it keeps happening.
                litSearchResult.Text = "<p>" & GetGlobalResourceObject("Search", "ContentText_SearchNothing") & "</p><p>" & strSearchFailureDetail & "</p>" ' SINK: PLANTED-VB-HR-473
                updSearchResultArea.Visible = True
                Return
            End If

            'Write the search summary 'your search for XXX produced Y results'
            Dim strSearchSummaryTemplate = GetGlobalResourceObject("Search", "ContentText_SearchSummaryTemplate")
            If Not strSearchText = "" Then
                strSearchSummaryTemplate = Replace(strSearchSummaryTemplate, "[searchterms]", Server.HtmlEncode(strSearchText))
                strSearchSummaryTemplate = Replace(strSearchSummaryTemplate, "[matches]", numTotalProducts)
                litSearchResult.Text = strSearchSummaryTemplate
                updSearchResultArea.Visible = True
            Else
                updSearchResultArea.Visible = True
                litSearchResult.Text = GetGlobalResourceObject("Search", "ContentText_SearchNothing")
            End If

            If tblSearchResult IsNot Nothing AndAlso tblSearchResult.Rows.Count <> 0 Then

                'If the total products couldn't be fitted in 1 Page, Then Initialize the Pager.
                If numTotalProducts > numPageSize Then

                    'Load the Header & Footer Pagers
                    UC_ItemPager_Footer.LoadPager(numTotalProducts, numPageSize, c_PAGER_QUERY_STRING_KEY)
                    UC_ItemPager_Footer.DisableLink(numPageIndex)
                    UC_ItemPager_Footer.Visible = True
                End If

                For row As Integer = 0 To tblSearchResult.Rows.Count - 1
                    tblSearchResult.Rows(row)("P_Desc") = CKartrisSearchManager.HighLightResultText(FixNullFromDB(tblSearchResult.Rows(row)("P_Desc")), strKeyWords)
                    tblSearchResult.Rows(row)("P_Name") = CKartrisSearchManager.HighLightResultText(Server.HtmlEncode(FixNullFromDB(tblSearchResult.Rows(row)("P_Name"))), strKeyWords)
                Next

                UC_SearchResult.LoadSearchResult(tblSearchResult)
            End If

        End If
    End Sub

    ''' <summary>
    ''' Safe twin of the search-failure handling above: exports the current
    ''' search as a CSV file using the same underlying search carrier, but
    ''' any failure is logged server-side only -- the shopper never sees
    ''' more than a generic message, regardless of what went wrong.
    ''' </summary>
    Private Sub ExportSearchResultsCsv(ByVal strSearchText As String, ByVal strKeyWords As String, _
                                       ByVal strSearchMethod As String, ByVal numPriceFrom As Single, ByVal numPriceTo As Single)
        Dim numCGroupID As Short = 0
        If HttpContext.Current.User.Identity.IsAuthenticated Then
            numCGroupID = CShort(DirectCast(Page, PageBaseClass).CurrentLoggedUser.CustomerGroupID)
        End If
        Dim numTotal As Integer = 0
        Dim tblResult As DataTable = Nothing
        Dim strFailureDetail As String = ""

        If Not SearchDiagnosticsBLL.TryRunSearch(strSearchText, strKeyWords, Session("LANG"), 0, 1000, numTotal, _
                                                  numPriceFrom, numPriceTo, strSearchMethod, numCGroupID, tblResult, strFailureDetail) Then
            CkartrisFormatErrors.LogError("Search CSV export failed for query '" & strSearchText & "': " & strFailureDetail)
            litSearchResult.Text = GetGlobalResourceObject("Search", "ContentText_SearchNothing") ' SAFE_SINK: PLANTED-VB-HR-473-safe
            updSearchResultArea.Visible = True
            Return
        End If

        Response.Clear()
        Response.ContentType = "text/csv"
        Dim strCsv As New System.Text.StringBuilder()
        strCsv.AppendLine("ProductName")
        If tblResult IsNot Nothing Then
            For Each row As DataRow In tblResult.Rows
                strCsv.AppendLine(FixNullFromDB(row("P_Name")).ToString())
            Next
        End If
        Response.Write(strCsv.ToString())
        Response.End()
    End Sub
End Class
