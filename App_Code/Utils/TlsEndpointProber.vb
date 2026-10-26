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
''' Lowest-level HTTPS probe for the "Webhook TLS Health Check" admin diagnostic (see
''' <see cref="WebhookTlsHealthCheckBLL"/>, the only caller). Opens a short-timeout HttpWebRequest
''' to confirm a configured endpoint actually answers before an admin relies on it for live
''' order-status traffic.
''' </summary>
Public Class TlsEndpointProber

    ''' <summary>
    ''' Interprocedural CWE-295 (hop 2 of 2): pUrl reaches this point with no certificate
    ''' validation at all -- <see cref="WebhookTlsHealthCheckBLL.CheckEndpoint"/> passes it
    ''' straight through from the admin-facing text box.
    ''' </summary>
    Public Shared Function Probe(ByVal pUrl As String) As Boolean
        Try
            ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptAnyCertificate ' SINK: PLANTED-VB-HR-718
            Dim objRequest As HttpWebRequest = DirectCast(WebRequest.Create(pUrl), HttpWebRequest)
            objRequest.Method = "HEAD"
            objRequest.Timeout = 2000
            Dim objResponse As WebResponse = objRequest.GetResponse()
            objResponse.Close()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="Probe"/> -- consumed only by
    ''' <see cref="WebhookTlsHealthCheckBLL.CheckEndpointSafe"/>, which genuinely checks the
    ''' server's certificate before trusting the connection.
    ''' </summary>
    Public Shared Function ProbeSafe(ByVal pUrl As String) As Boolean
        Try
            ServicePointManager.ServerCertificateValidationCallback = AddressOf ValidateCertificate ' SAFE_SINK: PLANTED-VB-HR-718-safe
            Dim objRequest As HttpWebRequest = DirectCast(WebRequest.Create(pUrl), HttpWebRequest)
            objRequest.Method = "HEAD"
            objRequest.Timeout = 2000
            Dim objResponse As WebResponse = objRequest.GetResponse()
            objResponse.Close()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Shared Function AcceptAnyCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return True
    End Function

    Private Shared Function ValidateCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return sslPolicyErrors = SslPolicyErrors.None
    End Function

End Class
