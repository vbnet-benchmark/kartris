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
''' Backs the admin "Import Email Template" feature on _GeneralFiles.aspx -- lets an admin
''' replace one of the storefront's customizable order/notification email templates.
''' </summary>
Public Class EmailTemplatesBLL

    ''' <summary>
    ''' Imports a posted email-template file under its own client-supplied name. The "mode"
    ''' parameter is the admin-selected dropdown value posted with the form -- "legacy" preserves
    ''' the original, unchecked import behaviour for installs/integrations that still post it;
    ''' "validated" (any other value falls through to the legacy branch, see ImportTemplate's
    ''' Select Case) routes through an extension allow-list first.
    ''' </summary>
    Public Shared Function ImportTemplate(ByVal strMode As String, ByVal objPostedFile As HttpPostedFile, ByVal strTemplatesFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strTemplatesFolder & objPostedFile.FileName)

        Select Case LCase(strMode)
            Case "validated"
                Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
                If strExt <> ".html" AndAlso strExt <> ".htm" AndAlso strExt <> ".txt" Then Return False
                ' SAFE_SINK: PLANTED-VB-HR-164-safe
                objPostedFile.SaveAs(strDestPath)
                Return True
            Case Else
                ' Legacy import path -- predates the allow-list above and performs no extension
                ' check at all. Kept so any install/integration still posting mode="legacy" (or no
                ' recognized mode) keeps working.
                ' SINK: PLANTED-VB-HR-164
                objPostedFile.SaveAs(strDestPath)
                Return True
        End Select
    End Function

End Class
