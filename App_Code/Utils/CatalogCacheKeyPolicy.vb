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
Imports Enyim.Caching.Memcached

''' <summary>
''' Genuine type/polymorphism-dependent CWE-20 "Memcache Injection" plant instance
''' (PLANTED-VB-HR-759). Which concrete policy Product.aspx.vb's new "memcachepoly=1" opt-in
''' resolves to -- and therefore whether the memcache key it builds is validated -- is decided
''' entirely inside GetCurrentPolicy(), from the real DB-backed "general.cache.keyvalidation" config
''' value read via KartSettingsManager.GetKartConfig. The call site itself (Product.aspx.vb) performs
''' no branching of its own; it only calls GetCurrentPolicy().StoreCachedProductView(...).
'''
''' Deliberately a NEW hierarchy, not a reuse of any pre-existing one. Project-wide survey (grep for
''' every "MustInherit Class"/"Interface" declaration under App_Code) found 29 such declarations:
''' _PageBaseClass, PageBaseClass, IKartrisWebAPI, SqlResourceProvider and QBWebConnectorSvc (the
''' already organically-secure QuickBooks SOAP base class, confirmed domain-irrelevant by an
''' earlier round's independent review) are domain-irrelevant infrastructure; the remaining 24
''' (AdminHelpTopicResolverBase, ComparisonNoticeRendererBase,
''' FeedDiagnosticCommandBase, KbUpdateNotifierBase, LoginCompletionPolicyBase,
''' OrderAttachmentResolverBase, OrderOwnedRecordRepositoryBase, PromotionTemplateResolverBase,
''' NotificationTokenGeneratorBase, CompositeDataBoundControlAdapter, CustomProductControl,
''' AffiliateLinkDestinationBase, CookieSecurityPolicyBase, DownloadStagingPolicyBase,
''' EmployeeDirectoryFilterBuilderBase, IOrderNotificationChannel, PartnerApiCertificatePolicyBase,
''' PaymentTerminalConnectivityPolicyBase, PreferenceFormatStrategy, PriceFeedParser,
''' RecentlyViewedFilterPolicyBase, RetryBackoffPolicyBase, SearchAuditLoggerBase,
''' SettlementCipherPolicyBase) were each cross-checked against Kartris.json by file-path grep and
''' confirmed already claimed by an earlier CWE round's plant -- nothing was free to reuse.
'''
''' Default-when-unset is the VULNERABLE policy (LegacyCatalogCacheKeyPolicy), matching this
''' project's own established "general.*" config convention (e.g.
''' DownloadStagingPolicyBase.GetCurrentPolicy, PLANTED-VB-HR-669) of defaulting to the
''' pre-existing, un-hardened behaviour until the store owner explicitly opts into the hardened one
''' by setting the flag.
''' </summary>
Public MustInherit Class CatalogCacheKeyPolicyBase

    Public MustOverride Sub StoreCachedProductView(ByVal strRawProductName As String, ByVal intProductID As Integer)

    Public Shared Function GetCurrentPolicy() As CatalogCacheKeyPolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.cache.keyvalidation"))) = "y" Then
            Return New ValidatedCatalogCacheKeyPolicy()
        Else
            Return New LegacyCatalogCacheKeyPolicy()
        End If
    End Function

End Class

''' <summary>
''' VULNERABLE (this is the unseeded/default-resolved concrete class): strRawProductName flows
''' straight through from Product.aspx.vb's new "memcachepoly=1" call -- this product's own stored,
''' free-text ProductName -- with no validation at all before it becomes part of the memcache key
''' handed to the client.
''' </summary>
Public Class LegacyCatalogCacheKeyPolicy
    Inherits CatalogCacheKeyPolicyBase

    Public Overrides Sub StoreCachedProductView(ByVal strRawProductName As String, ByVal intProductID As Integer)
        Dim strCacheKey As String = "kartris:productview:poly:" & strRawProductName
        MemcacheClientProvider.Client.Store(StoreMode.Set, strCacheKey, intProductID, TimeSpan.FromMinutes(10)) ' SINK: PLANTED-VB-HR-759
    End Sub

End Class

''' <summary>
''' SAFE: strRawProductName is run through MemcacheKeyBuilder.SanitizeKeyComponent before it ever
''' becomes part of the memcache key.
''' </summary>
Public Class ValidatedCatalogCacheKeyPolicy
    Inherits CatalogCacheKeyPolicyBase

    Public Overrides Sub StoreCachedProductView(ByVal strRawProductName As String, ByVal intProductID As Integer)
        Dim strCacheKey As String = "kartris:productview:poly:" & MemcacheKeyBuilder.SanitizeKeyComponent(strRawProductName)
        MemcacheClientProvider.Client.Store(StoreMode.Set, strCacheKey, intProductID, TimeSpan.FromMinutes(10)) ' SAFE_SINK: PLANTED-VB-HR-759-safe
    End Sub

End Class
