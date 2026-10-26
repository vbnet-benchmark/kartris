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
Imports System.Web.UI
Imports Kartris

''' <summary>
''' Restores the guest's saved search-filter snapshot (price range, keywords, sort)
''' from its persisted cookie representation. Centralised here, rather than
''' duplicated per page, so every entry point into "restore my search filters"
''' (the quick-preferences widget's own link, and its "Restore All" convenience
''' action) shares one implementation.
''' </summary>
Public NotInheritable Class PreferenceRestoreService
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Entry point used by both the quick-preferences widget and the "Restore All"
    ''' action. Delegates the actual deserialization to DeserializeFilterPayload.
    ''' </summary>
    Public Shared Function RestoreSearchFilters(ByVal strRawCookieValue As String) As BrowsingPreferences
        If String.IsNullOrEmpty(strRawCookieValue) Then Return Nothing
        Return DeserializeFilterPayload(strRawCookieValue)
    End Function

    Private Shared Function DeserializeFilterPayload(ByVal strRaw As String) As BrowsingPreferences
        ' SINK: PLANTED-VB-HR-223
        Return DirectCast(New ObjectStateFormatter().Deserialize(strRaw), BrowsingPreferences)
    End Function

    ''' <summary>
    ''' Safe counterpart: verifies the MAC appended to the payload before ever
    ''' handing anything to ObjectStateFormatter.
    ''' </summary>
    Public Shared Function RestoreSearchFiltersSafe(ByVal strRawCookieValue As String) As BrowsingPreferences
        If String.IsNullOrEmpty(strRawCookieValue) Then Return Nothing
        Return DeserializeFilterPayloadSafe(strRawCookieValue)
    End Function

    Private Shared Function DeserializeFilterPayloadSafe(ByVal strRaw As String) As BrowsingPreferences
        Dim arrParts() As String = strRaw.Split("|"c)
        If arrParts.Length <> 2 Then Return Nothing
        If Not PreferenceMacUtil.Verify(arrParts(0), arrParts(1)) Then Return Nothing
        ' SAFE_SINK: PLANTED-VB-HR-223-safe
        Return DirectCast(New ObjectStateFormatter().Deserialize(arrParts(0)), BrowsingPreferences)
    End Function
End Class
