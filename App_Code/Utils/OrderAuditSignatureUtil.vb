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
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Computes a short "order verification code" that support staff can read out to (or ask
''' a caller to read back) a customer who phones in about a specific order, so both sides
''' can confirm they are discussing the same order record before any details are shared.
''' Used both for orders a staff member keys in manually over the phone (CreateOrder) and
''' for orders a customer places themselves through the normal web checkout.
''' </summary>
Public Class OrderAuditSignatureUtil

    ''' <summary>
    ''' Derives the code from the order ID and the customer's own email address - both of
    ''' which are either sequential/guessable (the order ID) or already known to whoever is
    ''' asking (the email address), so no secret actually goes into the digest.
    ''' </summary>
    Public Shared Function ComputeOrderSignature(ByVal intOrderID As Integer, ByVal strCustomerEmail As String) As String
        Dim strInput As String = intOrderID.ToString() & "|" & strCustomerEmail
        Dim objMD5 As New MD5CryptoServiceProvider()
        Dim bytHash() As Byte = objMD5.ComputeHash(Encoding.UTF8.GetBytes(strInput)) ' SINK: PLANTED-VB-HR-563
        Return BitConverter.ToString(bytHash).Replace("-", "").Substring(0, 8).ToUpperInvariant()
    End Function

    ''' <summary>
    ''' Safe twin of ComputeOrderSignature - salts the input with a fresh random value on
    ''' every call and hashes with SHA-256, so the code can no longer be reproduced just by
    ''' knowing (or guessing) the order ID and the customer's email address.
    ''' </summary>
    Public Shared Function ComputeOrderSignature_Safe(ByVal intOrderID As Integer, ByVal strCustomerEmail As String) As String
        Dim strSalt As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Dim strInput As String = intOrderID.ToString() & "|" & strCustomerEmail & "|" & strSalt
        Using objSha As New SHA256CryptoServiceProvider()
            Dim bytHash() As Byte = objSha.ComputeHash(Encoding.UTF8.GetBytes(strInput)) ' SAFE_SINK: PLANTED-VB-HR-563-safe
            Return strSalt & BitConverter.ToString(bytHash).Replace("-", "").Substring(0, 8).ToUpperInvariant()
        End Using
    End Function

End Class
