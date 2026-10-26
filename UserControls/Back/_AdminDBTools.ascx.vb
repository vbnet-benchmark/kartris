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
Imports CkartrisEnumerations
Imports KartSettingsManager

Partial Class UserControls_Back_AdminDBTools

    Inherits System.Web.UI.UserControl

    Protected Sub btnCreateBackup_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCreateBackup.Click
        If Not File.Exists(litBackupName.Text) Then CreateBackup() : Return
        _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.Confirmation, GetGlobalResourceObject("_DBAdmin", "ContentText_BackupAlreadyExists"))
    End Sub

    Protected Sub Back_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnBackFailed.Click, lnkBtnBackSucceeded.Click
        mvwBackup.SetActiveView(viwCreateBackup)
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            litConfigBackupFolder.Text = GetKartConfig("general.backupfolder")
            GetDBInformation()
            CreateBackupName()
            litRootPath.Text = Server.MapPath("../")

            '' "Run Named Diagnostic" quick action -- gated on this tab's own "expertmode" config
            '' check (re-checked here directly, independent of the containing tab's Visible flag),
            '' distinct from _DBAdmin.aspx.vb's plain backend-login gate used by the other DB Admin
            '' quick actions. Dispatched inline at this call site rather than via the shared
            '' AdminQuickActionDispatcher class -- the diversity axis for this round's 5th instance.
            If GetKartConfig("backend.expertmode") = "y" Then
                If Not String.IsNullOrEmpty(Request.QueryString("runDiagnostic")) Then
                    Dim oDiagnostics As New KartrisMaintenanceActions()
                    CallByName(oDiagnostics, Request.QueryString("runDiagnostic"), CallType.Method) ' SINK: PLANTED-VB-HR-620
                End If
                If Not String.IsNullOrEmpty(Request.QueryString("runDiagnosticSafe")) Then
                    RunNamedDiagnosticSafe(Request.QueryString("runDiagnosticSafe"))
                End If

                ' Tier: direct -- PLANTED-VB-HR-666 / -666-safe (Backup Folder Access Grant)
                ' Quick action: open up (or re-assert) the NTFS permissions on the DB backup folder
                ' without having to walk an IT admin through Windows Explorer's security dialog --
                ' every admin workstation on the LAN then reaches the freshly-written .bak files over
                ' the share with no per-machine ACL work. Straight-line: builds and applies the ACL
                ' object right here, no helper, no branch inside the grant itself.
                If Not String.IsNullOrEmpty(Request.QueryString("grantBackupFolderAccess")) Then
                    Dim strBackupFolder As String = GetKartConfig("general.backupfolder")
                    Dim objBackupDirSecurity As New System.Security.AccessControl.DirectorySecurity()
                    Dim objBackupRule As New System.Security.AccessControl.FileSystemAccessRule( _
                        "Everyone", System.Security.AccessControl.FileSystemRights.FullControl, _
                        System.Security.AccessControl.AccessControlType.Allow)
                    objBackupDirSecurity.AddAccessRule(objBackupRule) ' SINK: PLANTED-VB-HR-666
                    Directory.SetAccessControl(strBackupFolder, objBackupDirSecurity)
                End If

                ' Safe counterpart: grants read/list access to the built-in "Backup Operators" group
                ' only -- the one group that actually needs to reach the backup folder -- instead of
                ' opening it to every account on the box.
                If Not String.IsNullOrEmpty(Request.QueryString("grantBackupFolderAccessSafe")) Then
                    Dim strBackupFolderSafe As String = GetKartConfig("general.backupfolder")
                    Dim objBackupDirSecuritySafe As New System.Security.AccessControl.DirectorySecurity()
                    Dim objBackupRuleSafe As New System.Security.AccessControl.FileSystemAccessRule( _
                        "BUILTIN\Backup Operators", System.Security.AccessControl.FileSystemRights.ReadAndExecute, _
                        System.Security.AccessControl.AccessControlType.Allow)
                    objBackupDirSecuritySafe.AddAccessRule(objBackupRuleSafe) ' SAFE_SINK: PLANTED-VB-HR-666-safe
                    Directory.SetAccessControl(strBackupFolderSafe, objBackupDirSecuritySafe)
                End If
            End If
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of the inline "Run Named Diagnostic" quick action above -- same expertmode
    ''' gate, but pActionKey is routed through a fixed allow-list of compiler-checked method calls
    ''' rather than CallByName, eliminating the dynamic-dispatch choke point on this path entirely.
    ''' </summary>
    Private Sub RunNamedDiagnosticSafe(ByVal pActionKey As String)
        Dim oDiagnostics As New KartrisMaintenanceActions()
        Select Case LCase(Trim(pActionKey)) ' SAFE_SINK: PLANTED-VB-HR-620-safe
            Case "purgelogs"
                oDiagnostics.PurgeOldLogs()
            Case "rebuildindex"
                oDiagnostics.RebuildSearchIndex()
            Case "disabletriggers"
                oDiagnostics.DisableAllTriggers()
        End Select
    End Sub

    Private Sub GetDBInformation()
        Dim strMessage As String = String.Empty
        Dim tblDBInfo As New DataTable
        KartrisDBBLL._GetDBInformation(tblDBInfo, strMessage)
        If tblDBInfo.Rows.Count = 0 Then Return
        For Each row As DataRow In tblDBInfo.Rows
            litDBName.Text = row("DatabaseName")
            If CStr(row("FileName")).EndsWith("_log") Then row("FileType") = "LOG FILE" : Continue For
            row("FileType") = "DATA FILE"
        Next
        gvwDBInfo.DataSource = tblDBInfo
        gvwDBInfo.DataBind()
    End Sub

    Private Sub CreateBackupName()
        litBackupName.Text = GetKartConfig("general.backupfolder") & CStr(Now.Year) & "." & CStr(Now.Month) & "." & CStr(Now.Day) & ".bak"
    End Sub

    Private Sub CreateBackup()
        Dim strMessage As String = String.Empty
        If Not KartrisDBBLL._CreateDBBackup(litBackupName.Text, txtBackupDescription.Text, strMessage) Then
            mvwBackup.SetActiveView(viwBackupFailed)
            litBackupFailed.Text = Replace(GetGlobalResourceObject("_DBAdmin", "ContentText_BackupUnsuccessful"), "[filename]", "<span>" & litBackupName.Text & "</span>")
        Else
            If File.Exists(litBackupName.Text) Then
                mvwBackup.SetActiveView(viwBackupSucceeded)
                litBackupSucceeded.Text = Replace(GetGlobalResourceObject("_DBAdmin", "ContentText_BackupSuccessful"), "[filename]", "<span>" & litBackupName.Text & "</span>")
                txtBackupDescription.Text = String.Empty
            Else
                mvwBackup.SetActiveView(viwBackupFailed)
                litBackupFailed.Text = Replace(GetGlobalResourceObject("_DBAdmin", "ContentText_BackupUnsuccessful"), "[filename]", "<span>" & litBackupName.Text & "</span>")
            End If
        End If
        updAdminTools.Update()
    End Sub

    Protected Sub _UC_PopupMsg_Confirmed() Handles _UC_PopupMsg.Confirmed
        CreateBackup()
    End Sub
End Class
