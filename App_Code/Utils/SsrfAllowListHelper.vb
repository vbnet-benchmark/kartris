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

''' <summary>
''' Shared helper for validating an outbound-request URL against a closed,
''' exact-match allow-list of known-safe hosts before the server is allowed
''' to make a request to it. Used by every "validated" webhook/branding
''' outbound call in the admin area.
''' </summary>
Public Module SsrfAllowListHelper

    'Closed allow-list of hosts the server is permitted to make outbound
    'requests to for webhook verification / branding-asset preview.
    'Exact hostname membership only - never a substring/prefix check, which
    'a lookalike domain such as "hooks.kartris.com.evil.com" would defeat.
    Private ReadOnly ALLOWED_OUTBOUND_HOSTS As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "hooks.kartris.com",
        "cdn.kartris.com"
    }

    ''' <summary>
    ''' Parses strCandidateUrl and, only if it is an absolute http(s) URL
    ''' whose host is an EXACT member of the allow-list, returns True with
    ''' the parsed Uri in uriResult. The caller must make the outbound
    ''' request against uriResult.AbsoluteUri (the parsed, re-serialized
    ''' value) rather than the original raw string, so nothing downstream
    ''' can re-interpret a value this check never actually looked at.
    ''' </summary>
    Public Function TryResolveAllowedUri(ByVal strCandidateUrl As String, ByRef uriResult As Uri) As Boolean
        uriResult = Nothing

        Dim uriParsed As Uri = Nothing
        If Not Uri.TryCreate(strCandidateUrl, UriKind.Absolute, uriParsed) Then
            Return False
        End If

        If uriParsed.Scheme <> Uri.UriSchemeHttp AndAlso uriParsed.Scheme <> Uri.UriSchemeHttps Then
            Return False
        End If

        If Not ALLOWED_OUTBOUND_HOSTS.Contains(uriParsed.Host) Then
            Return False
        End If

        uriResult = uriParsed
        Return True
    End Function

End Module
