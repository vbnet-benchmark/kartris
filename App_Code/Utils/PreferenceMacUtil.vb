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
Imports System.Configuration
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Shared HMAC sign/verify helper for the cookie-backed "Quick Preferences" feature
''' (BrowsingPreferencesBar.ascx.vb, PreferenceRestoreService, PreferenceFormatStrategy).
''' Hand-rolls, for a custom LosFormatter/ObjectStateFormatter payload, the same kind
''' of integrity check ASP.NET itself applies to __VIEWSTATE via EnableViewStateMac --
''' compute a MAC over the raw serialized payload when it is written, and reject
''' anything that does not verify when it is read back, before the payload is ever
''' handed to a formatter.
''' </summary>
Public NotInheritable Class PreferenceMacUtil
    Private Sub New()
    End Sub

    Private Shared Function GetKey() As Byte()
        Return Encoding.UTF8.GetBytes(Trim(ConfigurationManager.AppSettings("HashSalt")) & "PreferenceMacKey")
    End Function

    ''' <summary>
    ''' Computes a Base64 HMACSHA256 signature over the given payload.
    ''' </summary>
    Public Shared Function Sign(ByVal strPayload As String) As String
        Using objHmac As New HMACSHA256(GetKey())
            Dim bytHash() As Byte = objHmac.ComputeHash(Encoding.UTF8.GetBytes(strPayload))
            Return Convert.ToBase64String(bytHash)
        End Using
    End Function

    ''' <summary>
    ''' Verifies that strSignature is the correct HMACSHA256 signature for strPayload.
    ''' Returns False for any mismatch, including a payload that was tampered with
    ''' after signing.
    ''' </summary>
    Public Shared Function Verify(ByVal strPayload As String, ByVal strSignature As String) As Boolean
        If String.IsNullOrEmpty(strSignature) Then Return False
        Return FixedTimeEquals(Sign(strPayload), strSignature)
    End Function

    ''' <summary>
    ''' Constant-time string comparison, so verification time does not leak how much
    ''' of the signature was guessed correctly.
    ''' </summary>
    Private Shared Function FixedTimeEquals(ByVal strA As String, ByVal strB As String) As Boolean
        If strA Is Nothing OrElse strB Is Nothing OrElse strA.Length <> strB.Length Then Return False
        Dim numDiff As Integer = 0
        For i = 0 To strA.Length - 1
            numDiff = numDiff Or (AscW(strA.Chars(i)) Xor AscW(strB.Chars(i)))
        Next
        Return numDiff = 0
    End Function
End Class
