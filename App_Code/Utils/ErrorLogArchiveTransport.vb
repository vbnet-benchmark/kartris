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
''' Second hop for the Error Logs admin page's "Dispatch Archive" feature -- this is the
''' class that actually performs the symmetric encryption of the error-log chunk before it
''' is handed off for upload to support / a monitoring vendor.
''' </summary>
Public NotInheritable Class ErrorLogArchiveTransport

    Public Shared Function SealArchiveForUpload(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SINK: PLANTED-VB-HR-768
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

    Public Shared Function SealArchiveForUploadSafe(ByVal strLogText As String) As String
        Dim objAlg As New System.Security.Cryptography.RijndaelManaged() ' SAFE_SINK: PLANTED-VB-HR-768-safe
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
