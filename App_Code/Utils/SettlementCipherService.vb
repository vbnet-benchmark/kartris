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
''' Second hop for the Webhook &amp; Branding Settings admin page's "Manifest Export (Partner
''' Pickup)" feature -- this is the class that actually performs the symmetric encryption of
''' the partner settlement summary (IBAN / tax ID / payout amount).
''' </summary>
Public NotInheritable Class SettlementCipherService

    Public Shared Function Encrypt(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.TripleDESCryptoServiceProvider() ' SINK: PLANTED-VB-HR-753
        objAlg.Mode = System.Security.Cryptography.CipherMode.CBC
        objAlg.Padding = System.Security.Cryptography.PaddingMode.PKCS7
        objAlg.GenerateKey()
        objAlg.GenerateIV()
        Dim bytPlain() As Byte = System.Text.Encoding.UTF8.GetBytes(strSummary)
        Dim objEncryptor As System.Security.Cryptography.ICryptoTransform = objAlg.CreateEncryptor()
        Dim bytCipher() As Byte = objEncryptor.TransformFinalBlock(bytPlain, 0, bytPlain.Length)
        Return Convert.ToBase64String(objAlg.IV) & ":" & Convert.ToBase64String(bytCipher)
    End Function

    Public Shared Function EncryptSafe(ByVal strSummary As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-753-safe
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
