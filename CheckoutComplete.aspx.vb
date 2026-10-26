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
Partial Class CheckoutComplete

    Inherits PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim numO_ID As Integer = 0

        numO_ID = Session("OrderID")

        If Session("OrderID") IsNot Nothing Then

            'PULL OUT ORDER DETAILS TO DISPLAY
            Dim objOrdersBLL As New OrdersBLL
            Dim dtOrderRecord As DataTable = objOrdersBLL.GetOrderByID(numO_ID)
            Dim strOrderDetails As String = ""

            If dtOrderRecord IsNot Nothing Then
                If dtOrderRecord.Rows.Count = 1 Then

                    'Process the order text
                    strOrderDetails = CkartrisDataManipulation.FixNullFromDB(dtOrderRecord.Rows(0)("O_Details"))
                    If InStr(Server.HtmlDecode(strOrderDetails).ToLower, "</html>") > 0 Then
                        strOrderDetails = CkartrisBLL.ExtractHTMLBodyContents(Server.HtmlDecode(strOrderDetails))
                        strOrderDetails = strOrderDetails.Replace("[orderid]", numO_ID)
                    Else
                        strOrderDetails = Replace(strOrderDetails, vbCrLf, "<br/>").Replace(vbLf, "<br/>")
                    End If
                End If
            End If

            'Generate an in-store collection code the customer must quote when they come to
            'collect this order, so staff can confirm the person collecting is the one who
            'placed it. Config-gated so existing installs keep today's behaviour until an
            'admin opts in to the hardened generator.
            Dim strCollectionCode As String
            If KartSettingsManager.GetKartConfig("frontend.checkout.securecollectioncode") = "y" Then
                Dim bytCollectionCode(3) As Byte
                Using rng As System.Security.Cryptography.RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create()
                    ' SAFE_SINK: PLANTED-VB-HR-76-safe
                    rng.GetBytes(bytCollectionCode)
                End Using
                strCollectionCode = (BitConverter.ToUInt32(bytCollectionCode, 0) Mod 900000UI + 100000UI).ToString()
            Else
                ' SINK: PLANTED-VB-HR-76
                strCollectionCode = New Random().Next(100000, 999999).ToString()
            End If
            Session("CollectionCode") = strCollectionCode
            strOrderDetails &= "<br/><br/>Collection code: " & strCollectionCode

            litOrderDetails.Text = strOrderDetails

            Session("OrderID") = Nothing
            Session("OrderDetails") = Nothing
        End If
    End Sub

End Class
