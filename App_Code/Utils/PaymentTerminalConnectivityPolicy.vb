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
''' One "payment terminal connectivity" dispatch policy for the Admin/_PaymentGateways.aspx.vb
''' "Test Gateway Terminal (policy-driven)" diagnostic. Deliberately a genuine MustInherit/
''' MustOverride hierarchy, mirroring this project's own pre-existing CookieSecurityPolicyBase /
''' LoginCompletionPolicyBase shape (App_Code/Utils/CookieSecurityPolicy.vb,
''' App_Code/BLL/LoginCompletionPolicy.vb): the concrete policy is resolved purely from a server-side,
''' admin-set config value, never from anything visible at the call site in
''' PaymentTerminalConnectivityPolicyFactory.GetCurrentPolicy's caller.
''' </summary>
Public MustInherit Class PaymentTerminalConnectivityPolicyBase

    ''' <summary>Opens (and immediately closes) a connection to a terminal concentrator.</summary>
    Public MustOverride Sub Connect(ByVal pHost As String, ByVal pPort As Integer)

End Class

''' <summary>
''' Legacy policy: connects with no validation at all. Selected by
''' <see cref="PaymentTerminalConnectivityPolicyFactory"/> whenever
''' "admin.paymentgateways.hardenedterminalprobe" is unset or not "y" -- this project's established
''' unseeded-config-value-means-legacy-path convention (see KbUpdateNotifierBase in
''' App_Code/BLL/KBBLL.vb).
''' </summary>
Public Class LegacyPaymentTerminalConnectivityPolicy
    Inherits PaymentTerminalConnectivityPolicyBase

    ''' <summary>
    ''' Type/polymorphism-dependent CWE-941: pHost/pPort reach this override with no validation
    ''' anywhere on the chain -- exploitability depends purely on which concrete policy
    ''' GetCurrentPolicy() resolves to, not on anything visible at its caller's call site.
    ''' </summary>
    Public Overrides Sub Connect(ByVal pHost As String, ByVal pPort As Integer)
        Dim objClient As New TcpClient(pHost, pPort) ' SINK: PLANTED-VB-HR-704
        objClient.Close()
    End Sub

End Class

''' <summary>
''' Hardened policy: only connects once pHost/pPort have been checked against this project's own
''' registered terminal-concentrator roster. Selected by
''' <see cref="PaymentTerminalConnectivityPolicyFactory"/> when
''' "admin.paymentgateways.hardenedterminalprobe" = "y".
''' </summary>
Public Class HardenedPaymentTerminalConnectivityPolicy
    Inherits PaymentTerminalConnectivityPolicyBase

    Private Shared ReadOnly AllowedTerminalHosts() As String = {"pay-concentrator.kartris-gateways.net", "pay-concentrator2.kartris-gateways.net"}

    Public Overrides Sub Connect(ByVal pHost As String, ByVal pPort As Integer)
        If String.IsNullOrEmpty(pHost) Then Return
        If Array.IndexOf(AllowedTerminalHosts, LCase(Trim(pHost))) = -1 Then Return
        If pPort < 1024 OrElse pPort > 1040 Then Return

        Dim objClient As New TcpClient(pHost, pPort) ' SAFE_SINK: PLANTED-VB-HR-704-safe
        objClient.Close()
    End Sub

End Class

''' <summary>
''' Resolves the current <see cref="PaymentTerminalConnectivityPolicyBase"/> from a real, DB-backed,
''' admin-set config value -- never from a UI control or anything the caller passes in.
''' </summary>
Public Class PaymentTerminalConnectivityPolicyFactory

    Public Shared Function GetCurrentPolicy() As PaymentTerminalConnectivityPolicyBase
        If KartSettingsManager.GetKartConfig("admin.paymentgateways.hardenedterminalprobe") = "y" Then
            Return New HardenedPaymentTerminalConnectivityPolicy()
        Else
            Return New LegacyPaymentTerminalConnectivityPolicy()
        End If
    End Function

End Class
