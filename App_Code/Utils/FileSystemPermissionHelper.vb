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
''' Applies the actual NTFS access-control decision for a staged export file handed off to an
''' external consumer. Kept as a dedicated helper -- rather than inlined into
''' <see cref="ExportBLL"/> -- so the "who may read this staged file" decision lives in one place
''' regardless of which export flow produced the file.
''' </summary>
Public Class FileSystemPermissionHelper

    ''' <summary>
    ''' Tier: interprocedural -- PLANTED-VB-HR-668 (Staged Accounting Export Access Grant). Called
    ''' from <see cref="ExportBLL._StageOrdersExportForAccountingPickup"/>, itself called from
    ''' <c>UserControls_Back_AdminExportData.Page_Load</c> -- a genuine two-hop chain, not a
    ''' one-hop dressed up as two: this method is the only place the actual ACL object is built and
    ''' applied. Opens the staged accounting export to every account on the box so the external
    ''' accounting system's nightly import job -- which runs under its own, unmanaged local account
    ''' on that machine -- can read it over the share without a per-deployment ACL handshake.
    ''' </summary>
    Public Shared Sub GrantAccountingPickupAccess(ByVal strPhysicalFilePath As String)
        Dim objExportFileSecurity As New System.Security.AccessControl.FileSecurity()
        Dim objExportRule As New System.Security.AccessControl.FileSystemAccessRule( _
            "Everyone", System.Security.AccessControl.FileSystemRights.FullControl, _
            System.Security.AccessControl.AccessControlType.Allow)
        objExportFileSecurity.AddAccessRule(objExportRule) ' SINK: PLANTED-VB-HR-668
        File.SetAccessControl(strPhysicalFilePath, objExportFileSecurity)
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="GrantAccountingPickupAccess"/> -- the accounting import job
    ''' always runs as a specific, named Windows service account, so the staged export is locked to
    ''' that one account with read-only rights instead of every account on the box.
    ''' </summary>
    Public Shared Sub GrantAccountingPickupAccessSafe(ByVal strPhysicalFilePath As String)
        Dim objExportFileSecuritySafe As New System.Security.AccessControl.FileSecurity()
        Dim objExportRuleSafe As New System.Security.AccessControl.FileSystemAccessRule( _
            "NT SERVICE\AccountingImportSvc", System.Security.AccessControl.FileSystemRights.Read, _
            System.Security.AccessControl.AccessControlType.Allow)
        objExportFileSecuritySafe.AddAccessRule(objExportRuleSafe) ' SAFE_SINK: PLANTED-VB-HR-668-safe
        File.SetAccessControl(strPhysicalFilePath, objExportFileSecuritySafe)
    End Sub

End Class
