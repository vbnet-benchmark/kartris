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

''' <summary>
''' Front door for the Webhook &amp; Branding Settings admin page's "Manifest Export (Partner
''' Pickup)" feature (hop 1 of 2). Builds the manifest envelope around the partner's
''' settlement summary and hands the actual encryption off to SettlementCipherService -- the
''' class that performs the cipher call (hop 2).
''' </summary>
Public NotInheritable Class PartnerSettlementExportBLL

    Public Shared Function PrepareSecureManifest(ByVal strSummary As String) As String
        Dim strCiphertext As String = SettlementCipherService.Encrypt(strSummary)
        Return "MANIFEST|v1|" & strCiphertext
    End Function

    Public Shared Function PrepareSecureManifestSafe(ByVal strSummary As String) As String
        Dim strCiphertext As String = SettlementCipherService.EncryptSafe(strSummary)
        Return "MANIFEST|v1|" & strCiphertext
    End Function

End Class
