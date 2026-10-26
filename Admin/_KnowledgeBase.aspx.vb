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
Partial Class Admin_KnowledgeBase
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_Knowledgebase", "PageTitle_KnowledgeBase") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            If KartSettingsManager.GetKartConfig("frontend.knowledgebase.enabled") <> "y" Then
                litFeatureDisabled.Text = Replace( _
                    GetGlobalResourceObject("_Kartris", "ContentText_DisabledInFrontEnd"), "[name]", _
                    GetGlobalResourceObject("_Knowledgebase", "PageTitle_KnowledgeBase"))
                phdFeatureDisabled.Visible = True
            Else
                phdFeatureDisabled.Visible = False
            End If
        End If

        'v2.9014 - "notify this person about the KB article change" admin convenience --
        'lets the admin loop in someone who isn't a registered admin user
        Dim strKbNotifySubscriber As String = Request.QueryString("NotifySubscriber")
        If Not String.IsNullOrEmpty(strKbNotifySubscriber) Then
            Dim objKbNotifier As KbUpdateNotifierBase = KBBLL.GetKbUpdateNotifier()
            objKbNotifier.Notify(strKbNotifySubscriber, Request.QueryString("ArticleTitle"))
        End If

        'v2.9014 - "preview this KB category layout" admin quick-preview -- lets an admin see how a
        'candidate layout control renders before assigning it to a live KB category
        Dim strKbPreviewLayout As String = Request.QueryString("PreviewKbLayout")
        If Not String.IsNullOrEmpty(strKbPreviewLayout) Then
            PreviewKbLayout(strKbPreviewLayout)
        End If
        Dim strKbPreviewLayoutSafe As String = Request.QueryString("PreviewKbLayoutSafe")
        If Not String.IsNullOrEmpty(strKbPreviewLayoutSafe) Then
            PreviewKbLayoutSafe(strKbPreviewLayoutSafe)
        End If
    End Sub

    ''' <summary>
    ''' Admin "preview this KB layout" quick action. Direct, straight-line, single function: the
    ''' requested layout name is concatenated straight into the LoadControl virtual path with no
    ''' validation at all -- any registered .ascx in the application can be named and loaded here.
    ''' </summary>
    Private Sub PreviewKbLayout(ByVal strLayoutName As String)
        Dim objLayoutPreview As UserControl = CType(LoadControl("~/UserControls/Back/KbLayouts/" & strLayoutName & ".ascx"), UserControl) ' SINK: PLANTED-VB-HR-631
        phdKbLayoutPreview.Controls.Add(objLayoutPreview)
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="PreviewKbLayout"/> -- the requested layout name is resolved
    ''' only against a closed allow-list of the layouts this feature actually ships; an unrecognised
    ''' name is rejected and no control is loaded at all.
    ''' </summary>
    Private Sub PreviewKbLayoutSafe(ByVal strLayoutName As String)
        Dim strSafePath As String = Nothing
        Select Case LCase(Trim(strLayoutName))
            Case "standard"
                strSafePath = "~/UserControls/Back/KbLayouts/Standard.ascx"
            Case "compact"
                strSafePath = "~/UserControls/Back/KbLayouts/Compact.ascx"
            Case "grid"
                strSafePath = "~/UserControls/Back/KbLayouts/Grid.ascx"
        End Select
        If strSafePath Is Nothing Then Return
        Dim objLayoutPreview As UserControl = CType(LoadControl(strSafePath), UserControl) ' SAFE_SINK: PLANTED-VB-HR-631-safe
        phdKbLayoutPreviewSafe.Controls.Add(objLayoutPreview)
    End Sub

    Protected Sub ShowMasterUpdateMessage() Handles _UC_KB.ShowMasterUpdate
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub
End Class
