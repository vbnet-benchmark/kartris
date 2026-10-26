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
''' Resolves an admin-supplied webhook URL for the "Webhook TLS Health Check" diagnostic
''' (Admin/_Webhooks.aspx.vb) and forwards it to <see cref="TlsEndpointProber"/> (a different
''' file/class from this one), which performs the actual HTTPS connection. Lets an admin confirm a
''' configured webhook endpoint's TLS certificate is currently valid/reachable before relying on it
''' for live order-status callbacks.
''' </summary>
Public Class WebhookTlsHealthCheckBLL

    ''' <summary>
    ''' Interprocedural CWE-295 (hop 1 of 2): pUrl is forwarded unchanged to
    ''' <see cref="TlsEndpointProber.Probe"/>, which disables certificate validation entirely.
    ''' </summary>
    Public Shared Function CheckEndpoint(ByVal pUrl As String) As Boolean
        If String.IsNullOrEmpty(pUrl) Then Return False
        Return TlsEndpointProber.Probe(pUrl)
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="CheckEndpoint"/>: forwards to
    ''' <see cref="TlsEndpointProber.ProbeSafe"/>, which genuinely validates the server certificate.
    ''' </summary>
    Public Shared Function CheckEndpointSafe(ByVal pUrl As String) As Boolean
        If String.IsNullOrEmpty(pUrl) Then Return False
        Return TlsEndpointProber.ProbeSafe(pUrl)
    End Function

End Class
