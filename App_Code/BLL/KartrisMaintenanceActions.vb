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
''' The small, fixed set of DB-maintenance quick actions the "DB Admin Tools" tab is meant to
''' expose to a backend administrator (<see cref="Admin_DBAdmin"/> / <c>UserControls_Back_AdminDBTools</c>).
''' Deliberately plain INSTANCE members (never Shared) -- they are invoked by name via
''' <see cref="AdminQuickActionDispatcher"/>'s <c>Microsoft.VisualBasic.CallByName</c> call, which
''' requires a live object reference to resolve a member against. See that class's own remarks for
''' why naming a member this way is a real dynamic-dispatch security boundary, not just a style choice --
''' CallByName is not restricted to the three members below; it can resolve ANY public member of this
''' class, including ones inherited from Object.
''' </summary>
Public Class KartrisMaintenanceActions

    ''' <summary>Wraps <see cref="KartrisDBBLL._PurgeOldLogs"/> -- deletes admin-log rows older than the configured retention window.</summary>
    Public Sub PurgeOldLogs()
        Dim strMsg As String = String.Empty
        KartrisDBBLL._PurgeOldLogs(strMsg)
    End Sub

    ''' <summary>Wraps <see cref="KartrisDBBLL.StopFTS"/>/<see cref="KartrisDBBLL.SetupFTS"/> -- rebuilds the SQL Server full-text catalog used by the storefront's product search.</summary>
    Public Sub RebuildSearchIndex()
        KartrisDBBLL.StopFTS()
        KartrisDBBLL.SetupFTS()
    End Sub

    ''' <summary>Wraps <see cref="KartrisDBBLL._DisableAllTriggers"/> -- disables every DB trigger, the same maintenance step the (manual) bulk-import procedure documents.</summary>
    Public Sub DisableAllTriggers()
        KartrisDBBLL._DisableAllTriggers()
    End Sub

End Class
