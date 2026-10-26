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
''' Allow-list sanitizer for a memcache key component, shared by every safe twin introduced for this
''' CWE-20 "Memcache Injection" sub-pattern plant round (PLANTED-VB-HR-756..760). Keeps only
''' letters, digits, "-" and "_", dropping everything else -- including CR, LF, every other control
''' character, and plain whitespace, all of which are either directly protocol-significant to
''' memcached's own text protocol or (for whitespace) act as a key terminator -- then bounds the
''' result to MaxKeyComponentLength characters so even a maximal-length stored field (e.g. a product
''' name) stays comfortably inside memcached's own 250-byte key limit once the fixed key prefixes
''' used throughout this round are added on top.
'''
''' Mirrors GrimrieberOnlineShop's Services/MemcacheKeyBuilder.vb (same CWE-20 sub-pattern, same
''' planting campaign, independently re-derived from the shared, engine-blind planting-research
''' document rather than copied wholesale -- the shape is identical because the underlying mechanism
''' is identical, not because one project's code was read to write the other's).
''' </summary>
Public Class MemcacheKeyBuilder

    Private Const MaxKeyComponentLength As Integer = 60

    ''' <summary>
    ''' Keeps only [A-Za-z0-9_-], dropping CR/LF/control characters/whitespace/everything else, then
    ''' bounds the result to MaxKeyComponentLength characters. NOT the same as a length cap alone --
    ''' a length cap with no charset check (see this project's own <MaxLength> stored fields) still
    ''' leaves room for a short CRLF-carrying payload; this strips the dangerous bytes outright.
    ''' </summary>
    Public Shared Function SanitizeKeyComponent(ByVal strRaw As String) As String
        If String.IsNullOrEmpty(strRaw) Then Return String.Empty

        Dim objBuilder As New System.Text.StringBuilder()
        For Each chrCurrent As Char In strRaw
            If Char.IsLetterOrDigit(chrCurrent) OrElse chrCurrent = "-"c OrElse chrCurrent = "_"c Then
                objBuilder.Append(chrCurrent)
            End If
        Next

        Dim strCleaned As String = objBuilder.ToString()
        If strCleaned.Length > MaxKeyComponentLength Then
            strCleaned = strCleaned.Substring(0, MaxKeyComponentLength)
        End If
        Return strCleaned
    End Function

End Class
