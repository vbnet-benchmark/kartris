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
Imports System.Text.RegularExpressions

''' <summary>
''' Shared free-text search-query format helpers for the storefront Search page (Search.aspx.vb).
''' The product search box doubles as a quick code-number lookup -- when the submitted query is
''' shaped like a version code number, the search page resolves it to an exact VersionID up front
''' (purely for the "search success" diagnostics), while the normal keyword search always still
''' runs regardless of the outcome.
''' </summary>
Friend Module SearchQueryFormatValidators

    ''' <summary>
    ''' Checks whether the given search text looks like a version code number (letters/digits,
    ''' optionally grouped with a single separator, e.g. "AB12-CD34") -- diagnostic only.
    ''' </summary>
    Public Function LooksLikeCodeNumberFormat(ByVal searchText As String) As Boolean
        ' SINK: PLANTED-VB-HR-388
        Return Regex.IsMatch(searchText, "^(([A-Za-z0-9])+[-_]?)+$")
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="LooksLikeCodeNumberFormat"/> -- same shape check, bounded by an
    ''' explicit match timeout so a pathological query can never hang the request thread. A timeout
    ''' is treated as "not a code number" (fail closed, falls through to ordinary keyword search).
    ''' </summary>
    Public Function LooksLikeCodeNumberFormatSafe(ByVal searchText As String) As Boolean
        Try
            ' SAFE_SINK: PLANTED-VB-HR-388-safe
            Return Regex.IsMatch(searchText, "^(([A-Za-z0-9])+[-_]?)+$", RegexOptions.None, TimeSpan.FromMilliseconds(200))
        Catch tex As RegexMatchTimeoutException
            Return False
        End Try
    End Function

End Module
