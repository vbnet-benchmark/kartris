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
''' One "error-log archive cipher" dispatch policy for the Error Logs admin page's "Archive
''' (Compatibility Mode)" feature. Deliberately a genuine MustInherit/MustOverride hierarchy
''' -- a brand-new, independent hierarchy, rather than subclassing this project's existing
''' cipher-wrapper hierarchy (SettlementCipherPolicyBase, App_Code/Utils/SettlementCipherPolicy.vb,
''' the sibling CWE-326 round on this same project): that base class declares no constructor
''' and imposes no mode restriction of its own (its two existing subclasses both happen to use
''' CBC by independent implementation choice, not by any base-class enforcement), so reuse was
''' technically possible but would have meant subclassing an unrelated "partner settlement
''' export" policy for a distinct "error log archive" feature -- poor domain separation, not an
''' architectural necessity. The concrete cipher is resolved purely from a server-side,
''' admin-set config value, never from anything visible at the call site in
''' ErrorLogArchiveCipherFactory.GetCurrentCipher's caller.
''' </summary>
Public MustInherit Class ErrorLogArchiveCipherBase
    Public MustOverride Function Encrypt(ByVal strLogText As String) As String
End Class

''' <summary>
''' Legacy cipher: encrypts with ECB mode, so that identical recurring error messages (the
''' same stack trace firing many times during an incident) produce identical ciphertext
''' chunks -- intended to enable a cheap block-level dedup/compare pass before upload.
''' Selected by <see cref="ErrorLogArchiveCipherFactory"/> whenever
''' "general.errorlogarchive.dedupmode" is unset or not "y" -- this project's established
''' unseeded-config-value-means-legacy-path convention.
''' </summary>
Public Class LegacyDedupErrorLogArchiveCipher
    Inherits ErrorLogArchiveCipherBase

    ''' <summary>
    ''' Type/polymorphism-dependent CWE-327: whether the error-log chunk is encrypted with
    ''' the broken ECB mode depends purely on which concrete cipher GetCurrentCipher()
    ''' resolves to, not on anything visible at its caller's call site.
    ''' </summary>
    Public Overrides Function Encrypt(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SINK: PLANTED-VB-HR-769
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.ECB
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strLogText)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return Convert.ToBase64String(bytCipher)
    End Function
End Class

''' <summary>
''' Modern cipher: encrypts with CBC mode and a freshly-generated IV. Selected by
''' <see cref="ErrorLogArchiveCipherFactory"/> when "general.errorlogarchive.dedupmode" =
''' "y".
''' </summary>
Public Class ModernErrorLogArchiveCipher
    Inherits ErrorLogArchiveCipherBase

    Public Overrides Function Encrypt(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-769-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strLogText)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return Convert.ToBase64String(objAlg.IV) & ":" & Convert.ToBase64String(bytCipher)
    End Function
End Class

''' <summary>
''' Resolves the current <see cref="ErrorLogArchiveCipherBase"/> from a real, DB-backed,
''' admin-set config value -- never from a UI control or anything the caller passes in.
''' </summary>
Public Class ErrorLogArchiveCipherFactory

    Public Shared Function GetCurrentCipher() As ErrorLogArchiveCipherBase
        If KartSettingsManager.GetKartConfig("general.errorlogarchive.dedupmode") = "y" Then
            Return New ModernErrorLogArchiveCipher()
        Else
            Return New LegacyDedupErrorLogArchiveCipher()
        End If
    End Function

End Class
