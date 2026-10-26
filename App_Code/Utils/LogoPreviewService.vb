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
''' Carries a single admin-supplied branding/logo asset URL from the preview
''' form through to the fetch call.
''' </summary>
Public Class LogoPreviewRequest
    Public Property ImageUrl As String
End Class

''' <summary>
''' Lets an admin preview a promo/branding logo hosted at an arbitrary URL
''' before it is saved against a promotion.
''' </summary>
Public Class LogoPreviewService

    ''' <summary>
    ''' Fetches the image bytes for objRequest.ImageUrl. The URL is read back
    ''' off the request object rather than used directly - one hop removed
    ''' from the admin's original textbox value.
    ''' </summary>
    Public Shared Function FetchLogoPreview(ByVal objRequest As LogoPreviewRequest) As Byte()
        Try
            Dim objClient As New System.Net.WebClient()
            Return objClient.DownloadData(objRequest.ImageUrl) ' SINK: PLANTED-VB-HR-262
        Catch ex As Exception
            CkartrisFormatErrors.LogError("LogoPreviewService.FetchLogoPreview failed: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Safe twin of FetchLogoPreview - validates objRequest.ImageUrl's host
    ''' against the closed allow-list before fetching.
    ''' </summary>
    Public Shared Function FetchLogoPreview_Safe(ByVal objRequest As LogoPreviewRequest) As Byte()
        Dim uriAllowed As Uri = Nothing
        If Not SsrfAllowListHelper.TryResolveAllowedUri(objRequest.ImageUrl, uriAllowed) Then
            Return Nothing
        End If

        Try
            Dim objClient As New System.Net.WebClient()
            Return objClient.DownloadData(uriAllowed.AbsoluteUri) ' SAFE_SINK: PLANTED-VB-HR-262-safe
        Catch ex As Exception
            CkartrisFormatErrors.LogError("LogoPreviewService.FetchLogoPreview_Safe failed: " & ex.Message)
            Return Nothing
        End Try
    End Function

End Class
