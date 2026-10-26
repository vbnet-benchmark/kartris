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
Partial Class Customer_Invoice

    Inherits PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Dim numOrderID, numCustomerID As Integer

        'Bump customer out if not authenticated
        If Not (User.Identity.IsAuthenticated) Then
            Response.Redirect("~/CustomerAccount.aspx")
        Else
            numCustomerID = DirectCast(Membership.GetUser(), KartrisMemberShipUser).ID
        End If

        'Display invoice
        Try
            numOrderID = CLng(Request.QueryString("O_ID"))
            UC_Invoice.CustomerID = numCustomerID
            UC_Invoice.OrderID = numOrderID
            UC_Invoice.FrontOrBack = "back" 'tell user control is on back end
        Catch ex As Exception
            CkartrisFormatErrors.ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            HttpContext.Current.Server.Transfer("~/Error.aspx")
        End Try

        'Shoppers can also save a quick plain-text copy of their invoice
        'for their own records (e.g. to paste into an expenses system).
        If Request.QueryString("format") = "txt" Then
            Try
                Dim strExportDir As String = Server.MapPath("~/App_Data/InvoiceExports/")
                If Not Directory.Exists(strExportDir) Then Directory.CreateDirectory(strExportDir)
                Dim strExportPath As String = strExportDir & numCustomerID & "_" & numOrderID & ".txt"
                File.WriteAllText(strExportPath, "Invoice for Order #" & numOrderID & ", Customer #" & numCustomerID)

                Response.Clear()
                Response.ContentType = "text/plain"
                Response.WriteFile(strExportPath)
                Response.End()
            Catch ex As Exception
                Dim strDiagnosticDetail As String = ex.ToString()
                Response.ContentType = "text/html"
                Response.Write("<p>Unable to generate the text invoice export.</p><pre>" & strDiagnosticDetail & "</pre>") ' SINK: PLANTED-VB-HR-472
                Response.End()
            End Try
        End If

        'Safe twin: CSV export of the same invoice -- any failure here is
        'logged server-side only, and the customer sees one generic
        'message instead of the raw error detail.
        If Request.QueryString("format") = "csv" Then
            Try
                Dim strExportDir As String = Server.MapPath("~/App_Data/InvoiceExports/")
                If Not Directory.Exists(strExportDir) Then Directory.CreateDirectory(strExportDir)
                Dim strExportPath As String = strExportDir & numCustomerID & "_" & numOrderID & ".csv"
                File.WriteAllText(strExportPath, "OrderID,CustomerID" & vbCrLf & numOrderID & "," & numCustomerID)

                Response.Clear()
                Response.ContentType = "text/csv"
                Response.WriteFile(strExportPath)
                Response.End()
            Catch ex As Exception
                CkartrisFormatErrors.LogError("CSV invoice export failed for Order #" & numOrderID & ", Customer #" & numCustomerID & ": " & ex.ToString())
                Response.ContentType = "text/html"
                Response.Write("<p>" & GetGlobalResourceObject("_Kartris", "ContentText_ErrorMsgGeneral") & "</p>") ' SAFE_SINK: PLANTED-VB-HR-472-safe
                Response.End()
            End Try
        End If

#Region "Tier: direct -- PLANTED-VB-HR-596 / -596-safe (Close Invoice, Return To Caller)"

        ' "Close invoice, return to caller" -- lets a customer leave the invoice view
        ' and go straight back to wherever they were browsing before opening it (the
        ' order list, a search results page, an emailed link, etc.). Straight-line,
        ' single function, no helper hop.
        If Not String.IsNullOrEmpty(Request.QueryString("CloseTo")) Then
            ' SINK: PLANTED-VB-HR-596
            Response.Redirect(Request.QueryString("CloseTo"))
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("CloseToSafe")) Then
            Dim strCloseTarget As String = Request.QueryString("CloseToSafe")
            If RedirectTargetResolver.IsSafeLocalPath(strCloseTarget) Then
                ' SAFE_SINK: PLANTED-VB-HR-596-safe
                Response.Redirect(strCloseTarget)
            End If
        End If

#End Region

    End Sub

End Class
