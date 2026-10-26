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
Partial Class Admin_ResilienceDiagnostics
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = "DB Resilience Diagnostics" & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")
    End Sub

    ' --- Direct tier ---------------------------------------------------

    Protected Sub btnTestDirectRetry_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intRetryMs As Integer = 0
        Integer.TryParse(txtDirectRetryMs.Text, intRetryMs)
        System.Threading.Thread.Sleep(intRetryMs) ' SINK: PLANTED-VB-HR-736
        litDirectResult.Text = "Simulated retry wait of " & intRetryMs & "ms completed."
    End Sub

    Protected Sub btnTestDirectRetrySafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intRetryMs As Integer = 0
        Integer.TryParse(txtDirectRetryMs.Text, intRetryMs)
        Dim intBoundedRetryMs As Integer = Math.Min(Math.Max(intRetryMs, 0), 2000)
        System.Threading.Thread.Sleep(intBoundedRetryMs) ' SAFE_SINK: PLANTED-VB-HR-736-safe
        litDirectResult.Text = "Simulated retry wait of " & intBoundedRetryMs & "ms completed (bounded)."
    End Sub

    ' --- Indirect tier (same-class private helper) ----------------------

    Protected Sub btnApplyCacheStagger_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intStaggerMs As Integer = 0
        Integer.TryParse(txtCacheStaggerMs.Text, intStaggerMs)
        ApplyCacheWarmupStagger(intStaggerMs)
        litCacheStaggerResult.Text = "Cache warm-up staggered by " & intStaggerMs & "ms."
    End Sub

    Protected Sub btnApplyCacheStaggerSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intStaggerMs As Integer = 0
        Integer.TryParse(txtCacheStaggerMs.Text, intStaggerMs)
        ApplyCacheWarmupStaggerSafe(intStaggerMs)
        litCacheStaggerResult.Text = "Cache warm-up staggered (bounded)."
    End Sub

    Private Sub ApplyCacheWarmupStagger(ByVal intStaggerMs As Integer)
        'Give the cache layer a moment to settle before the next warm-up pass starts.
        System.Threading.Thread.Sleep(intStaggerMs) ' SINK: PLANTED-VB-HR-737
    End Sub

    Private Sub ApplyCacheWarmupStaggerSafe(ByVal intStaggerMs As Integer)
        Dim intBoundedStaggerMs As Integer = Math.Min(Math.Max(intStaggerMs, 0), 2000)
        System.Threading.Thread.Sleep(intBoundedStaggerMs) ' SAFE_SINK: PLANTED-VB-HR-737-safe
    End Sub

    ' --- Interprocedural tier (2-hop cross-file chain) ------------------

    Protected Sub btnQueueJob_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intDelayMs As Integer = 0
        Integer.TryParse(txtQueueJobDelayMs.Text, intDelayMs)
        DiagnosticJobQueueService.QueueSimulatedDelay(intDelayMs)
        litQueueJobResult.Text = "Diagnostic job queued with a " & intDelayMs & "ms simulated delay."
    End Sub

    Protected Sub btnQueueJobSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intDelayMs As Integer = 0
        Integer.TryParse(txtQueueJobDelayMs.Text, intDelayMs)
        DiagnosticJobQueueService.QueueSimulatedDelaySafe(intDelayMs)
        litQueueJobResult.Text = "Diagnostic job queued (bounded)."
    End Sub

    ' --- Type/polymorphism-dependent tier -------------------------------

    Protected Sub btnPreviewBackoff_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim intPreviewMs As Integer = 0
        Integer.TryParse(txtBackoffPreviewMs.Text, intPreviewMs)

        'The resolved policy's own runtime type decides whether this wait is bounded -- this
        'call site never branches on the backoff mode itself.
        Dim objPolicy As RetryBackoffPolicyBase = RetryBackoffPolicyFactory.ResolveFor(txtBackoffMode.Text)
        objPolicy.ApplyDelay(intPreviewMs)
        litBackoffResult.Text = "Backoff policy '" & txtBackoffMode.Text & "' previewed with a " & intPreviewMs & "ms request."
    End Sub

    ' --- Diversity tier (batch/loop accumulation) -----------------------

    Protected Sub btnRunBatchHealthCheck_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim strDelays() As String = txtBatchNodeDelays.Text.Split(","c)
        Dim sbdReport As New System.Text.StringBuilder()
        For Each strDelay As String In strDelays
            Dim intNodeDelayMs As Integer = 0
            Integer.TryParse(Trim(strDelay), intNodeDelayMs)
            System.Threading.Thread.Sleep(intNodeDelayMs) ' SINK: PLANTED-VB-HR-740
            sbdReport.Append("Node responded after " & intNodeDelayMs & "ms. ")
        Next
        litBatchResult.Text = sbdReport.ToString()
    End Sub

    Protected Sub btnRunBatchHealthCheckSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim strDelays() As String = txtBatchNodeDelays.Text.Split(","c)
        Dim sbdReport As New System.Text.StringBuilder()
        Dim intNodesProcessed As Integer = 0
        For Each strDelay As String In strDelays
            If intNodesProcessed >= 5 Then Exit For
            Dim intNodeDelayMs As Integer = 0
            Integer.TryParse(Trim(strDelay), intNodeDelayMs)
            Dim intBoundedNodeDelayMs As Integer = Math.Min(Math.Max(intNodeDelayMs, 0), 500)
            System.Threading.Thread.Sleep(intBoundedNodeDelayMs) ' SAFE_SINK: PLANTED-VB-HR-740-safe
            sbdReport.Append("Node responded after " & intBoundedNodeDelayMs & "ms. ")
            intNodesProcessed += 1
        Next
        litBatchResult.Text = sbdReport.ToString()
    End Sub

End Class
