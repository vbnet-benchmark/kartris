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
''' Strategy for restoring a BrowsingPreferences object from its persisted
''' "theme preference" cookie representation. Two format generations exist side by
''' side: the original "legacy" format (reuses LosFormatter directly -- the same
''' formatter ASP.NET's own ViewState machinery uses) and a newer "signed" format
''' that MAC-protects the payload before trusting it. Which concrete strategy runs
''' is picked, at runtime, by the format tag the client's own ThemePrefs cookie
''' claims to use -- see BrowsingPreferencesBar.ascx.vb's GetThemeStrategy.
''' </summary>
Public MustInherit Class PreferenceFormatStrategy
    Public MustOverride Function Restore(ByVal strPayload As String) As BrowsingPreferences
End Class

''' <summary>
''' Original theme-preference format. Reuses LosFormatter directly, exactly like
''' ASP.NET's own ViewState machinery -- but, unlike framework ViewState, this
''' custom reuse of it has no EnableViewStateMac-style integrity check of its own.
''' </summary>
Public Class LegacyThemeFormatStrategy
    Inherits PreferenceFormatStrategy

    Public Overrides Function Restore(ByVal strPayload As String) As BrowsingPreferences
        If String.IsNullOrEmpty(strPayload) Then Return Nothing
        ' SINK: PLANTED-VB-HR-224
        Return DirectCast(New LosFormatter().Deserialize(strPayload), BrowsingPreferences)
    End Function
End Class

''' <summary>
''' Newer, MAC-protected theme-preference format. The payload and its signature
''' travel together (separated by "|"); the signature is verified before
''' LosFormatter ever sees the payload, so a tampered cookie is rejected instead of
''' deserialized.
''' </summary>
Public Class SignedThemeFormatStrategy
    Inherits PreferenceFormatStrategy

    Public Overrides Function Restore(ByVal strPayload As String) As BrowsingPreferences
        If String.IsNullOrEmpty(strPayload) Then Return Nothing
        Dim arrParts() As String = strPayload.Split("|"c)
        If arrParts.Length <> 2 Then Return Nothing
        If Not PreferenceMacUtil.Verify(arrParts(0), arrParts(1)) Then Return Nothing
        ' SAFE_SINK: PLANTED-VB-HR-224-safe
        Return DirectCast(New LosFormatter().Deserialize(arrParts(0)), BrowsingPreferences)
    End Function
End Class
