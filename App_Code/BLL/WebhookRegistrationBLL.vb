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
''' Registers a merchant's order-status webhook URL and verifies it is
''' reachable before activating it. A thin BLL layer over the lower-level
''' WebhookPingHelper, mirroring the pattern already used by ShippingBLL /
''' CurrenciesBLL over their own DAL adapters.
''' </summary>
Public Class WebhookRegistrationBLL

    Public Shared Function RegisterAndVerifyWebhook(ByVal strWebhookUrl As String) As Boolean
        'Persisting the URL itself is handled by the settings table elsewhere -
        'this call only confirms the endpoint actually responds before the
        'webhook is marked active.
        Return WebhookPingHelper.Ping(strWebhookUrl)
    End Function

    Public Shared Function RegisterAndVerifyWebhook_Safe(ByVal strWebhookUrl As String) As Boolean
        Return WebhookPingHelper.Ping_Safe(strWebhookUrl)
    End Function

End Class
