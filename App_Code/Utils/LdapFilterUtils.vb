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
''' Shared helpers for building LDAP search-filter strings for the corporate-directory
''' verification tools (Admin/_LoginsList.aspx.vb, Admin/_StaffDirectoryLookup.aspx.vb). Also
''' hosts <see cref="CorporateDirectoryBLL"/>'s interprocedural filter-building step -- the raw,
''' unescaped builder here is called from that BLL class, not used directly on a search result
''' itself.
''' </summary>
Public Class LdapFilterUtils

    ''' <summary>
    ''' Escapes the five RFC 4515 LDAP-filter-special characters (backslash, asterisk, the two
    ''' parentheses, and NUL) in a single character-by-character pass. Deliberately NOT a sequence
    ''' of String.Replace() calls -- escaping one character at a time into a fresh buffer means no
    ''' escape sequence this function just emitted can ever be re-matched by a later step, which a
    ''' chained-Replace approach cannot guarantee unless the replacements are ordered exactly right
    ''' (backslash strictly first).
    ''' </summary>
    Public Shared Function EscapeLdapFilterValue(ByVal strValue As String) As String
        If strValue Is Nothing Then Return String.Empty

        Dim sbEscaped As New System.Text.StringBuilder(strValue.Length)
        For Each chCurrent As Char In strValue
            Select Case chCurrent
                Case "\"c
                    sbEscaped.Append("\5c")
                Case "*"c
                    sbEscaped.Append("\2a")
                Case "("c
                    sbEscaped.Append("\28")
                Case ")"c
                    sbEscaped.Append("\29")
                Case Chr(0)
                    sbEscaped.Append("\00")
                Case Else
                    sbEscaped.Append(chCurrent)
            End Select
        Next
        Return sbEscaped.ToString()
    End Function

    ''' <summary>
    ''' Builds the raw "find a user by sAMAccountName" filter with no escaping. Called by
    ''' <see cref="CorporateDirectoryBLL.VerifyEmployeeBySamAccountName"/> -- the concatenation
    ''' happens here, the hand-off to DirectorySearcher.Filter happens back in that caller, which is
    ''' the genuine two-file hop for the interprocedural plant instance.
    ''' </summary>
    Public Shared Function BuildSamAccountNameFilter(ByVal strSamAccountName As String) As String
        Return "(&(objectClass=user)(sAMAccountName=" & strSamAccountName & "))"
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="BuildSamAccountNameFilter"/> -- same template, but the
    ''' untrusted value is escaped via <see cref="EscapeLdapFilterValue"/> before being substituted.
    ''' </summary>
    Public Shared Function BuildSamAccountNameFilterSafe(ByVal strSamAccountName As String) As String
        Return "(&(objectClass=user)(sAMAccountName=" & EscapeLdapFilterValue(strSamAccountName) & "))"
    End Function

End Class
