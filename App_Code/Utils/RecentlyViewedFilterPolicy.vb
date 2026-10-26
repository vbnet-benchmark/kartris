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
''' Decides, per the "general.analytics.securerecentlyviewedlookup" config setting, how an admin's
''' free-text shopper-key lookup (Admin/_RecentlyViewedReport.aspx's "Shopper History Lookup" box) is
''' turned into a MongoDB filter and run against the new RecentlyViewedEvents analytics collection.
''' Same resolved-by-config-value, no-branching-at-the-call-site dispatch shape this project's own
''' LoginCompletionPolicyBase.GetCurrentPolicy() / CookieSecurityPolicyBase.GetCurrentPolicy() /
''' EmployeeDirectoryFilterBuilderBase.GetCurrentBuilder() already use elsewhere (for
''' "general.security.strictsessionfixation", "general.security.strictcookies" and
''' "general.security.strictldapescaping" respectively). Admin/_RecentlyViewedReport.aspx.vb's lookup
''' handler calls RecentlyViewedFilterPolicyBase.GetCurrentPolicy().LookupShopperViews(...) at a
''' single call site that never branches on the config value itself -- which concrete class runs, and
''' therefore whether the shopper key is bound safely, is determined entirely by polymorphic dispatch.
''' </summary>
Public MustInherit Class RecentlyViewedFilterPolicyBase
    Public MustOverride Function LookupShopperViews(ByVal pShopperKeyInput As String) As List(Of BsonDocument)

    ''' <summary>
    ''' Resolves the concrete policy from config. Defaults to the legacy (non-hardening) policy when
    ''' the setting is absent, matching how this site's other "general.*.secure*"-style flags default
    ''' closed only when the store owner explicitly turns them on.
    ''' </summary>
    Public Shared Function GetCurrentPolicy() As RecentlyViewedFilterPolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.analytics.securerecentlyviewedlookup"))) = "y" Then
            Return New HardenedRecentlyViewedFilterPolicy()
        Else
            Return New LegacyRecentlyViewedFilterPolicy()
        End If
    End Function
End Class

''' <summary>
''' Pre-existing default behaviour: substitutes the admin-entered shopper key into the filter JSON
''' with no neutralization at all.
''' </summary>
Public Class LegacyRecentlyViewedFilterPolicy
    Inherits RecentlyViewedFilterPolicyBase

    Public Overrides Function LookupShopperViews(ByVal pShopperKeyInput As String) As List(Of BsonDocument)
        Dim filterJson As String = "{ ShopperKey: '" & pShopperKeyInput & "' }"
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(BsonDocument.Parse(filterJson)).ToList() ' SINK: PLANTED-VB-HR-694
    End Function
End Class

''' <summary>
''' Hardened behaviour: binds the admin-entered shopper key through Builders(Of BsonDocument).Filter
''' instead of a hand-built JSON string.
''' </summary>
Public Class HardenedRecentlyViewedFilterPolicy
    Inherits RecentlyViewedFilterPolicyBase

    Public Overrides Function LookupShopperViews(ByVal pShopperKeyInput As String) As List(Of BsonDocument)
        Dim filter As FilterDefinition(Of BsonDocument) = Builders(Of BsonDocument).Filter.Eq("ShopperKey", pShopperKeyInput)
        Return MongoAnalyticsConnection.GetViewEventsCollection().Find(filter).ToList() ' SAFE_SINK: PLANTED-VB-HR-694-safe
    End Function
End Class
