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
''' Resolves the actual redirect URL for a referral-link "kind" named in
''' CustomerAffiliates.aspx.vb's RL_Kind/RL_KindSafe querystring dispatch. The call
''' site itself never branches on the kind -- AffiliateLinkDestinationFactory does
''' that, returning whichever concrete subclass applies, the same
''' resolved-by-factory dispatch shape CookieSecurityPolicyBase.GetCurrentPolicy()
''' and LoginCompletionPolicyBase.GetCurrentPolicy() already use elsewhere in this
''' project.
''' </summary>
Public MustInherit Class AffiliateLinkDestinationBase
    Public MustOverride Function ResolveUrl(ByVal strRawValue As String) As String
End Class

''' <summary>
''' A referral link pointing at a specific product -- the raw value is only ever used
''' as a numeric product ID, never as part of a URL path.
''' </summary>
Public Class ProductAffiliateLinkDestination
    Inherits AffiliateLinkDestinationBase

    Public Overrides Function ResolveUrl(ByVal strRawValue As String) As String
        Return "~/Product.aspx?P_ID=" & Val(strRawValue)
    End Function
End Class

''' <summary>
''' A referral link pointing at a specific category -- same shape as the product one.
''' </summary>
Public Class CategoryAffiliateLinkDestination
    Inherits AffiliateLinkDestinationBase

    Public Overrides Function ResolveUrl(ByVal strRawValue As String) As String
        Return "~/Category.aspx?C_ID=" & Val(strRawValue)
    End Function
End Class

''' <summary>
''' A referral link pointing somewhere this site doesn't have its own page type for
''' (e.g. a promotional landing page an affiliate built themselves) -- the only kind
''' that uses the raw value as a URL rather than as a numeric ID, and the one the
''' factory falls back to for any kind it doesn't otherwise recognise.
''' </summary>
Public Class CustomAffiliateLinkDestination
    Inherits AffiliateLinkDestinationBase

    Public Overrides Function ResolveUrl(ByVal strRawValue As String) As String
        ' This is the subclass CustomerAffiliates.aspx.vb's "RL_Kind" redirector
        ' actually reaches at runtime for any unrecognised kind -- see
        ' AffiliateLinkDestinationFactory.GetDestination's "Case Else" below.
        Return "~" & strRawValue
    End Function
End Class

''' <summary>
''' Safe twin of CustomAffiliateLinkDestination -- same "unrecognised kind" role, but
''' only ever returns a URL that passes RedirectTargetResolver.IsSafeLocalPath,
''' falling back to the site's own default landing page otherwise.
''' </summary>
Public Class CustomAffiliateLinkDestinationSafe
    Inherits AffiliateLinkDestinationBase

    Public Overrides Function ResolveUrl(ByVal strRawValue As String) As String
        Return RedirectTargetResolver.ResolveSafe(strRawValue)
    End Function
End Class

''' <summary>
''' Resolves the concrete AffiliateLinkDestinationBase for a given "kind" string.
''' Every call site that needs a destination goes through here rather than
''' branching on the kind itself -- the dispatch is fully hidden inside the factory.
''' </summary>
Public Class AffiliateLinkDestinationFactory
    Public Shared Function GetDestination(ByVal strKind As String) As AffiliateLinkDestinationBase
        Select Case LCase(Trim(strKind))
            Case "product"
                Return New ProductAffiliateLinkDestination()
            Case "category"
                Return New CategoryAffiliateLinkDestination()
            Case Else
                Return New CustomAffiliateLinkDestination()
        End Select
    End Function

    ''' <summary>
    ''' Safe counterpart of GetDestination -- identical dispatch, except the
    ''' "unrecognised kind" fallback resolves to the hardened subclass instead.
    ''' </summary>
    Public Shared Function GetDestinationSafe(ByVal strKind As String) As AffiliateLinkDestinationBase
        Select Case LCase(Trim(strKind))
            Case "product"
                Return New ProductAffiliateLinkDestination()
            Case "category"
                Return New CategoryAffiliateLinkDestination()
            Case Else
                Return New CustomAffiliateLinkDestinationSafe()
        End Select
    End Function
End Class
