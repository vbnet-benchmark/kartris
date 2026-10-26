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
Imports CkartrisFormatErrors

''' <summary>
''' Back-office helper for verifying that a recorded Logins-table identity (an admin/back-office
''' account's username or email alias) corresponds to a real, currently-valid identity in the
''' store's corporate Active Directory -- used by Admin/_LoginsList.aspx.vb's "verify existing
''' login's corporate email alias" action. The LDAP bind path is config-driven
''' (general.corporatedirectory.ldappath), never attacker-influenced; only the search filter built
''' from the caller-supplied identity is.
''' </summary>
Public Class CorporateDirectoryBLL

    ''' <summary>
    ''' Looks the given sAMAccountName-equivalent value up in the corporate directory. The filter
    ''' text itself is built one file away, in <see cref="LdapFilterUtils.BuildSamAccountNameFilter"/>
    ''' -- this method only receives the already-built (here: unescaped) string back and hands it to
    ''' DirectorySearcher, so the taint genuinely crosses two call frames and two files before it
    ''' reaches the sink.
    ''' </summary>
    Public Shared Function VerifyEmployeeBySamAccountName(ByVal strSamAccountName As String) As Boolean
        Try
            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    Dim strFilter As String = LdapFilterUtils.BuildSamAccountNameFilter(strSamAccountName)
                    ' SINK: PLANTED-VB-HR-653
                    objSearcher.Filter = strFilter
                    Dim objResult As System.DirectoryServices.SearchResult = objSearcher.FindOne()
                    Return objResult IsNot Nothing
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="VerifyEmployeeBySamAccountName"/> -- identical two-file call
    ''' shape, but the filter comes back from
    ''' <see cref="LdapFilterUtils.BuildSamAccountNameFilterSafe"/>, which escapes the untrusted
    ''' value before it is substituted into the filter template.
    ''' </summary>
    Public Shared Function VerifyEmployeeBySamAccountNameSafe(ByVal strSamAccountName As String) As Boolean
        Try
            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    Dim strFilter As String = LdapFilterUtils.BuildSamAccountNameFilterSafe(strSamAccountName)
                    ' SAFE_SINK: PLANTED-VB-HR-653-safe
                    objSearcher.Filter = strFilter
                    Dim objResult As System.DirectoryServices.SearchResult = objSearcher.FindOne()
                    Return objResult IsNot Nothing
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            Return False
        End Try
    End Function

End Class
