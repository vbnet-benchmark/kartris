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
Imports CkartrisEnumerations

Partial Class Admin_AdminLogs
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_DBAdmin", "ContentText_AdminLogs") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            'v2.9014 - "show help for this log category" quick-help feature -- the category picks
            'which AdminHelpTopicResolverBase handles the request, dispatched by
            'AdminHelpTopicResolverFactory; the topic name itself is never validated by the base
            'resolver. Both the category and the topic name are read directly from the querystring
            'at this call site, never from a postback-bound control.
            Dim strLogHelpTopic As String = Request.QueryString("LogHelpTopic")
            If Not String.IsNullOrEmpty(strLogHelpTopic) Then
                Dim objResolver As AdminHelpTopicResolverBase = AdminHelpTopicResolverFactory.GetResolver(Request.QueryString("LogHelpCategory"))
                Dim objTopicControl As Control = objResolver.LoadTopicControl(Me, strLogHelpTopic)
                If objTopicControl IsNot Nothing Then phdLogHelpTopic.Controls.Add(objTopicControl)
            End If
            Dim strLogHelpTopicSafe As String = Request.QueryString("LogHelpTopicSafe")
            If Not String.IsNullOrEmpty(strLogHelpTopicSafe) Then
                Dim objResolverSafe As AdminHelpTopicResolverBase = AdminHelpTopicResolverFactory.GetResolver(Request.QueryString("LogHelpCategorySafe"))
                Dim objTopicControlSafe As Control = objResolverSafe.LoadTopicControlSafe(Me, strLogHelpTopicSafe)
                If objTopicControlSafe IsNot Nothing Then phdLogHelpTopicSafe.Controls.Add(objTopicControlSafe)
            End If
        End If

    End Sub

    Protected Sub _UC_AdminLog_AdminLogsUpdated() Handles _UC_AdminLog.AdminLogsUpdated
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub
End Class
