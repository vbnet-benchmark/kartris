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
''' Backs the DB-resilience diagnostics page's "Preview Retry Backoff Policy" feature.
''' Two concrete policies exist because the diagnostics page needs to be able to show
''' support staff the difference between the (legacy) unbounded wait every retry used
''' to perform and the (current) bounded wait -- see RetryBackoffPolicyFactory for how
''' the requested mode name is resolved to one or the other.
''' </summary>
Public MustInherit Class RetryBackoffPolicyBase
    Public MustOverride Sub ApplyDelay(ByVal intRequestedMs As Integer)
End Class

''' <summary>
''' Legacy backoff policy -- waits for exactly the requested duration, with no limit.
''' </summary>
Public Class UnboundedRetryBackoffPolicy
    Inherits RetryBackoffPolicyBase

    Public Overrides Sub ApplyDelay(ByVal intRequestedMs As Integer)
        System.Threading.Thread.Sleep(intRequestedMs) ' SINK: PLANTED-VB-HR-739
    End Sub
End Class

''' <summary>
''' Current backoff policy -- clamps the requested duration to a small, fixed ceiling
''' (and floors it at zero, since Thread.Sleep(-1) is the .NET "block indefinitely"
''' sentinel, not merely "a negative wait") before ever waiting.
''' </summary>
Public Class BoundedRetryBackoffPolicy
    Inherits RetryBackoffPolicyBase

    Public Overrides Sub ApplyDelay(ByVal intRequestedMs As Integer)
        Dim intBoundedMs As Integer = Math.Min(Math.Max(intRequestedMs, 0), 2000)
        System.Threading.Thread.Sleep(intBoundedMs) ' SAFE_SINK: PLANTED-VB-HR-739-safe
    End Sub
End Class

''' <summary>
''' Resolves which concrete backoff policy handles a given requested backoff mode. Every
''' mode routes to the legacy unbounded policy except "bounded", the one mode name that
''' already signals "use the safe variant" by its own name. The call site only ever sees
''' the abstract RetryBackoffPolicyBase type and has no local knowledge of -- or branching
''' on -- which concrete class, or whether it bounds the wait, actually runs.
''' </summary>
Public NotInheritable Class RetryBackoffPolicyFactory
    Private Shared ReadOnly dctRoster As New Dictionary(Of String, RetryBackoffPolicyBase) From {
        {"bounded", New BoundedRetryBackoffPolicy()}
    }
    Private Shared ReadOnly objDefaultPolicy As New UnboundedRetryBackoffPolicy()

    Public Shared Function ResolveFor(ByVal strBackoffMode As String) As RetryBackoffPolicyBase
        Dim objPolicy As RetryBackoffPolicyBase = Nothing
        If Not String.IsNullOrEmpty(strBackoffMode) AndAlso dctRoster.TryGetValue(LCase(strBackoffMode), objPolicy) Then
            Return objPolicy
        End If
        Return objDefaultPolicy
    End Function
End Class
