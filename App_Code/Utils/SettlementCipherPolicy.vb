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
''' One "partner settlement export cipher" dispatch policy for the Webhook &amp; Branding
''' Settings admin page's "Export (Cipher Policy-Driven)" feature. Deliberately a genuine
''' MustInherit/MustOverride hierarchy, mirroring this project's own pre-existing
''' PaymentTerminalConnectivityPolicyBase / CookieSecurityPolicyBase shape
''' (App_Code/Utils/PaymentTerminalConnectivityPolicy.vb, App_Code/Utils/CookieSecurityPolicy.vb):
''' the concrete policy is resolved purely from a server-side, admin-set config value, never
''' from anything visible at the call site in SettlementCipherPolicyFactory.GetCurrentPolicy's
''' caller.
''' </summary>
Public MustInherit Class SettlementCipherPolicyBase
    Public MustOverride Function Encrypt(ByVal strSummary As String) As String
End Class

''' <summary>
''' Legacy policy: encrypts with TripleDES. Selected by
''' <see cref="SettlementCipherPolicyFactory"/> whenever
''' "security.partnersettlementexport.moderncrypto" is unset or not "y" -- this project's
''' established unseeded-config-value-means-legacy-path convention.
''' </summary>
Public Class LegacySettlementCipherPolicy
    Inherits SettlementCipherPolicyBase

    ''' <summary>
    ''' Type/polymorphism-dependent CWE-326: whether the partner settlement summary is
    ''' encrypted with the legacy TripleDES cipher depends purely on which concrete policy
    ''' GetCurrentPolicy() resolves to, not on anything visible at its caller's call site.
    ''' </summary>
    Public Overrides Function Encrypt(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.TripleDESCryptoServiceProvider() ' SINK: PLANTED-VB-HR-754
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return Convert.ToBase64String(objAlg.IV) & ":" & Convert.ToBase64String(bytCipher)
    End Function
End Class

''' <summary>
''' Modern policy: encrypts with RijndaelManaged configured for a 256-bit key (effectively
''' AES-256). Selected by <see cref="SettlementCipherPolicyFactory"/> when
''' "security.partnersettlementexport.moderncrypto" = "y".
''' </summary>
Public Class ModernSettlementCipherPolicy
    Inherits SettlementCipherPolicyBase

    Public Overrides Function Encrypt(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-754-safe
        objAlg.KeySize = 256
        objAlg.BlockSize = 128
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return Convert.ToBase64String(objAlg.IV) & ":" & Convert.ToBase64String(bytCipher)
    End Function
End Class

''' <summary>
''' Resolves the current <see cref="SettlementCipherPolicyBase"/> from a real, DB-backed,
''' admin-set config value -- never from a UI control or anything the caller passes in.
''' </summary>
Public Class SettlementCipherPolicyFactory

    Public Shared Function GetCurrentPolicy() As SettlementCipherPolicyBase
        If KartSettingsManager.GetKartConfig("security.partnersettlementexport.moderncrypto") = "y" Then
            Return New ModernSettlementCipherPolicy()
        Else
            Return New LegacySettlementCipherPolicy()
        End If
    End Function

End Class
