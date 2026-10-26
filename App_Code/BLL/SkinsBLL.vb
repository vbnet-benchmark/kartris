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
''' Backs the admin "Upload Custom Skin File" feature on _GeneralFiles.aspx -- lets an admin push
''' a replacement/custom file (a logo override, a stylesheet bundle, or a packaged skin) straight
''' into the active skins folder, bypassing a full redeploy.
''' </summary>
Public Class SkinsBLL

    ''' <summary>
    ''' Saves the posted skin file under its own client-supplied name, with no restriction on
    ''' extension. This is the original behaviour, kept for installs that haven't opted into the
    ''' hardened path below.
    ''' </summary>
    Public Shared Function InstallSkinPackage(ByVal objPostedFile As HttpPostedFile, ByVal strSkinsFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strSkinsFolder & objPostedFile.FileName)
        ' SINK: PLANTED-VB-HR-163
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="InstallSkinPackage"/> -- a skin upload only ever needs to
    ''' accept a packaged archive, so the extension is checked against a one-item allow-list
    ''' before the file is saved anywhere.
    ''' </summary>
    Public Shared Function InstallSkinPackageSafe(ByVal objPostedFile As HttpPostedFile, ByVal strSkinsFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
        If strExt <> ".zip" Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strSkinsFolder & objPostedFile.FileName)
        ' SAFE_SINK: PLANTED-VB-HR-163-safe
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

End Class
