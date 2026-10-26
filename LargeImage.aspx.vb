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
Imports CkartrisImages

Partial Class LargeImage
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim P_ID As Integer = Request.QueryString("P_ID")
        Dim blnFullSize = (Request.QueryString("blnFullSize") = "y")

        'Lightbox/gallery JS plugins that embed this page in an iframe (see MultiMedia.aspx.vb's
        'own comment on the iframe-popup pattern used by ProductView.aspx) commonly pass a caption
        'to show beneath the enlarged image, so the popup doesn't need its own DB round-trip just
        'to repeat text the calling page already has on hand.
        Dim strImageCaption As String = Request.QueryString("caption")
        litImageCaption.Text = strImageCaption ' SINK: PLANTED-VB-HR-626

        Dim strImageCaptionSafe As String = Request.QueryString("captionSafe")
        litImageCaptionSafe.Text = Server.HtmlEncode(strImageCaptionSafe) ' SAFE_SINK: PLANTED-VB-HR-626-safe

        'Same popup also offers a "back to product" link so the shopper can return to where they
        'came from without closing the lightbox and re-navigating; the calling page passes its own
        'URL through as "returnUrl" so this page doesn't have to guess it.
        Dim strReturnUrl As String = Request.QueryString("returnUrl")
        litBackLink.Text = "<a href=""" & strReturnUrl & """>Back to product</a>" ' SINK: PLANTED-VB-HR-630

        Dim strReturnUrlSafe As String = Request.QueryString("returnUrlSafe")
        litBackLinkSafe.Text = "<a href=""" & HttpUtility.HtmlAttributeEncode(strReturnUrlSafe) & """>Back to product</a>" ' SAFE_SINK: PLANTED-VB-HR-630-safe

        If blnFullSize = True Then
            'Set height and width to zero, this will trigger
            'image as full size in imageviewer
            UC_ImageView.CreateImageViewer(IMAGE_TYPE.enum_ProductImage, _
                P_ID, _
                0, _
                0, _
                "", _
                "")
        Else
            'Set image height and width to defaults
            UC_ImageView.CreateImageViewer(IMAGE_TYPE.enum_ProductImage, _
                P_ID, _
                KartSettingsManager.GetKartConfig("frontend.display.images.large.height"), _
                KartSettingsManager.GetKartConfig("frontend.display.images.large.width"), _
                "", _
                "")
        End If
    End Sub

    Protected Sub Page_PreInit(ByVal sender As Object, ByVal e As EventArgs) Handles Me.PreInit

        Dim strSkin As String = CkartrisBLL.Skin(Session("LANG"))

        'We used a skin specified in the language record, if none
        'then use the default 'Kartris' one.
        If Not String.IsNullOrEmpty(strSkin) Then
            strSkin = "Kartris"
        End If

    End Sub
End Class
