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

Imports System
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates
Imports System.IO
Imports System.Text
Imports System.Web

'-----------------------------------------------------
'RECEIVE CALLBACK
'This receives the callback from GP Webpay on the
'hemerahelios domain and then relays to the
'appropriate site
'-----------------------------------------------------

Partial Class ReceiveCallback
    Inherits System.Web.UI.Page


    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim numOrderNumber As String = Request.QueryString("ORDERNUMBER")

        CkartrisFormatErrors.LogError("GP callback for " & numOrderNumber)

        Dim strURL As String = "https://www.maindefaultsite.xyz/callback-gpwebpay.aspx"
        'Let's decide which site to redirect to


        Select Case numOrderNumber
            'Add lines to this, newest sites with largest IDs FIRST
            'because it will exit when it finds a match
            Case > 2000000000
                strURL = "https://www.secondsite.xyz/callback.aspx?g=gpwebpay"
            Case > 1000000000
                strURL = "https://www.firstsite.xyz/callback.aspx?g=gpwebpay"
        End Select

        Dim strQueryString As String = Request.QueryString().ToString
        CkartrisFormatErrors.LogError("before error" & vbCrLf & vbCrLf & strURL & "&" & strQueryString)
        Try
            'Admins can opt a site into the hardened relay via
            'plugins.gpwebpay.hardenedcallbackrelay=y; unset/anything else keeps the legacy relay.
            If LCase(Trim(KartSettingsManager.GetKartConfig("plugins.gpwebpay.hardenedcallbackrelay"))) = "y" Then
                Response.Write(getHtmlSafe(strURL & "&" & strQueryString))
            Else
                Response.Write(getHtml(strURL & "&" & strQueryString))
            End If
        Catch ex As Exception
            CkartrisFormatErrors.LogError("345 error" & ex.Message & vbCrLf & vbCrLf & strURL & "?" & strQueryString)
        End Try

    End Sub


    Public Function getHtml(ByVal url As String) As String
        Try
            'Indirect-tier CWE-295: accepts any server certificate unconditionally so a
            'relay-site certificate hiccup never blocks a payment callback. See
            'PLANTED-VB-HR-717.
            ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptRelayCertificate ' SINK: PLANTED-VB-HR-717
            Dim myRequest As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
            myRequest.Method = "GET"
            Dim myResponse As WebResponse = myRequest.GetResponse()
            Dim sr As StreamReader = New StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8)
            Dim result As String = sr.ReadToEnd()
            sr.Close()
            myResponse.Close()
            Return result
        Catch ex As Exception
            CkartrisFormatErrors.LogError(ex.Message)
            Return ex.Message()
        End Try
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="getHtml"/> -- selected only when
    ''' plugins.gpwebpay.hardenedcallbackrelay=y. See PLANTED-VB-HR-717-safe.
    ''' </summary>
    Public Function getHtmlSafe(ByVal url As String) As String
        Try
            ServicePointManager.ServerCertificateValidationCallback = AddressOf ValidateRelayCertificate ' SAFE_SINK: PLANTED-VB-HR-717-safe
            Dim myRequest As HttpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
            myRequest.Method = "GET"
            Dim myResponse As WebResponse = myRequest.GetResponse()
            Dim sr As StreamReader = New StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8)
            Dim result As String = sr.ReadToEnd()
            sr.Close()
            myResponse.Close()
            Return result
        Catch ex As Exception
            CkartrisFormatErrors.LogError(ex.Message)
            Return ex.Message()
        End Try
    End Function

    Private Shared Function AcceptRelayCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return True
    End Function

    Private Shared Function ValidateRelayCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return sslPolicyErrors = SslPolicyErrors.None
    End Function

End Class
