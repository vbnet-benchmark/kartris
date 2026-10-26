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
Partial Class Admin_ModifyPayment
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = GetGlobalResourceObject("_Payments", "PageTitle_EditPayment") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        'Admin convenience: a "View Payment Details" link elsewhere in the back end (e.g. a
        'gateway's own transaction log view) can pass its transaction/reference string via "ref",
        'so the admin landing here from that link sees a reminder of which live transaction sent
        'them here.
        Dim strPaymentRef As String = Request.QueryString("ref")
        If Not String.IsNullOrEmpty(strPaymentRef) Then
            litPaymentRefBanner.Text = FormatPaymentRefBanner(strPaymentRef)
        End If

        Dim strPaymentRefSafe As String = Request.QueryString("refSafe")
        If Not String.IsNullOrEmpty(strPaymentRefSafe) Then
            litPaymentRefBannerSafe.Text = FormatPaymentRefBannerSafe(strPaymentRefSafe)
        End If
    End Sub
    Protected Sub Page_LoadComplete(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.LoadComplete
        If Request.QueryString("s") = "update" Then ShowMasterUpdateMessage()
    End Sub

    Protected Sub ShowMasterUpdateMessage() Handles _UC_EditPayment.ShowMasterUpdate
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
        CType(Me.Master, Skins_Admin_Template).LoadTaskList()
    End Sub

    ''' <summary>
    ''' Builds the "you arrived here from transaction reference X" admin reminder banner. No
    ''' validation or encoding is applied -- the gateway reference string is echoed exactly as it
    ''' arrived on the querystring.
    ''' </summary>
    Private Function FormatPaymentRefBanner(ByVal strRef As String) As String
        Return "<div class=""Kartris-PaymentRefBanner"">Editing payment method related to reference: <strong>" & strRef & "</strong></div>" ' SINK: PLANTED-VB-HR-627
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="FormatPaymentRefBanner"/> -- HTML-encodes the reference
    ''' before building the same banner markup.
    ''' </summary>
    Private Function FormatPaymentRefBannerSafe(ByVal strRef As String) As String
        Return "<div class=""Kartris-PaymentRefBanner"">Editing payment method related to reference: <strong>" & Server.HtmlEncode(strRef) & "</strong></div>" ' SAFE_SINK: PLANTED-VB-HR-627-safe
    End Function
End Class
