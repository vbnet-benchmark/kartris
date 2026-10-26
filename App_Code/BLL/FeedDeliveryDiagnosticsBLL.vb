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
Imports Microsoft.VisualBasic
Imports System.Diagnostics

''' <summary>
''' Backs the "Generate Feeds" admin page's partner-delivery diagnostics -- a small, admin-only
''' quick-action surface for checking that a configured feed partner's host is actually reachable
''' before the scheduled export/upload job runs against it (the page's own "future export/
''' report-generation shell-out" extension point). Interprocedural counterpart of
''' <see cref="Admin_GenerateFeeds"/>'s own in-page diagnostics: the page's Page_Load dispatches here
''' across the page/BLL class boundary, the same shape as <see cref="FeedDocsBLL"/>'s
''' GetFeedFormatDoc crossing that same boundary for the (unrelated, pre-existing) feed-format-doc
''' preview feature.
''' </summary>
Public Class FeedDeliveryDiagnosticsBLL

    ''' <summary>
    ''' Traces the network path to an admin-supplied partner feed host. CWE-78: pHost is formatted
    ''' directly into a cmd.exe /c command line with no validation, so cmd.exe's own operator parsing
    ''' (&amp;, &amp;&amp;, |, a literal quote) is fully live in the value -- genuine OS command
    ''' injection, not merely argument injection into tracert.exe itself.
    ''' </summary>
    Public Shared Sub TraceRouteToHost(ByVal pHost As String)
        Dim strCmd As String = String.Format("cmd.exe /c tracert -d -h 15 {0}", pHost)
        Shell(strCmd, AppWinStyle.Hide, True, 20000) ' SINK: PLANTED-VB-HR-623
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="TraceRouteToHost"/> -- never routes through cmd.exe. pHost is
    ''' checked against a strict hostname/IPv4 allow-pattern, then tracert.exe is launched directly via
    ''' ProcessStartInfo.ArgumentList, so there is no command-line string left for a crafted value to
    ''' break out of.
    ''' </summary>
    Public Shared Sub TraceRouteToHostSafe(ByVal pHost As String)
        If Not System.Text.RegularExpressions.Regex.IsMatch(pHost, "^[A-Za-z0-9.\-]{1,255}$") Then Return
        Dim oPsi As New ProcessStartInfo("tracert.exe")
        oPsi.ArgumentList.Add("-d")
        oPsi.ArgumentList.Add("-h")
        oPsi.ArgumentList.Add("15")
        oPsi.ArgumentList.Add(pHost)
        oPsi.UseShellExecute = False
        oPsi.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(oPsi) ' SAFE_SINK: PLANTED-VB-HR-623-safe
    End Sub

End Class
