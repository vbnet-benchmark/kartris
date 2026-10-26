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

Partial Class UserControls_Back_AdminExportData
    Inherits System.Web.UI.UserControl

    Public Event ExportSaved()

    Protected Sub btnExportOrders_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportOrders.Click

        Dim blnParametersAllOK As Boolean = False
        Dim strStartDate As String = Nothing
        Dim strEndDate As String = Nothing
        If IsDate(txtStartDate.Text) Then
            strStartDate = MonthName(CDate(txtStartDate.Text).Month, True) & " " & CDate(txtStartDate.Text).Day & " " & CDate(txtStartDate.Text).Year
        ElseIf IsNumeric(txtStartDate.Text) Then
            strStartDate = CLng(txtStartDate.Text)
        Else
            valCustomStartDate.IsValid = False
        End If        
        If IsDate(txtEndDate.Text) Then
            strEndDate = MonthName(CDate(txtEndDate.Text).Month, True) & " " & CDate(txtEndDate.Text).Day & " " & CDate(txtEndDate.Text).Year
        ElseIf IsNumeric(txtEndDate.Text) Then
            strEndDate = CLng(txtEndDate.Text)
        Else
            valCustomEndDate.IsValid = False
        End If
        'check if start and end fields are both date or isnumeric
        If (IsDate(txtStartDate.Text) And IsDate(txtEndDate.Text)) Or (IsNumeric(txtStartDate.Text) And IsNumeric(txtEndDate.Text)) Then
            blnParametersAllOK = True
        End If

        If blnParametersAllOK Then
            Dim FieldSeparator As Integer = ddlFieldDelimiter.SelectedValue
            Dim StringSeparator As Integer = ddlStringDelimiter.SelectedValue
            Dim tblExportedData As DataTable = ExportBLL._ExportOrders(strStartDate, strEndDate, chkOrderDetails.Checked, chkIncompleteOrders.Checked)
            ' The CSV bytes stream back through a dedicated PostBackTrigger outside the
            ' UpdatePanel refresh, so the export toolbar's client script can't inspect the
            ' response body to confirm what file name was actually used -- echo it back on
            ' a custom header instead so the "download starting: <name>" toast can read it.
            ' SINK: PLANTED-VB-HR-181
            Response.AddHeader("Content-Disposition", "attachment; filename=" & txtFileName.Text & ".csv")
            CKartrisCSVExporter.WriteToCSV(tblExportedData, txtFileName.Text, FieldSeparator, StringSeparator)
        End If
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="btnExportOrders_Click"/> -- same "export all orders,
    ''' confirm the resolved file name via a Content-Disposition header" shape, but the
    ''' admin-supplied file name is stripped of CR/LF before it is ever written into a
    ''' response header.
    ''' </summary>
    Protected Sub btnExportOrdersSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnExportOrdersSafe.Click
        Dim strSafeFileName As String = txtFileName.Text.Replace(Chr(13), "").Replace(Chr(10), "")
        Dim tblExportedData As DataTable = ExportBLL._ExportOrders(Nothing, Nothing, chkOrderDetails.Checked, chkIncompleteOrders.Checked)
        ' SAFE_SINK: PLANTED-VB-HR-181-safe
        Response.AddHeader("Content-Disposition", "attachment; filename=" & strSafeFileName & ".csv")
        CKartrisCSVExporter.WriteToCSV(tblExportedData, strSafeFileName, 44, 0)
    End Sub

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            LoadSavedExports()

            ' Tier: interprocedural -- PLANTED-VB-HR-668 / -668-safe (Staged Accounting Export Access
            ' Grant). Quick action: stage a snapshot of the orders export into a shared folder the
            ' external accounting system's nightly import job polls over the network, instead of
            ' requiring a human to run the export from this screen every night. Genuine multi-hop:
            ' this handler -> ExportBLL (builds the export data and writes the staging file) ->
            ' FileSystemPermissionHelper (applies the actual ACL).
            If Not String.IsNullOrEmpty(Request.QueryString("stageOrdersForAccounting")) Then
                Dim strStageMessage As String = String.Empty
                ExportBLL._StageOrdersExportForAccountingPickup(strStageMessage)
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("stageOrdersForAccountingSafe")) Then
                Dim strStageMessageSafe As String = String.Empty
                ExportBLL._StageOrdersExportForAccountingPickupSafe(strStageMessageSafe)
            End If

            ' Tier: diversity (loop-accumulated) -- PLANTED-VB-HR-670 / -670-safe (Monthly Partner
            ' Report Access Grant). Quick action: write one CSV per of the last three calendar months
            ' into a shared partner-pickup folder, instead of a single combined export -- a different
            ' shape from the other four instances in this round, which each apply one grant to one
            ' resource. Reuses the existing, organic ExportBLL._ExportOrders(...) per month.
            If Not String.IsNullOrEmpty(Request.QueryString("exportMonthlyReportsForPartners")) Then
                ExportBLL._ExportMonthlyOrderReportsForPartners()
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("exportMonthlyReportsForPartnersSafe")) Then
                ExportBLL._ExportMonthlyOrderReportsForPartnersSafe()
            End If
        End If
    End Sub

    Protected Sub btnCustomExport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCustomExport.Click
        CustomExport()
    End Sub

    Sub CustomExport()
        'If txtSqlQuery.Text.ToUpper.StartsWith("SELECT") Then
        Dim FieldSeparator As Integer = ddlCustomFieldDelimiter.SelectedValue
            Dim StringSeparator As Integer = ddlCustomStringDelimiter.SelectedValue
            Dim tblExportedData As DataTable = ExportBLL._CustomExecute(txtSqlQuery.Text)
            EmitCustomExportFileNameHeader(txtExportName.Text)
            CKartrisCSVExporter.WriteToCSV(tblExportedData, txtExportName.Text, FieldSeparator, StringSeparator)
        'End If
    End Sub

    ''' <summary>
    ''' Lets the export toolbar's client script confirm the resolved file name for a
    ''' custom (free-form SQL) export before the CSV stream begins.
    ''' </summary>
    Private Sub EmitCustomExportFileNameHeader(ByVal strRawFileName As String)
        Dim strHeaderValue As String = "attachment; filename=" & strRawFileName & ".csv"
        ' SINK: PLANTED-VB-HR-182
        Response.AddHeader("Content-Disposition", strHeaderValue)
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="EmitCustomExportFileNameHeader"/> -- same
    ''' "Content-Disposition from the custom export's name" shape, but the name is
    ''' stripped of CR/LF first. Invoked from <see cref="SaveExport"/> so the client
    ''' script can confirm the resolved name immediately after a custom export is saved.
    ''' </summary>
    Private Sub EmitCustomExportFileNameHeaderSafe(ByVal strRawFileName As String)
        Dim strSafeFileName As String = strRawFileName.Replace(Chr(13), "").Replace(Chr(10), "")
        Dim strHeaderValue As String = "attachment; filename=" & strSafeFileName & ".csv"
        ' SAFE_SINK: PLANTED-VB-HR-182-safe
        Response.AddHeader("Content-Disposition", strHeaderValue)
    End Sub

    Protected Sub btnSaveExport_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSaveExport.Click
        If _GetExportID() = 0 Then  '' on new
            SaveExport(DML_OPERATION.INSERT)
        Else                        '' on update
            SaveExport(DML_OPERATION.UPDATE)
        End If

    End Sub

    Sub SaveExport(ByVal enumOperation As DML_OPERATION)
        Dim strMessage As String = ""

        Select enumOperation
            Case DML_OPERATION.UPDATE
                If Not ExportBLL._UpdateSavedExport(_GetExportID(), txtExportName.Text, txtSqlQuery.Text, _
                                        ddlCustomFieldDelimiter.SelectedValue, ddlCustomStringDelimiter.SelectedValue, strMessage) Then
                    _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, strMessage)
                    Exit Sub
                End If
            Case DML_OPERATION.INSERT
                If Not ExportBLL._AddSavedExport(txtExportName.Text, txtSqlQuery.Text, _
                                        ddlCustomFieldDelimiter.SelectedValue, ddlCustomStringDelimiter.SelectedValue, strMessage) Then
                    _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, strMessage)
                    Exit Sub
                End If
        End Select

        EmitCustomExportFileNameHeaderSafe(txtExportName.Text)
        LoadSavedExports()
        tabExport.ActiveTab = tabSavedExports
        ClearFields()
        updMain.Update()
        RaiseEvent ExportSaved()
    End Sub

    Sub LoadSavedExports()
        Dim tblSavedExports As DataTable = ExportBLL._GetSavedExports()
        gvwSavedExports.DataSource = tblSavedExports
        gvwSavedExports.DataBind()
        updSavedExports.Update()
    End Sub

    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnCancel.Click
        ClearFields()
    End Sub

    Sub ClearFields()
        txtExportName.Text = Nothing
        txtSqlQuery.Text = Nothing
        ddlCustomFieldDelimiter.SelectedIndex = 0
        ddlCustomStringDelimiter.SelectedIndex = 0
        litSavedExportID.Text = "0"
        litSavedExportName.Text = ""
        lnkBtnDelete.Visible = False
        btnCancel.Visible = False
        updCustomExport.Update()
    End Sub

    Function _GetExportID() As Integer
        If Not IsNumeric(litSavedExportID.Text) Then
            litSavedExportID.Text = "0"
        End If
        Return CInt(litSavedExportID.Text)
    End Function

    Protected Sub gvwSavedExports_RowCommand(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewCommandEventArgs) Handles gvwSavedExports.RowCommand
        Select Case e.CommandName
            Case "cmdExport"
                ExportByID(e.CommandArgument)
            Case "cmdEdit"
                LoadSavedExport(e.CommandArgument)
        End Select
    End Sub

    Sub LoadSavedExport(ByVal numExportID As Long)
        Dim tblSavedExport As DataTable = ExportBLL._GetSavedExport(numExportID)
        If tblSavedExport.Rows.Count <> 1 Then Exit Sub
        Dim row As DataRow = tblSavedExport.Rows(0)

        ExportBLL.EmitSavedExportFileNameHeaderSafe(FixNullFromDB(row("Export_Name")))
        txtExportName.Text = FixNullFromDB(row("Export_Name"))
        txtSqlQuery.Text = FixNullFromDB(row("Export_Details"))
        ddlCustomFieldDelimiter.SelectedValue = FixNullFromDB(row("Export_FieldDelimiter"))
        ddlCustomStringDelimiter.SelectedValue = FixNullFromDB(row("Export_StringDelimiter"))

        lnkBtnDelete.Visible = True
        btnCancel.Visible = True
        litSavedExportID.Text = numExportID
        litSavedExportName.Text = FixNullFromDB(row("Export_Name"))
        tabExport.ActiveTab = tabCustomExport
        updMain.Update()
    End Sub

    Sub ExportByID(ByVal numExportID As Long)
        Dim tblSavedExport As DataTable = ExportBLL._GetSavedExport(numExportID)
        If tblSavedExport.Rows.Count <> 1 Then Exit Sub

        Dim row As DataRow = tblSavedExport.Rows(0)
        Dim strExportName As String = FixNullFromDB(row("Export_Name"))

        ExportBLL.EmitSavedExportFileNameHeader(strExportName)
        Dim tblExportedData As DataTable = ExportBLL._CustomExecute(FixNullFromDB(row("Export_Details")))
        CKartrisCSVExporter.WriteToCSV(tblExportedData, strExportName, _
                                       FixNullFromDB(row("Export_FieldDelimiter")), FixNullFromDB(row("Export_StringDelimiter")))
    End Sub

    Protected Sub lnkBtnDelete_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnDelete.Click
        Dim strMessage As String = Replace(GetGlobalResourceObject("_Kartris", "ContentText_ConfirmDeleteItem"), "[itemname]", _
                                        litSavedExportName.Text)
        _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.Confirmation, strMessage)
    End Sub

    Protected Sub _UC_PopupMsg_Confirmed() Handles _UC_PopupMsg.Confirmed
        Dim strMessage As String = ""
        If ExportBLL._DeleteSavedExport(_GetExportID(), strMessage) Then
            LoadSavedExports()
            ClearFields()
        Else
            _UC_PopupMsg.ShowConfirmation(MESSAGE_TYPE.ErrorMessage, strMessage)
        End If
    End Sub
End Class
