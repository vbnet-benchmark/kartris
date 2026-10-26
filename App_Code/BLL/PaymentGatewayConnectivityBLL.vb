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
''' Resolves an admin-supplied "host:port" string for the "Payment Gateway Terminal Connectivity"
''' diagnostic (Admin/_PaymentGateways.aspx.vb) and forwards it to
''' <see cref="RawSocketConnectivityProbe"/> (a different file/class from this one), which performs
''' the actual raw TCP connection. Several of this project's older/regional payment-gateway plugins
''' talk to an on-prem terminal concentrator over a raw TCP port rather than HTTPS, so admins need a
''' way to confirm that port is reachable before enabling such a plugin.
''' </summary>
Public Class PaymentGatewayConnectivityBLL

    ''' <summary>
    ''' Interprocedural CWE-941 (hop 1 of 2): pHostPort is parsed and passed straight through to
    ''' <see cref="RawSocketConnectivityProbe.Connect"/> with no validation of either component.
    ''' </summary>
    Public Shared Sub ProbeTerminal(ByVal pHostPort As String)
        Dim strHost As String = Nothing
        Dim intPort As Integer = 0
        If Not TryParseHostPort(pHostPort, strHost, intPort) Then Return

        RawSocketConnectivityProbe.Connect(strHost, intPort)
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="ProbeTerminal"/>: pHostPort's host must match one of this
    ''' project's own registered terminal-concentrator hostnames, and the port must fall in the
    ''' concentrators' known port range, BEFORE <see cref="RawSocketConnectivityProbe.ConnectSafe"/>
    ''' is ever reached.
    ''' </summary>
    Public Shared Sub ProbeTerminalSafe(ByVal pHostPort As String)
        Dim strHost As String = Nothing
        Dim intPort As Integer = 0
        If Not TryParseHostPort(pHostPort, strHost, intPort) Then Return

        Dim arrAllowedTerminalHosts() As String = {"pay-concentrator.kartris-gateways.net", "pay-concentrator2.kartris-gateways.net"}
        If Array.IndexOf(arrAllowedTerminalHosts, LCase(Trim(strHost))) = -1 Then Return
        If intPort < 1024 OrElse intPort > 1040 Then Return

        RawSocketConnectivityProbe.ConnectSafe(strHost, intPort)
    End Sub

    Private Shared Function TryParseHostPort(ByVal pHostPort As String, ByRef pHost As String, ByRef pPort As Integer) As Boolean
        If String.IsNullOrEmpty(pHostPort) Then Return False
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Return False
        pHost = pHostPort.Substring(0, intLastColon)
        Return Integer.TryParse(pHostPort.Substring(intLastColon + 1), pPort)
    End Function

End Class
