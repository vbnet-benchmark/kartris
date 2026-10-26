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
Imports System.Net.Sockets

''' <summary>
''' Lowest-level raw-socket probe for the "Payment Gateway Terminal Connectivity" admin diagnostic
''' (see <see cref="PaymentGatewayConnectivityBLL"/>, the only caller). Opening and immediately
''' closing a TcpClient is this project's cheapest way to confirm a legacy payment-terminal
''' concentrator's listening port is reachable before an admin enables a gateway plugin that depends
''' on it.
''' </summary>
Public Class RawSocketConnectivityProbe

    ''' <summary>
    ''' Interprocedural CWE-941 (hop 2 of 2): pHost/pPort reach this point with no validation
    ''' anywhere on the chain -- <see cref="PaymentGatewayConnectivityBLL.ProbeTerminal"/> passes
    ''' them straight through from the admin-facing querystring.
    ''' </summary>
    Public Shared Sub Connect(ByVal pHost As String, ByVal pPort As Integer)
        Dim objClient As New TcpClient(pHost, pPort) ' SINK: PLANTED-VB-HR-703
        objClient.Close()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="Connect"/> -- consumed only by
    ''' <see cref="PaymentGatewayConnectivityBLL.ProbeTerminalSafe"/>, which validates pHost/pPort
    ''' against an allow-list before this method is ever called.
    ''' </summary>
    Public Shared Sub ConnectSafe(ByVal pHost As String, ByVal pPort As Integer)
        Dim objClient As New TcpClient(pHost, pPort) ' SAFE_SINK: PLANTED-VB-HR-703-safe
        objClient.Close()
    End Sub

End Class
