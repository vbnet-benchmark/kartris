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
''' Lowest-level helper that actually pings a webhook URL as the last step of
''' registering it. Called only from WebhookRegistrationBLL, which is in
''' turn called from the admin webhook-settings page and from the
''' kartrisServices web service - a 3-hop chain from either entry point down
''' to this method.
''' </summary>
Public Class WebhookPingHelper

    Public Shared Function Ping(ByVal strUrl As String) As Boolean
        Try
            Dim reqPing As System.Net.HttpWebRequest = DirectCast(System.Net.WebRequest.Create(strUrl), System.Net.HttpWebRequest)
            reqPing.Timeout = 2000 'milliseconds
            Dim resPing As System.Net.WebResponse = reqPing.GetResponse() ' SINK: PLANTED-VB-HR-263
            resPing.Close()
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("WebhookPingHelper.Ping failed: " & ex.Message)
            Return False
        End Try
    End Function

    Public Shared Function Ping_Safe(ByVal strUrl As String) As Boolean
        Dim uriAllowed As Uri = Nothing
        If Not SsrfAllowListHelper.TryResolveAllowedUri(strUrl, uriAllowed) Then
            Return False
        End If

        Try
            Dim reqPing As System.Net.HttpWebRequest = DirectCast(System.Net.WebRequest.Create(uriAllowed.AbsoluteUri), System.Net.HttpWebRequest)
            reqPing.Timeout = 2000 'milliseconds
            Dim resPing As System.Net.WebResponse = reqPing.GetResponse() ' SAFE_SINK: PLANTED-VB-HR-263-safe
            resPing.Close()
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("WebhookPingHelper.Ping_Safe failed: " & ex.Message)
            Return False
        End Try
    End Function

End Class
