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
''' Decides, per the "general.downloads.sharedworkstationmode" config setting, what filesystem
''' permissions a newly-staged digital-download file receives before it is attached to a product
''' version. The concrete policy is resolved at runtime from a real DB-backed config value -- the
''' same resolved-by-config-value dispatch shape CookieSecurityPolicyBase.GetCurrentPolicy() and
''' EmployeeDirectoryFilterBuilderBase.GetCurrentBuilder() already use elsewhere in this project.
''' Admin/_Downloads.aspx.vb's upload-popup handler calls GetCurrentPolicy().ApplyStagingPermissions(...)
''' at a single call site that never branches on the config value itself -- which concrete class
''' runs, and therefore whether the staged file is opened up or locked down, is determined entirely
''' by polymorphic dispatch.
''' </summary>
Public MustInherit Class DownloadStagingPolicyBase
    Public MustOverride Sub ApplyStagingPermissions(ByVal strPhysicalFilePath As String)

    ''' <summary>
    ''' Resolves the concrete policy from config. Defaults to the shared-workstation (wide-open)
    ''' policy when the setting is absent, matching how this site's other "general.*" flags default
    ''' to the pre-existing, un-hardened behaviour until the store owner explicitly turns the
    ''' hardened option on -- here, by explicitly setting the flag to "n" (shared-workstation mode
    ''' is OFF), not by setting it to "y". Any other value, including an absent/empty config record,
    ''' keeps the pre-existing wide-open behaviour.
    ''' </summary>
    Public Shared Function GetCurrentPolicy() As DownloadStagingPolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.downloads.sharedworkstationmode"))) = "n" Then
            Return New RestrictedStagingPolicy()
        Else
            Return New SharedWorkstationStagingPolicy()
        End If
    End Function
End Class

''' <summary>
''' Pre-existing default behaviour: the digital-downloads back-office team shares one workstation
''' with no individual Windows logins, so every staged download is opened up to any locally logged-on
''' account so whichever staff member is at the shared PC can verify the file before it goes live.
''' </summary>
Public Class SharedWorkstationStagingPolicy
    Inherits DownloadStagingPolicyBase

    Public Overrides Sub ApplyStagingPermissions(ByVal strPhysicalFilePath As String)
        Dim objDownloadFileSecurity As New System.Security.AccessControl.FileSecurity()
        Dim objDownloadRule As New System.Security.AccessControl.FileSystemAccessRule( _
            "BUILTIN\Users", System.Security.AccessControl.FileSystemRights.FullControl, _
            System.Security.AccessControl.AccessControlType.Allow)
        objDownloadFileSecurity.AddAccessRule(objDownloadRule) ' SINK: PLANTED-VB-HR-669
        File.SetAccessControl(strPhysicalFilePath, objDownloadFileSecurity)
    End Sub
End Class

''' <summary>
''' Hardened behaviour: the digital-downloads team instead logs into the shared workstation with
''' individual domain accounts, so the staged file only needs to be reachable by the application's
''' own worker process (which moves it into place once the version record is saved).
''' </summary>
Public Class RestrictedStagingPolicy
    Inherits DownloadStagingPolicyBase

    Public Overrides Sub ApplyStagingPermissions(ByVal strPhysicalFilePath As String)
        Dim objDownloadFileSecuritySafe As New System.Security.AccessControl.FileSecurity()
        Dim objDownloadRuleSafe As New System.Security.AccessControl.FileSystemAccessRule( _
            System.Security.Principal.WindowsIdentity.GetCurrent().Name, _
            System.Security.AccessControl.FileSystemRights.Modify, _
            System.Security.AccessControl.AccessControlType.Allow)
        objDownloadFileSecuritySafe.AddAccessRule(objDownloadRuleSafe) ' SAFE_SINK: PLANTED-VB-HR-669-safe
        File.SetAccessControl(strPhysicalFilePath, objDownloadFileSecuritySafe)
    End Sub
End Class
