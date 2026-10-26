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

''' <summary>
''' Second hop of the interprocedural CWE-20 "Memcache Injection" plant instance
''' (PLANTED-VB-HR-758). Reached from CategoriesBLL.WarmListingCache / WarmListingCacheSafe (first
''' hop), themselves reached from Category.aspx.vb's new "catcache"/"catcachesafe" opt-in query
''' params (the call site) -- two hops, three files total between the untrusted source and the
''' actual memcache client call made here.
'''
''' Kept as its own class, one hop further from the page than CategoriesBLL itself, the same
''' separation-of-concerns convention this project already uses for e.g. MongoAnalyticsConnection
''' being split out from RecentlyViewedBLL.
''' </summary>
Public Class CatalogMemcacheService

    ''' <summary>
    ''' VULNERABLE: strRawListingHint is a free-text "listinghint" querystring value -- a brand-new,
    ''' dormant-by-default request parameter introduced for this feature, with no charset or length
    ''' restriction of any kind -- concatenated straight into the memcache key alongside the (purely
    ''' numeric, harmless) category id.
    ''' </summary>
    Public Shared Function ReadCategoryListingCache(ByVal intCategoryID As Integer, ByVal strRawListingHint As String) As String
        Dim strCacheKey As String = "kartris:catlisting:" & intCategoryID & ":" & strRawListingHint
        Return MemcacheClientProvider.Client.Get(Of String)(strCacheKey) ' SINK: PLANTED-VB-HR-758
    End Function

    ''' <summary>
    ''' SAFE: strRawListingHint is run through MemcacheKeyBuilder.SanitizeKeyComponent before it ever
    ''' becomes part of the memcache key, so it can never carry CR/LF or any other
    ''' protocol-significant byte, regardless of what the request originally supplied.
    ''' </summary>
    Public Shared Function ReadCategoryListingCacheSafe(ByVal intCategoryID As Integer, ByVal strRawListingHint As String) As String
        Dim strCacheKey As String = "kartris:catlisting:" & intCategoryID & ":" & MemcacheKeyBuilder.SanitizeKeyComponent(strRawListingHint)
        Return MemcacheClientProvider.Client.Get(Of String)(strCacheKey) ' SAFE_SINK: PLANTED-VB-HR-758-safe
    End Function

End Class
