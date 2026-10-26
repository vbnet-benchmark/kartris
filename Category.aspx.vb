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
Imports CkartrisDataManipulation

Partial Class Category
    Inherits PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'We set a value to keep track of any trapped
        'error handled, this way, we can avoid throwing
        'a generic error on top of the handled one.
        Dim strErrorThrown As String = ""

        If Not Page.IsPostBack Then
            Dim strCurrentPath As String = Request.RawUrl.ToString.ToLower
            If Not (InStr(strCurrentPath, "/skins/") > 0 Or InStr(strCurrentPath, "/javascript/") > 0 Or InStr(strCurrentPath, "/images/") > 0) Then
                Try
                    Dim strActiveTab As String = Request.QueryString("T")
                    Dim intCategoryID As Integer = Request.QueryString("CategoryID")

                    Dim numLangID As Short = 1 'default to default language
                    Try
                        numLangID = CShort(Session("LANG"))
                    Catch ex As Exception
                        'numLangID = Request.QueryString("L")
                    End Try

                    UC_CategoryView.LoadCategory(intCategoryID, Session("LANG"))
                    If UC_CategoryView.IsCategoryExist OrElse intCategoryID = 0 Then
                        Dim objCategoriesBLL As New CategoriesBLL
                        Me.CanonicalTag = CkartrisBLL.WebShopURL & Mid(SiteMapHelper.CreateURL(SiteMapHelper.Page.CanonicalCategory, intCategoryID), 2)
                        Me.MetaDescription = objCategoriesBLL.GetMetaDescriptionByCategoryID(intCategoryID, numLangID)
                        Me.MetaKeywords = objCategoriesBLL.GetMetaKeywordsByCategoryID(intCategoryID, numLangID)

                        UC_SubCategoryView.LoadSubCategories(intCategoryID, Session("LANG"), UC_CategoryView.SubCategoryDisplayType)
                        UC_CategoryProductsView.LoadCategoryProducts(intCategoryID, Session("LANG"), UC_CategoryView.ProductsDisplayType, UC_CategoryView.ProductsDisplayOrder, UC_CategoryView.ProductsSortDirection)

                        If UC_SubCategoryView.TotalItems > 0 Then
                            litSubCatHeader.Text &= " <span class=""total"">(" & UC_SubCategoryView.TotalItems & ")</span>"
                            If strActiveTab = "S" Then tabContainer.ActiveTabIndex = 0
                        Else
                            tabSubCats.Enabled = False
                            tabSubCats.Visible = False
                        End If
                        If UC_CategoryProductsView.TotalItems > 0 Then
                            litProductsHeader.Text &= " <span class=""total"">(" & UC_CategoryProductsView.TotalItems & ")</span>"
                            If strActiveTab <> "S" Then tabContainer.ActiveTabIndex = 1
                        ElseIf Request.QueryString("f") <> "1" Then
                            tabProducts.Enabled = False
                            tabProducts.Visible = False
                        End If

                        'If we are filtering in powerpack, always use the products tab, even if zero products/results
                        If Request.QueryString("f") = 1 Then
                            tabContainer.ActiveTabIndex = 1
                        End If

                        If Not Page.IsPostBack AndAlso KartSettingsManager.GetKartConfig("general.products.hittracking") = "y" Then
                            StatisticsBLL.AddNewStatsRecord("C",
                                            GetIntegerQS("CategoryID"), GetIntegerQS("strParent"))
                        End If
                        'If intCategoryID = 0 Then UC_BreadCrumbTrail.SiteMapProvider = "BreadCrumbSitemap"
                    Else
                        'An item was called with correctly formatted URL, but
                        'the ID doesn't appear to pull out an item, so it's
                        'likely the item is no longer available.
                        strErrorThrown = "404"
                        Try
                            Server.ClearError()
                            HttpContext.Current.Server.Execute("~/404.aspx")
                        Catch exError As Exception

                        End Try
                    End If
                Catch ex As Exception
                    'Some other error occurred - it seems the ID of the item
                    'exists, but loading or displaying the item caused some
                    'other error.
                    If strErrorThrown = "" Then '404s won't get logged
                        CkartrisFormatErrors.ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
                        HttpContext.Current.Server.Execute("~/Error.aspx")
                    End If
                End Try
            End If
        End If

        'Lets a shopper or store admin run a quick health-check of the
        'cached files this category page depends on, one check at a time,
        'so a single broken cache file doesn't stop the others from being
        'reported.
        If Request.QueryString("diag") = "1" Then
            Dim intDiagCategoryID As Integer = Request.QueryString("CategoryID")
            Dim arrCacheFiles() As String = {"thumbs.cache", "feed.cache", "sitemap.cache"}
            Dim strDiagResults As New System.Text.StringBuilder()
            For Each strCacheFile As String In arrCacheFiles
                Try
                    Dim strCachePath As String = Server.MapPath("~/App_Data/CategoryCache/" & intDiagCategoryID & "/" & strCacheFile)
                    Dim strCacheContents As String = File.ReadAllText(strCachePath)
                    strDiagResults.AppendLine(strCacheFile & ": OK (" & strCacheContents.Length & " bytes)")
                Catch ex As Exception
                    strDiagResults.AppendLine(strCacheFile & ": FAILED - " & ex.Message) ' SINK: PLANTED-VB-HR-475
                End Try
            Next
            Response.Write("<pre>" & strDiagResults.ToString() & "</pre>")
            Response.End()
        End If

        'Safe twin: same per-cache-file health-check loop, but each
        'failure is logged server-side only and shown generically -- the
        'shopper just learns which named check didn't pass, never why.
        If Request.QueryString("diagsafe") = "1" Then
            Dim intDiagSafeCategoryID As Integer = Request.QueryString("CategoryID")
            Dim arrCacheFilesSafe() As String = {"thumbs.cache", "feed.cache", "sitemap.cache"}
            Dim strDiagResultsSafe As New System.Text.StringBuilder()
            For Each strCacheFile As String In arrCacheFilesSafe
                Try
                    Dim strCachePath As String = Server.MapPath("~/App_Data/CategoryCache/" & intDiagSafeCategoryID & "/" & strCacheFile)
                    Dim strCacheContents As String = File.ReadAllText(strCachePath)
                    strDiagResultsSafe.AppendLine(strCacheFile & ": OK (" & strCacheContents.Length & " bytes)")
                Catch ex As Exception
                    CkartrisFormatErrors.LogError("Category cache diagnostic failed for CategoryID=" & intDiagSafeCategoryID & ", file=" & strCacheFile & ": " & ex.ToString())
                    strDiagResultsSafe.AppendLine(strCacheFile & ": " & GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgGeneral")) ' SAFE_SINK: PLANTED-VB-HR-475-safe
                End Try
            Next
            Response.Write("<pre>" & strDiagResultsSafe.ToString() & "</pre>")
            Response.End()
        End If

        'New: shopper-suppliable "listing hint" (a sort/display-variant label) used to look up a
        'cached category-listing from the shared memcached tier, routed through CategoriesBLL ->
        'CatalogMemcacheService -- a genuine two-hop chain from this page to the actual memcache
        'client call. Dormant by default, same opt-in shape as diag/diagsafe above.
        If Request.QueryString("catcache") = "1" Then
            Dim intCacheCategoryID As Integer = Request.QueryString("CategoryID")
            Dim strListingHint As String = Request.QueryString("listinghint")
            CategoriesBLL.WarmListingCache(intCacheCategoryID, strListingHint)
        End If
        If Request.QueryString("catcachesafe") = "1" Then
            Dim intCacheCategoryIDSafe As Integer = Request.QueryString("CategoryID")
            Dim strListingHintSafe As String = Request.QueryString("listinghint")
            CategoriesBLL.WarmListingCacheSafe(intCacheCategoryIDSafe, strListingHintSafe)
        End If
    End Sub

    Protected Sub Page_LoadComplete(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LoadComplete
        If Session("NewBasketItem") = 1 Then
            Dim MiniBasket As Object = Master.FindControl("UC_MiniBasket")
            MiniBasket.LoadMiniBasket()
            Session("NewBasketItem") = 0
        End If
    End Sub
End Class

