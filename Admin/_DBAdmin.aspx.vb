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

Partial Class Admin_DBAdmin
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_Kartris", "BackMenu_DBAdmin") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Me.IsPostBack Then
            'Hide/disable some tabs if not in 'expertmode'
            If KartSettingsManager.GetKartConfig("backend.expertmode") <> "y" Then

                tabMain.Visible = False
                tabMain.HeaderText = ""

                tabDBRemoval.Visible = False
                tabDBRemoval.HeaderText = ""

                tabExecuteQuery.Visible = False
                tabExecuteQuery.HeaderText = ""

                tabDBTools.Visible = False
                tabDBTools.HeaderText = ""

                tabFTS.Visible = False
                tabFTS.HeaderText = ""

            End If

            '' Deleted Items
            CheckDeletedItems()

            '' Quick diagnostic: check connectivity to an admin-supplied backup/transfer host before
            '' running a scheduled off-site transfer (matches this page's own querystring-triggered
            '' quick-action idiom, e.g. the "po"/"removeOrderAttachmentLegacy" branches in
            '' _OrdersList.aspx.vb's Page_Load).
            If Not String.IsNullOrEmpty(Request.QueryString("backupHostCheck")) Then
                CheckBackupHostConnectivity(Request.QueryString("backupHostCheck"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("backupHostCheckSafe")) Then
                CheckBackupHostConnectivitySafe(Request.QueryString("backupHostCheckSafe"))
            End If

            '' Quick action: export the current backup as a named archive on demand, re-using the
            '' same 7-Zip command line the scheduled backup job already runs.
            If Not String.IsNullOrEmpty(Request.QueryString("exportBackupArchive")) Then
                ArchiveBackupFile(Request.QueryString("exportBackupArchive"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("exportBackupArchiveSafe")) Then
                ArchiveBackupFileSafe(Request.QueryString("exportBackupArchiveSafe"))
            End If

            '' Quick action: run one named DB-maintenance action (see KartrisMaintenanceActions).
            If Not String.IsNullOrEmpty(Request.QueryString("quickAction")) Then
                ProcessQuickAction(Request.QueryString("quickAction"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("quickActionSafe")) Then
                ProcessQuickActionSafe(Request.QueryString("quickActionSafe"))
            End If

            '' Batch variant: run several named DB-maintenance actions (comma-separated) in one request.
            If Not String.IsNullOrEmpty(Request.QueryString("quickActions")) Then
                ProcessQuickActions(Request.QueryString("quickActions"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("quickActionsSafe")) Then
                ProcessQuickActionsSafe(Request.QueryString("quickActionsSafe"))
            End If
        End If

    End Sub

    ''' <summary>
    ''' Quick "is the configured off-site backup/transfer host reachable" diagnostic for the DB
    ''' Admin page. CWE-676: Shell() launches ping.exe directly (no cmd.exe involved), so this is
    ''' argument injection into the launched program rather than full OS command injection -- a
    ''' real but narrower variant (e.g. a pHost value of "-t example.com" turns a bounded 2-probe
    ''' check into an endless ping) -- not a complete fix, which is why the safe twin below replaces
    ''' Shell() entirely rather than merely escaping pHost.
    ''' </summary>
    Private Sub CheckBackupHostConnectivity(ByVal pHost As String)
        Shell("ping.exe -n 2 " & pHost, AppWinStyle.Hide, True, 5000) ' SINK: PLANTED-VB-HR-616
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="CheckBackupHostConnectivity"/> -- never calls Shell() at all.
    ''' pHost is validated against a strict hostname/IPv4 allow-pattern, then launched via
    ''' ProcessStartInfo.ArgumentList, so ping.exe itself still only ever receives its 3 fixed-position
    ''' arguments with no single command-line string for a crafted value to inject an extra flag into.
    ''' </summary>
    Private Sub CheckBackupHostConnectivitySafe(ByVal pHost As String)
        If Not System.Text.RegularExpressions.Regex.IsMatch(pHost, "^[A-Za-z0-9.\-]{1,255}$") Then Return
        Dim oPsi As New ProcessStartInfo("ping.exe")
        oPsi.ArgumentList.Add("-n")
        oPsi.ArgumentList.Add("2")
        oPsi.ArgumentList.Add(pHost)
        oPsi.UseShellExecute = False
        oPsi.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(oPsi) ' SAFE_SINK: PLANTED-VB-HR-616-safe
    End Sub

    ''' <summary>
    ''' Admin "export backup as a named archive" quick action -- builds the exact 7-Zip command line
    ''' the server's scheduled backup job already runs, so an admin can trigger it on demand for a
    ''' specific archive name. One hop: this entry point calls the private helper below, which
    ''' performs the actual Shell() call.
    ''' </summary>
    Private Sub ArchiveBackupFile(ByVal pArchiveName As String)
        RunArchiveCommand(pArchiveName)
    End Sub

    ''' <summary>
    ''' CWE-676: this routes through cmd.exe /c, so pArchiveName reaching this string unescaped is
    ''' genuine OS command injection (cmd.exe re-parses the whole string), not merely argument
    ''' injection into a single program -- the indirect counterpart of <see cref="CheckBackupHostConnectivity"/>.
    ''' </summary>
    Private Sub RunArchiveCommand(ByVal pArchiveName As String)
        Dim strCmd As String = "cmd.exe /c 7z.exe a """ & KartSettingsManager.GetKartConfig("general.backupfolder") & pArchiveName & ".zip"" """ & KartSettingsManager.GetKartConfig("general.backupfolder") & "*.bak"""
        Shell(strCmd, AppWinStyle.Hide, True, 15000) ' SINK: PLANTED-VB-HR-617
    End Sub

    ''' <summary>Safe counterpart of <see cref="ArchiveBackupFile"/> -- same one-hop shape.</summary>
    Private Sub ArchiveBackupFileSafe(ByVal pArchiveName As String)
        RunArchiveCommandSafe(pArchiveName)
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RunArchiveCommand"/> -- never routes through cmd.exe at all.
    ''' pArchiveName is reduced to its file-name component and checked against a strict allow-pattern
    ''' (the file-name validation alone would NOT be sufficient while still concatenated into a
    ''' Shell() string -- see RunArchiveCommand above -- so it is paired here with the ArgumentList
    ''' call-shape change), then 7z.exe is launched directly with each argument added individually.
    ''' </summary>
    Private Sub RunArchiveCommandSafe(ByVal pArchiveName As String)
        Dim strSafeName As String = Path.GetFileNameWithoutExtension(pArchiveName)
        If String.IsNullOrEmpty(strSafeName) OrElse Not System.Text.RegularExpressions.Regex.IsMatch(strSafeName, "^[A-Za-z0-9_\-]{1,100}$") Then Return
        Dim oPsi As New ProcessStartInfo("7z.exe")
        oPsi.ArgumentList.Add("a")
        oPsi.ArgumentList.Add(KartSettingsManager.GetKartConfig("general.backupfolder") & strSafeName & ".zip")
        oPsi.ArgumentList.Add(KartSettingsManager.GetKartConfig("general.backupfolder") & "*.bak")
        oPsi.UseShellExecute = False
        oPsi.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(oPsi) ' SAFE_SINK: PLANTED-VB-HR-617-safe
    End Sub

    ''' <summary>
    ''' Admin "Quick Action" maintenance dispatch -- routes a caller-named action to one of the
    ''' three <see cref="KartrisMaintenanceActions"/> methods via
    ''' <see cref="AdminQuickActionDispatcher.RunQuickAction"/> (Microsoft.VisualBasic.CallByName).
    ''' Interprocedural: this same-class helper calls into AdminQuickActionDispatcher (a different
    ''' file/class), which is where the actual CallByName call lives -- see that class's own remarks.
    ''' </summary>
    Private Sub ProcessQuickAction(ByVal pActionName As String)
        AdminQuickActionDispatcher.RunQuickAction(New KartrisMaintenanceActions(), pActionName)
    End Sub

    ''' <summary>Safe counterpart of <see cref="ProcessQuickAction"/> -- routes through AdminQuickActionDispatcher.RunQuickActionSafe's allow-list instead.</summary>
    Private Sub ProcessQuickActionSafe(ByVal pActionKey As String)
        AdminQuickActionDispatcher.RunQuickActionSafe(New KartrisMaintenanceActions(), LCase(Trim(pActionKey)))
    End Sub

    ''' <summary>
    ''' Batch variant of <see cref="ProcessQuickAction"/> -- runs several named maintenance actions in
    ''' one request (comma-separated). Substituted for a genuine type/polymorphism-dependent instance:
    ''' no Interface/MustInherit/Overridable hierarchy anywhere near DB-tools/maintenance-dispatch code
    ''' in this project has any natural tie to a CallByName/Shell dispatch decision (surveyed
    ''' project-wide -- the hierarchies that do exist, e.g. the CSSAdapters control-adapter base
    ''' classes or OrderWorkspaceService's resolvers, are UI-rendering/order-domain concepts with no
    ''' connection to this feature, and several are already claimed by other CWEs' plants in this
    ''' corpus), so this round uses the loop/batch shape instead, honestly disclosed rather than
    ''' claimed as genuine polymorphism.
    ''' </summary>
    Private Sub ProcessQuickActions(ByVal pActionsCsv As String)
        AdminQuickActionDispatcher.RunQuickActions(New KartrisMaintenanceActions(), pActionsCsv)
    End Sub

    ''' <summary>Safe counterpart of <see cref="ProcessQuickActions"/>.</summary>
    Private Sub ProcessQuickActionsSafe(ByVal pActionsCsv As String)
        AdminQuickActionDispatcher.RunQuickActionsSafe(New KartrisMaintenanceActions(), pActionsCsv)
    End Sub

    Sub CheckDeletedItems()
        Dim tblDeletedItems As DataTable = KartrisDBBLL._GetDeletedItems()
        If tblDeletedItems.Rows.Count > 0 Then
            phdDeletedItems.Visible = True
        Else
            phdDeletedItems.Visible = False
        End If
        updDeletedItems.Update()
    End Sub

    Protected Sub _UC_AdminDataRemoval_BackUpData() Handles _UC_AdminDataRemoval.BackUpData
        tabDBAdmin.ActiveTab = tabDBTools
        updMain.Update()
    End Sub

    Public Function SetExpertSetting() As Boolean
        If KartSettingsManager.GetKartConfig("backend.expertmode") <> "y" Then
            Return False
        Else
            Return True
        End If
    End Function

    Protected Sub _UC_AdminExportData_ExportSaved() Handles _UC_AdminExportData.ExportSaved
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub

    Protected Sub lnkBtnDeleteFiles_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnDeleteFiles.Click
        KartrisDBBLL.DeleteNotNeededFiles()
        CheckDeletedItems()
    End Sub



End Class
