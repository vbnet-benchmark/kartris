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
''' Shared helper for validating a caller-supplied redirect destination before it is
''' used in a Response.Redirect call. Two independent, OWASP-recommended patterns are
''' offered -- IsSafeLocalPath (a same-origin / relative-path check) and
''' IsAllowedDestination (a closed allow-list of known-good destinations) -- callers
''' pick whichever fits their feature. Also used as the second hop of
''' CustomerAccount.aspx.vb's two-file post-login redirect-resolution chain (see
''' PageBaseClass.vb.ResolvePostLoginDestination/-Safe).
''' </summary>
Public Class RedirectTargetResolver

    ''' <summary>
    ''' The small, fixed set of destinations the "allow-list" pattern below ever
    ''' accepts -- nothing outside this set is ever returned, regardless of how a
    ''' caller spells it.
    ''' </summary>
    Private Shared ReadOnly AllowedDestinations As String() = New String() {
        "~/Customer.aspx",
        "~/CustomerAccount.aspx",
        "~/CustomerViewOrder.aspx",
        "~/CustomerInvoice.aspx",
        "~/Basket.aspx"
    }

    ''' <summary>
    ''' Pattern 2 (OWASP Unvalidated Redirects and Forwards Cheat Sheet): only permit
    ''' redirection to one of a small, hardcoded set of known-internal destinations.
    ''' Strictly stronger than a path-shape check, since there is no parsing or
    ''' normalization surface to get wrong at all.
    ''' </summary>
    Public Shared Function IsAllowedDestination(ByVal strTarget As String) As Boolean
        If String.IsNullOrEmpty(strTarget) Then Return False
        For Each strAllowed As String In AllowedDestinations
            If String.Equals(strTarget, strAllowed, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    ''' <summary>
    ''' Pattern 1 (same-origin / relative-path check): the value must be a genuine
    ''' application-relative path -- a single leading slash, never a protocol-relative
    ''' ("//host"), backslash-normalized ("/\host", "~/\host"), dot-segment-normalized
    ''' ("/..//host" -- RFC 3986 remove_dot_segments collapses this to "//host" in the
    ''' browser even though no literal "//host" appears up front), or control-character
    ''' smuggled variant of any of those.
    '''
    ''' Checks run in an order chosen to foreclose each normalization step a browser
    ''' could apply AFTER this check has already passed the raw value:
    ''' 1) Control characters first, because the WHATWG URL spec strips every ASCII
    '''    tab/CR/LF out of a URL before a browser does its own backslash-to-slash
    '''    normalization -- a value that only becomes "//host"/"/\host" after that
    '''    stripping would otherwise slip past a check that ran its other tests first.
    ''' 2) Any ".." segment next, because RFC 3986 dot-segment removal can turn a value
    '''    with no literal leading "//" (e.g. "/..//evil.com") into one that does, and
    '''    ASP.NET has already percent-decoded the querystring by the time this method
    '''    sees it, so a percent-encoded "%2e%2e" arrives here as a literal "..".
    ''' Only once both of those are ruled out is it safe to check the literal prefix.
    ''' </summary>
    Public Shared Function IsSafeLocalPath(ByVal strTarget As String) As Boolean
        If String.IsNullOrEmpty(strTarget) Then Return False

        For Each c As Char In strTarget
            If AscW(c) < 32 Then Return False
        Next

        If strTarget.Contains("..") Then Return False

        Dim strCandidate As String = strTarget
        If strCandidate.StartsWith("~/", StringComparison.Ordinal) Then
            strCandidate = strCandidate.Substring(1)
        ElseIf strCandidate.StartsWith("~", StringComparison.Ordinal) Then
            strCandidate = "/" & strCandidate.Substring(1)
        End If

        If Not strCandidate.StartsWith("/", StringComparison.Ordinal) Then Return False
        If strCandidate.StartsWith("//", StringComparison.Ordinal) Then Return False
        If strCandidate.StartsWith("/\", StringComparison.Ordinal) Then Return False

        Return True
    End Function

    ''' <summary>
    ''' Vulnerable hop (the second hop of the post-login redirect-resolution chain
    ''' started by PageBaseClass.vb.ResolvePostLoginDestination): builds an
    ''' app-relative URL from the raw value with no check at all.
    ''' </summary>
    Public Shared Function Resolve(ByVal strRawValue As String) As String
        Return "~" & strRawValue
    End Function

    ''' <summary>
    ''' Safe counterpart of Resolve: only returns the app-relative URL when it passes
    ''' IsSafeLocalPath; otherwise falls back to the site's own default landing page.
    ''' </summary>
    Public Shared Function ResolveSafe(ByVal strRawValue As String) As String
        Dim strCandidate As String = "~" & strRawValue
        If IsSafeLocalPath(strCandidate) Then
            Return strCandidate
        Else
            Return "~/Customer.aspx?action=home"
        End If
    End Function

End Class
