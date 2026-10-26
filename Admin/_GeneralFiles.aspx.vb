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
Imports CkartrisEnumerations
Imports CkartrisDataManipulation
Imports KartSettingsManager

Partial Class Admin_GeneralFiles
    Inherits _PageBaseClass

    Dim blnFileSaved As Boolean = False

    Dim strFilesFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "General/"

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Page.IsPostBack Then
            Page.ClientScript.GetPostBackEventReference(Me, String.Empty)
        Else
            Page.Form.Enctype = "multipart/form-data"
            LoadGeneralFiles()
        End If
        Page.Title = GetGlobalResourceObject("_Kartris", "PageTitle_GeneralFiles") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")
    End Sub

    Public Function FormatFilePath(ByVal strFilename As String) As String
        Return strFilesFolder & strFilename
    End Function

    Private Sub LoadGeneralFiles()
        Dim dtbFiles As New DataTable
        dtbFiles.Columns.Add(New DataColumn("F_Name", Type.GetType("System.String")))
        dtbFiles.Columns.Add(New DataColumn("F_Type", Type.GetType("System.String")))
        dtbFiles.Columns.Add(New DataColumn("F_Size", Type.GetType("System.String")))
        dtbFiles.Columns.Add(New DataColumn("F_LastUpdated", Type.GetType("System.String")))

        Dim dirGeneralFiles As New DirectoryInfo(Server.MapPath(strFilesFolder))
        Dim filInfo() As FileInfo
        If Not dirGeneralFiles.Exists() Then Exit Sub
        filInfo = dirGeneralFiles.GetFiles()
        Dim strName As String = "", strType As String = "", _
            strSize As String = "", strLastUpdated As String = ""
        For i As Integer = 0 To filInfo.GetUpperBound(0)
            strName = filInfo(i).Name
            strType = filInfo(i).Extension
            strSize = CStr(filInfo(i).Length() / 1000) & " KB"
            strLastUpdated = CkartrisDisplayFunctions.FormatDate(filInfo(i).LastWriteTime, "t", Session("_LANG"))
            If strName.ToLower = "web.config" Then Continue For
            dtbFiles.Rows.Add(strName, strType, strSize, strLastUpdated)
        Next

        If dtbFiles.Rows.Count > 0 Then
            gvwFiles.DataSource = dtbFiles
            gvwFiles.DataBind()
            mvwGeneralFiles.SetActiveView(viwFiles)
        Else
            mvwGeneralFiles.SetActiveView(viwNoFiles)
        End If
    End Sub

    Protected Sub gvwFiles_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvwFiles.PageIndexChanging
        gvwFiles.PageIndex = e.NewPageIndex
        LoadGeneralFiles()
    End Sub

    Protected Sub lnkUpload_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkUpload.Click
        If filUploader.HasFile Then
            If Not IsFileExist(filUploader.FileName) Then
                '' To avoid saving the file twice
                If Not blnFileSaved Then
                    Try
                        ' SINK: PLANTED-VB-HR-161
                        filUploader.SaveAs(Server.MapPath(strFilesFolder & filUploader.FileName))
                        CType(Me.Master, Skins_Admin_Template).DataUpdated()
                        LoadGeneralFiles()
                    Catch ex As Exception
                        litStatus2.Text = ex.Message
                        popExtender2.Show()
                        blnFileSaved = False
                    End Try
                End If
            Else
                litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_AlreadyExists")
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        CType(Me.Master, Skins_Admin_Template).LoadCategoryMenu()
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Extensions the "General Files" safe upload variant will accept. Deliberately a strict
    ''' allow-list (never a MIME-type/ContentType check, and never a deny-list alone) -- see
    ''' lnkUploadSafe_Click.
    ''' </summary>
    Private Shared ReadOnly SafeGeneralFileExtensions As String() = {".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".zip", ".png", ".jpg", ".jpeg", ".gif"}

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkUpload_Click"/> -- same "General Files" upload shape,
    ''' but the file's extension is checked against a strict allow-list before it is ever saved.
    ''' </summary>
    Protected Sub lnkUploadSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadSafe.Click
        If filUploaderSafe.HasFile Then
            Dim strExt As String = Path.GetExtension(filUploaderSafe.FileName).ToLowerInvariant()
            If Not SafeGeneralFileExtensions.Contains(strExt) Then
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            ElseIf IsFileExist(filUploaderSafe.FileName) Then
                litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_AlreadyExists")
                popExtender2.Show()
            Else
                Try
                    ' SAFE_SINK: PLANTED-VB-HR-161-safe
                    filUploaderSafe.SaveAs(Server.MapPath(strFilesFolder & filUploaderSafe.FileName))
                    CType(Me.Master, Skins_Admin_Template).DataUpdated()
                    LoadGeneralFiles()
                Catch ex As Exception
                    litStatus2.Text = ex.Message
                    popExtender2.Show()
                End Try
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        CType(Me.Master, Skins_Admin_Template).LoadCategoryMenu()
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Upload Custom Skin File" feature -- lets an admin push a replacement/custom file
    ''' (e.g. a logo override or a packaged skin bundle) directly into the active skins folder
    ''' without a full deployment. The actual save lives in SkinsBLL; this handler only manages
    ''' the postback plumbing.
    ''' </summary>
    Protected Sub lnkUploadTheme_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadTheme.Click
        If filUploaderTheme.HasFile Then
            Dim strSkinsFolder As String = "~/Skins/CustomUploaded/"
            If Not Directory.Exists(Server.MapPath(strSkinsFolder)) Then Directory.CreateDirectory(Server.MapPath(strSkinsFolder))
            If SkinsBLL.InstallSkinPackage(filUploaderTheme.PostedFile, strSkinsFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkUploadTheme_Click"/> -- same "upload a custom skin file"
    ''' shape, routed to SkinsBLL's allow-listed save instead.
    ''' </summary>
    Protected Sub lnkUploadThemeSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadThemeSafe.Click
        If filUploaderThemeSafe.HasFile Then
            Dim strSkinsFolder As String = "~/Skins/CustomUploaded/"
            If Not Directory.Exists(Server.MapPath(strSkinsFolder)) Then Directory.CreateDirectory(Server.MapPath(strSkinsFolder))
            If SkinsBLL.InstallSkinPackageSafe(filUploaderThemeSafe.PostedFile, strSkinsFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted. Only .zip skin packages are accepted."
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Import Email Template" feature -- lets an admin replace one of the storefront's
    ''' customizable order/notification email templates. ddlTemplateMode preserves a "legacy"
    ''' import path (pre-dating the allow-list) alongside the newer "validated" one, so an existing
    ''' integration/automation script that still posts the legacy mode value keeps working.
    ''' </summary>
    Protected Sub lnkImportTemplate_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkImportTemplate.Click
        If filUploaderTemplate.HasFile Then
            Dim strTemplatesFolder As String = "~/EmailTemplates/"
            If Not Directory.Exists(Server.MapPath(strTemplatesFolder)) Then Directory.CreateDirectory(Server.MapPath(strTemplatesFolder))
            If EmailTemplatesBLL.ImportTemplate(ddlTemplateMode.SelectedValue, filUploaderTemplate.PostedFile, strTemplatesFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Bulk Import Attachments" feature -- lets an admin post several supporting documents
    ''' at once (e.g. for a knowledge-base article) in a single submit. Loops every posted file and
    ''' hands each one to AttachmentsBLL individually, so one bad file in the batch doesn't abort
    ''' the rest.
    ''' </summary>
    Protected Sub lnkBulkImport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBulkImport.Click
        Dim strAttachmentsFolder As String = "~/Attachments/"
        If Not Directory.Exists(Server.MapPath(strAttachmentsFolder)) Then Directory.CreateDirectory(Server.MapPath(strAttachmentsFolder))
        Dim intImported As Integer = 0
        For i As Integer = 0 To Request.Files.Count - 1
            Dim objPostedFile As HttpPostedFile = Request.Files(i)
            If objPostedFile IsNot Nothing AndAlso objPostedFile.ContentLength > 0 Then
                If AttachmentsBLL.ImportAttachment(objPostedFile, strAttachmentsFolder) Then intImported += 1
            End If
        Next
        If intImported > 0 Then
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkBulkImport_Click"/> -- same batch/multi-file loop shape,
    ''' routed to AttachmentsBLL's allow-listed save instead.
    ''' </summary>
    Protected Sub lnkBulkImportSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBulkImportSafe.Click
        Dim strAttachmentsFolder As String = "~/Attachments/"
        If Not Directory.Exists(Server.MapPath(strAttachmentsFolder)) Then Directory.CreateDirectory(Server.MapPath(strAttachmentsFolder))
        Dim intImported As Integer = 0
        For i As Integer = 0 To Request.Files.Count - 1
            Dim objPostedFile As HttpPostedFile = Request.Files(i)
            If objPostedFile IsNot Nothing AndAlso objPostedFile.ContentLength > 0 Then
                If AttachmentsBLL.ImportAttachmentSafe(objPostedFile, strAttachmentsFolder) Then intImported += 1
            End If
        Next
        If intImported > 0 Then
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = "No files were imported -- check that every file has a permitted extension."
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Upload RMA Return Photo" feature -- lets an admin attach a photo of a returned
    ''' item (customer-submitted or taken in-store) to a return-merchandise-authorization record.
    ''' Direct save, no helper -- see vbnet-cwe-434-planting-research.md Sec. 3's "direct" shape.
    ''' </summary>
    Protected Sub lnkUploadRMA_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkUploadRMA.Click
        If filRMA.HasFile Then
            Dim strRMAFolder As String = "~/Uploads/RMA/"
            If Not Directory.Exists(Server.MapPath(strRMAFolder)) Then Directory.CreateDirectory(Server.MapPath(strRMAFolder))
            ' SINK: PLANTED-VB-HR-326
            filRMA.SaveAs(Server.MapPath(strRMAFolder & filRMA.FileName))
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Extensions the "RMA Return Photo" safe upload variant will accept. Deliberately a strict
    ''' allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeRMAPhotoExtensions As String() = {".png", ".jpg", ".jpeg", ".gif"}

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkUploadRMA_Click"/> -- same "RMA Return Photo" upload
    ''' shape, but the extension is checked against a strict allow-list before the file is ever
    ''' saved.
    ''' </summary>
    Protected Sub lnkUploadRMASafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadRMASafe.Click
        If filRMASafe.HasFile Then
            Dim strExt As String = Path.GetExtension(filRMASafe.FileName).ToLowerInvariant()
            If Not SafeRMAPhotoExtensions.Contains(strExt) Then
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            Else
                Dim strRMAFolder As String = "~/Uploads/RMA/"
                If Not Directory.Exists(Server.MapPath(strRMAFolder)) Then Directory.CreateDirectory(Server.MapPath(strRMAFolder))
                ' SAFE_SINK: PLANTED-VB-HR-326-safe
                filRMASafe.SaveAs(Server.MapPath(strRMAFolder & filRMASafe.FileName))
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Builds the on-disk destination for an uploaded vendor-storefront banner overlay from the
    ''' browser-supplied filename -- no validation; the original behaviour for
    ''' lnkUploadBanner_Click below. See vbnet-cwe-434-planting-research.md Sec. 3's "indirect
    ''' (same-class private helper)" shape.
    ''' </summary>
    Private Function ResolveBannerSavePath(ByVal strFileName As String) As String
        Dim strBannerFolder As String = "~/Uploads/VendorBanners/"
        If Not Directory.Exists(Server.MapPath(strBannerFolder)) Then Directory.CreateDirectory(Server.MapPath(strBannerFolder))
        Return Server.MapPath(strBannerFolder & strFileName)
    End Function

    ''' <summary>
    ''' Admin "Upload Vendor Banner Overlay" feature -- lets an admin push a replacement banner
    ''' graphic for a merchant storefront page without a full redeploy.
    ''' </summary>
    Protected Sub lnkUploadBanner_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkUploadBanner.Click
        If filBanner.HasFile Then
            ' SINK: PLANTED-VB-HR-327
            filBanner.SaveAs(ResolveBannerSavePath(filBanner.FileName))
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Extensions the "Vendor Banner Overlay" safe upload variant will accept. Deliberately a
    ''' strict allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeBannerExtensions As String() = {".png", ".jpg", ".jpeg", ".gif", ".svg"}

    ''' <summary>
    ''' Safe counterpart of <see cref="ResolveBannerSavePath"/> -- returns Nothing (rejecting the
    ''' upload) unless the extension is on the allow-list, instead of blindly building a save
    ''' path for whatever name the browser sent.
    ''' </summary>
    Private Function ResolveBannerSavePathSafe(ByVal strFileName As String) As String
        Dim strExt As String = Path.GetExtension(strFileName).ToLowerInvariant()
        If Not SafeBannerExtensions.Contains(strExt) Then Return Nothing
        Dim strBannerFolder As String = "~/Uploads/VendorBanners/"
        If Not Directory.Exists(Server.MapPath(strBannerFolder)) Then Directory.CreateDirectory(Server.MapPath(strBannerFolder))
        Return Server.MapPath(strBannerFolder & strFileName)
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkUploadBanner_Click"/> -- same "Vendor Banner Overlay"
    ''' upload shape, routed through the allow-listed ResolveBannerSavePathSafe instead of the
    ''' unchecked ResolveBannerSavePath above.
    ''' </summary>
    Protected Sub lnkUploadBannerSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadBannerSafe.Click
        If filBannerSafe.HasFile Then
            Dim strSafePath As String = ResolveBannerSavePathSafe(filBannerSafe.FileName)
            If strSafePath IsNot Nothing Then
                ' SAFE_SINK: PLANTED-VB-HR-327-safe
                filBannerSafe.SaveAs(strSafePath)
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Upload Merchant Agreement Document" feature -- lets an admin attach a signed
    ''' merchant/seller agreement document to the merchant's on-file records. Page -&gt; BLL,
    ''' crossing the file/class boundary -- see vbnet-cwe-434-planting-research.md Sec. 3's
    ''' "interprocedural" shape, same Page-calls-BLL pattern as SkinsBLL/AttachmentsBLL above.
    ''' </summary>
    Protected Sub lnkUploadAgreement_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkUploadAgreement.Click
        If filAgreement.HasFile Then
            Dim strAgreementFolder As String = "~/Uploads/MerchantAgreements/"
            If Not Directory.Exists(Server.MapPath(strAgreementFolder)) Then Directory.CreateDirectory(Server.MapPath(strAgreementFolder))
            If MerchantDocumentsBLL.ImportAgreement(filAgreement.PostedFile, strAgreementFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkUploadAgreement_Click"/> -- same "Merchant Agreement
    ''' Document" upload shape, routed to MerchantDocumentsBLL's allow-listed save instead.
    ''' </summary>
    Protected Sub lnkUploadAgreementSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadAgreementSafe.Click
        If filAgreementSafe.HasFile Then
            Dim strAgreementFolder As String = "~/Uploads/MerchantAgreements/"
            If Not Directory.Exists(Server.MapPath(strAgreementFolder)) Then Directory.CreateDirectory(Server.MapPath(strAgreementFolder))
            If MerchantDocumentsBLL.ImportAgreementSafe(filAgreementSafe.PostedFile, strAgreementFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Upload Compliance Certificate" feature -- lets an admin attach a certification
    ''' document (ISO or insurance) to the storefront's compliance records. No genuine
    ''' interface/MustInherit/Overridable dispatch exists over any upload path in this project
    ''' (confirmed by grep before writing this -- see vbnet-cwe-434-planting-research.md Sec. 3's
    ''' substituted-dispatch axis), so the certificate-type decision is a Select Case on the
    ''' posted dropdown value instead, inside ComplianceDocumentsBLL, mirroring this project's own
    ''' EmailTemplatesBLL.ImportTemplate dispatch-on-mode shape.
    ''' </summary>
    Protected Sub lnkUploadCompliance_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkUploadCompliance.Click
        If filCompliance.HasFile Then
            Dim strComplianceFolder As String = "~/Uploads/Compliance/"
            If Not Directory.Exists(Server.MapPath(strComplianceFolder)) Then Directory.CreateDirectory(Server.MapPath(strComplianceFolder))
            If ComplianceDocumentsBLL.ImportCertificate(ddlComplianceType.SelectedValue, filCompliance.PostedFile, strComplianceFolder) Then
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
            Else
                litStatus2.Text = "An attempt was made to upload a file with an extension that is not permitted."
                popExtender2.Show()
            End If
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Admin "Bulk Upload Supplier Invoice Scans" feature -- lets an admin post up to three
    ''' scanned invoice pages from a supplier in one submit. Unlike the existing "Bulk Import
    ''' Attachments" loop above (which walks the whole Request.Files collection by index), this
    ''' loop identifies its own files by a name-prefix filter over Request.Files.AllKeys, so it
    ''' only ever processes the inputs it declared -- a different construction shape for the same
    ''' multi-file tier (see vbnet-cwe-434-planting-research.md Sec. 3's loop/batch diversity
    ''' note; this is this round's 5th/diversity tier, repeating the loop/batch shape).
    ''' </summary>
    Protected Sub lnkBulkUploadInvoices_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkBulkUploadInvoices.Click
        Dim strInvoiceFolder As String = "~/Uploads/SupplierInvoices/"
        If Not Directory.Exists(Server.MapPath(strInvoiceFolder)) Then Directory.CreateDirectory(Server.MapPath(strInvoiceFolder))
        Dim intImported As Integer = 0
        For Each strKey As String In Request.Files.AllKeys
            If strKey.StartsWith("invScan") AndAlso Not strKey.EndsWith("Safe") Then
                Dim objPostedFile As HttpPostedFile = Request.Files(strKey)
                If objPostedFile IsNot Nothing AndAlso objPostedFile.ContentLength > 0 Then
                    ' SINK: PLANTED-VB-HR-330
                    objPostedFile.SaveAs(Server.MapPath(strInvoiceFolder & objPostedFile.FileName))
                    intImported += 1
                End If
            End If
        Next
        If intImported > 0 Then
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = GetGlobalResourceObject("_Kartris", "ContentText_NoFile")
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    ''' <summary>
    ''' Extensions the "Bulk Upload Supplier Invoice Scans" safe variant will accept. Deliberately
    ''' a strict allow-list (never a MIME-type/ContentType check, and never a deny-list alone).
    ''' </summary>
    Private Shared ReadOnly SafeInvoiceScanExtensions As String() = {".pdf", ".png", ".jpg", ".jpeg"}

    ''' <summary>
    ''' Safe counterpart of <see cref="lnkBulkUploadInvoices_Click"/> -- same
    ''' name-prefix-filtered batch-loop shape, but each file's extension is checked against a
    ''' strict allow-list before it is ever saved.
    ''' </summary>
    Protected Sub lnkBulkUploadInvoicesSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBulkUploadInvoicesSafe.Click
        Dim strInvoiceFolder As String = "~/Uploads/SupplierInvoices/"
        If Not Directory.Exists(Server.MapPath(strInvoiceFolder)) Then Directory.CreateDirectory(Server.MapPath(strInvoiceFolder))
        Dim intImported As Integer = 0
        For Each strKey As String In Request.Files.AllKeys
            If strKey.StartsWith("invScan") AndAlso strKey.EndsWith("Safe") Then
                Dim objPostedFile As HttpPostedFile = Request.Files(strKey)
                If objPostedFile IsNot Nothing AndAlso objPostedFile.ContentLength > 0 Then
                    Dim strExt As String = Path.GetExtension(objPostedFile.FileName).ToLowerInvariant()
                    If SafeInvoiceScanExtensions.Contains(strExt) Then
                        ' SAFE_SINK: PLANTED-VB-HR-330-safe
                        objPostedFile.SaveAs(Server.MapPath(strInvoiceFolder & objPostedFile.FileName))
                        intImported += 1
                    End If
                End If
            End If
        Next
        If intImported > 0 Then
            CType(Me.Master, Skins_Admin_Template).DataUpdated()
        Else
            litStatus2.Text = "No files were imported -- check that every file has a permitted extension."
            popExtender2.Show()
        End If
        updMain.Update()
    End Sub

    Function IsFileExist(strFileName As String) As Boolean
        Dim dirGeneralFiles As New DirectoryInfo(Server.MapPath(strFilesFolder))
        If Not dirGeneralFiles.Exists() Then Directory.CreateDirectory(Server.MapPath(strFilesFolder))
        Dim filInfo() As FileInfo
        filInfo = dirGeneralFiles.GetFiles(strFileName)
        If filInfo.Length > 0 Then Return True
        Return False
    End Function

    Protected Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        popExtender.Show()
    End Sub

    Protected Sub gvwFiles_RowCommand(sender As Object, e As GridViewCommandEventArgs) Handles gvwFiles.RowCommand
        If e.CommandName = "DeleteFile" Then
            hidFileNameToDelete.Value = e.CommandArgument
            Dim strMessage As String = Replace(GetGlobalResourceObject("_Kartris", "ContentText_ConfirmDeleteItem"), "[itemname]", _
                                        e.CommandArgument)
            _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.Confirmation, strMessage)
        End If
    End Sub

    Protected Sub _UC_PopupMsg_Confirmed() Handles _UC_PopupMsg.Confirmed
        If Not String.IsNullOrEmpty(hidFileNameToDelete.Value) Then
            Try
                File.Delete(Server.MapPath(strFilesFolder & hidFileNameToDelete.Value))
                hidFileNameToDelete.Value = ""
                LoadGeneralFiles()
                CType(Me.Master, Skins_Admin_Template).DataUpdated()
                updMain.Update()
            Catch ex As Exception
                _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, ex.Message)
            End Try
        End If
    End Sub
End Class
