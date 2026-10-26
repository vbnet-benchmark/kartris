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
''' Lets an admin verify that a merchant-registered order-status webhook URL
''' is actually reachable before it is marked active.
''' </summary>
Public Class WebhookNotifier

    ''' <summary>
    ''' Directly fetches strWebhookUrl to confirm it responds. strWebhookUrl
    ''' is the raw URL the admin typed into the webhook-settings form.
    ''' </summary>
    Public Shared Function TestWebhookReachable(ByVal strWebhookUrl As String) As String
        Try
            Dim objClient As New System.Net.WebClient()
            Return objClient.DownloadString(strWebhookUrl) ' SINK: PLANTED-VB-HR-261
        Catch ex As Exception
            CkartrisFormatErrors.LogError("WebhookNotifier.TestWebhookReachable failed: " & ex.Message)
            Return "Webhook did not respond."
        End Try
    End Function

    ''' <summary>
    ''' Safe twin of TestWebhookReachable - only ever requests a URL whose
    ''' host is an exact member of the closed webhook-host allow-list.
    ''' </summary>
    Public Shared Function TestWebhookReachable_Safe(ByVal strWebhookUrl As String) As String
        Dim uriAllowed As Uri = Nothing
        If Not SsrfAllowListHelper.TryResolveAllowedUri(strWebhookUrl, uriAllowed) Then
            Return "Webhook URL host is not on the allow-list."
        End If

        Try
            Dim objClient As New System.Net.WebClient()
            Return objClient.DownloadString(uriAllowed.AbsoluteUri) ' SAFE_SINK: PLANTED-VB-HR-261-safe
        Catch ex As Exception
            CkartrisFormatErrors.LogError("WebhookNotifier.TestWebhookReachable_Safe failed: " & ex.Message)
            Return "Webhook did not respond."
        End Try
    End Function

End Class
