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
Imports System.Text

Partial Class Admin_Stats
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = GetGlobalResourceObject("_Kartris", "BackMenu_Statistics") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            'v2.9014 - "Recent Search" dashboard widget -- re-renders using whichever result-card
            'template the admin last chose on the Admin Search page (see SearchResultTemplateService,
            'a different file/class; the value crossed a Session round trip with no sanitization at
            'the write site)
            SearchResultTemplateService.RenderLastSearchCardTemplate()
            SearchResultTemplateService.RenderLastSearchCardTemplateSafe()

            'v2.9014 - admin "preview a batch of dashboard widget panels" quick action -- renders
            'each named panel inline so an admin can review a candidate set of panels before adding
            'them to the live dashboard layout
            Dim strPreviewWidgetPanels As String = Request.QueryString("PreviewWidgetPanels")
            If Not String.IsNullOrEmpty(strPreviewWidgetPanels) Then
                PreviewWidgetPanels(strPreviewWidgetPanels)
            End If
            Dim strPreviewWidgetPanelsSafe As String = Request.QueryString("PreviewWidgetPanelsSafe")
            If Not String.IsNullOrEmpty(strPreviewWidgetPanelsSafe) Then
                PreviewWidgetPanelsSafe(strPreviewWidgetPanelsSafe)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Diversity tier: renders a comma-separated batch of dashboard widget panels in one request,
    ''' using the Server.Execute(path, writer) overload to capture each panel's rendered output into
    ''' a string instead of streaming it straight to the response -- gives sink-API diversity from
    ''' this round's other LoadControl/Server.Execute(path) instances. No validation of any panel
    ''' name at all.
    ''' </summary>
    Private Sub PreviewWidgetPanels(ByVal strPanelNamesCsv As String)
        Dim arrPanelNames As String() = Split(strPanelNamesCsv, ",")
        Dim sbdOutput As New StringBuilder()
        For Each strPanelName As String In arrPanelNames
            Dim objWriter As New StringWriter()
            HttpContext.Current.Server.Execute("~/Admin/DashboardPanels/" & Trim(strPanelName) & ".aspx", objWriter) ' SINK: PLANTED-VB-HR-635
            sbdOutput.Append(objWriter.ToString())
        Next
        litWidgetPanelsPreview.Text = sbdOutput.ToString()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="PreviewWidgetPanels"/> -- each requested panel name is
    ''' resolved only against a closed allow-list of the panels this feature actually ships; an
    ''' unrecognised name is skipped entirely rather than falling back to a best-guess path.
    ''' </summary>
    Private Sub PreviewWidgetPanelsSafe(ByVal strPanelNamesCsv As String)
        Dim dicAllowedPanels As New Dictionary(Of String, String) From {
            {"sales", "~/Admin/DashboardPanels/Sales.aspx"},
            {"traffic", "~/Admin/DashboardPanels/Traffic.aspx"},
            {"stock", "~/Admin/DashboardPanels/Stock.aspx"}
        }
        Dim arrPanelNames As String() = Split(strPanelNamesCsv, ",")
        Dim sbdOutput As New StringBuilder()
        For Each strPanelName As String In arrPanelNames
            Dim strSafePath As String = Nothing
            If dicAllowedPanels.TryGetValue(LCase(Trim(strPanelName)), strSafePath) Then
                Dim objWriter As New StringWriter()
                HttpContext.Current.Server.Execute(strSafePath, objWriter) ' SAFE_SINK: PLANTED-VB-HR-635-safe
                sbdOutput.Append(objWriter.ToString())
            End If
        Next
        litWidgetPanelsPreviewSafe.Text = sbdOutput.ToString()
    End Sub
End Class
