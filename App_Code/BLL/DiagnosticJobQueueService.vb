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
''' Front door for the DB-resilience diagnostics page's "Queue Simulated Job" feature.
''' Accepts a requested processing delay and hands it off to DiagnosticJobExecutor, which
''' is the class that actually performs the simulated wait.
''' </summary>
Public NotInheritable Class DiagnosticJobQueueService

    Public Shared Sub QueueSimulatedDelay(ByVal intDelayMs As Integer)
        DiagnosticJobExecutor.ExecuteDelay(intDelayMs)
    End Sub

    Public Shared Sub QueueSimulatedDelaySafe(ByVal intDelayMs As Integer)
        DiagnosticJobExecutor.ExecuteDelaySafe(intDelayMs)
    End Sub

End Class
