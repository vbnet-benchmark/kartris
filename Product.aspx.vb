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
Imports Enyim.Caching.Memcached

Partial Class Product
    Inherits PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim strCurrentPath As String = Request.RawUrl.ToString.ToLower

        'We set a value to keep track of any trapped
        'error handled, this way, we can avoid throwing
        'a generic error on top of the handled one.
        Dim strErrorThrown As String = ""

        If Not (InStr(strCurrentPath, "/skins/") > 0 Or InStr(strCurrentPath, "/javascript/") > 0 Or InStr(strCurrentPath, "/images/") > 0) Then
            Try
                Dim intProductID As Integer = Request.QueryString("ProductID")
                Dim objProductsBLL As New ProductsBLL

                Dim numLangID As Short = 1 'default to default language
                Try
                    numLangID = CShort(Session("LANG"))
                Catch ex As Exception
                    'numLangID = Request.QueryString("L")
                End Try


                Try
                    UC_ProductView.LoadProduct(intProductID, numLangID)
                Catch ex As Exception
                    'Response.End()
                End Try

                If UC_ProductView.IsProductExist Then

                    If Not Page.IsPostBack Then
                        If KartSettingsManager.GetKartConfig("general.products.hittracking") = "y" Then StatisticsBLL.AddNewStatsRecord("P", GetIntegerQS("ProductID"), GetIntegerQS("strParent"))
                        Session("RecentProducts") = Session("RecentProducts") & GetIntegerQS("ProductID") & "~~~~" & _
                        Server.HtmlDecode(UC_ProductView.ProductName) & "||||"

                        'Above we use four hashes or pipes because these should not occur naturally in product names

                        'New MongoDB-backed recently-viewed analytics side-store (RecentlyViewedBLL):
                        'durable, per-shopper, query-able companion to the Session("RecentProducts")
                        'tracking just above. Best-effort -- never blocks a real product-page view on
                        'an analytics failure.
                        Try
                            Dim strShopperKey As String = If(User.Identity.IsAuthenticated, "cust-" & CurrentLoggedUser.ID.ToString(), "sess-" & Session.SessionID)
                            RecentlyViewedBLL.RecordProductView(strShopperKey, intProductID, UC_ProductView.ProductName, GetIntegerQS("strParent"))

                            'New, dormant-by-default "tag a recently-viewed item with a personal note"
                            'utility (confirmed new via grep: ?viewtag=/?viewtagsafe= are used nowhere
                            'else in the organic app).
                            Dim strViewTag As String = Request.QueryString("viewtag")
                            If Not String.IsNullOrEmpty(strViewTag) Then
                                RecentlyViewedBLL.TagRecentView(strShopperKey, intProductID, strViewTag)
                            End If
                            Dim strViewTagSafe As String = Request.QueryString("viewtagsafe")
                            If Not String.IsNullOrEmpty(strViewTagSafe) Then
                                RecentlyViewedBLL.TagRecentViewSafe(strShopperKey, intProductID, strViewTagSafe)
                            End If
                        Catch ex As Exception
                            'Best-effort analytics write -- never block a real product-page view
                        End Try

                        'New: memcached-backed "recently rendered" product-view cache, keyed off
                        'this product's own stored name -- lets a second request for the identical
                        'product short-circuit the normal ProductsBLL lookup. Reachable via the new
                        '"memcacheview"/"memcacheviewsafe" opt-in query params, the same
                        '"dormant by default" shape as checkstock/notifystock below.
                        If Request.QueryString("memcacheview") = "1" Then
                            Dim strViewCacheKey As String = BuildProductViewCacheKey(UC_ProductView.ProductName)
                            Dim objCachedView As Integer? = MemcacheClientProvider.Client.Get(Of Integer?)(strViewCacheKey) ' SINK: PLANTED-VB-HR-757
                            If Not objCachedView.HasValue Then
                                MemcacheClientProvider.Client.Store(StoreMode.Set, strViewCacheKey, intProductID, TimeSpan.FromMinutes(10))
                            End If
                        End If
                        If Request.QueryString("memcacheviewsafe") = "1" Then
                            Dim strViewCacheKeySafe As String = BuildProductViewCacheKeySafe(UC_ProductView.ProductName)
                            Dim objCachedViewSafe As Integer? = MemcacheClientProvider.Client.Get(Of Integer?)(strViewCacheKeySafe) ' SAFE_SINK: PLANTED-VB-HR-757-safe
                            If Not objCachedViewSafe.HasValue Then
                                MemcacheClientProvider.Client.Store(StoreMode.Set, strViewCacheKeySafe, intProductID, TimeSpan.FromMinutes(10))
                            End If
                        End If

                        'New: per-deployment-configurable product-view caching policy (brand-new
                        'CatalogCacheKeyPolicyBase hierarchy -- see that file for the project-wide
                        'hierarchy-reuse survey). Which concrete policy runs -- and therefore
                        'whether the resulting memcache key is validated -- is resolved entirely
                        'inside GetCurrentPolicy() from the "general.cache.keyvalidation" DB-backed
                        'config value; this call site performs no branching of its own.
                        If Request.QueryString("memcachepoly") = "1" Then
                            CatalogCacheKeyPolicyBase.GetCurrentPolicy().StoreCachedProductView(UC_ProductView.ProductName, intProductID)
                        End If

                        If intProductID > 0 Then
                            Me.CanonicalTag = CkartrisBLL.WebShopURL & Mid(SiteMapHelper.CreateURL(SiteMapHelper.Page.CanonicalProduct, intProductID), 2)
                            Me.MetaDescription = objProductsBLL.GetMetaDescriptionByProductID(intProductID, numLangID)
                            Me.MetaKeywords = objProductsBLL.GetMetaKeywordsByProductID(intProductID, numLangID)
                        End If
                    End If
                Else
                    'An item was called with correctly formatted URL, but
                    'the ID doesn't appear to pull out an item, so it's
                    'likely the item is no longer available.
                    strErrorThrown = "404"
                    Try
                        Server.ClearError()
                        HttpContext.Current.Server.Execute("~/404.aspx")
                    Catch ex As Exception

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

        'Lets a shopper manually re-check live stock availability direct
        'from the supplier feed for this product, bypassing the cached
        'figure shown on the page -- useful when a popular item shows as
        'in stock but has just sold out upstream.
        If Request.QueryString("checkstock") = "1" Then
            Dim intCheckProductID As Integer = Request.QueryString("ProductID")
            Try
                Dim strStockXml As String = SupplierStockFeedBLL.GetLiveStockLevelXml(intCheckProductID)
                Response.Write("<div class=""stockresult"">" & Server.HtmlEncode(strStockXml) & "</div>")
            Catch ex As Exception
                If TypeOf ex Is SqlException Then
                    Response.Write("<div class=""stockerror"">Database error checking stock: " & CType(ex, SqlException).Message & "</div>") ' SINK: PLANTED-VB-HR-474
                Else
                    'Only the SqlException branch above is the planted instance -- every other
                    'exception type is handled generically so there is exactly one unsanitized
                    'leak line to locate, matching this CWE's own ICMServer-round precedent
                    'of keeping non-marked branches safe.
                    CkartrisFormatErrors.LogError("Stock check failed for ProductID=" & intCheckProductID & ": " & ex.ToString())
                    Response.Write("<div class=""stockerror"">" & GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgGeneral") & "</div>")
                End If
            End Try
            Response.End()
        End If

        'Safe twin: same live-stock re-check, but routed through the
        '"Notify me when back in stock" feature instead -- any failure is
        'logged server-side only and the shopper sees one generic message
        'regardless of which exception type was actually raised.
        If Request.QueryString("notifystock") = "1" Then
            Dim intNotifyProductID As Integer = Request.QueryString("ProductID")
            Try
                Dim strStockXml As String = SupplierStockFeedBLL.GetLiveStockLevelXml(intNotifyProductID)
                Response.Write("<div class=""stockresult"">" & Server.HtmlEncode(strStockXml) & "</div>")
            Catch ex As Exception
                Dim strLogDetail As String = "Stock feed error for ProductID=" & intNotifyProductID & ": " & ex.ToString()
                If TypeOf ex Is SqlException Then
                    CkartrisFormatErrors.LogError("SQL - " & strLogDetail)
                ElseIf TypeOf ex Is IOException Then
                    CkartrisFormatErrors.LogError("IO - " & strLogDetail)
                Else
                    CkartrisFormatErrors.LogError(strLogDetail)
                End If
                Response.Write("<div class=""stockerror"">" & GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgGeneral") & "</div>") ' SAFE_SINK: PLANTED-VB-HR-474-safe
            End Try
            Response.End()
        End If
    End Sub

    Protected Sub Page_LoadComplete(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LoadComplete
        If Session("NewBasketItem") = 1 Then
            Dim MiniBasket As Object = Master.FindControl("UC_MiniBasket")
            MiniBasket.LoadMiniBasket()
            Session("NewBasketItem") = 0
        End If
    End Sub

    ''' <summary>
    ''' Indirect-tier plant instance (PLANTED-VB-HR-757), same-class private helper. Builds the raw
    ''' memcache key straight from this product's own stored ProductName -- free text maintained by
    ''' whoever manages the catalog, no charset restriction -- with no validation at all.
    ''' </summary>
    Private Function BuildProductViewCacheKey(ByVal strRawProductName As String) As String
        Return "kartris:productview:" & strRawProductName
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="BuildProductViewCacheKey"/>: the same product name is run through
    ''' MemcacheKeyBuilder.SanitizeKeyComponent first.
    ''' </summary>
    Private Function BuildProductViewCacheKeySafe(ByVal strRawProductName As String) As String
        Return "kartris:productview:" & MemcacheKeyBuilder.SanitizeKeyComponent(strRawProductName)
    End Function
End Class
