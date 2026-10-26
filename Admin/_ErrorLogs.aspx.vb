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
Imports CkartrisEnumerations

Partial Class Admin_ErrorLogs
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_DBAdmin", "ContentText_ErrorLogs") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

    End Sub

    ' --- Error Log Secure Archive (CWE-327) -----------------------------
    ' Packages accumulated error-log text into an encrypted archive bundle before it is
    ' handed off to support / a monitoring vendor. Recurring, identical error messages (the
    ' same stack trace firing thousands of times during an incident) are deliberately
    ' encrypted so that identical plaintext chunks produce identical ciphertext chunks -- a
    ' cheap block-level dedup/compare pass can then be run on the archive before upload,
    ' saving bandwidth on a noisy error flood. Entirely disjoint from this project's own
    ' "Partner Settlement Secure Export" CWE-326 plant (App_Code/Utils/SettlementCipherPolicy.vb
    ' / SettlementCipherService.vb / Admin/_Webhooks.aspx.vb) -- no file touched by that round
    ' is modified here, and the algorithm/key size is held identical between the vulnerable
    ' and hardened twins below; only the cipher MODE (and the presence of IV generation)
    ' differs.

    ' Direct tier: the cipher is instantiated straight-line in the click handler itself, no
    ' helper, no branch.

    Protected Sub btnArchiveQuick_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveQuick.Click
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SINK: PLANTED-VB-HR-766
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.ECB
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(txtArchiveQuick.Text)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        litArchiveQuickResult.Text = "Archived error log chunk (" & bytCipher.Length & " bytes ciphertext, dedup-friendly)."
    End Sub

    Protected Sub btnArchiveQuickSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveQuickSafe.Click
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-766-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(txtArchiveQuick.Text)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        litArchiveQuickResult.Text = "Archived error log chunk (" & bytCipher.Length & " bytes ciphertext, hardened), IV=" & Convert.ToBase64String(objAlg.IV)
    End Sub

    ' Indirect tier: same-class private helper performs the actual cipher call, one hop away
    ' from the postback handler.

    Protected Sub btnArchiveStaged_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveStaged.Click
        litArchiveStagedResult.Text = Server.HtmlEncode(EncryptErrorLogArchiveStaged(txtArchiveStaged.Text))
    End Sub

    Protected Sub btnArchiveStagedSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveStagedSafe.Click
        litArchiveStagedResult.Text = Server.HtmlEncode(EncryptErrorLogArchiveStagedSafe(txtArchiveStaged.Text))
    End Sub

    Private Function EncryptErrorLogArchiveStaged(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SINK: PLANTED-VB-HR-767
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.ECB
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strLogText)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return "Staged archive ready (" & bytCipher.Length & " bytes ciphertext, dedup-friendly)."
    End Function

    Private Function EncryptErrorLogArchiveStagedSafe(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-767-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strLogText)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return "Staged archive ready (" & bytCipher.Length & " bytes ciphertext, hardened), IV=" & Convert.ToBase64String(objAlg.IV)
    End Function

    ''' <summary>
    ''' Interprocedural tier (genuine 2-hop cross-file): forwards the error-log chunk to
    ''' ErrorLogArchiveDispatchService.PrepareAndSealArchive (hop 1, a different file/class),
    ''' which forwards it again to ErrorLogArchiveTransport.SealArchiveForUpload (hop 2, a
    ''' third file/class) -- the class that actually performs the cipher call.
    ''' </summary>
    Protected Sub btnArchiveDispatch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveDispatch.Click
        litArchiveDispatchResult.Text = Server.HtmlEncode(ErrorLogArchiveDispatchService.PrepareAndSealArchive(txtArchiveDispatch.Text))
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnArchiveDispatch_Click"/>.
    ''' </summary>
    Protected Sub btnArchiveDispatchSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveDispatchSafe.Click
        litArchiveDispatchResult.Text = Server.HtmlEncode(ErrorLogArchiveDispatchService.PrepareAndSealArchiveSafe(txtArchiveDispatch.Text))
    End Sub

    ''' <summary>
    ''' Dispatches through ErrorLogArchiveCipherFactory -- type/polymorphism-dependent
    ''' CWE-327: no branching of any kind is visible here. Whether the resolved cipher uses
    ''' the legacy, dedup-friendly ECB mode (LegacyDedupErrorLogArchiveCipher) or the modern
    ''' CBC mode (ModernErrorLogArchiveCipher) depends purely on a config value the factory
    ''' reads for itself.
    ''' </summary>
    Protected Sub btnArchiveCompatibilityMode_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveCompatibilityMode.Click
        Dim objCipher As ErrorLogArchiveCipherBase = ErrorLogArchiveCipherFactory.GetCurrentCipher()
        litArchiveCompatibilityModeResult.Text = Server.HtmlEncode(objCipher.Encrypt(txtArchiveCompatibilityMode.Text))
    End Sub

    ' Diversity tier: batch/loop over several error-log chunks (one per line), each
    ' independently encrypted and fed into the combined archive report -- neither item is a
    ' dead end. Bounded both by item count (MAX_ARCHIVE_BATCH_ITEMS) and by per-item length
    ' (MAX_ARCHIVE_CHUNK_LENGTH), per this campaign's established "both bounds" discipline
    ' for batch constructs -- this is defensive bounding only (neither bound is itself a
    ' CWE-327 sanitizer; the mode is still ECB in the vulnerable twin).

    Private Const MAX_ARCHIVE_BATCH_ITEMS As Integer = 20
    Private Const MAX_ARCHIVE_CHUNK_LENGTH As Integer = 4000

    Protected Sub btnArchiveBatch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveBatch.Click
        Dim strChunks() As String = txtArchiveBatch.Text.Split(";"c)
        Dim sbdReport As New System.Text.StringBuilder()
        Dim intProcessed As Integer = 0
        For Each strChunk As String In strChunks
            If intProcessed >= MAX_ARCHIVE_BATCH_ITEMS Then Exit For
            Dim strBoundedChunk As String = strChunk
            If strBoundedChunk.Length > MAX_ARCHIVE_CHUNK_LENGTH Then
                strBoundedChunk = strBoundedChunk.Substring(0, MAX_ARCHIVE_CHUNK_LENGTH)
            End If
            Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SINK: PLANTED-VB-HR-770
            objAlg.KeySize = 256
            objAlg.BlockSize = 128
            objAlg.Mode = System.Security.Cryptography.CipherMode.ECB
            objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
            objAlg.GenerateKey()
            Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strBoundedChunk)
            Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
            Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
            sbdReport.Append("Chunk archived (" & bytCipher.Length & " bytes). ")
            intProcessed += 1
        Next
        litArchiveBatchResult.Text = sbdReport.ToString()
    End Sub

    Protected Sub btnArchiveBatchSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnArchiveBatchSafe.Click
        Dim strChunks() As String = txtArchiveBatch.Text.Split(";"c)
        Dim sbdReport As New System.Text.StringBuilder()
        Dim intProcessed As Integer = 0
        For Each strChunk As String In strChunks
            If intProcessed >= MAX_ARCHIVE_BATCH_ITEMS Then Exit For
            Dim strBoundedChunk As String = strChunk
            If strBoundedChunk.Length > MAX_ARCHIVE_CHUNK_LENGTH Then
                strBoundedChunk = strBoundedChunk.Substring(0, MAX_ARCHIVE_CHUNK_LENGTH)
            End If
            Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-770-safe
            objAlg.KeySize = 256
            objAlg.BlockSize = 128
            objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
            objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
            objAlg.GenerateKey()
            objAlg.GenerateIV()
            Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strBoundedChunk)
            Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
            Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
            sbdReport.Append("Chunk archived (" & bytCipher.Length & " bytes, hardened). ")
            intProcessed += 1
        Next
        litArchiveBatchResult.Text = sbdReport.ToString()
    End Sub

End Class
