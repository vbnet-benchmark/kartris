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
''' Backs the admin "Preview Promotion Banner Template" quick-preview feature on
''' _Promotions.aspx. Each promotion style (seasonal, clearance, flash sale) keeps its own
''' folder of banner template fragments; a concrete resolver knows only its own folder, while
''' the shared template-reading logic lives once in this base class.
''' </summary>
Public MustInherit Class PromotionTemplateResolverBase

    ''' <summary>
    ''' Base folder (relative to the site root) holding this promotion style's banner template
    ''' fragments.
    ''' </summary>
    Protected MustOverride Function GetTemplateFolder() As String

    ''' <summary>
    ''' Loads a banner template fragment by name for inline preview in the Admin Promotions list.
    ''' No validation -- the requested template file name comes straight from the admin's preview
    ''' link; only the style (which concrete resolver handles the request) is dispatched safely by
    ''' PromotionTemplateResolverFactory below.
    ''' </summary>
    Public Function ReadTemplate(ByVal strTemplateFile As String) As String
        Dim strTemplatePath As String = HttpContext.Current.Server.MapPath(GetTemplateFolder() & strTemplateFile)
        If Not File.Exists(strTemplatePath) Then Return Nothing
        Dim srTemplate As New StreamReader(strTemplatePath) ' SINK: PLANTED-VB-HR-504
        Dim strContent As String = srTemplate.ReadToEnd()
        srTemplate.Close()
        Return strContent
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="ReadTemplate"/> -- reduces the requested template file name
    ''' to its bare file-name component before resolving it against this style's template folder,
    ''' so it can never escape that folder.
    ''' </summary>
    Public Function ReadTemplateSafe(ByVal strTemplateFile As String) As String
        Dim strSafeFile As String = Path.GetFileName(strTemplateFile)
        Dim strTemplatePath As String = HttpContext.Current.Server.MapPath(GetTemplateFolder() & strSafeFile)
        If Not File.Exists(strTemplatePath) Then Return Nothing
        Dim srTemplate As New StreamReader(strTemplatePath) ' SAFE_SINK: PLANTED-VB-HR-504-safe
        Dim strContent As String = srTemplate.ReadToEnd()
        srTemplate.Close()
        Return strContent
    End Function

End Class

''' <summary>
''' Banner templates for the "Seasonal" promotion style.
''' </summary>
Public Class SeasonalPromotionTemplateResolver
    Inherits PromotionTemplateResolverBase

    Protected Overrides Function GetTemplateFolder() As String
        Return "~/Uploads/PromotionTemplates/Seasonal/"
    End Function
End Class

''' <summary>
''' Banner templates for the "Clearance" promotion style.
''' </summary>
Public Class ClearancePromotionTemplateResolver
    Inherits PromotionTemplateResolverBase

    Protected Overrides Function GetTemplateFolder() As String
        Return "~/Uploads/PromotionTemplates/Clearance/"
    End Function
End Class

''' <summary>
''' Banner templates for the "Flash Sale" promotion style.
''' </summary>
Public Class FlashSalePromotionTemplateResolver
    Inherits PromotionTemplateResolverBase

    Protected Overrides Function GetTemplateFolder() As String
        Return "~/Uploads/PromotionTemplates/FlashSale/"
    End Function
End Class

''' <summary>
''' Dispatches to the concrete <see cref="PromotionTemplateResolverBase"/> for a requested
''' promotion style key. Unrecognised keys fall back to the Seasonal resolver (the original
''' default style before Clearance/FlashSale were added).
''' </summary>
Public NotInheritable Class PromotionTemplateResolverFactory

    Public Shared Function GetResolver(ByVal strStyle As String) As PromotionTemplateResolverBase
        Select Case strStyle
            Case "clearance"
                Return New ClearancePromotionTemplateResolver()
            Case "flashsale"
                Return New FlashSalePromotionTemplateResolver()
            Case Else
                Return New SeasonalPromotionTemplateResolver()
        End Select
    End Function

End Class
