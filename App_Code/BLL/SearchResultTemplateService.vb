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
''' Backs the Admin Stats dashboard's "Recent Search" widget, which re-renders using whichever
''' quick-search result-card template the admin last chose on the Admin Search page -- a
''' cross-page, cross-request preference remembered in Session so returning to the dashboard
''' shows results in the same style the admin was just using.
''' </summary>
Public Class SearchResultTemplateService

    ''' <summary>
    ''' Interprocedural / cross-file Session round trip: the admin's last-chosen quick-search
    ''' result-card template, written with no validation by Admin_Search (a different
    ''' page/class, Admin/_Search.aspx.vb) on a previous request, is read back here and executed
    ''' directly -- the round trip through Session does not neutralize it.
    ''' </summary>
    Public Shared Sub RenderLastSearchCardTemplate()
        Dim strTemplate As String = CStr(HttpContext.Current.Session("AdminSearchCardTemplate"))
        If String.IsNullOrEmpty(strTemplate) Then Return
        HttpContext.Current.Server.Execute("~/Admin/SearchCardTemplates/" & strTemplate & ".aspx") ' SINK: PLANTED-VB-HR-633
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RenderLastSearchCardTemplate"/> -- the stored template key is
    ''' resolved only against a closed allow-list of the templates this feature actually ships; an
    ''' unrecognised key is skipped and nothing is executed.
    ''' </summary>
    Public Shared Sub RenderLastSearchCardTemplateSafe()
        Dim strTemplate As String = CStr(HttpContext.Current.Session("AdminSearchCardTemplateSafe"))
        If String.IsNullOrEmpty(strTemplate) Then Return
        Dim strPath As String = Nothing
        Select Case LCase(Trim(strTemplate))
            Case "cardbasic"
                strPath = "~/Admin/SearchCardTemplates/CardBasic.aspx"
            Case "cardrich"
                strPath = "~/Admin/SearchCardTemplates/CardRich.aspx"
        End Select
        If strPath Is Nothing Then Return
        HttpContext.Current.Server.Execute(strPath) ' SAFE_SINK: PLANTED-VB-HR-633-safe
    End Sub

End Class
