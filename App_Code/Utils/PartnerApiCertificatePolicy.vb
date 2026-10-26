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
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates

''' <summary>
''' One "payment/shipping gateway API connectivity" dispatch policy for the
''' Admin/_PaymentGateways.aspx.vb "Test Gateway API Certificate (policy-driven)" diagnostic.
''' Deliberately a genuine MustInherit/MustOverride hierarchy, mirroring this project's own
''' pre-existing PaymentTerminalConnectivityPolicyBase shape (App_Code/Utils/
''' PaymentTerminalConnectivityPolicy.vb -- the raw-TCP terminal diagnostic): the concrete policy
''' is resolved purely from a server-side, admin-set config value, never from anything visible at
''' the call site in PartnerApiCertificatePolicyFactory.GetCurrentPolicy's caller.
''' </summary>
Public MustInherit Class PartnerApiCertificatePolicyBase

    ''' <summary>Decides whether a given server certificate should be trusted.</summary>
    Public MustOverride Function AcceptCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean

    ''' <summary>Opens (and immediately closes) an HTTPS connection to a gateway's API endpoint.</summary>
    Public Sub Connect(ByVal pUrl As String)
        ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptCertificate
        Dim objRequest As HttpWebRequest = DirectCast(WebRequest.Create(pUrl), HttpWebRequest)
        objRequest.Method = "HEAD"
        objRequest.Timeout = 2000
        Dim objResponse As WebResponse = objRequest.GetResponse()
        objResponse.Close()
    End Sub

End Class

''' <summary>
''' Legacy policy: accepts any server certificate, including expired/self-signed/wrong-hostname/
''' attacker-controlled ones. Selected by <see cref="PartnerApiCertificatePolicyFactory"/> whenever
''' "general.security.strictpartnerapicert" is unset or not "y" -- this project's established
''' unseeded-config-value-means-legacy-path convention (see KbUpdateNotifierBase in
''' App_Code/BLL/KBBLL.vb).
''' </summary>
Public Class LegacyPartnerApiCertificatePolicy
    Inherits PartnerApiCertificatePolicyBase

    ''' <summary>
    ''' Type/polymorphism-dependent CWE-295: exploitability depends purely on which concrete
    ''' policy GetCurrentPolicy() resolves to, not on anything visible at its caller's call site.
    ''' </summary>
    Public Overrides Function AcceptCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return True ' SINK: PLANTED-VB-HR-719
    End Function

End Class

''' <summary>
''' Hardened policy: only trusts a certificate with no policy errors at all. Selected by
''' <see cref="PartnerApiCertificatePolicyFactory"/> when "general.security.strictpartnerapicert"
''' = "y".
''' </summary>
Public Class HardenedPartnerApiCertificatePolicy
    Inherits PartnerApiCertificatePolicyBase

    Public Overrides Function AcceptCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return sslPolicyErrors = SslPolicyErrors.None ' SAFE_SINK: PLANTED-VB-HR-719-safe
    End Function

End Class

''' <summary>
''' Resolves the current <see cref="PartnerApiCertificatePolicyBase"/> from a real, DB-backed,
''' admin-set config value -- never from a UI control or anything the caller passes in.
''' </summary>
Public Class PartnerApiCertificatePolicyFactory

    Public Shared Function GetCurrentPolicy() As PartnerApiCertificatePolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.security.strictpartnerapicert"))) = "y" Then
            Return New HardenedPartnerApiCertificatePolicy()
        Else
            Return New LegacyPartnerApiCertificatePolicy()
        End If
    End Function

End Class
