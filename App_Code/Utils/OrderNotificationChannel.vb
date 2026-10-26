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
Imports System.Collections.Generic

''' <summary>
''' A channel the engine uses to push an order-status update to a merchant's
''' registered notification endpoint.
''' </summary>
Public Interface IOrderNotificationChannel
    Function SendOrderUpdate(ByVal strNotifyUrl As String, ByVal strPayload As String) As Boolean
End Interface

''' <summary>
''' Legacy direct-post channel, kept only for the small roster of merchants
''' who integrated before the validated channel existed (see
''' OrderNotificationChannelFactory). Posts the payload straight to whatever
''' URL the merchant configured, with no reachability/host check.
''' </summary>
Public Class LegacyDirectPostChannel
    Implements IOrderNotificationChannel

    Public Function SendOrderUpdate(ByVal strNotifyUrl As String, ByVal strPayload As String) As Boolean Implements IOrderNotificationChannel.SendOrderUpdate
        Try
            Dim objClient As New System.Net.WebClient()
            objClient.Headers(System.Net.HttpRequestHeader.ContentType) = "application/json"
            objClient.UploadString(strNotifyUrl, strPayload) ' SINK: PLANTED-VB-HR-265
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("LegacyDirectPostChannel.SendOrderUpdate failed: " & ex.Message)
            Return False
        End Try
    End Function
End Class

''' <summary>
''' Modern channel used for every merchant not on the legacy roster. Only
''' ever posts to a URL whose host is on the closed allow-list.
''' </summary>
Public Class ValidatedPostChannel
    Implements IOrderNotificationChannel

    Public Function SendOrderUpdate(ByVal strNotifyUrl As String, ByVal strPayload As String) As Boolean Implements IOrderNotificationChannel.SendOrderUpdate
        Dim uriAllowed As Uri = Nothing
        If Not SsrfAllowListHelper.TryResolveAllowedUri(strNotifyUrl, uriAllowed) Then
            Return False
        End If

        Try
            Dim objClient As New System.Net.WebClient()
            objClient.Headers(System.Net.HttpRequestHeader.ContentType) = "application/json"
            objClient.UploadString(uriAllowed.AbsoluteUri, strPayload) ' SAFE_SINK: PLANTED-VB-HR-265-safe
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("ValidatedPostChannel.SendOrderUpdate failed: " & ex.Message)
            Return False
        End Try
    End Function
End Class

''' <summary>
''' Dispatches to the correct notification-channel implementation based on
''' the caller-supplied merchant ID, checked against a fixed, server-side
''' roster of merchants still on the legacy integration path. The caller
''' (an admin, or any requester of the unauthenticated SendOrderStatusNotification
''' WebMethod) does supply the merchant ID itself; what they cannot do is
''' edit the roster, so dispatch to the legacy channel is limited to the
''' fixed set of merchant IDs on that roster rather than being an arbitrary,
''' request-chosen trust decision.
''' </summary>
Public Class OrderNotificationChannelFactory

    Private Shared ReadOnly LEGACY_MERCHANT_IDS As New HashSet(Of Integer) From {1001, 1002, 1003}

    Public Shared Function GetChannel(ByVal intMerchantId As Integer) As IOrderNotificationChannel
        If LEGACY_MERCHANT_IDS.Contains(intMerchantId) Then
            Return New LegacyDirectPostChannel()
        Else
            Return New ValidatedPostChannel()
        End If
    End Function

End Class
