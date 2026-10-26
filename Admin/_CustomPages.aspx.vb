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
Partial Class Admin_customPages
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = GetGlobalResourceObject("_Pages", "PageTitle_Pages") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        'Admin "stage & confirm" asset-removal utility for custom-page header images/PDFs --
        'stage on one request (_CustomPages.aspx?stageDeletePageAsset=<fileName>), confirm on
        'a later, separate request (_CustomPages.aspx?confirmDeletePageAsset=1) once the
        'admin has reviewed the staged name in the confirmation prompt shown in between.
        If Not String.IsNullOrEmpty(Request.QueryString("stageDeletePageAsset")) Then
            Session("StagedPageAsset") = Request.QueryString("stageDeletePageAsset")
        End If
        If Request.QueryString("confirmDeletePageAsset") = "1" Then
            ConfirmDeletePageAsset()
        End If

        If Not String.IsNullOrEmpty(Request.QueryString("stageDeletePageAssetSafe")) Then
            Session("StagedPageAssetSafe") = Request.QueryString("stageDeletePageAssetSafe")
        End If
        If Request.QueryString("confirmDeletePageAssetSafe") = "1" Then
            ConfirmDeletePageAssetSafe()
        End If
    End Sub

    ''' <summary>
    ''' Server-controlled registry of page assets the real custom-page editor actually
    ''' produced -- a fixed mapping table, never a user-suppliable list.
    ''' </summary>
    Private Shared ReadOnly RegisteredPageAssets As New List(Of String) From {
        "header_banner.jpg",
        "terms_pdf_v2.pdf"
    }

    ''' <summary>
    ''' Cross-request CWE-73: the asset name staged into Session on an earlier, separate
    ''' request is read back here and deleted with NO re-validation at the point the
    ''' mutation actually happens -- taint survives the full stage/confirm round trip
    ''' unchanged.
    ''' </summary>
    Private Sub ConfirmDeletePageAsset()
        Dim strStagedName As String = CStr(Session("StagedPageAsset"))
        If String.IsNullOrEmpty(strStagedName) Then Exit Sub
        Dim strPageAssetsFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "PageAssets/"
        Dim strPath As String = Server.MapPath(strPageAssetsFolder & strStagedName)
        If File.Exists(strPath) Then
            ' SINK: PLANTED-VB-HR-530
            File.Delete(strPath)
        End If
        Session("StagedPageAsset") = Nothing
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="ConfirmDeletePageAsset"/>: the step that actually
    ''' performs the mutation gets its own check -- the staged name is reduced via
    ''' Path.GetFileName ("."/".."/empty explicitly rejected) and must still be a registered
    ''' page asset at CONFIRM time, not merely trusted from the earlier staging request.
    ''' </summary>
    Private Sub ConfirmDeletePageAssetSafe()
        Dim strStagedName As String = CStr(Session("StagedPageAssetSafe"))
        If String.IsNullOrEmpty(strStagedName) Then Exit Sub
        Dim strSafeName As String = Path.GetFileName(strStagedName)
        If String.IsNullOrEmpty(strSafeName) OrElse strSafeName = "." OrElse strSafeName = ".." Then
            Session("StagedPageAssetSafe") = Nothing
            Exit Sub
        End If
        If Not RegisteredPageAssets.Contains(strSafeName) Then
            Session("StagedPageAssetSafe") = Nothing
            Exit Sub
        End If
        Dim strPageAssetsFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "PageAssets/"
        Dim strPath As String = Server.MapPath(strPageAssetsFolder & strSafeName)
        If File.Exists(strPath) Then
            ' SAFE_SINK: PLANTED-VB-HR-530-safe
            File.Delete(strPath)
        End If
        Session("StagedPageAssetSafe") = Nothing
    End Sub

    Protected Sub ShowMasterUpdateMessage() Handles _UC_CustomPages.ShowMasterUpdate
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub
End Class
