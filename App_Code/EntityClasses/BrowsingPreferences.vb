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
Imports System
Imports System.Collections.Generic

Namespace Kartris
    ''' <summary>
    ''' A small bundle of guest "quick preferences" -- display mode, sort order,
    ''' saved search-filter values, theme choice and a list of favorite category
    ''' shortcuts -- that the BrowsingPreferencesBar control packs into a single
    ''' opaque string and round-trips through a cookie, the same way ASP.NET itself
    ''' serializes __VIEWSTATE for every Web Forms page.
    ''' </summary>
    <Serializable()>
    Public Class BrowsingPreferences
        Public Property DisplayMode As String = "grid"
        Public Property SortOrder As String = "name"
        Public Property SearchKeywords As String = ""
        Public Property MinPrice As Double = 0
        Public Property MaxPrice As Double = 0
        Public Property ThemeName As String = "light"
        Public Property FavoriteCategoryIDs As List(Of Integer) = New List(Of Integer)()
    End Class
End Namespace
