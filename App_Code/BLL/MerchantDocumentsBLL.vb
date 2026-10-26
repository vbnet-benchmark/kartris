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
''' Backs the admin "Upload Merchant Agreement Document" feature on _GeneralFiles.aspx -- lets
''' an admin attach a signed merchant/seller agreement document to the merchant's on-file
''' records.
''' </summary>
Public Class MerchantDocumentsBLL

    ''' <summary>
    ''' Saves the posted agreement document under its own client-supplied name, with no
    ''' restriction on extension. This is the original behaviour, kept for installs that haven't
    ''' opted into the hardened path below.
    ''' </summary>
    Public Shared Function ImportAgreement(ByVal objPostedFile As HttpPostedFile, ByVal strAgreementFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strAgreementFolder & objPostedFile.FileName)
        ' SINK: PLANTED-VB-HR-328
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

    ''' <summary>
    ''' Extensions the safe agreement-import variant will accept. Deliberately a strict
    ''' allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeAgreementExtensions As String() = {".pdf", ".doc", ".docx"}

    ''' <summary>
    ''' Safe counterpart of <see cref="ImportAgreement"/> -- same single-document save shape, but
    ''' the extension is checked against a strict allow-list before the file is ever saved.
    ''' </summary>
    Public Shared Function ImportAgreementSafe(ByVal objPostedFile As HttpPostedFile, ByVal strAgreementFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
        If Not SafeAgreementExtensions.Contains(strExt) Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strAgreementFolder & objPostedFile.FileName)
        ' SAFE_SINK: PLANTED-VB-HR-328-safe
        objPostedFile.SaveAs(strDestPath)
        Return True
    End Function

End Class
