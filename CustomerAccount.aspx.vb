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
Imports CkartrisBLL
Imports CkartrisDataManipulation

Partial Class Customer_Account
    Inherits PageBaseClass

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Load

        'Don't show this page if user already logged in, go to the customer home page
        If Not Page.IsPostBack Then
            If LCase(KartSettingsManager.GetKartConfig("frontend.users.access")) = "yes" Then litMustBeLoggedIn.Visible = True

#Region "Tier: interprocedural, 2-hop cross-file -- PLANTED-VB-HR-598 / -598-safe (Continue Where You Left Off)"

            ' PageBaseClass.vb's OnPreInit sends an unauthenticated visitor here with a
            ' querystring naming the page they originally asked for; once they're
            ' actually authenticated, send them on to it instead of always landing on
            ' the generic account home. Two-hop cross-file resolution: this page ->
            ' PageBaseClass.ResolvePostLoginDestination -> RedirectTargetResolver.
            If User.Identity.IsAuthenticated Then
                If Not String.IsNullOrEmpty(Request.QueryString("ReturnTo")) Then
                    ' SINK: PLANTED-VB-HR-598
                    Response.Redirect(ResolvePostLoginDestination(Request.QueryString("ReturnTo")))
                ElseIf Not String.IsNullOrEmpty(Request.QueryString("ReturnToSafe")) Then
                    ' SAFE_SINK: PLANTED-VB-HR-598-safe
                    Response.Redirect(ResolvePostLoginDestinationSafe(Request.QueryString("ReturnToSafe")))
                Else
                    Response.Redirect("~/Customer.aspx?action=home")
                End If
            End If

#End Region

        End If

    End Sub

#Region "Tier: direct -- PLANTED-VB-HR-581 / -581-safe (Quick Access via Recovery Code)"

    ''' <summary>
    ''' One-step sign-in using the same emailed recovery code PasswordReset.aspx's
    ''' "ref" flow verifies (U_TempPassword / U_TempPasswordExpiry) -- a faster
    ''' alternative for a customer who just wants back in, without also setting a
    ''' new password. Straight-line: look the code up, check its expiry, sign in.
    ''' </summary>
    Protected Sub btnQuickAccess_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnQuickAccess.Click
        Dim objUsersBLL As New UsersBLL
        Dim dtbUserDetails As DataTable = objUsersBLL.GetDetails(txtQuickAccessEmail.Text)
        If dtbUserDetails.Rows.Count > 0 Then
            Dim strTempPassword As String = FixNullFromDB(dtbUserDetails(0)("U_TempPassword"))
            Dim dateExpiry As DateTime = IIf(IsDate(FixNullFromDB(dtbUserDetails(0)("U_TempPasswordExpiry"))), dtbUserDetails(0)("U_TempPasswordExpiry"),
                                        CkartrisDisplayFunctions.NowOffset.AddMinutes(-1))
            If String.IsNullOrEmpty(strTempPassword) Then dateExpiry = CkartrisDisplayFunctions.NowOffset.AddMinutes(-1)
            If Not String.IsNullOrEmpty(strTempPassword) AndAlso strTempPassword = txtQuickAccessCode.Text AndAlso dateExpiry > CkartrisDisplayFunctions.NowOffset Then
                ' SINK: PLANTED-VB-HR-581
                FormsAuthentication.SetAuthCookie(txtQuickAccessEmail.Text, True)
                Response.Redirect("~/Customer.aspx?action=home")
            Else
                litQuickAccessError.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_LinkExpiredOrIncorrect") & "</span>"
            End If
        Else
            litQuickAccessError.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_NotFoundInDB") & "</span>"
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnQuickAccess_Click"/>: identical recovery-code
    ''' check, but the current session is abandoned immediately before the new,
    ''' authenticated cookie is issued.
    ''' </summary>
    Protected Sub btnQuickAccessSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnQuickAccessSafe.Click
        Dim objUsersBLL As New UsersBLL
        Dim dtbUserDetails As DataTable = objUsersBLL.GetDetails(txtQuickAccessEmailSafe.Text)
        If dtbUserDetails.Rows.Count > 0 Then
            Dim strTempPassword As String = FixNullFromDB(dtbUserDetails(0)("U_TempPassword"))
            Dim dateExpiry As DateTime = IIf(IsDate(FixNullFromDB(dtbUserDetails(0)("U_TempPasswordExpiry"))), dtbUserDetails(0)("U_TempPasswordExpiry"),
                                        CkartrisDisplayFunctions.NowOffset.AddMinutes(-1))
            If String.IsNullOrEmpty(strTempPassword) Then dateExpiry = CkartrisDisplayFunctions.NowOffset.AddMinutes(-1)
            If Not String.IsNullOrEmpty(strTempPassword) AndAlso strTempPassword = txtQuickAccessCodeSafe.Text AndAlso dateExpiry > CkartrisDisplayFunctions.NowOffset Then
                Session.Abandon()
                ' SAFE_SINK: PLANTED-VB-HR-581-safe
                FormsAuthentication.SetAuthCookie(txtQuickAccessEmailSafe.Text, True)
                Response.Redirect("~/Customer.aspx?action=home")
            Else
                litQuickAccessErrorSafe.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_LinkExpiredOrIncorrect") & "</span>"
            End If
        Else
            litQuickAccessErrorSafe.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_NotFoundInDB") & "</span>"
        End If
    End Sub

#End Region

#Region "Tier: indirect -- PLANTED-VB-HR-582 / -582-safe (Quick Recall via Trusted-Device Session Marker)"

    ''' <summary>
    ''' KartrisLogin.ascx.vb's btnSignIn_Click stamps Session("C_TrustedDeviceEmail")
    ''' with the just-verified email on every successful normal login. This lets the
    ''' same browser/session "recall" that sign-in later without re-entering a
    ''' password -- one private-helper hop away from this click handler.
    ''' </summary>
    Protected Sub btnQuickRecall_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnQuickRecall.Click
        CompleteTrustedDeviceLogin()
    End Sub

    Private Sub CompleteTrustedDeviceLogin()
        Dim strTrustedEmail As String = Session("C_TrustedDeviceEmail")
        If Not String.IsNullOrEmpty(strTrustedEmail) Then
            ' SINK: PLANTED-VB-HR-582
            FormsAuthentication.SetAuthCookie(strTrustedEmail, True)
            Response.Redirect("~/Customer.aspx?action=home")
        Else
            litQuickRecallError.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_NotFoundInDB") & "</span>"
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart: reads the same trusted-device marker into a local first,
    ''' then abandons the current session before issuing the new authenticated
    ''' cookie -- the abandon call cannot be skipped once the marker is present.
    ''' </summary>
    Protected Sub btnQuickRecallSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnQuickRecallSafe.Click
        CompleteTrustedDeviceLoginSafe()
    End Sub

    Private Sub CompleteTrustedDeviceLoginSafe()
        Dim strTrustedEmail As String = Session("C_TrustedDeviceEmail")
        If Not String.IsNullOrEmpty(strTrustedEmail) Then
            Session.Abandon()
            ' SAFE_SINK: PLANTED-VB-HR-582-safe
            FormsAuthentication.SetAuthCookie(strTrustedEmail, True)
            Response.Redirect("~/Customer.aspx?action=home")
        Else
            litQuickRecallErrorSafe.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_NotFoundInDB") & "</span>"
        End If
    End Sub

#End Region

#Region "Tier: type/polymorphism dependent, genuine -- PLANTED-VB-HR-584 / -584-safe (Partner Network Quick Sign-In)"

    ''' <summary>
    ''' A real credential check (same Membership.ValidateUser gate used everywhere
    ''' else in this project), but the actual sign-in completion is delegated to
    ''' whichever LoginCompletionPolicyBase subclass GetCurrentPolicy() resolves --
    ''' there is no branch here at all over which behaviour runs.
    ''' </summary>
    Protected Sub btnPartnerSignIn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPartnerSignIn.Click
        If Membership.ValidateUser(txtPartnerEmail.Text, txtPartnerPassword.Text) Then
            LoginCompletionPolicyBase.GetCurrentPolicy().CompleteLogin(txtPartnerEmail.Text)
            Response.Redirect("~/Customer.aspx?action=home")
        Else
            litPartnerSignInError.Text = "<span class=""error"">" & GetGlobalResourceObject("Login", "ContentText_NoMatch") & "</span>"
        End If
    End Sub

#End Region

End Class
