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

Imports System.Web

''' <summary>
''' Renders the free-text note a shopper can optionally attach when managing their product
''' comparison list on Compare.aspx (e.g. "wrong item, meant the XL version") as a small inline
''' confirmation banner. Which concrete renderer runs is picked, at runtime, by the
''' "noticeStyle" querystring value Compare.aspx.vb's DoCompare() reads -- the call site itself
''' makes no encode/no-encode decision; that is purely a property of which
''' ComparisonNoticeRendererBase subclass ComparisonNoticeRendererFactory.GetRenderer resolves
''' the request to.
'''
''' Deliberately a new hierarchy (plant-introduced for this round, not organic) -- surveyed
''' project-wide for a pre-existing MustInherit/MustOverride/Overridable/Interface hierarchy with
''' a natural tie to an HTML-rendering decision and found every candidate already claimed by an
''' earlier plant: PreferenceFormatStrategy (PLANTED-VB-HR-224), OrderAttachmentResolverBase (a
''' path-traversal plant), LoginCompletionPolicyBase (PLANTED-VB-HR-584),
''' AffiliateLinkDestinationBase (PLANTED-VB-HR-599), FeedDiagnosticCommandBase (the just-landed
''' CWE-78 round), CompositeDataBoundControlAdapter (PLANTED-VB-HR-544), KbUpdateNotifierBase
''' (PLANTED-VB-HR-204), and OrderWorkspaceService's inner record-type dispatch
''' (PLANTED-VB-HR-358/-359/-360). This hierarchy follows the same resolved-by-querystring/config
''' dispatch shape those already use.
''' </summary>
Public MustInherit Class ComparisonNoticeRendererBase
    Public MustOverride Function Render(ByVal strNote As String) As String
End Class

''' <summary>
''' Default/unknown style -- and the one Compare.aspx.vb actually reaches for any "noticeStyle"
''' it doesn't recognise, including none at all. Wraps the note directly into the banner markup
''' with no encoding.
''' </summary>
Public Class PlainComparisonNoticeRenderer
    Inherits ComparisonNoticeRendererBase

    Public Overrides Function Render(ByVal strNote As String) As String
        Return "<div class=""Kartris-CompareNotice"">" & strNote & "</div>" ' SINK: PLANTED-VB-HR-629
    End Function
End Class

''' <summary>
''' "encoded" style -- same banner markup, but the note is HTML-encoded first.
''' </summary>
Public Class EncodedComparisonNoticeRenderer
    Inherits ComparisonNoticeRendererBase

    Public Overrides Function Render(ByVal strNote As String) As String
        Return "<div class=""Kartris-CompareNotice"">" & HttpUtility.HtmlEncode(strNote) & "</div>" ' SAFE_SINK: PLANTED-VB-HR-629-safe
    End Function
End Class

''' <summary>
''' Resolves the concrete renderer for a given "noticeStyle" querystring value. Any value other
''' than "encoded" (including none at all) falls back to the unsafe default -- the same
''' "Case Else" fallback shape AffiliateLinkDestinationFactory and LoginCompletionPolicyBase
''' already use elsewhere in this project.
''' </summary>
Public Class ComparisonNoticeRendererFactory
    Public Shared Function GetRenderer(ByVal strStyle As String) As ComparisonNoticeRendererBase
        Select Case LCase(Trim(strStyle))
            Case "encoded"
                Return New EncodedComparisonNoticeRenderer()
            Case Else
                Return New PlainComparisonNoticeRenderer()
        End Select
    End Function
End Class
