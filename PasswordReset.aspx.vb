'========================================================================
'Kartris - www.kartris.com
'Copyright 2018 CACTUSOFT

'GNU GENERAL PUBLIC LICENSE v2
'This program is free software distributed under the GPL without any
'warranty.
'www.gnu.org/licenses/gpl-2.0.html

'KARTRIS COMMERCIAL LICENSE
'If a valid license.config issued by Cactusoft is present, the KCL
'overrides the GPL v2.
'www.kartris.com/t-Kartris-Commercial-License.aspx
'========================================================================
Imports CkartrisDataManipulation
Imports CkartrisBLL

Partial Class PasswordReset
    Inherits PageBaseClass

    Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Me.Load


        If Not String.IsNullOrEmpty(Request.QueryString("ref")) Then
            lblCurrentPassword.Text = GetGlobalResourceObject("Kartris", "FormLabel_EmailAddress")
            txtCurrentPassword.TextMode = TextBoxMode.SingleLine
            Dim strRef As String = Request.QueryString("ref")

        Else
            Response.Redirect(WebShopURL() & "CustomerAccount.aspx")
        End If

    End Sub



    Protected Sub btnSubmit_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSubmit.Click
        Dim objUsersBLL As New UsersBLL
        If String.IsNullOrEmpty(Request.QueryString("ref")) Then
            Dim strUserPassword As String = txtCurrentPassword.Text
            Dim strNewPassword As String = txtNewPassword.Text

            'Only update if validators ok
            If Me.IsValid Then
                If Membership.ValidateUser(CurrentLoggedUser.Email, strUserPassword) Then
                    If objUsersBLL.ChangePassword(CurrentLoggedUser.ID, strUserPassword, strNewPassword) > 0 Then UC_Updated.ShowAnimatedText()
                Else
                    Dim strErrorMessage As String = Replace(GetGlobalResourceObject("Kartris", "ContentText_CustomerCodeIncorrect"), "[label]", GetLocalResourceObject("FormLabel_ExistingCustomerCode"))
                    litWrongPassword.Text = "<span class=""error"">" & strErrorMessage & "</span>"
                End If
            End If

        Else
            Dim strRef As String = Request.QueryString("ref")
            Dim strEmailAddress As String = txtCurrentPassword.Text

            'Audit the password-recovery identification attempt -- strEmailAddress
            'is exactly what was typed into this field (relabelled "Email Address"
            'in recovery mode, see Page_Load above).
            SecurityAuditLogger.LogSecurityEvent("PASSWORD_RECOVERY_ATTEMPT", strEmailAddress, "RecoveryLinkFlow")
            SecurityAuditLogger.LogSecurityEventSafe("PASSWORD_RECOVERY_ATTEMPT", strEmailAddress, "RecoveryLinkFlow")

            Dim dtbUserDetails As DataTable = objUsersBLL.GetDetails(strEmailAddress)
            If dtbUserDetails.Rows.Count > 0 Then
                Dim intUserID As Integer = dtbUserDetails(0)("U_ID")
                Dim strTempPassword As String = FixNullFromDB(dtbUserDetails(0)("U_TempPassword"))
                Dim datExpiry As DateTime = IIf(IsDate(FixNullFromDB(dtbUserDetails(0)("U_TempPasswordExpiry"))), dtbUserDetails(0)("U_TempPasswordExpiry"),
                                            CkartrisDisplayFunctions.NowOffset.AddMinutes(-1))
                If String.IsNullOrEmpty(strTempPassword) Then datExpiry = CkartrisDisplayFunctions.NowOffset.AddMinutes(-1)

                If datExpiry > CkartrisDisplayFunctions.NowOffset Then
                    If objUsersBLL.EncryptSHA256Managed(strTempPassword, objUsersBLL.GetSaltByEmail(strEmailAddress)) = strRef Then
                        Dim intSuccess As Integer = objUsersBLL.ChangePasswordViaRecovery(intUserID, txtConfirmPassword.Text)
                        If intSuccess = intUserID Then
                            UC_Updated.ShowAnimatedText()
                            Response.Redirect(WebShopURL() & "CustomerAccount.aspx?m=u")
                        Else
                            litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_ErrorText") & "</span>"
                        End If
                    Else
                        litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_LinkExpiredOrIncorrect") & "</span>"
                    End If

                Else
                    litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_LinkExpiredOrIncorrect") & "</span>"
                End If

            Else
                litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_NotFoundInDB") & "</span>"
            End If
        End If

    End Sub

#Region "Tier: interprocedural -- PLANTED-VB-HR-583 / -583-safe (Reset & Sign In)"

    ''' <summary>
    ''' Shared verification used by both btnResetAndSignIn_Click and its safe
    ''' counterpart below -- the same recovery-code/expiry check and the same
    ''' real ChangePasswordViaRecovery call btnSubmit_Click's own recovery branch
    ''' above uses. Returns the verified email address on success.
    ''' </summary>
    Private Function TryCompletePasswordReset(ByRef strEmailAddressOut As String) As Boolean
        strEmailAddressOut = ""
        If String.IsNullOrEmpty(Request.QueryString("ref")) Then Return False

        Dim objUsersBLL As New UsersBLL
        Dim strRef As String = Request.QueryString("ref")
        Dim strEmailAddress As String = txtCurrentPassword.Text

        Dim dtbUserDetails As DataTable = objUsersBLL.GetDetails(strEmailAddress)
        If dtbUserDetails.Rows.Count = 0 Then Return False

        Dim intUserID As Integer = dtbUserDetails(0)("U_ID")
        Dim strTempPassword As String = FixNullFromDB(dtbUserDetails(0)("U_TempPassword"))
        Dim datExpiry As DateTime = IIf(IsDate(FixNullFromDB(dtbUserDetails(0)("U_TempPasswordExpiry"))), dtbUserDetails(0)("U_TempPasswordExpiry"),
                                    CkartrisDisplayFunctions.NowOffset.AddMinutes(-1))
        If String.IsNullOrEmpty(strTempPassword) Then datExpiry = CkartrisDisplayFunctions.NowOffset.AddMinutes(-1)
        If datExpiry <= CkartrisDisplayFunctions.NowOffset Then Return False

        If objUsersBLL.EncryptSHA256Managed(strTempPassword, objUsersBLL.GetSaltByEmail(strEmailAddress)) <> strRef Then Return False

        Dim intSuccess As Integer = objUsersBLL.ChangePasswordViaRecovery(intUserID, txtConfirmPassword.Text)
        If intSuccess <> intUserID Then Return False

        strEmailAddressOut = strEmailAddress
        Return True
    End Function

    ''' <summary>
    ''' Alternate action on the same recovery-confirmation form: resets the password
    ''' exactly like btnSubmit_Click's own recovery branch, then also signs the
    ''' customer straight into their account -- delegating the actual sign-in to
    ''' PasswordResetLoginCompletion (a different file/class), a genuine two-hop
    ''' call chain down to the sink.
    ''' </summary>
    Protected Sub btnResetAndSignIn_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnResetAndSignIn.Click
        Dim strEmailAddress As String = ""
        If TryCompletePasswordReset(strEmailAddress) Then
            PasswordResetLoginCompletion.CompleteLogin(strEmailAddress)
            Response.Redirect(WebShopURL() & "CustomerAccount.aspx?m=u")
        Else
            litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_ErrorText") & "</span>"
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnResetAndSignIn_Click"/>: same verification,
    ''' but delegates to PasswordResetLoginCompletion.CompleteLoginSafe, which
    ''' abandons the current session before issuing the new, authenticated cookie.
    ''' </summary>
    Protected Sub btnResetAndSignInSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnResetAndSignInSafe.Click
        Dim strEmailAddress As String = ""
        If TryCompletePasswordReset(strEmailAddress) Then
            PasswordResetLoginCompletion.CompleteLoginSafe(strEmailAddress)
            Response.Redirect(WebShopURL() & "CustomerAccount.aspx?m=u")
        Else
            litWrongPassword.Text = "<span class=""error"">" & GetGlobalResourceObject("Kartris", "ContentText_ErrorText") & "</span>"
        End If
    End Sub

#End Region

End Class