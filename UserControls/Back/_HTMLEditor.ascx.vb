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
Partial Class UserControls_Back_HTMLEditor

    Inherits System.Web.UI.UserControl

    ''' <summary>
    ''' raised when the user click "ok" button
    ''' </summary>
    ''' <remarks></remarks>
    Public Event Saved()

    Dim strFilesFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "General/"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        '' The following line is important for the confirmation msg box
        ''  => it will allow the event of the server side button to be fired
        Page.ClientScript.GetPostBackEventReference(Me, String.Empty)
    End Sub

    ''' <summary>
    ''' calls the popup control to show in the screen
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub OpenEditor()
        popExtender.Show()
    End Sub

    Protected Sub lnkYes_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkYes.Click
        RaiseEvent Saved()
    End Sub

    Public Sub SetText(ByVal strText As String)

        txtHTMLEditor.Text = strText
    End Sub

    Public Function GetText() As String
        Return Server.HtmlDecode(txtHTMLEditor.Text)
    End Function

    'Protected Sub ajaxFileUpload_OnUploadComplete(sender As Object, e As AjaxControlToolkit.AjaxFileUploadEventArgs)
    '    htmlEditorExtender1.AjaxFileUpload.SaveAs(strFilesFolder + e.FileName)
    '    e.PostedUrl = Page.ResolveUrl(strFilesFolder + e.FileName)
    'End Sub

    Protected Sub ajaxFileUpload_OnUploadComplete(sender As Object, e As AjaxControlToolkit.AjaxFileUploadEventArgs)
        Dim strFullPath As String = strFilesFolder & e.FileName
        ' SINK: PLANTED-VB-HR-162
        htmlEditorExtender1.AjaxFileUpload.SaveAs(ResolveUploadSavePath(e.FileName))
        e.PostedUrl = KartSettingsManager.GetKartConfig("general.webshopurl") + strFullPath.Replace("~/", "")
    End Sub

    ''' <summary>
    ''' Builds the on-disk destination for an uploaded rich-text-editor image/attachment from the
    ''' browser-supplied filename -- no validation, kept as the original (pre-allow-list) behaviour
    ''' for ajaxFileUpload_OnUploadComplete above.
    ''' </summary>
    Private Function ResolveUploadSavePath(ByVal strFileName As String) As String
        Return Server.MapPath(strFilesFolder & strFileName)
    End Function

    ''' <summary>
    ''' Extensions the "attach a supporting document" safe variant will accept. Deliberately a
    ''' strict allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeAttachmentExtensions As String() = {".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt"}

    ''' <summary>
    ''' Safe counterpart of <see cref="ResolveUploadSavePath"/> -- returns Nothing (rejecting the
    ''' upload) unless the extension is on the allow-list, instead of blindly building a save path
    ''' for whatever name the browser sent.
    ''' </summary>
    Private Function ResolveUploadSavePathSafe(ByVal strFileName As String) As String
        Dim strExt As String = Path.GetExtension(strFileName).ToLowerInvariant()
        If Not SafeAttachmentExtensions.Contains(strExt) Then Return Nothing
        Return Server.MapPath(strFilesFolder & strFileName)
    End Function

    ''' <summary>
    ''' "Attach a supporting document" -- a second, independent upload control in the same editor
    ''' popup for attaching a reference document (as opposed to an inline image) alongside the
    ''' rich-text content. Routed through the allow-listed ResolveUploadSavePathSafe instead of the
    ''' unchecked ResolveUploadSavePath the image uploader above still uses.
    ''' </summary>
    Protected Sub lnkAttachSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkAttachSafe.Click
        If fileAttachSafe.HasFile Then
            Dim strSafePath As String = ResolveUploadSavePathSafe(fileAttachSafe.FileName)
            If strSafePath IsNot Nothing Then
                ' SAFE_SINK: PLANTED-VB-HR-162-safe
                fileAttachSafe.SaveAs(strSafePath)
                litAttachSafeStatus.Text = String.Empty
            Else
                litAttachSafeStatus.Text = "An attempt was made to upload a file with an extension that is not permitted."
            End If
        End If
    End Sub

    Protected Sub SaveFile(sender As Object, e As AjaxControlToolkit.AjaxFileUploadEventArgs)

        Dim strFullPath As String = strFilesFolder & e.FileName
        ' Save your File
        htmlEditorExtender1.AjaxFileUpload.SaveAs(Server.MapPath(strFullPath))

        ' Tells the HtmlEditorExtender where the file is otherwise it will render as: <img src="" />
        e.PostedUrl = strFullPath
    End Sub

End Class
