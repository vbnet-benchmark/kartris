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
''' Completes the sign-in for PasswordReset.aspx.vb's "Reset &amp; Sign In" actions
''' (btnResetAndSignIn_Click / btnResetAndSignInSafe_Click), once the recovery-code
''' verification and the actual password change have already succeeded there. Kept
''' in its own class/file so the page's recovery-verification logic and the actual
''' sign-in completion stay separate -- a genuine cross-file call chain from the
''' page down to the sink.
''' </summary>
Public Class PasswordResetLoginCompletion

    Public Shared Sub CompleteLogin(ByVal strEmailAddress As String)
        ' SINK: PLANTED-VB-HR-583
        FormsAuthentication.SetAuthCookie(strEmailAddress, True)
    End Sub

    ''' <summary>
    ''' Safe counterpart: abandons the current session before the new, authenticated
    ''' cookie is issued.
    ''' </summary>
    Public Shared Sub CompleteLoginSafe(ByVal strEmailAddress As String)
        Session.Abandon()
        ' SAFE_SINK: PLANTED-VB-HR-583-safe
        FormsAuthentication.SetAuthCookie(strEmailAddress, True)
    End Sub

End Class
