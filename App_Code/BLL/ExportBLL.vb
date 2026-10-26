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
Imports kartrisExportData
Imports kartrisExportDataTableAdapters
Imports CkartrisDisplayFunctions
Imports CkartrisFormatErrors
Imports CkartrisDataManipulation
Imports System.Web.HttpContext
Public Class ExportBLL
    Private Shared _Adptr As SavedExportsTblAdptr = Nothing
    Protected Shared ReadOnly Property Adptr() As SavedExportsTblAdptr
        Get
            _Adptr = New SavedExportsTblAdptr
            Return _Adptr
        End Get
    End Property

    Public Shared Function _GetSavedExports() As DataTable
        Return Adptr._GetData()
    End Function

    Public Shared Function _GetSavedExport(ByVal numExportID As Long) As DataTable
        Return Adptr._GetByID(numExportID)
    End Function

    Public Shared Function _AddSavedExport(ByVal strName As String, ByVal strDetails As String, ByVal numFieldDelimiter As Integer, _
                                           ByVal numStringDelimiter As Integer, ByVal strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdAddExport As SqlCommand = sqlConn.CreateCommand
            cmdAddExport.CommandText = "_spKartrisSavedExports_Add"
            Dim savePoint As SqlTransaction = Nothing
            cmdAddExport.CommandType = CommandType.StoredProcedure
            Try
                cmdAddExport.Parameters.AddWithValue("@Name", strName)
                cmdAddExport.Parameters.AddWithValue("@DateCreated", NowOffset)
                cmdAddExport.Parameters.AddWithValue("@Details", strDetails)
                cmdAddExport.Parameters.AddWithValue("@FieldDelimiter", numFieldDelimiter)
                cmdAddExport.Parameters.AddWithValue("@StringDelimiter", numStringDelimiter)
                cmdAddExport.Parameters.AddWithValue("@New_ID", 0).Direction = ParameterDirection.Output

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdAddExport.Transaction = savePoint

                cmdAddExport.ExecuteNonQuery()

                If cmdAddExport.Parameters("@New_ID").Value Is Nothing OrElse _
                cmdAddExport.Parameters("@New_ID").Value Is DBNull.Value Then
                    Throw New ApplicationException(GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgDBCustom"))
                End If

                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
                If Not savePoint Is Nothing Then savePoint.Rollback()
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False

    End Function
    Public Shared Function _UpdateSavedExport(ByVal numExportID As Long, ByVal strName As String, ByVal strDetails As String, _
                                              ByVal numFieldDelimiter As Integer, ByVal numStringDelimiter As Integer, ByVal strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateExport As SqlCommand = sqlConn.CreateCommand
            cmdUpdateExport.CommandText = "_spKartrisSavedExports_Update"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateExport.CommandType = CommandType.StoredProcedure
            Try
                cmdUpdateExport.Parameters.AddWithValue("@Name", strName)
                cmdUpdateExport.Parameters.AddWithValue("@DateModified", NowOffset)
                cmdUpdateExport.Parameters.AddWithValue("@Details", strDetails)
                cmdUpdateExport.Parameters.AddWithValue("@FieldDelimiter", numFieldDelimiter)
                cmdUpdateExport.Parameters.AddWithValue("@StringDelimiter", numStringDelimiter)
                cmdUpdateExport.Parameters.AddWithValue("@ExportID", numExportID)

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateExport.Transaction = savePoint

                cmdUpdateExport.ExecuteNonQuery()

                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
                If Not savePoint Is Nothing Then savePoint.Rollback()
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False

    End Function

    Public Shared Function _DeleteSavedExport(ByVal numExportID As Long, ByVal strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdDeleteExport As SqlCommand = sqlConn.CreateCommand
            cmdDeleteExport.CommandText = "_spKartrisSavedExports_Delete"
            Dim savePoint As SqlTransaction = Nothing
            cmdDeleteExport.CommandType = CommandType.StoredProcedure
            Try
                cmdDeleteExport.Parameters.AddWithValue("@ExportID", numExportID)

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdDeleteExport.Transaction = savePoint

                cmdDeleteExport.ExecuteNonQuery()

                savePoint.Commit()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
                If Not savePoint Is Nothing Then savePoint.Rollback()
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

    Public Shared Function _ExportOrders(ByVal strDateFrom As String, ByVal strDateTo As String, ByVal blnIncludeDetails As Boolean, ByVal blnIncompleteOrders As Boolean) As DataTable
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)

            Dim cmdExecuteQuery As SqlCommand = sqlConn.CreateCommand
            cmdExecuteQuery.CommandText = "_spKartrisDB_ExportOrders"

            cmdExecuteQuery.CommandType = CommandType.StoredProcedure
            cmdExecuteQuery.Parameters.AddWithValue("@StartDate", FixNullToDB(strDateFrom))
            cmdExecuteQuery.Parameters.AddWithValue("@EndDate", FixNullToDB(strDateTo))
            cmdExecuteQuery.Parameters.AddWithValue("@IncludeDetails", blnIncludeDetails)
            cmdExecuteQuery.Parameters.AddWithValue("@IncludeIncomplete", blnIncompleteOrders)
            cmdExecuteQuery.CommandTimeout = 3600

            Try
                Dim tblExport As New DataTable()
                Using adptr As New SqlDataAdapter(cmdExecuteQuery)
                    adptr.Fill(tblExport)
                    Return tblExport
                End Using
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            Finally
            End Try
        End Using
        Return Nothing
    End Function
    Public Shared Function _CustomExecute(ByVal strQuery As String) As DataTable

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)

            Dim cmdExecuteQuery As SqlCommand = sqlConn.CreateCommand
            cmdExecuteQuery.CommandText = "_spKartrisDB_ExecuteQuery"

            cmdExecuteQuery.CommandType = CommandType.StoredProcedure
            cmdExecuteQuery.Parameters.AddWithValue("@QueryText", FixNullToDB(strQuery, "s"))
            cmdExecuteQuery.CommandTimeout = 3600

            Try
                Dim tblExport As New DataTable()
                Using adptr As New SqlDataAdapter(cmdExecuteQuery)
                    adptr.Fill(tblExport)
                    Return tblExport
                End Using
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            End Try
        End Using

        Return Nothing
    End Function

    ''' <summary>
    ''' Lets the Saved Exports toolbar's client script confirm the resolved file name for
    ''' a saved (re-run) export before the CSV stream begins. The name originates from the
    ''' admin-supplied "Export Name" field at save time (see <c>_AddSavedExport</c> /
    ''' <c>_UpdateSavedExport</c>), persisted and re-read here, so this is a stored/second-order
    ''' flow rather than a direct one.
    ''' </summary>
    Public Shared Sub EmitSavedExportFileNameHeader(ByVal strExportName As String)
        ' SINK: PLANTED-VB-HR-183
        Current.Response.AddHeader("Content-Disposition", "attachment; filename=" & strExportName & ".csv")
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="EmitSavedExportFileNameHeader"/> -- same "stored
    ''' export name echoed back via a response header" shape, but the name is stripped of
    ''' CR/LF before it is ever written into a header. Invoked when a saved export is
    ''' loaded for editing rather than re-run.
    ''' </summary>
    Public Shared Sub EmitSavedExportFileNameHeaderSafe(ByVal strExportName As String)
        Dim strSafeExportName As String = If(strExportName, "").Replace(Chr(13), "").Replace(Chr(10), "")
        ' SAFE_SINK: PLANTED-VB-HR-183-safe
        Current.Response.AddHeader("X-Export-Name-Preview", strSafeExportName)
    End Sub

    ''' <summary>
    ''' Tier: interprocedural -- PLANTED-VB-HR-668 / -668-safe (Staged Accounting Export Access
    ''' Grant). Builds a full orders export (reusing the existing, organic <see cref="_ExportOrders"/>)
    ''' and writes it to a shared folder the external accounting system's nightly import job polls,
    ''' instead of streaming it back to an admin's browser. Genuine second hop: the actual ACL
    ''' decision is made by <see cref="FileSystemPermissionHelper"/>, a different class in a
    ''' different file, not inline here and not in the calling UI handler.
    ''' </summary>
    Public Shared Sub _StageOrdersExportForAccountingPickup(ByRef strMsg As String)
        Dim tblOrdersExport As DataTable = _ExportOrders(Nothing, Nothing, True, False)
        Dim strStagingFolder As String = Current.Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "AccountingExports/")
        If Not Directory.Exists(strStagingFolder) Then Directory.CreateDirectory(strStagingFolder)
        Dim strStagingFilePath As String = Path.Combine(strStagingFolder, "orders_export.csv")
        WriteDataTableToCsv(tblOrdersExport, strStagingFilePath)

        FileSystemPermissionHelper.GrantAccountingPickupAccess(strStagingFilePath)
        strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="_StageOrdersExportForAccountingPickup"/> -- same staging shape,
    ''' but the ACL grant (still made by <see cref="FileSystemPermissionHelper"/>) is scoped to the
    ''' one named service account the accounting import job actually runs as.
    ''' </summary>
    Public Shared Sub _StageOrdersExportForAccountingPickupSafe(ByRef strMsg As String)
        Dim tblOrdersExport As DataTable = _ExportOrders(Nothing, Nothing, True, False)
        Dim strStagingFolder As String = Current.Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "AccountingExports/")
        If Not Directory.Exists(strStagingFolder) Then Directory.CreateDirectory(strStagingFolder)
        Dim strStagingFilePath As String = Path.Combine(strStagingFolder, "orders_export.csv")
        WriteDataTableToCsv(tblOrdersExport, strStagingFilePath)

        FileSystemPermissionHelper.GrantAccountingPickupAccessSafe(strStagingFilePath)
        strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
    End Sub

    ''' <summary>
    ''' Tier: diversity (loop-accumulated) -- PLANTED-VB-HR-670 (Monthly Partner Report Access Grant).
    ''' Writes one CSV per of the last three calendar months into a shared partner-pickup folder,
    ''' reusing the existing, organic <see cref="_ExportOrders"/> per month -- and, unlike every other
    ''' instance in this round, applies its ACL grant inside the loop body, once per file produced,
    ''' rather than a single grant on one resource.
    ''' </summary>
    Public Shared Sub _ExportMonthlyOrderReportsForPartners()
        Dim strPartnerReportsFolder As String = Current.Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "PartnerReports/")
        If Not Directory.Exists(strPartnerReportsFolder) Then Directory.CreateDirectory(strPartnerReportsFolder)

        For intMonthsAgo As Integer = 0 To 2
            Dim dtMonthStart As DateTime = DateAdd(DateInterval.Month, -intMonthsAgo, Now).AddDays(1 - Now.Day)
            Dim dtMonthEnd As DateTime = dtMonthStart.AddMonths(1).AddDays(-1)
            Dim strStartDate As String = MonthName(dtMonthStart.Month, True) & " " & dtMonthStart.Day & " " & dtMonthStart.Year
            Dim strEndDate As String = MonthName(dtMonthEnd.Month, True) & " " & dtMonthEnd.Day & " " & dtMonthEnd.Year
            Dim tblMonthOrders As DataTable = _ExportOrders(strStartDate, strEndDate, True, False)

            Dim strMonthFilePath As String = Path.Combine(strPartnerReportsFolder, dtMonthStart.ToString("yyyy-MM") & "_Orders.csv")
            WriteDataTableToCsv(tblMonthOrders, strMonthFilePath)

            Dim objMonthFileSecurity As New System.Security.AccessControl.FileSecurity()
            Dim objMonthRule As New System.Security.AccessControl.FileSystemAccessRule( _
                "Everyone", System.Security.AccessControl.FileSystemRights.FullControl, _
                System.Security.AccessControl.AccessControlType.Allow)
            objMonthFileSecurity.AddAccessRule(objMonthRule) ' SINK: PLANTED-VB-HR-670
            File.SetAccessControl(strMonthFilePath, objMonthFileSecurity)
        Next
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="_ExportMonthlyOrderReportsForPartners"/> -- same per-month loop
    ''' and file layout, but each file is locked to the named service account the partner-pickup job
    ''' runs as, with read-only rights, instead of every account on the box.
    ''' </summary>
    Public Shared Sub _ExportMonthlyOrderReportsForPartnersSafe()
        Dim strPartnerReportsFolder As String = Current.Server.MapPath(KartSettingsManager.GetKartConfig("general.uploadfolder") & "PartnerReports/")
        If Not Directory.Exists(strPartnerReportsFolder) Then Directory.CreateDirectory(strPartnerReportsFolder)

        For intMonthsAgo As Integer = 0 To 2
            Dim dtMonthStart As DateTime = DateAdd(DateInterval.Month, -intMonthsAgo, Now).AddDays(1 - Now.Day)
            Dim dtMonthEnd As DateTime = dtMonthStart.AddMonths(1).AddDays(-1)
            Dim strStartDate As String = MonthName(dtMonthStart.Month, True) & " " & dtMonthStart.Day & " " & dtMonthStart.Year
            Dim strEndDate As String = MonthName(dtMonthEnd.Month, True) & " " & dtMonthEnd.Day & " " & dtMonthEnd.Year
            Dim tblMonthOrders As DataTable = _ExportOrders(strStartDate, strEndDate, True, False)

            Dim strMonthFilePath As String = Path.Combine(strPartnerReportsFolder, dtMonthStart.ToString("yyyy-MM") & "_Orders.csv")
            WriteDataTableToCsv(tblMonthOrders, strMonthFilePath)

            Dim objMonthFileSecuritySafe As New System.Security.AccessControl.FileSecurity()
            Dim objMonthRuleSafe As New System.Security.AccessControl.FileSystemAccessRule( _
                "NT SERVICE\PartnerPickupSvc", System.Security.AccessControl.FileSystemRights.Read, _
                System.Security.AccessControl.AccessControlType.Allow)
            objMonthFileSecuritySafe.AddAccessRule(objMonthRuleSafe) ' SAFE_SINK: PLANTED-VB-HR-670-safe
            File.SetAccessControl(strMonthFilePath, objMonthFileSecuritySafe)
        Next
    End Sub

    ''' <summary>
    ''' Small shared helper backing the two export flows above: flattens a DataTable to a
    ''' comma-separated file on disk. Not part of the planted shape itself -- just the plumbing that
    ''' gets a real file onto the filesystem for the permission grant to apply to.
    ''' </summary>
    Private Shared Sub WriteDataTableToCsv(ByVal tblData As DataTable, ByVal strFilePath As String)
        If tblData Is Nothing Then Return
        Using swCsv As New StreamWriter(strFilePath, False)
            For Each col As DataColumn In tblData.Columns
                swCsv.Write(col.ColumnName & ",")
            Next
            swCsv.WriteLine()
            For Each row As DataRow In tblData.Rows
                For Each col As DataColumn In tblData.Columns
                    swCsv.Write(Replace(CStr(row(col)), ",", " ") & ",")
                Next
                swCsv.WriteLine()
            Next
        End Using
    End Sub
End Class
