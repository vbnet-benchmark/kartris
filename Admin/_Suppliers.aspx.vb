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
Imports KartSettingsManager
Imports System.Web.HttpContext
Imports System.Net.Sockets

Partial Class Admin_Suppliers

    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = GetGlobalResourceObject("_Suppliers", "PageTitle_Suppliers") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")
        If Not Page.IsPostBack Then
            GetSuppliersList()

            'Admin quick utility: "remove an onboarding document" link, sent out in internal
            'supplier-onboarding review emails as
            '_Suppliers.aspx?removeSupplierDoc=<fileName>
            If Not String.IsNullOrEmpty(Request.QueryString("removeSupplierDoc")) Then
                RemoveSupplierDocument(Request.QueryString("removeSupplierDoc"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("removeSupplierDocSafe")) Then
                RemoveSupplierDocumentSafe(Request.QueryString("removeSupplierDocSafe"))
            End If

            'Admin quick utility: verify a newly onboarded supplier's EDI/stock-feed delivery
            'endpoint is reachable before enabling automated sync, sent in supplier-onboarding
            'review emails as _Suppliers.aspx?testSupplierEndpoint=host:port
            If Not String.IsNullOrEmpty(Request.QueryString("testSupplierEndpoint")) Then
                TestSupplierEndpoint(Request.QueryString("testSupplierEndpoint"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("testSupplierEndpointSafe")) Then
                TestSupplierEndpointSafe(Request.QueryString("testSupplierEndpointSafe"))
            End If

            'Admin quick utility: bulk-verify connectivity to a comma-separated batch of
            '"host:port" supplier endpoints before a scheduled bulk stock-sync run.
            If Not String.IsNullOrEmpty(Request.QueryString("testSupplierEndpointsBulk")) Then
                TestSupplierEndpointsBulk(Request.QueryString("testSupplierEndpointsBulk"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("testSupplierEndpointsBulkSafe")) Then
                TestSupplierEndpointsBulkSafe(Request.QueryString("testSupplierEndpointsBulkSafe"))
            End If
        End If
    End Sub

    ''' <summary>
    ''' Direct CWE-73: the document name comes straight from the request and is
    ''' concatenated onto the supplier-documents folder with no check that it is a document
    ''' any supplier record actually has on file -- deletes whatever file is named,
    ''' registered or not.
    ''' </summary>
    Private Sub RemoveSupplierDocument(ByVal strDocName As String)
        Dim strSupplierDocsFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "SupplierDocs/"
        Dim strPath As String = Server.MapPath(strSupplierDocsFolder & strDocName)
        If File.Exists(strPath) Then
            ' SINK: PLANTED-VB-HR-526
            File.Delete(strPath)
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RemoveSupplierDocument"/>: the file-name component is
    ''' reduced via Path.GetFileName ("."/".."/empty explicitly rejected, since GetFileName
    ''' alone leaves those three values unchanged) AND the resulting name must be one
    ''' SupplierDocumentsBLL actually has on record for some supplier before it is deleted.
    ''' </summary>
    Private Sub RemoveSupplierDocumentSafe(ByVal strDocName As String)
        Dim strSafeName As String = Path.GetFileName(strDocName)
        If String.IsNullOrEmpty(strSafeName) OrElse strSafeName = "." OrElse strSafeName = ".." Then Exit Sub
        If Not SupplierDocumentsBLL.IsRegisteredDocument(strSafeName) Then Exit Sub
        Dim strSupplierDocsFolder As String = KartSettingsManager.GetKartConfig("general.uploadfolder") & "SupplierDocs/"
        Dim strPath As String = Server.MapPath(strSupplierDocsFolder & strSafeName)
        If File.Exists(strPath) Then
            ' SAFE_SINK: PLANTED-VB-HR-526-safe
            File.Delete(strPath)
        End If
    End Sub

    ''' <summary>
    ''' Server-side allow-list of this project's own registered EDI/stock-feed partner hosts, used by
    ''' the safe connectivity-check twins below.
    ''' </summary>
    Private Shared ReadOnly AllowedSupplierEdiHosts() As String = {"edi.kartris-partners.com", "edi2.kartris-partners.com"}

    ''' <summary>
    ''' Direct CWE-941: pHostPort ("host:port") comes straight off the request and is used to open a
    ''' raw TCP connection with no check that the host is a supplier endpoint this project actually
    ''' knows about -- the admin link that reaches this (per the onboarding-email convention above)
    ''' can be pointed at any internal or external host:port the server can reach.
    ''' </summary>
    Private Sub TestSupplierEndpoint(ByVal pHostPort As String)
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Exit Sub
        Dim strHost As String = pHostPort.Substring(0, intLastColon)
        Dim intPort As Integer
        If Not Integer.TryParse(pHostPort.Substring(intLastColon + 1), intPort) Then Exit Sub

        Dim objClient As New TcpClient(strHost, intPort) ' SINK: PLANTED-VB-HR-701
        objClient.Close()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="TestSupplierEndpoint"/>: the host component of pHostPort must
    ''' match one of the pre-registered partner hostnames in AllowedSupplierEdiHosts before any
    ''' connection is attempted.
    ''' </summary>
    Private Sub TestSupplierEndpointSafe(ByVal pHostPort As String)
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Exit Sub
        Dim strHost As String = pHostPort.Substring(0, intLastColon)
        Dim intPort As Integer
        If Not Integer.TryParse(pHostPort.Substring(intLastColon + 1), intPort) Then Exit Sub
        If Array.IndexOf(AllowedSupplierEdiHosts, LCase(Trim(strHost))) = -1 Then Exit Sub

        Dim objClient As New TcpClient(strHost, intPort) ' SAFE_SINK: PLANTED-VB-HR-701-safe
        objClient.Close()
    End Sub

    ''' <summary>
    ''' Diversity/batch CWE-941: one raw TCP connection attempt per comma-separated "host:port" entry
    ''' in pHostPortList, none validated. The per-entry pass/fail summary for the whole batch is
    ''' genuinely consumed downstream via this project's own pre-existing
    ''' KartrisDBBLL._AddAdminLog admin-audit-trail helper (fully parameterized stored-procedure call).
    ''' </summary>
    Private Sub TestSupplierEndpointsBulk(ByVal pHostPortList As String)
        Dim arrEntries() As String = Split(pHostPortList, ",")
        Dim dicResults As New Dictionary(Of String, Boolean)
        For Each strEntry As String In arrEntries
            Dim strTrimmedEntry As String = Trim(strEntry)
            If strTrimmedEntry = "" Then Continue For
            Dim intLastColon As Integer = strTrimmedEntry.LastIndexOf(":"c)
            If intLastColon <= 0 OrElse intLastColon = strTrimmedEntry.Length - 1 Then Continue For
            Dim strHost As String = strTrimmedEntry.Substring(0, intLastColon)
            Dim intPort As Integer
            If Not Integer.TryParse(strTrimmedEntry.Substring(intLastColon + 1), intPort) Then Continue For

            Try
                Dim objClient As New TcpClient(strHost, intPort) ' SINK: PLANTED-VB-HR-705
                objClient.Close()
                dicResults(strTrimmedEntry) = True
            Catch
                dicResults(strTrimmedEntry) = False
            End Try
        Next

        Dim sbdSummary As New System.Text.StringBuilder()
        For Each kvpResult As KeyValuePair(Of String, Boolean) In dicResults
            sbdSummary.Append(kvpResult.Key).Append("=").Append(kvpResult.Value).Append("; ")
        Next
        KartrisDBBLL._AddAdminLog(HttpContext.Current.Session("_User"), ADMIN_LOG_TABLE.ExecuteQuery, "Supplier endpoint bulk connectivity check: " & sbdSummary.ToString(), "", "0")
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="TestSupplierEndpointsBulk"/>: every entry's host must match
    ''' AllowedSupplierEdiHosts BEFORE any connection in the batch is attempted -- a bad entry is
    ''' skipped individually rather than the validation being bypassed for the whole batch.
    ''' </summary>
    Private Sub TestSupplierEndpointsBulkSafe(ByVal pHostPortList As String)
        Dim arrEntries() As String = Split(pHostPortList, ",")
        Dim dicResults As New Dictionary(Of String, Boolean)
        For Each strEntry As String In arrEntries
            Dim strTrimmedEntry As String = Trim(strEntry)
            If strTrimmedEntry = "" Then Continue For
            Dim intLastColon As Integer = strTrimmedEntry.LastIndexOf(":"c)
            If intLastColon <= 0 OrElse intLastColon = strTrimmedEntry.Length - 1 Then Continue For
            Dim strHost As String = strTrimmedEntry.Substring(0, intLastColon)
            Dim intPort As Integer
            If Not Integer.TryParse(strTrimmedEntry.Substring(intLastColon + 1), intPort) Then Continue For
            If Array.IndexOf(AllowedSupplierEdiHosts, LCase(Trim(strHost))) = -1 Then
                dicResults(strTrimmedEntry) = False
                Continue For
            End If

            Try
                Dim objClient As New TcpClient(strHost, intPort) ' SAFE_SINK: PLANTED-VB-HR-705-safe
                objClient.Close()
                dicResults(strTrimmedEntry) = True
            Catch
                dicResults(strTrimmedEntry) = False
            End Try
        Next

        Dim sbdSummary As New System.Text.StringBuilder()
        For Each kvpResult As KeyValuePair(Of String, Boolean) In dicResults
            sbdSummary.Append(kvpResult.Key).Append("=").Append(kvpResult.Value).Append("; ")
        Next
        KartrisDBBLL._AddAdminLog(HttpContext.Current.Session("_User"), ADMIN_LOG_TABLE.ExecuteQuery, "Supplier endpoint bulk connectivity check (safe): " & sbdSummary.ToString(), "", "0")
    End Sub

    Sub GetSuppliersList()
        mvwSuppliersData.SetActiveView(viwSuppliersList)

        gvwSuppliers.DataSource = Nothing
        gvwSuppliers.DataBind()

        Dim tblSuppliersList As New DataTable
        tblSuppliersList = GetSuppliersFromCache()

        If tblSuppliersList.Rows.Count = 0 Then mvwSuppliersData.SetActiveView(viwNoItems)

        gvwSuppliers.DataSource = tblSuppliersList
        gvwSuppliers.DataBind()

    End Sub

    Protected Sub gvwSuppliers_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvwSuppliers.PageIndexChanging
        gvwSuppliers.PageIndex = e.NewPageIndex
        GetSuppliersList()
    End Sub

    Private Sub gvwSuppliers_RowCommand(ByVal src As Object, ByVal e As GridViewCommandEventArgs) Handles gvwSuppliers.RowCommand
        Select Case e.CommandName
            Case "EditSupplier"
                gvwSuppliers.SelectedIndex = CInt(e.CommandArgument) - (gvwSuppliers.PageSize * gvwSuppliers.PageIndex)
                litSupplierID.Text = gvwSuppliers.SelectedValue
                PrepareExistingSupplier()
            Case "viwLinkedProducts"
                gvwSuppliers.SelectedIndex = CInt(e.CommandArgument) - (gvwSuppliers.PageSize * gvwSuppliers.PageIndex)
                litSupplierID.Text = gvwSuppliers.SelectedValue
                GetLinkedProducts()
        End Select
    End Sub

    Private Sub GetLinkedProducts()
        pnlNoLinkedProducts.Visible = False
        gvwLinkedProducts.DataSource = Nothing
        gvwLinkedProducts.DataBind()

        Dim tblLinkedProducts As New DataTable
        Dim objProductsBLL As New ProductsBLL
        tblLinkedProducts = objProductsBLL._GetProductsBySupplier(Session("_LANG"), GetSupplierID())

        If tblLinkedProducts.Rows.Count = 0 Then
            pnlNoLinkedProducts.Visible = True
            gvwLinkedProducts.Visible = False
        Else
            pnlNoLinkedProducts.Visible = False
            gvwLinkedProducts.Visible = True
            gvwLinkedProducts.DataSource = tblLinkedProducts
            gvwLinkedProducts.DataBind()
        End If

        mvwSuppliers.SetActiveView(viwLinkedProducts)
        updSupplierDetails.Update()

    End Sub

    Protected Sub lnkBtnCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnCancel.Click
        litSupplierID.Text = "0"
        mvwSuppliers.SetActiveView(viwSuppliersData)
        updSupplierDetails.Update()
    End Sub

    Protected Sub lnkBtnNewSupplier_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnNewSupplier.Click
        PrepareNewSupplier()
    End Sub

    Protected Sub lnkBtnSave_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnSave.Click
        '' calling the save method for (INSERT/UPDATE)
        If GetSupplierID() = 0 Then '' new
            If Not SaveSupplier(DML_OPERATION.INSERT) Then Return
        Else                        '' update
            If Not SaveSupplier(DML_OPERATION.UPDATE) Then Return
        End If

        'Show animated 'updated' message


        CType(Me.Master, Skins_Admin_Template).DataUpdated()

        GetSuppliersList()

        litSupplierID.Text = "0"
        mvwSuppliers.SetActiveView(viwSuppliersData)
        updSupplierDetails.Update()
    End Sub

    Function SaveSupplier(ByVal enumOperation As DML_OPERATION) As Boolean

        Dim strSupplierName As String = txtSupplierName.Text
        Dim blnLive As Boolean = chkSupplierLive.Checked
        Dim objUsersBLL As New UsersBLL

        Dim strMessage As String = ""
        Select Case enumOperation
            Case DML_OPERATION.UPDATE
                If Not objUsersBLL._UpdateSuppliers(GetSupplierID(), strSupplierName, blnLive, strMessage) Then
                    _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, strMessage)
                    Return False
                End If
            Case DML_OPERATION.INSERT
                If Not objUsersBLL._AddSuppliers(strSupplierName, blnLive, strMessage) Then
                    _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, strMessage)
                    Return False
                End If
        End Select

        RefreshSuppliersCache()
        Return True

    End Function

    Private Function GetSupplierID() As Integer
        If litSupplierID.Text <> "" Then
            Return CLng(litSupplierID.Text)
        End If
        Return 0
    End Function

    Sub PrepareNewSupplier()
        litSupplierID.Text = "0"
        txtSupplierName.Text = Nothing
        chkSupplierLive.Checked = False
        mvwSuppliers.SetActiveView(viwDetails)
        updSupplierDetails.Update()
    End Sub

    Sub PrepareExistingSupplier()
        chkSupplierLive.Checked = DirectCast(gvwSuppliers.SelectedRow.Cells(1).FindControl("chkSup_Live"), CheckBox).Checked
        txtSupplierName.Text = gvwSuppliers.SelectedRow.Cells(0).Text
        mvwSuppliers.SetActiveView(viwDetails)
        updSupplierDetails.Update()
    End Sub

    Protected Sub lnkBtnHideLinkedProducts_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnHideLinkedProducts.Click
        mvwSuppliers.SetActiveView(viwSuppliersData)
    End Sub

    Protected Sub gvwLinkedProducts_PageIndexChanging(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewPageEventArgs) Handles gvwLinkedProducts.PageIndexChanging
        gvwLinkedProducts.PageIndex = e.NewPageIndex
        GetLinkedProducts()
    End Sub
End Class
