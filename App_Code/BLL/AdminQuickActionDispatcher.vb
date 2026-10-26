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

''' <summary>
''' Routes a backend-admin-supplied action-name string to a method on a
''' <see cref="KartrisMaintenanceActions"/> instance, via <c>Microsoft.VisualBasic.CallByName</c>.
''' CWE-676 (Use of a Potentially Dangerous Function): CallByName resolves <c>pActionName</c> at run
''' time against EVERY public member of pTarget's actual type, not merely the three maintenance
''' actions <see cref="KartrisMaintenanceActions"/> was written to expose -- including members
''' inherited from Object itself (e.g. "ToString", "GetType", "Equals"). Callers of
''' <see cref="RunQuickAction"/>/<see cref="RunQuickActions"/> are responsible for translating any
''' caller-supplied value through an explicit allow-list before it ever reaches pActionName; this
''' dispatcher performs no such check on those two methods, by design, so the allow-list-checked
''' <see cref="RunQuickActionSafe"/>/<see cref="RunQuickActionsSafe"/> counterparts below never call
''' CallByName at all.
''' </summary>
Public Class AdminQuickActionDispatcher

    ''' <summary>Single named action, e.g. "PurgeOldLogs". See class remarks -- ProcName is not validated here.</summary>
    Public Shared Sub RunQuickAction(ByVal pTarget As Object, ByVal pActionName As String)
        CallByName(pTarget, pActionName, CallType.Method) ' SINK: PLANTED-VB-HR-618
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RunQuickAction"/> -- pActionKey is translated through a fixed,
    ''' closed allow-list of compiler-checked method calls. There is no remaining dynamic-dispatch
    ''' surface on this path at all: an unrecognized key simply runs nothing.
    ''' </summary>
    Public Shared Sub RunQuickActionSafe(ByVal pTarget As KartrisMaintenanceActions, ByVal pActionKey As String)
        Select Case pActionKey ' SAFE_SINK: PLANTED-VB-HR-618-safe
            Case "purgelogs"
                pTarget.PurgeOldLogs()
            Case "rebuildindex"
                pTarget.RebuildSearchIndex()
            Case "disabletriggers"
                pTarget.DisableAllTriggers()
        End Select
    End Sub

    ''' <summary>Comma-separated batch of named actions run in one request. See class remarks -- same unvalidated CallByName as <see cref="RunQuickAction"/>, just looped.</summary>
    Public Shared Sub RunQuickActions(ByVal pTarget As Object, ByVal pActionsCsv As String)
        For Each strAction As String In Split(pActionsCsv, ",")
            CallByName(pTarget, Trim(strAction), CallType.Method) ' SINK: PLANTED-VB-HR-619
        Next
    End Sub

    ''' <summary>Safe, allow-list-routed counterpart of <see cref="RunQuickActions"/> -- each batch element goes through <see cref="RunQuickActionSafe"/>, never CallByName.</summary>
    Public Shared Sub RunQuickActionsSafe(ByVal pTarget As KartrisMaintenanceActions, ByVal pActionsCsv As String)
        For Each strAction As String In Split(pActionsCsv, ",")
            RunQuickActionSafe(pTarget, LCase(Trim(strAction)))
        Next ' SAFE_SINK: PLANTED-VB-HR-619-safe
    End Sub

End Class
