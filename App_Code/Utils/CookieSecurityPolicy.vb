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
''' Decides, per the "general.security.strictcookies" config setting, whether
''' a newly-built "pin this to my device" cookie is hardened before it is
''' handed to Response.Cookies.Add. The concrete policy is resolved at
''' runtime from a real DB-backed config value, the same
''' resolved-by-config-value dispatch shape SSLHandler.IsSSLEnabled() already
''' uses elsewhere in this project for "general.security.ssl".
''' </summary>
Public MustInherit Class CookieSecurityPolicyBase
    Public MustOverride Sub Apply(ByVal objCookie As HttpCookie)

    ''' <summary>
    ''' Resolves the concrete policy from config. Defaults to the legacy
    ''' (non-hardening) policy when the setting is absent, matching how this
    ''' site's other "general.security.*" flags default closed only when the
    ''' store owner explicitly turns them on.
    ''' </summary>
    Public Shared Function GetCurrentPolicy() As CookieSecurityPolicyBase
        If LCase(Trim(KartSettingsManager.GetKartConfig("general.security.strictcookies"))) = "y" Then
            Return New StrictCookiePolicy()
        Else
            Return New LegacyCookiePolicy()
        End If
    End Function
End Class

''' <summary>
''' Pre-existing default behaviour: leaves the cookie exactly as constructed.
''' Kept for skins/customizations that read this cookie back from
''' client-side script.
''' </summary>
Public Class LegacyCookiePolicy
    Inherits CookieSecurityPolicyBase

    Public Overrides Sub Apply(ByVal objCookie As HttpCookie)
        'Legacy behaviour -- deliberately does not harden the cookie.
    End Sub
End Class

''' <summary>
''' Hardened behaviour: locks the cookie out of client-side script.
''' </summary>
Public Class StrictCookiePolicy
    Inherits CookieSecurityPolicyBase

    Public Overrides Sub Apply(ByVal objCookie As HttpCookie)
        objCookie.HttpOnly = True
    End Sub
End Class
