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
Imports System.Web.UI

''' <summary>
''' Backs the Admin Logs page's "show help for this log category" quick-help feature. Each log
''' category keeps its own folder of help-topic fragments; a concrete resolver knows only its own
''' folder, while the shared control-loading logic lives once in this base class.
''' </summary>
Public MustInherit Class AdminHelpTopicResolverBase

    ''' <summary>
    ''' Base folder (relative to the site root) holding this log category's help-topic fragments.
    ''' </summary>
    Protected MustOverride Function GetTopicFolder() As String

    ''' <summary>
    ''' Loads a help-topic fragment by name for inline display on the Admin Logs page. No
    ''' validation of the requested topic name -- only WHICH concrete resolver (and so which
    ''' folder) handles the request is dispatched safely, by AdminHelpTopicResolverFactory below.
    ''' </summary>
    Public Function LoadTopicControl(ByVal objHostControl As TemplateControl, ByVal strTopicName As String) As Control
        Return objHostControl.LoadControl(GetTopicFolder() & strTopicName & ".ascx") ' SINK: PLANTED-VB-HR-634
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="LoadTopicControl"/> -- the requested topic name is resolved
    ''' only against a closed allow-list of the topics this category actually ships; an
    ''' unrecognised name returns Nothing and no control is loaded at all.
    ''' </summary>
    Public Function LoadTopicControlSafe(ByVal objHostControl As TemplateControl, ByVal strTopicName As String) As Control
        Dim strSafeName As String = Nothing
        Select Case LCase(Trim(strTopicName))
            Case "overview"
                strSafeName = "Overview"
            Case "troubleshooting"
                strSafeName = "Troubleshooting"
        End Select
        If strSafeName Is Nothing Then Return Nothing
        Return objHostControl.LoadControl(GetTopicFolder() & strSafeName & ".ascx") ' SAFE_SINK: PLANTED-VB-HR-634-safe
    End Function

End Class

''' <summary>
''' Help topics for the Order Log category.
''' </summary>
Public Class OrderLogHelpTopicResolver
    Inherits AdminHelpTopicResolverBase

    Protected Overrides Function GetTopicFolder() As String
        Return "~/UserControls/Back/HelpTopics/OrderLog/"
    End Function
End Class

''' <summary>
''' Help topics for the Security Log category.
''' </summary>
Public Class SecurityLogHelpTopicResolver
    Inherits AdminHelpTopicResolverBase

    Protected Overrides Function GetTopicFolder() As String
        Return "~/UserControls/Back/HelpTopics/SecurityLog/"
    End Function
End Class

''' <summary>
''' Help topics for the System Log category (also the fallback for an unrecognised category).
''' </summary>
Public Class SystemLogHelpTopicResolver
    Inherits AdminHelpTopicResolverBase

    Protected Overrides Function GetTopicFolder() As String
        Return "~/UserControls/Back/HelpTopics/SystemLog/"
    End Function
End Class

''' <summary>
''' Dispatches to the concrete AdminHelpTopicResolverBase for a requested log category. Unrecognised
''' categories fall back to the System Log resolver.
''' </summary>
Public NotInheritable Class AdminHelpTopicResolverFactory

    Public Shared Function GetResolver(ByVal strCategory As String) As AdminHelpTopicResolverBase
        Select Case strCategory
            Case "order"
                Return New OrderLogHelpTopicResolver()
            Case "security"
                Return New SecurityLogHelpTopicResolver()
            Case Else
                Return New SystemLogHelpTopicResolver()
        End Select
    End Function

End Class
