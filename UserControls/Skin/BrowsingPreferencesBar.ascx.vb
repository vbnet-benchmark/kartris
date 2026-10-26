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
Imports System.Collections.Generic
Imports System.Web.UI
Imports System.Web.Script.Serialization
Imports CkartrisDisplayFunctions
Imports Kartris

''' <summary>
''' Sitewide "Quick Preferences" bar (included from every front-end page via
''' Skins/Kartris/Template.master). Lets a guest shopper save a handful of small
''' display/search/theme/favorites preferences into cookies and restore them again
''' on a later visit -- without needing an account -- by reusing LosFormatter /
''' ObjectStateFormatter the same way ASP.NET's own ViewState machinery does.
'''
''' Each preference bundle below has a "legacy" restore path (the original
''' implementation, which trusts the cookie's bytes unconditionally) and, for some
''' bundles, a parallel "signed"/JSON restore path added later once the lack of any
''' integrity check on the legacy path was noticed. Every restore path is reachable
''' both from its own dedicated link and from the shared "Restore All" action below.
''' </summary>
Partial Class UserControls_Skin_BrowsingPreferencesBar
    Inherits System.Web.UI.UserControl

    Private Const COOKIE_DISPLAY As String = "DisplayPrefs"
    Private Const COOKIE_DISPLAY_SAFE As String = "DisplayPrefsSigned"
    Private Const COOKIE_SORT As String = "SortPrefs"
    Private Const COOKIE_SORT_SAFE As String = "SortPrefsJson"
    Private Const COOKIE_FILTERS As String = "SearchFilterPrefs"
    Private Const COOKIE_FILTERS_SAFE As String = "SearchFilterPrefsSigned"
    Private Const COOKIE_THEME As String = "ThemePrefs"
    Private Const COOKIE_FAVORITES As String = "FavoritesBatch"
    Private Const COOKIE_FAVORITES_SAFE As String = "FavoritesBatchSafe"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        'Nothing auto-restores on first view -- the guest explicitly opts in via one
        'of the "Restore" links below, or restores everything at once via "Restore All".
    End Sub

#Region "Tier: direct -- PLANTED-VB-HR-221 / -221-safe (Display Mode)"

    Protected Sub lnkRestoreDisplay_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreDisplay.Click
        RestoreDisplayPreferencesInline()
    End Sub

    Private Sub RestoreDisplayPreferencesInline()
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_DISPLAY))
        If objCookie IsNot Nothing AndAlso Not String.IsNullOrEmpty(objCookie.Value) Then
            ' SINK: PLANTED-VB-HR-221
            ApplyDisplayPreferences(DirectCast(New LosFormatter().Deserialize(System.Web.HttpUtility.UrlDecode(objCookie.Value)), BrowsingPreferences))
        End If
    End Sub

    Protected Sub lnkRestoreDisplaySafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreDisplaySafe.Click
        RestoreDisplayPreferencesSafeInline()
    End Sub

    Private Sub RestoreDisplayPreferencesSafeInline()
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_DISPLAY_SAFE))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Exit Sub
        Dim arrParts() As String = System.Web.HttpUtility.UrlDecode(objCookie.Value).Split("|"c)
        If arrParts.Length <> 2 Then Exit Sub
        If Not PreferenceMacUtil.Verify(arrParts(0), arrParts(1)) Then Exit Sub
        ' SAFE_SINK: PLANTED-VB-HR-221-safe
        ApplyDisplayPreferences(DirectCast(New LosFormatter().Deserialize(arrParts(0)), BrowsingPreferences))
    End Sub

    Private Sub ApplyDisplayPreferences(ByVal objPrefs As BrowsingPreferences)
        If objPrefs Is Nothing Then Exit Sub
        litDisplayMode.Text = Server.HtmlEncode(objPrefs.DisplayMode)
    End Sub

#End Region

#Region "Tier: indirect -- PLANTED-VB-HR-222 / -222-safe (Sort Order)"

    Protected Sub lnkRestoreSort_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreSort.Click
        ApplySortPreferences(RestoreSortPreferences())
    End Sub

    ''' <summary>
    ''' One private helper hop away from its two callers (this control's own click
    ''' handler, and "Restore All" below) -- the actual Deserialize call lives here,
    ''' not inline in either caller.
    ''' </summary>
    Private Function RestoreSortPreferences() As BrowsingPreferences
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_SORT))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Return Nothing
        Dim strRaw As String = System.Web.HttpUtility.UrlDecode(objCookie.Value)
        ' SINK: PLANTED-VB-HR-222
        Return DirectCast(New ObjectStateFormatter().Deserialize(strRaw), BrowsingPreferences)
    End Function

    Protected Sub lnkRestoreSortSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreSortSafe.Click
        ApplySortPreferences(RestoreSortPreferencesSafe())
    End Sub

    Private Function RestoreSortPreferencesSafe() As BrowsingPreferences
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_SORT_SAFE))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Return Nothing
        Dim strJson As String = System.Web.HttpUtility.UrlDecode(objCookie.Value)
        ' SAFE_SINK: PLANTED-VB-HR-222-safe
        Return New JavaScriptSerializer().Deserialize(Of BrowsingPreferences)(strJson)
    End Function

    Private Sub ApplySortPreferences(ByVal objPrefs As BrowsingPreferences)
        If objPrefs Is Nothing Then Exit Sub
        litSortOrder.Text = Server.HtmlEncode(objPrefs.SortOrder)
    End Sub

#End Region

#Region "Tier: interprocedural (2-hop service) -- PLANTED-VB-HR-223 / -223-safe (Search Filters)"

    Protected Sub lnkRestoreSearchFilters_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreSearchFilters.Click
        ApplySearchFilters(PreferenceRestoreService.RestoreSearchFilters(GetCookieRaw(COOKIE_FILTERS)))
    End Sub

    Protected Sub lnkRestoreSearchFiltersSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreSearchFiltersSafe.Click
        ApplySearchFilters(PreferenceRestoreService.RestoreSearchFiltersSafe(GetCookieRaw(COOKIE_FILTERS_SAFE)))
    End Sub

    Private Sub ApplySearchFilters(ByVal objPrefs As BrowsingPreferences)
        If objPrefs Is Nothing Then Exit Sub
        litSearchFilters.Text = Server.HtmlEncode(objPrefs.SearchKeywords) & " (" & objPrefs.MinPrice & " - " & objPrefs.MaxPrice & ")"
    End Sub

#End Region

#Region "Tier: type/polymorphism dependent -- PLANTED-VB-HR-224 / -224-safe (Theme)"

    Protected Sub lnkRestoreTheme_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreTheme.Click
        RestoreThemeViaStrategy()
    End Sub

    Private Sub RestoreThemeViaStrategy()
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_THEME))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Exit Sub
        Dim strRaw As String = System.Web.HttpUtility.UrlDecode(objCookie.Value)
        Dim numSeparator As Integer = strRaw.IndexOf(":"c)
        If numSeparator < 0 Then Exit Sub
        Dim strFormatTag As String = strRaw.Substring(0, numSeparator)
        Dim strPayload As String = strRaw.Substring(numSeparator + 1)
        Dim objStrategy As PreferenceFormatStrategy = GetThemeStrategy(strFormatTag)
        ApplyThemePreferences(objStrategy.Restore(strPayload))
    End Sub

    ''' <summary>
    ''' Picks the strategy that matches the format tag the client's own ThemePrefs
    ''' cookie claims to use. Any tag other than the modern "signed" one --
    ''' including anything unrecognised -- falls back to the original, unsigned
    ''' LosFormatter strategy, so this single call site always runs through genuine
    ''' polymorphic dispatch: which override actually executes (and therefore
    ''' whether PLANTED-VB-HR-224 or PLANTED-VB-HR-224-safe fires) is decided purely
    ''' by strFormatTag, which the client fully controls.
    ''' </summary>
    Private Function GetThemeStrategy(ByVal strFormatTag As String) As PreferenceFormatStrategy
        If String.Equals(strFormatTag, "signed", StringComparison.OrdinalIgnoreCase) Then
            Return New SignedThemeFormatStrategy()
        End If
        Return New LegacyThemeFormatStrategy()
    End Function

    Private Sub ApplyThemePreferences(ByVal objPrefs As BrowsingPreferences)
        If objPrefs Is Nothing Then Exit Sub
        litTheme.Text = Server.HtmlEncode(objPrefs.ThemeName)
    End Sub

#End Region

#Region "Tier: loop/batch -- PLANTED-VB-HR-225 / -225-safe (Favorite Lists)"

    Protected Sub lnkRestoreFavorites_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreFavorites.Click
        RestoreFavoritesBatches()
    End Sub

    ''' <summary>
    ''' FavoritesBatch stores several independently-saved favorite-category lists
    ''' as a ";"-delimited list of chunks, each one its own LosFormatter payload --
    ''' a guest can have saved more than one "favorites" snapshot over time (e.g.
    ''' one per device). Every chunk is deserialized in turn.
    ''' </summary>
    Private Sub RestoreFavoritesBatches()
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_FAVORITES))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Exit Sub
        Dim arrChunks() As String = System.Web.HttpUtility.UrlDecode(objCookie.Value).Split(";"c)
        Dim objRestoredBatches As New List(Of BrowsingPreferences)
        For Each strChunk As String In arrChunks
            If String.IsNullOrEmpty(strChunk) Then Continue For
            ' SINK: PLANTED-VB-HR-225
            Dim objBatch As BrowsingPreferences = DirectCast(New LosFormatter().Deserialize(strChunk), BrowsingPreferences)
            If objBatch IsNot Nothing Then objRestoredBatches.Add(objBatch)
        Next
        ApplyFavoriteBatches(objRestoredBatches)
    End Sub

    Protected Sub lnkRestoreFavoritesSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreFavoritesSafe.Click
        RestoreFavoritesBatchesSafe()
    End Sub

    ''' <summary>
    ''' Safe counterpart: each chunk carries its own "payload|signature" pair, and
    ''' a chunk whose signature does not verify is skipped instead of deserialized.
    ''' </summary>
    Private Sub RestoreFavoritesBatchesSafe()
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(COOKIE_FAVORITES_SAFE))
        If objCookie Is Nothing OrElse String.IsNullOrEmpty(objCookie.Value) Then Exit Sub
        Dim arrChunks() As String = System.Web.HttpUtility.UrlDecode(objCookie.Value).Split(";"c)
        Dim objRestoredBatches As New List(Of BrowsingPreferences)
        For Each strChunk As String In arrChunks
            If String.IsNullOrEmpty(strChunk) Then Continue For
            Dim arrParts() As String = strChunk.Split("|"c)
            If arrParts.Length <> 2 Then Continue For
            If Not PreferenceMacUtil.Verify(arrParts(0), arrParts(1)) Then Continue For
            ' SAFE_SINK: PLANTED-VB-HR-225-safe
            Dim objBatch As BrowsingPreferences = DirectCast(New LosFormatter().Deserialize(arrParts(0)), BrowsingPreferences)
            If objBatch IsNot Nothing Then objRestoredBatches.Add(objBatch)
        Next
        ApplyFavoriteBatches(objRestoredBatches)
    End Sub

    Private Sub ApplyFavoriteBatches(ByVal objBatches As List(Of BrowsingPreferences))
        litFavoritesCount.Text = objBatches.Count.ToString()
    End Sub

#End Region

#Region "Restore All -- the second, independent entry point for every tier above"

    ''' <summary>
    ''' Convenience action that restores every saved preference bundle in one click.
    ''' Each call below reaches exactly the same restore path as that bundle's own
    ''' dedicated link, giving every PLANTED-VB-HR-22x / -22x-safe pair a second,
    ''' independent way to be reached.
    ''' </summary>
    Protected Sub lnkRestoreAll_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkRestoreAll.Click
        RestoreDisplayPreferencesInline()
        RestoreDisplayPreferencesSafeInline()
        ApplySortPreferences(RestoreSortPreferences())
        ApplySortPreferences(RestoreSortPreferencesSafe())
        ApplySearchFilters(PreferenceRestoreService.RestoreSearchFilters(GetCookieRaw(COOKIE_FILTERS)))
        ApplySearchFilters(PreferenceRestoreService.RestoreSearchFiltersSafe(GetCookieRaw(COOKIE_FILTERS_SAFE)))
        RestoreThemeViaStrategy()
        RestoreFavoritesBatches()
        RestoreFavoritesBatchesSafe()
    End Sub

#End Region

#Region "Save current -- producers only, not sinks, so the feature round-trips for real"

    ''' <summary>
    ''' Seeds every "legacy"-format cookie with a sample preference bundle, so the
    ''' restore links above have something realistic to restore. Not a sink: this
    ''' writes trusted, server-constructed data, it never reads anything back.
    ''' </summary>
    Protected Sub lnkSaveAll_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkSaveAll.Click
        Dim objDisplayPrefs As New BrowsingPreferences With {.DisplayMode = "list"}
        SetCookie(COOKIE_DISPLAY, SerializeWithLosFormatter(objDisplayPrefs))

        Dim objSortPrefs As New BrowsingPreferences With {.SortOrder = "price_asc"}
        SetCookie(COOKIE_SORT, SerializeWithObjectStateFormatter(objSortPrefs))

        Dim objFilterPrefs As New BrowsingPreferences With {.SearchKeywords = "garden chairs", .MinPrice = 10, .MaxPrice = 250}
        SetCookie(COOKIE_FILTERS, SerializeWithObjectStateFormatter(objFilterPrefs))

        Dim objThemePrefs As New BrowsingPreferences With {.ThemeName = "dark"}
        SetCookie(COOKIE_THEME, "legacy:" & SerializeWithLosFormatter(objThemePrefs))

        Dim objFavoritesA As New BrowsingPreferences With {.FavoriteCategoryIDs = New List(Of Integer) From {4, 9}}
        Dim objFavoritesB As New BrowsingPreferences With {.FavoriteCategoryIDs = New List(Of Integer) From {12}}
        SetCookie(COOKIE_FAVORITES, SerializeWithLosFormatter(objFavoritesA) & ";" & SerializeWithLosFormatter(objFavoritesB))
    End Sub

    ''' <summary>
    ''' Seeds every "signed"/safe-format cookie counterpart with the same kind of
    ''' sample data, MAC-protected (or JSON-encoded) as appropriate.
    ''' </summary>
    Protected Sub lnkSaveAllSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkSaveAllSafe.Click
        Dim objDisplayPrefs As New BrowsingPreferences With {.DisplayMode = "list"}
        Dim strDisplayPayload As String = SerializeWithLosFormatter(objDisplayPrefs)
        SetCookie(COOKIE_DISPLAY_SAFE, strDisplayPayload & "|" & PreferenceMacUtil.Sign(strDisplayPayload))

        Dim objSortPrefs As New BrowsingPreferences With {.SortOrder = "price_asc"}
        SetCookie(COOKIE_SORT_SAFE, New JavaScriptSerializer().Serialize(objSortPrefs))

        Dim objFilterPrefs As New BrowsingPreferences With {.SearchKeywords = "garden chairs", .MinPrice = 10, .MaxPrice = 250}
        Dim strFilterPayload As String = SerializeWithObjectStateFormatter(objFilterPrefs)
        SetCookie(COOKIE_FILTERS_SAFE, strFilterPayload & "|" & PreferenceMacUtil.Sign(strFilterPayload))

        Dim objThemePrefs As New BrowsingPreferences With {.ThemeName = "dark"}
        Dim strThemePayload As String = SerializeWithLosFormatter(objThemePrefs)
        SetCookie(COOKIE_THEME, "signed:" & strThemePayload & "|" & PreferenceMacUtil.Sign(strThemePayload))

        Dim objFavoritesA As New BrowsingPreferences With {.FavoriteCategoryIDs = New List(Of Integer) From {4, 9}}
        Dim strChunkA As String = SerializeWithLosFormatter(objFavoritesA)
        SetCookie(COOKIE_FAVORITES_SAFE, strChunkA & "|" & PreferenceMacUtil.Sign(strChunkA))
    End Sub

    Private Function SerializeWithLosFormatter(ByVal objPrefs As BrowsingPreferences) As String
        Dim objWriter As New IO.StringWriter()
        Dim objFormatter As New LosFormatter()
        objFormatter.Serialize(objWriter, objPrefs)
        Return objWriter.ToString()
    End Function

    Private Function SerializeWithObjectStateFormatter(ByVal objPrefs As BrowsingPreferences) As String
        Return New ObjectStateFormatter().Serialize(objPrefs)
    End Function

    Private Sub SetCookie(ByVal strCookieBaseName As String, ByVal strValue As String)
        Dim objCookie As New HttpCookie(HttpSecureCookie.GetCookieName(strCookieBaseName), System.Web.HttpUtility.UrlEncode(strValue))
        objCookie.Expires = NowOffset.AddDays(30)
        Response.Cookies.Add(objCookie)
    End Sub

    Private Function GetCookieRaw(ByVal strCookieBaseName As String) As String
        Dim objCookie As HttpCookie = Request.Cookies(HttpSecureCookie.GetCookieName(strCookieBaseName))
        If objCookie Is Nothing Then Return String.Empty
        Return System.Web.HttpUtility.UrlDecode(objCookie.Value)
    End Function

#End Region

End Class
