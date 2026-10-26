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
''' Back-office "Corporate Staff Directory Lookup" tool -- a small companion to
''' Admin/_LoginsList.aspx's per-account directory-verify actions, for ad-hoc directory search
''' (quick search, policy-controlled search, batch verify) rather than verifying one specific
''' Logins-table row.
''' </summary>
Partial Class Admin_StaffDirectoryLookup
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = "Corporate Staff Directory Lookup" & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")
    End Sub

    ''' <summary>
    ''' Builds the "quick search" filter from the raw search term with no escaping. One-hop,
    ''' same-class helper -- called only by <see cref="btnQuickSearch_Click"/>.
    ''' </summary>
    Private Function BuildQuickSearchFilter(ByVal strSearchTerm As String) As String
        Return "(&(objectClass=user)(sAMAccountName=" & strSearchTerm & "))"
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="BuildQuickSearchFilter"/> -- escapes the search term via
    ''' LdapFilterUtils.EscapeLdapFilterValue before substituting it into the filter template.
    ''' </summary>
    Private Function BuildQuickSearchFilterSafe(ByVal strSearchTerm As String) As String
        Return "(&(objectClass=user)(sAMAccountName=" & LdapFilterUtils.EscapeLdapFilterValue(strSearchTerm) & "))"
    End Function

    ''' <summary>
    ''' Indirect plant instance: reads txtQuickSearch.Text and passes it through the private
    ''' BuildQuickSearchFilter helper above before handing the result to DirectorySearcher.Filter.
    ''' </summary>
    Protected Sub btnQuickSearch_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    Dim strFilter As String = BuildQuickSearchFilter(txtQuickSearch.Text)
                    ' SINK: PLANTED-VB-HR-652
                    objSearcher.Filter = strFilter
                    Dim objResult As System.DirectoryServices.SearchResult = objSearcher.FindOne()
                    lblQuickSearchResult.Text = If(objResult IsNot Nothing, "Match found.", "No match.")
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblQuickSearchResult.Text = "Directory lookup failed."
        End Try
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnQuickSearch_Click"/>, via BuildQuickSearchFilterSafe.
    ''' </summary>
    Protected Sub btnQuickSearchSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    Dim strFilter As String = BuildQuickSearchFilterSafe(txtQuickSearch.Text)
                    ' SAFE_SINK: PLANTED-VB-HR-652-safe
                    objSearcher.Filter = strFilter
                    Dim objResult As System.DirectoryServices.SearchResult = objSearcher.FindOne()
                    lblQuickSearchResult.Text = If(objResult IsNot Nothing, "Match found.", "No match.")
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblQuickSearchResult.Text = "Directory lookup failed."
        End Try
    End Sub

    ''' <summary>
    ''' Type/polymorphism-dependent plant instance: the search term from txtPolicySearch.Text flows
    ''' into a single, non-branching call site -- EmployeeDirectoryFilterBuilderBase.GetCurrentBuilder()
    ''' resolves which concrete builder (escaping or not) runs, entirely from the
    ''' "general.security.strictldapescaping" config setting. This method itself never checks that
    ''' setting or branches on it.
    ''' </summary>
    Protected Sub btnPolicySearch_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    objSearcher.Filter = EmployeeDirectoryFilterBuilderBase.GetCurrentBuilder().BuildFilter(txtPolicySearch.Text)
                    Dim objResult As System.DirectoryServices.SearchResultCollection = objSearcher.FindAll()
                    lblPolicySearchResult.Text = objResult.Count & " match(es) found."
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblPolicySearchResult.Text = "Directory lookup failed."
        End Try
    End Sub

    ''' <summary>
    ''' Diversity plant instance: loops over one username per line of txtBatchUsernames.Text,
    ''' building a single compound "(|...)" OR-filter across every submitted username, with no
    ''' escaping -- a materially different construction shape (loop-accumulated compound filter)
    ''' from the straight-line/single-clause instances above, mirroring this project's existing
    ''' TicketsBLL.BulkAppendTicketTag loop-building-a-batch-statement shape for a different sink.
    ''' </summary>
    Protected Sub btnBatchVerify_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim strClauses As String = ""
            For Each strUsername As String In txtBatchUsernames.Text.Replace(vbCr, "").Split(vbLf)
                If strUsername.Trim().Length = 0 Then Continue For
                strClauses &= "(sAMAccountName=" & strUsername.Trim() & ")"
            Next

            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    ' SINK: PLANTED-VB-HR-655
                    objSearcher.Filter = "(&(objectClass=user)(|" & strClauses & "))"
                    Dim objResults As System.DirectoryServices.SearchResultCollection = objSearcher.FindAll()
                    lblBatchVerifyResult.Text = objResults.Count & " match(es) found."
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblBatchVerifyResult.Text = "Directory lookup failed."
        End Try
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnBatchVerify_Click"/> -- same loop-accumulated compound
    ''' filter shape, but each submitted username is escaped via
    ''' LdapFilterUtils.EscapeLdapFilterValue before being appended to the clause list.
    ''' </summary>
    Protected Sub btnBatchVerifySafe_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Dim strClauses As String = ""
            For Each strUsername As String In txtBatchUsernames.Text.Replace(vbCr, "").Split(vbLf)
                If strUsername.Trim().Length = 0 Then Continue For
                strClauses &= "(sAMAccountName=" & LdapFilterUtils.EscapeLdapFilterValue(strUsername.Trim()) & ")"
            Next

            Using objEntry As New System.DirectoryServices.DirectoryEntry(KartSettingsManager.GetKartConfig("general.corporatedirectory.ldappath"))
                Using objSearcher As New System.DirectoryServices.DirectorySearcher(objEntry)
                    ' SAFE_SINK: PLANTED-VB-HR-655-safe
                    objSearcher.Filter = "(&(objectClass=user)(|" & strClauses & "))"
                    Dim objResults As System.DirectoryServices.SearchResultCollection = objSearcher.FindAll()
                    lblBatchVerifyResult.Text = objResults.Count & " match(es) found."
                End Using
            End Using
        Catch ex As Exception
            ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            lblBatchVerifyResult.Text = "Directory lookup failed."
        End Try
    End Sub

End Class
