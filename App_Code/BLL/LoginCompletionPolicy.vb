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
''' Decides, per the "general.security.strictsessionfixation" config setting, how a
''' just-verified sign-in (e.g. CustomerAccount.aspx's "Partner Network Quick
''' Sign-In") is actually completed. The concrete policy is resolved at runtime from
''' a real DB-backed config value, the same resolved-by-config-value dispatch shape
''' CookieSecurityPolicyBase.GetCurrentPolicy() already uses elsewhere in this
''' project for "general.security.strictcookies".
''' </summary>
Public MustInherit Class LoginCompletionPolicyBase
    Public MustOverride Sub CompleteLogin(ByVal strEmailAddress As String)

    ''' <summary>
    ''' Resolves the concrete policy from config. Defaults to the legacy
    ''' (non-hardening) policy when the setting is absent, matching how this
    ''' site's other "general.security.*" flags default closed only when the
    ''' store owner explicitly turns them on.
    ''' </summary>
    Public Shared Function GetCurrentPolicy() As LoginCompletionPolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.security.strictsessionfixation"))) = "y" Then
            Return New HardenedLoginCompletionPolicy()
        Else
            Return New LegacyLoginCompletionPolicy()
        End If
    End Function
End Class

''' <summary>
''' Pre-existing default behaviour: issues the authenticated cookie directly,
''' leaving whatever session the browser already held in place.
''' </summary>
Public Class LegacyLoginCompletionPolicy
    Inherits LoginCompletionPolicyBase

    Public Overrides Sub CompleteLogin(ByVal strEmailAddress As String)
        ' SINK: PLANTED-VB-HR-584
        FormsAuthentication.SetAuthCookie(strEmailAddress, True)
    End Sub
End Class

''' <summary>
''' Hardened behaviour: abandons the current session before the new, authenticated
''' cookie is issued.
''' </summary>
Public Class HardenedLoginCompletionPolicy
    Inherits LoginCompletionPolicyBase

    Public Overrides Sub CompleteLogin(ByVal strEmailAddress As String)
        Session.Abandon()
        ' SAFE_SINK: PLANTED-VB-HR-584-safe
        FormsAuthentication.SetAuthCookie(strEmailAddress, True)
    End Sub
End Class
