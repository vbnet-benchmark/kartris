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

Partial Class Admin_Webhooks
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = "Webhooks & Branding"
    End Sub

    Protected Sub btnTestWebhook_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnTestWebhook.Click
        litWebhookResult.Text = Server.HtmlEncode(WebhookNotifier.TestWebhookReachable(txtWebhookUrl.Text))
    End Sub

    Protected Sub btnTestWebhookSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnTestWebhookSafe.Click
        litWebhookResult.Text = Server.HtmlEncode(WebhookNotifier.TestWebhookReachable_Safe(txtWebhookUrl.Text))
    End Sub

    Protected Sub btnRegisterWebhook_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRegisterWebhook.Click
        Dim blnOk As Boolean = WebhookRegistrationBLL.RegisterAndVerifyWebhook(txtWebhookUrl.Text)
        litWebhookResult.Text = If(blnOk, "Webhook registered and verified.", "Webhook could not be verified.")
    End Sub

    Protected Sub btnRegisterWebhookSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRegisterWebhookSafe.Click
        Dim blnOk As Boolean = WebhookRegistrationBLL.RegisterAndVerifyWebhook_Safe(txtWebhookUrl.Text)
        litWebhookResult.Text = If(blnOk, "Webhook registered and verified.", "Webhook could not be verified.")
    End Sub

    Protected Sub btnSendOrderNotification_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSendOrderNotification.Click
        Dim intMerchantId As Integer = 0
        Integer.TryParse(txtNotifyMerchantId.Text, intMerchantId)
        Dim objChannel As IOrderNotificationChannel = OrderNotificationChannelFactory.GetChannel(intMerchantId)
        Dim blnOk As Boolean = objChannel.SendOrderUpdate(txtNotifyUrl.Text, txtNotifyPayload.Text)
        litNotifyResult.Text = If(blnOk, "Notification sent.", "Notification failed.")
    End Sub

    Protected Sub btnPreviewLogo_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewLogo.Click
        Dim objRequest As New LogoPreviewRequest()
        objRequest.ImageUrl = txtLogoUrl.Text
        Dim bytImage() As Byte = LogoPreviewService.FetchLogoPreview(objRequest)
        litLogoResult.Text = "Fetched " & If(bytImage Is Nothing, 0, bytImage.Length) & " bytes."
    End Sub

    Protected Sub btnPreviewLogoSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewLogoSafe.Click
        Dim objRequest As New LogoPreviewRequest()
        objRequest.ImageUrl = txtLogoUrl.Text
        Dim bytImage() As Byte = LogoPreviewService.FetchLogoPreview_Safe(objRequest)
        litLogoResult.Text = "Fetched " & If(bytImage Is Nothing, 0, bytImage.Length) & " bytes."
    End Sub

    Protected Sub btnPreviewLogoFromParts_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewLogoFromParts.Click
        Dim objJob As New LogoFetchJob()
        objJob.SetSource(txtLogoBaseHost.Text, txtLogoPath.Text)
        Dim bytImage() As Byte = objJob.FetchWithRetry()
        litLogoPartsResult.Text = "Fetched " & If(bytImage Is Nothing, 0, bytImage.Length) & " bytes."
    End Sub

    Protected Sub btnPreviewLogoFromPartsSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewLogoFromPartsSafe.Click
        Dim objJob As New LogoFetchJob()
        objJob.SetSource_Safe(txtLogoBaseHost.Text, txtLogoPath.Text)
        Dim bytImage() As Byte = objJob.FetchWithRetry_Safe()
        litLogoPartsResult.Text = "Fetched " & If(bytImage Is Nothing, 0, bytImage.Length) & " bytes."
    End Sub

    ''' <summary>
    ''' Interprocedural CWE-295 (hop 0 of 2): forwards the admin-entered URL to
    ''' WebhookTlsHealthCheckBLL (a different file/class), which forwards it again to
    ''' TlsEndpointProber (a third file/class), which performs the actual HTTPS connection with no
    ''' certificate validation.
    ''' </summary>
    Protected Sub btnCheckWebhookTls_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCheckWebhookTls.Click
        Dim blnOk As Boolean = WebhookTlsHealthCheckBLL.CheckEndpoint(txtWebhookTlsUrl.Text)
        litWebhookTlsResult.Text = If(blnOk, "Endpoint reachable.", "Endpoint unreachable.")
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnCheckWebhookTls_Click"/>.
    ''' </summary>
    Protected Sub btnCheckWebhookTlsSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCheckWebhookTlsSafe.Click
        Dim blnOk As Boolean = WebhookTlsHealthCheckBLL.CheckEndpointSafe(txtWebhookTlsUrl.Text)
        litWebhookTlsResult.Text = If(blnOk, "Endpoint reachable.", "Endpoint unreachable.")
    End Sub

    ' --- Partner Settlement Secure Export (CWE-326) ---------------------
    ' Direct tier: the cipher is instantiated straight-line in the click handler itself,
    ' no helper, no branch.

    Protected Sub btnExportSettlementDirect_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementDirect.Click
        Dim objAlg As New System.Security.Cryptography.TripleDESCryptoServiceProvider() ' SINK: PLANTED-VB-HR-751
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(txtSettlementSummaryDirect.Text)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        litSettlementDirectResult.Text = "Exported settlement summary (" & bytCipher.Length & " bytes ciphertext), IV=" & Convert.ToBase64String(objAlg.IV)
    End Sub

    Protected Sub btnExportSettlementDirectSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementDirectSafe.Click
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-751-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(txtSettlementSummaryDirect.Text)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        litSettlementDirectResult.Text = "Exported settlement summary (" & bytCipher.Length & " bytes ciphertext, hardened), IV=" & Convert.ToBase64String(objAlg.IV)
    End Sub

    ' Indirect tier: same-class private helper performs the actual cipher call, one hop
    ' away from the postback handler.

    Protected Sub btnExportSettlementStaged_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementStaged.Click
        litSettlementStagedResult.Text = Server.HtmlEncode(EncryptSettlementSummaryStaged(txtSettlementSummaryStaged.Text))
    End Sub

    Protected Sub btnExportSettlementStagedSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementStagedSafe.Click
        litSettlementStagedResult.Text = Server.HtmlEncode(EncryptSettlementSummaryStagedSafe(txtSettlementSummaryStaged.Text))
    End Sub

    Private Function EncryptSettlementSummaryStaged(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.TripleDESCryptoServiceProvider() ' SINK: PLANTED-VB-HR-752
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return "Staged export ready (" & bytCipher.Length & " bytes ciphertext), IV=" & Convert.ToBase64String(objAlg.IV)
    End Function

    Private Function EncryptSettlementSummaryStagedSafe(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-752-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return "Staged export ready (" & bytCipher.Length & " bytes ciphertext, hardened), IV=" & Convert.ToBase64String(objAlg.IV)
    End Function

    ''' <summary>
    ''' Interprocedural tier (genuine 2-hop cross-file): forwards the admin-entered settlement
    ''' summary to PartnerSettlementExportBLL.PrepareSecureManifest (hop 1, a different
    ''' file/class), which forwards it again to SettlementCipherService.Encrypt (hop 2, a third
    ''' file/class) -- the class that actually performs the cipher call.
    ''' </summary>
    Protected Sub btnExportSettlementManifest_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementManifest.Click
        litSettlementManifestResult.Text = Server.HtmlEncode(PartnerSettlementExportBLL.PrepareSecureManifest(txtSettlementSummaryManifest.Text))
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnExportSettlementManifest_Click"/>.
    ''' </summary>
    Protected Sub btnExportSettlementManifestSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementManifestSafe.Click
        litSettlementManifestResult.Text = Server.HtmlEncode(PartnerSettlementExportBLL.PrepareSecureManifestSafe(txtSettlementSummaryManifest.Text))
    End Sub

    ''' <summary>
    ''' Dispatches through SettlementCipherPolicyFactory -- Type/polymorphism-dependent CWE-326:
    ''' no branching of any kind is visible here. Whether the resolved policy encrypts with the
    ''' legacy TripleDES cipher (LegacySettlementCipherPolicy) or the modern AES-256 cipher
    ''' (ModernSettlementCipherPolicy) depends purely on a config value the factory reads for
    ''' itself.
    ''' </summary>
    Protected Sub btnExportSettlementPolicy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementPolicy.Click
        Dim objPolicy As SettlementCipherPolicyBase = SettlementCipherPolicyFactory.GetCurrentPolicy()
        litSettlementPolicyResult.Text = Server.HtmlEncode(objPolicy.Encrypt(txtSettlementSummaryPolicy.Text))
    End Sub

    ' Diversity tier: batch/loop over several partner settlement summaries (one per line),
    ' each independently encrypted -- neither item is a dead end, both feed the rendered report.

    Protected Sub btnExportSettlementBatch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementBatch.Click
        Dim strSummaries() As String = txtSettlementSummaryBatch.Text.Split(";"c)
        Dim sbdReport As New System.Text.StringBuilder()
        For Each strSummary As String In strSummaries
            Dim objAlg As New System.Security.Cryptography.TripleDESCryptoServiceProvider() ' SINK: PLANTED-VB-HR-755
            objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
            objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
            objAlg.GenerateKey()
            objAlg.GenerateIV()
            Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
            Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
            Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
            sbdReport.Append("Partner record encrypted (" & bytCipher.Length & " bytes). ")
        Next
        litSettlementBatchResult.Text = sbdReport.ToString()
    End Sub

    Protected Sub btnExportSettlementBatchSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportSettlementBatchSafe.Click
        Dim strSummaries() As String = txtSettlementSummaryBatch.Text.Split(";"c)
        Dim sbdReport As New System.Text.StringBuilder()
        For Each strSummary As String In strSummaries
            Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-755-safe
            objAlg.KeySize = 256
            objAlg.BlockSize = 128
            objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
            objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
            objAlg.GenerateKey()
            objAlg.GenerateIV()
            Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
            Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
            Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
            sbdReport.Append("Partner record encrypted (" & bytCipher.Length & " bytes, hardened). ")
        Next
        litSettlementBatchResult.Text = sbdReport.ToString()
    End Sub

End Class
