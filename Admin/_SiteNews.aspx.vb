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

Partial Class Admin_SiteNews
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_News", "PageTitle_FrontNewsItems") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            If KartSettingsManager.GetKartConfig("frontend.navigationmenu.sitenews") <> "y" Then
                litFeatureDisabled.Text = Replace( _
                    GetGlobalResourceObject("_Kartris", "ContentText_DisabledInFrontEnd"), "[name]", _
                    GetGlobalResourceObject("_News", "PageTitle_FrontNewsItems"))
                phdFeatureDisabled.Visible = True
            Else
                phdFeatureDisabled.Visible = False
            End If
        End If

        'v2.9014 - "notify these people too" admin convenience when publishing a site
        'news article -- accepts several recipient addresses in one go
        Dim arrNotifyEmails As String() = Request.QueryString.GetValues("NotifySiteNewsEmails")
        If arrNotifyEmails IsNot Nothing AndAlso arrNotifyEmails.Length > 0 Then
            NewsBLL.NotifySiteNewsSubscribers(arrNotifyEmails, Request.QueryString("ArticleTitle"))
        End If
        Dim arrNotifyEmailsValidated As String() = Request.QueryString.GetValues("NotifySiteNewsEmailsValidated")
        If arrNotifyEmailsValidated IsNot Nothing AndAlso arrNotifyEmailsValidated.Length > 0 Then
            NewsBLL.NotifySiteNewsSubscribersValidated(arrNotifyEmailsValidated, Request.QueryString("ArticleTitle"))
        End If

        'v2.9014 - "preview this site-news display template" admin quick-preview -- lets an admin
        'see how a candidate template renders before assigning it to the live news feed
        Dim strNewsPreviewTemplate As String = Request.QueryString("PreviewNewsTemplate")
        If Not String.IsNullOrEmpty(strNewsPreviewTemplate) Then
            RenderNewsTemplatePreview(strNewsPreviewTemplate)
        End If
        Dim strNewsPreviewTemplateSafe As String = Request.QueryString("PreviewNewsTemplateSafe")
        If Not String.IsNullOrEmpty(strNewsPreviewTemplateSafe) Then
            RenderNewsTemplatePreviewSafe(strNewsPreviewTemplateSafe)
        End If
    End Sub

    ''' <summary>
    ''' Indirect entry point -- one hop through the same-class helper below.
    ''' </summary>
    Private Sub RenderNewsTemplatePreview(ByVal strTemplateName As String)
        BuildAndLoadNewsTemplate(strTemplateName)
    End Sub

    ''' <summary>
    ''' Builds the virtual path across several statements before loading it -- the requested
    ''' template name is never validated at any point.
    ''' </summary>
    Private Sub BuildAndLoadNewsTemplate(ByVal strTemplateName As String)
        Dim strPath As String = "~/UserControls/Back/NewsTemplates/"
        strPath = strPath & strTemplateName
        strPath = strPath & ".ascx"
        Dim objTemplate As UserControl = CType(LoadControl(strPath), UserControl) ' SINK: PLANTED-VB-HR-632
        phdNewsTemplatePreview.Controls.Add(objTemplate)
    End Sub

    Private Sub RenderNewsTemplatePreviewSafe(ByVal strTemplateName As String)
        BuildAndLoadNewsTemplateSafe(strTemplateName)
    End Sub

    ''' <summary>
    ''' Safe counterpart -- the template name is looked up in a closed Dictionary allow-list before
    ''' any path is built at all; an unrecognised key is skipped entirely rather than falling back
    ''' to a best-guess path.
    ''' </summary>
    Private Sub BuildAndLoadNewsTemplateSafe(ByVal strTemplateName As String)
        Dim dicAllowed As New Dictionary(Of String, String) From {
            {"headline", "~/UserControls/Back/NewsTemplates/Headline.ascx"},
            {"digest", "~/UserControls/Back/NewsTemplates/Digest.ascx"},
            {"full", "~/UserControls/Back/NewsTemplates/Full.ascx"}
        }
        Dim strPath As String = Nothing
        If Not dicAllowed.TryGetValue(LCase(Trim(strTemplateName)), strPath) Then Return
        Dim objTemplate As UserControl = CType(LoadControl(strPath), UserControl) ' SAFE_SINK: PLANTED-VB-HR-632-safe
        phdNewsTemplatePreviewSafe.Controls.Add(objTemplate)
    End Sub
End Class
