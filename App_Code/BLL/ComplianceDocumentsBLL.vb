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
''' Backs the admin "Upload Compliance Certificate" feature on _GeneralFiles.aspx -- lets an
''' admin attach a certification document (ISO or insurance) to the storefront's compliance
''' records. The certificate-type dropdown value selects which validation branch runs.
''' </summary>
Public Class ComplianceDocumentsBLL

    ''' <summary>
    ''' Extensions the "insurance" certificate branch will accept. Deliberately a strict
    ''' allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeCertificateExtensions As String() = {".pdf", ".doc", ".docx"}

    ''' <summary>
    ''' Imports a posted compliance-certificate file under its own client-supplied name. The
    ''' "strCertType" parameter is the admin-selected dropdown value posted with the form --
    ''' "insurance" routes through an extension allow-list first; "iso" (or anything else) falls
    ''' through to the original, unchecked import behaviour, kept for installs/integrations that
    ''' still post the older "iso" certificate-upload flow from before the allow-list existed.
    ''' </summary>
    Public Shared Function ImportCertificate(ByVal strCertType As String, ByVal objPostedFile As HttpPostedFile, ByVal strComplianceFolder As String) As Boolean
        If objPostedFile Is Nothing OrElse objPostedFile.ContentLength = 0 Then Return False
        Dim strDestPath As String = HttpContext.Current.Server.MapPath(strComplianceFolder & objPostedFile.FileName)

        Select Case LCase(strCertType)
            Case "insurance"
                Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
                If Not SafeCertificateExtensions.Contains(strExt) Then Return False
                ' SAFE_SINK: PLANTED-VB-HR-329-safe
                objPostedFile.SaveAs(strDestPath)
                Return True
            Case Else
                ' ISO-certificate import path -- predates the allow-list above and performs no
                ' extension check at all. Kept so any install/integration still posting
                ' strCertType="iso" (or no recognized type) keeps working.
                ' SINK: PLANTED-VB-HR-329
                objPostedFile.SaveAs(strDestPath)
                Return True
        End Select
    End Function

End Class
