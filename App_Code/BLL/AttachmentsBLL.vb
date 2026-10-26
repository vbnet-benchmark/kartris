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
''' Backs the admin "Bulk Import Attachments" feature on _GeneralFiles.aspx -- lets an admin post
''' several supporting documents at once (e.g. for a knowledge-base article) in a single submit.
''' Called once per posted file from the page's batch loop.
''' </summary>
Public Class AttachmentsBLL

    ''' <summary>
    ''' Saves one posted attachment under its own client-supplied name, with no restriction on
    ''' extension. This is the original behaviour, kept for installs that haven't opted into the
    ''' hardened path below.
    ''' </summary>
    Public Shared Function ImportAttachment(ByVal objPostedFile As HttpPostedFile, ByVal strAttachmentsFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strAttachmentsFolder & objPostedFile.FileName)
        ' SINK: PLANTED-VB-HR-165
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

    ''' <summary>
    ''' Extensions the safe bulk-import variant will accept. Deliberately a strict allow-list
    ''' (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeAttachmentExtensions As String() = {".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip", ".png", ".jpg", ".jpeg", ".gif"}

    ''' <summary>
    ''' Safe counterpart of <see cref="ImportAttachment"/> -- same per-file save shape, but the
    ''' extension is checked against a strict allow-list before the file is ever saved.
    ''' </summary>
    Public Shared Function ImportAttachmentSafe(ByVal objPostedFile As HttpPostedFile, ByVal strAttachmentsFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
        If Not SafeAttachmentExtensions.Contains(strExt) Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strAttachmentsFolder & objPostedFile.FileName)
        ' SAFE_SINK: PLANTED-VB-HR-165-safe
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

End Class
