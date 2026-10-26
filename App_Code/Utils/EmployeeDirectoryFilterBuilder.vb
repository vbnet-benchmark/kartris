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
''' Decides, per the "general.security.strictldapescaping" config setting, whether a
''' corporate-directory search term is escaped before it is substituted into an LDAP filter. The
''' concrete builder is resolved at runtime from a real DB-backed config value -- the same
''' resolved-by-config-value dispatch shape LoginCompletionPolicyBase.GetCurrentPolicy() and
''' CookieSecurityPolicyBase.GetCurrentPolicy() already use elsewhere in this project (for
''' "general.security.strictsessionfixation" and "general.security.strictcookies" respectively).
''' Admin/_StaffDirectoryLookup.aspx.vb's "policy search" action calls GetCurrentBuilder().BuildFilter(...)
''' at a single call site that never branches on the config value itself -- which concrete class
''' runs, and therefore whether the search term is escaped, is determined entirely by polymorphic
''' dispatch.
''' </summary>
Public MustInherit Class EmployeeDirectoryFilterBuilderBase
    Public MustOverride Function BuildFilter(ByVal strSearchTerm As String) As String

    ''' <summary>
    ''' Resolves the concrete builder from config. Defaults to the legacy (non-hardening) builder
    ''' when the setting is absent, matching how this site's other "general.security.*" flags
    ''' default closed only when the store owner explicitly turns them on.
    ''' </summary>
    Public Shared Function GetCurrentBuilder() As EmployeeDirectoryFilterBuilderBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.security.strictldapescaping"))) = "y" Then
            Return New HardenedEmployeeDirectoryFilterBuilder()
        Else
            Return New LegacyEmployeeDirectoryFilterBuilder()
        End If
    End Function
End Class

''' <summary>
''' Pre-existing default behaviour: substitutes the search term into the filter template with no
''' neutralization at all.
''' </summary>
Public Class LegacyEmployeeDirectoryFilterBuilder
    Inherits EmployeeDirectoryFilterBuilderBase

    Public Overrides Function BuildFilter(ByVal strSearchTerm As String) As String
        ' SINK: PLANTED-VB-HR-654
        Return "(&(objectClass=user)(|(sAMAccountName=" & strSearchTerm & ")(mail=" & strSearchTerm & ")))"
    End Function
End Class

''' <summary>
''' Hardened behaviour: escapes the search term via LdapFilterUtils.EscapeLdapFilterValue before it
''' is substituted into the same filter template.
''' </summary>
Public Class HardenedEmployeeDirectoryFilterBuilder
    Inherits EmployeeDirectoryFilterBuilderBase

    Public Overrides Function BuildFilter(ByVal strSearchTerm As String) As String
        Dim strEscapedTerm As String = LdapFilterUtils.EscapeLdapFilterValue(strSearchTerm)
        ' SAFE_SINK: PLANTED-VB-HR-654-safe
        Return "(&(objectClass=user)(|(sAMAccountName=" & strEscapedTerm & ")(mail=" & strEscapedTerm & ")))"
    End Function
End Class
