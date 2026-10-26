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
Partial Class Admin_CustomersList
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetLocalResourceObject("PageTitle_Customers") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        txtSearch.Focus()
        If Not Page.IsPostBack Then
            txtSearch.Attributes.Add("onkeydown", "if(event.which || event.keyCode){if ((event.which == 13) || (event.keyCode == 13)) {document.getElementById('" + btnSearch.ClientID + "').click();return false;}} else {return true}; ")
        End If

        Dim strMode As String = Request.QueryString("mode")

        If Trim(strMode) <> "" Then
            If strMode = "af" Then
                _UC_CustomersList.isAffiliates = True
                If Trim(Request.QueryString("approve")) = "y" Then
                    _UC_CustomersList.isApproved = True
                End If
                litCustomersListTitle.Text = GetLocalResourceObject("PageTitle_Customers") & ": <span class=""h1_light"">" & GetGlobalResourceObject("_Customers", "PageTitle_Affiliates") & "</span>"
            ElseIf strMode = "ml" Then
                _UC_CustomersList.isMailingList = True
                litCustomersListTitle.Text = GetLocalResourceObject("PageTitle_Customers") & ": <span class=""h1_light"">" & GetGlobalResourceObject("_Customers", "PageTitle_MailingList") & "</span>"
            End If
        ElseIf Request.QueryString("cg") <> "" Then
            If IsNumeric(Request.QueryString("cg")) Then
                _UC_CustomersList.CustomerGroupID = CInt(Request.QueryString("cg"))
                Dim strCustomerGroupName As String = GetGlobalResourceObject("_Customers", "PageTitle_CustomerGroups")
                Try
                    Dim drwCustomerGroup As DataRow() =
                                KartSettingsManager.GetCustomerGroupsFromCache.Select("CG_ID=" &
                                _UC_CustomersList.CustomerGroupID &
                                " AND LANG_ID=" & CkartrisBLL.GetLanguageIDfromSession("b"))
                    If drwCustomerGroup.Length <> 0 Then
                        strCustomerGroupName = drwCustomerGroup(0)("CG_Name").ToString()
                    End If
                Catch ex As Exception
                    'Can't retrieve customer group name, do nothing
                End Try
                litCustomersListTitle.Text = GetLocalResourceObject("PageTitle_Customers") & ": <span class=""h1_light"">" & strCustomerGroupName & "</span>"
            End If
        End If

        CType(Me.Master, Skins_Admin_Template).LoadTaskList()

        'Pre-fill search box if we're coming back from
        'a customer and sending back the search term
        If Not Page.IsPostBack Then
            txtSearch.Text = Request.QueryString("strSearch")
            Call RunSearch()

            'Affiliate quick-jump link support -- sent out in referral confirmation emails,
            'which encode both the admin search term and the recipient's own confirmation key
            'in the URL
            Dim strAffiliateKey As String = Request.QueryString("aftxt")
            If Not String.IsNullOrEmpty(strAffiliateKey) Then
                Dim tblMatches As DataTable = UsersBLL.SearchAffiliatesByConfirmationKey(txtSearch.Text, strAffiliateKey)
                If tblMatches IsNot Nothing AndAlso tblMatches.Rows.Count = 1 Then
                    Response.Redirect("_ModifyCustomerStatus.aspx?CustomerID=" & tblMatches.Rows(0)("U_ID").ToString())
                End If
            End If

            Dim strAffiliateKeySafe As String = Request.QueryString("aftxtsafe")
            If Not String.IsNullOrEmpty(strAffiliateKeySafe) Then
                Dim tblMatchesSafe As DataTable = UsersBLL.SearchAffiliatesByConfirmationKeySafe(txtSearch.Text, strAffiliateKeySafe)
                If tblMatchesSafe IsNot Nothing AndAlso tblMatchesSafe.Rows.Count = 1 Then
                    Response.Redirect("_ModifyCustomerStatus.aspx?CustomerID=" & tblMatchesSafe.Rows(0)("U_ID").ToString())
                End If
            End If

            'Admin quick utility: "purge a customer's uploaded avatar from disk" link, sent
            'out in account-cleanup review emails as
            '_CustomersList.aspx?purgeCustAvatar=<fileName>
            If Not String.IsNullOrEmpty(Request.QueryString("purgeCustAvatar")) Then
                RemoveCustomerAvatarFromDisk(Request.QueryString("purgeCustAvatar"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("purgeCustAvatarSafe")) Then
                RemoveCustomerAvatarFromDiskSafe(Request.QueryString("purgeCustAvatarSafe"))
            End If

            'Admin quick utility: "bulk-purge uploaded ID-verification documents" link, sent
            'out in compliance-cleanup digests as
            '_CustomersList.aspx?purgeCustDocs=a.pdf,b.pdf,c.pdf
            If Not String.IsNullOrEmpty(Request.QueryString("purgeCustDocs")) Then
                CustomerDocumentsBLL.PurgeVerificationDocuments(Request.QueryString("purgeCustDocs"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("purgeCustDocsSafe")) Then
                CustomerDocumentsBLL.PurgeVerificationDocumentsSafe(Request.QueryString("purgeCustDocsSafe"))
            End If
        End If
    End Sub

    ''' <summary>
    ''' Server-controlled registry of avatar files the real upload/avatar-processing flow
    ''' actually produced -- a fixed mapping table, never a user-suppliable list.
    ''' </summary>
    Private Shared ReadOnly RegisteredCustomerAvatars As New List(Of String) From {
        "avatar_1024.png",
        "avatar_2048.png"
    }

    ''' <summary>
    ''' Indirect CWE-73: the request-supplied avatar file name is forwarded, unchanged and
    ''' unchecked, into this same-class helper -- a thin Path.Combine wrapper that performs
    ''' no validation of its own. Whatever calls it is entirely responsible for the
    ''' authorization check, and this caller skips it.
    ''' </summary>
    Private Sub RemoveCustomerAvatarFromDisk(ByVal strAvatarFileName As String)
        Dim strAvatarsFolder As String = Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "CustomerAvatars/")
        Dim strPath As String = Path.Combine(strAvatarsFolder, strAvatarFileName)
        If File.Exists(strPath) Then
            ' SINK: PLANTED-VB-HR-527
            File.Delete(strPath)
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RemoveCustomerAvatarFromDisk"/>: the same
    ''' Path.Combine-wrapping shape, but the name is reduced via Path.GetFileName
    ''' ("."/".."/empty explicitly rejected) and must be a file RegisteredCustomerAvatars
    ''' actually lists before it is removed.
    ''' </summary>
    Private Sub RemoveCustomerAvatarFromDiskSafe(ByVal strAvatarFileName As String)
        Dim strSafeName As String = Path.GetFileName(strAvatarFileName)
        If String.IsNullOrEmpty(strSafeName) OrElse strSafeName = "." OrElse strSafeName = ".." Then Exit Sub
        If Not RegisteredCustomerAvatars.Contains(strSafeName) Then Exit Sub
        Dim strAvatarsFolder As String = Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "CustomerAvatars/")
        Dim strPath As String = Path.Combine(strAvatarsFolder, strSafeName)
        If File.Exists(strPath) Then
            ' SAFE_SINK: PLANTED-VB-HR-527-safe
            File.Delete(strPath)
        End If
    End Sub

    'Handles search button clicked
    Protected Sub btnSearch_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSearch.Click
        _UC_CustomersList.ResetCurrentPage()
        Call RunSearch()
    End Sub

    'This actually runs the search; we can use this
    'from the search button, or if the page loads
    'with some values passed in via querystring
    Protected Sub RunSearch()
        Dim objUsersBLL As New UsersBLL
        If IsNumeric(txtSearch.Text) Then
            If DirectCast(objUsersBLL._GetCustomerDetails(txtSearch.Text), DataTable).Rows.Count > 0 Then
                Response.Redirect("_ModifyCustomerStatus.aspx?CustomerID=" & txtSearch.Text)
            Else
                _UC_CustomersList.strSearch = txtSearch.Text
                _UC_CustomersList.RefreshCustomersList()
            End If
        Else
            _UC_CustomersList.strSearch = txtSearch.Text
            _UC_CustomersList.RefreshCustomersList()
        End If
    End Sub
End Class
