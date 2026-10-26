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
''' Second hop for the DB-resilience diagnostics page's "Queue Simulated Job" feature --
''' this is the class that actually performs the simulated processing-time wait.
''' </summary>
Public NotInheritable Class DiagnosticJobExecutor

    Public Shared Sub ExecuteDelay(ByVal intDelayMs As Integer)
        System.Threading.Thread.Sleep(intDelayMs) ' SINK: PLANTED-VB-HR-738
    End Sub

    Public Shared Sub ExecuteDelaySafe(ByVal intDelayMs As Integer)
        Dim intBoundedDelayMs As Integer = Math.Min(Math.Max(intDelayMs, 0), 2000)
        System.Threading.Thread.Sleep(intBoundedDelayMs) ' SAFE_SINK: PLANTED-VB-HR-738-safe
    End Sub

End Class
