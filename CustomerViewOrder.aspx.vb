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
Imports KartSettingsManager

Partial Class Customer_ViewOrder
	Inherits PageBaseClass

	Protected APP_PricesIncTax, APP_ShowTaxDisplay As Boolean
	Private objCurrency As New CurrenciesBLL
	Private objBasket As New kartris.Basket
	Private numTaxDue, numTotalPriceExTax, numTotalPriceIncTax, numCouponDiscount, numCustomerDiscount, numShipping, numOrderHandlingCharge, numTotal As Double
	Private numDiscountPercentage, numPromotionDiscountTotal, numCouponDiscountTotal, CP_DiscountValue As Double
	Private strCouponCode, CP_DiscountType, CP_CouponCode As String
	Private numShippingPriceIncTax, numShippingPriceExTax, numOrderHandlingPriceIncTax, numOrderHandlingPriceExTax, numFinalTotalPriceInTaxGateway As Double
	Private numCurrencyIDGateway, numCurrencyID As Double

	Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
		Dim numCustomerID, numOrderID As Integer
        Dim tblOrder As Data.DataTable


        If Not (User.Identity.IsAuthenticated) Then
            Response.Redirect("~/CustomerAccount.aspx")
        Else
            numCustomerID = CurrentLoggedUser.ID
        End If

        If ConfigurationManager.AppSettings("TaxRegime").ToLower = "us" Or ConfigurationManager.AppSettings("TaxRegime").ToLower = "simple" Then
            APP_PricesIncTax = False
            APP_ShowTaxDisplay = False
        Else
            APP_PricesIncTax = GetKartConfig("general.tax.pricesinctax") = "y"
            APP_ShowTaxDisplay = GetKartConfig("frontend.display.showtax") = "y"
        End If


        numOrderID = Val(Request.QueryString("O_ID"))

        UC_CustomerOrder.ShowOrderSummary = True
        UC_CustomerOrder.OrderID = numOrderID

        Dim objBasketBLL As New BasketBLL
        tblOrder = objBasketBLL.GetCustomerOrderDetails(numOrderID)

        If tblOrder.Rows.Count > 0 Then
            If tblOrder.Rows(0).Item("O_CustomerID") <> numCustomerID Then
                phdOrderStatus.Visible = False
            End If
        Else
            phdOrderStatus.Visible = False
        End If

        ' --- "My Order Workspace" item lookups (order documents / order notes attached to a past
        ' order), dispatched by their own querystring parameters alongside the existing O_ID dispatch
        ' above. Reached from a "My Order Workspace" link on this same, already-authenticated page. ---
        If Not String.IsNullOrEmpty(Request.QueryString("WS_Doc")) Then
            'Lets a customer re-view a previously generated order document (e.g. a customs form or
            'packing slip) directly from its record ID
            Dim numDocID As Integer = Val(Request.QueryString("WS_Doc"))
            Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
            Using conn As New SqlConnection(strConnString)
                Dim cmd As New SqlCommand("SELECT DocumentName, Notes FROM dbo.OrderDocuments WHERE DocumentID = @id", conn)
                cmd.Parameters.AddWithValue("@id", numDocID)
                conn.Open()
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        ' SINK: PLANTED-VB-HR-356
                        litOrderWorkspaceResult.Text = "Document: " & Server.HtmlEncode(CStr(reader("DocumentName"))) & " - " & Server.HtmlEncode(CStr(reader("Notes")))
                    Else
                        litOrderWorkspaceResult.Text = "Document not found"
                    End If
                End Using
            End Using
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_DocSafe")) Then
            'Same order-document lookup as "WS_Doc", but only ever returns a document that actually
            'belongs to an order owned by the current customer
            Dim numDocID As Integer = Val(Request.QueryString("WS_DocSafe"))
            Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
            Using conn As New SqlConnection(strConnString)
                Dim cmd As New SqlCommand("SELECT DocumentName, Notes FROM dbo.OrderDocuments WHERE DocumentID = @id AND OwnerCustomerID = @ownerCustomerId", conn)
                cmd.Parameters.AddWithValue("@id", numDocID)
                cmd.Parameters.AddWithValue("@ownerCustomerId", numCustomerID)
                conn.Open()
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If reader.Read() Then
                        ' SAFE_SINK: PLANTED-VB-HR-356-safe
                        litOrderWorkspaceResult.Text = "Document: " & Server.HtmlEncode(CStr(reader("DocumentName"))) & " - " & Server.HtmlEncode(CStr(reader("Notes")))
                    Else
                        litOrderWorkspaceResult.Text = "Document not found"
                    End If
                End Using
            End Using
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_Note")) Then
            'Re-shows a note attached to an order, via a small per-page helper that does the actual
            'lookup -- used by the order workspace's "quick notes" refresh
            Dim numNoteID As Integer = Val(Request.QueryString("WS_Note"))
            litOrderWorkspaceResult.Text = FetchOrderNoteUnchecked(numNoteID)
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_NoteSafe")) Then
            'Safe twin of "WS_Note" -- the helper only ever returns a note for an order the current
            'customer actually owns
            Dim numNoteID As Integer = Val(Request.QueryString("WS_NoteSafe"))
            litOrderWorkspaceResult.Text = FetchOrderNoteForCurrentUser(numNoteID, numCustomerID)
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_Job")) Then
            'Re-shows a background order-export job's status, from the order workspace's cross-file
            'service layer, shared with the generic item viewer and the batch document refresh below
            Dim numJobID As Integer = Val(Request.QueryString("WS_Job"))
            Dim objJob As OrderWorkspaceRecord = OrderWorkspaceService.GetExportJobByID(numJobID)
            If objJob IsNot Nothing Then
                litOrderWorkspaceResult.Text = "Export job: " & Server.HtmlEncode(objJob.ItemDetail)
            Else
                litOrderWorkspaceResult.Text = "Export job not found"
            End If
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_JobSafe")) Then
            'Safe twin of "WS_Job" -- only ever returns a job for an order the current customer
            'actually owns
            Dim numJobID As Integer = Val(Request.QueryString("WS_JobSafe"))
            Dim objJob As OrderWorkspaceRecord = OrderWorkspaceService.GetExportJobForOwner(numJobID, numCustomerID)
            If objJob IsNot Nothing Then
                litOrderWorkspaceResult.Text = "Export job: " & Server.HtmlEncode(objJob.ItemDetail)
            Else
                litOrderWorkspaceResult.Text = "Export job not found"
            End If
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_Item")) Then
            'Generic workspace-item viewer: "WS_Item" carries the record ID, "WS_ItemType" selects
            'which of the three workspace tables (document/note/export job) to read it from
            Dim numItemID As Integer = Val(Request.QueryString("WS_Item"))
            Dim strItemType As String = Request.QueryString("WS_ItemType")
            Dim objItem As OrderWorkspaceRecord = OrderWorkspaceService.GetWorkspaceItemByTypeAndID(strItemType, numItemID)
            If objItem IsNot Nothing Then
                litOrderWorkspaceResult.Text = "Item: " & Server.HtmlEncode(objItem.ItemDetail)
            Else
                litOrderWorkspaceResult.Text = "Item not found"
            End If
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_ItemSafe")) Then
            'Safe twin of "WS_Item" -- only ever returns an item the current customer actually owns,
            'regardless of which of the three tables "WS_ItemType" resolves to
            Dim numItemID As Integer = Val(Request.QueryString("WS_ItemSafe"))
            Dim strItemType As String = Request.QueryString("WS_ItemType")
            Dim objItem As OrderWorkspaceRecord = OrderWorkspaceService.GetWorkspaceItemByTypeAndIDForOwner(strItemType, numItemID, numCustomerID)
            If objItem IsNot Nothing Then
                litOrderWorkspaceResult.Text = "Item: " & Server.HtmlEncode(objItem.ItemDetail)
            Else
                litOrderWorkspaceResult.Text = "Item not found"
            End If
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_Batch")) Then
            'Bulk "my order documents" refresh: a comma-separated list of previously-seen document
            'IDs is re-read in one request instead of one request per row
            Dim arrRawIDs As String() = Request.QueryString("WS_Batch").Split(","c)
            Dim lstIDs As New List(Of Integer)
            For Each strID As String In arrRawIDs
                lstIDs.Add(Val(strID))
            Next
            Dim lstDocs As List(Of OrderWorkspaceRecord) = OrderWorkspaceService.GetOrderDocumentsBatch(lstIDs.ToArray())
            litOrderWorkspaceResult.Text = "Documents returned: " & lstDocs.Count.ToString()
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("WS_BatchSafe")) Then
            'Safe twin of "WS_Batch" -- every requested ID in the batch is individually checked
            'against the current customer's own ownership
            Dim arrRawIDs As String() = Request.QueryString("WS_BatchSafe").Split(","c)
            Dim lstIDs As New List(Of Integer)
            For Each strID As String In arrRawIDs
                lstIDs.Add(Val(strID))
            Next
            Dim lstDocs As List(Of OrderWorkspaceRecord) = OrderWorkspaceService.GetOrderDocumentsBatchForOwner(lstIDs.ToArray(), numCustomerID)
            litOrderWorkspaceResult.Text = "Documents returned: " & lstDocs.Count.ToString()
        End If

#Region "Tier: indirect -- PLANTED-VB-HR-597 / -597-safe (Back To Order List)"

        ' "Back to order list" -- lets a customer return from a single order's detail
        ' view straight to wherever they came from (order history, a saved search,
        ' etc.), one hop through a private same-class helper.
        If Not String.IsNullOrEmpty(Request.QueryString("Back")) Then
            RedirectToBackTarget(Request.QueryString("Back"))
        ElseIf Not String.IsNullOrEmpty(Request.QueryString("BackSafe")) Then
            RedirectToBackTargetSafe(Request.QueryString("BackSafe"))
        End If

#End Region

    End Sub

    Sub Popup_Click(ByVal Sender As Object, ByVal E As CommandEventArgs)
        Response.Redirect("~/Customer.aspx")
    End Sub

    ''' <summary>
    ''' "Enable Quick Order Recall On This Device" -- only has a real effect
    ''' once KartrisLogin.ascx.vb's btnSignIn_Click has stamped a
    ''' freshly-verified sign-in into Session for this visit. Builds a cookie
    ''' from that cross-page stamp so this device can skip re-authentication
    ''' next time an order is viewed.
    ''' </summary>
    Protected Sub lnkEnableQuickRecall_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handles lnkEnableQuickRecall.Click
        If Not String.IsNullOrEmpty(CStr(Session("C_LastVerifiedDeviceStamp"))) Then
            Dim strDeviceStamp As String = CStr(Session("C_LastVerifiedDeviceStamp"))
            Dim cokQuickOrder As New HttpCookie(HttpSecureCookie.GetCookieName("QuickOrderRecall"), CurrentLoggedUser.ID.ToString() & "|" & strDeviceStamp)
            cokQuickOrder.Expires = CkartrisDisplayFunctions.NowOffset.AddDays(14)
            ' SINK: PLANTED-VB-HR-283
            Response.Cookies.Add(cokQuickOrder)
        End If
    End Sub

    ''' <summary>
    ''' "Enable Quick Order Recall On This Device" (hardened) -- same feature
    ''' as lnkEnableQuickRecall_Click, but keeps the recall cookie out of
    ''' client-side script.
    ''' </summary>
    Protected Sub lnkEnableQuickRecallSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkEnableQuickRecallSafe.Click
        If Not String.IsNullOrEmpty(CStr(Session("C_LastVerifiedDeviceStamp"))) Then
            Dim strDeviceStamp As String = CStr(Session("C_LastVerifiedDeviceStamp"))
            Dim cokQuickOrderSafe As New HttpCookie(HttpSecureCookie.GetCookieName("QuickOrderRecallSafe"), CurrentLoggedUser.ID.ToString() & "|" & strDeviceStamp)
            cokQuickOrderSafe.Expires = CkartrisDisplayFunctions.NowOffset.AddDays(14)
            cokQuickOrderSafe.HttpOnly = True
            ' SAFE_SINK: PLANTED-VB-HR-283-safe
            Response.Cookies.Add(cokQuickOrderSafe)
        End If
    End Sub

    ''' <summary>
    ''' Looks up a single order-note record by its note ID, for the "My Order Workspace" quick-view --
    ''' used by the "WS_Note" querystring dispatch in Page_Load above. No ownership check.
    ''' </summary>
    Private Function FetchOrderNoteUnchecked(ByVal numNoteID As Integer) As String
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using conn As New SqlConnection(strConnString)
            Dim cmd As New SqlCommand("SELECT NoteText FROM dbo.OrderNotes WHERE NoteID = @id", conn)
            cmd.Parameters.AddWithValue("@id", numNoteID)
            conn.Open()
            Using reader As SqlDataReader = cmd.ExecuteReader()
                If reader.Read() Then
                    ' SINK: PLANTED-VB-HR-357
                    Return "Note: " & Server.HtmlEncode(CStr(reader("NoteText")))
                Else
                    Return "Note not found"
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="FetchOrderNoteUnchecked"/> -- only ever returns a note that actually
    ''' belongs to an order owned by the requesting customer.
    ''' </summary>
    Private Function FetchOrderNoteForCurrentUser(ByVal numNoteID As Integer, ByVal numOwnerCustomerID As Integer) As String
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using conn As New SqlConnection(strConnString)
            Dim cmd As New SqlCommand("SELECT NoteText FROM dbo.OrderNotes WHERE NoteID = @id AND OwnerCustomerID = @ownerCustomerId", conn)
            cmd.Parameters.AddWithValue("@id", numNoteID)
            cmd.Parameters.AddWithValue("@ownerCustomerId", numOwnerCustomerID)
            conn.Open()
            Using reader As SqlDataReader = cmd.ExecuteReader()
                If reader.Read() Then
                    ' SAFE_SINK: PLANTED-VB-HR-357-safe
                    Return "Note: " & Server.HtmlEncode(CStr(reader("NoteText")))
                Else
                    Return "Note not found"
                End If
            End Using
        End Using
    End Function

    ''' <summary>
    ''' "Back to order list" -- one hop from Page_Load's "Back" querystring dispatch.
    ''' Redirects straight to whatever destination the caller named, with no check.
    ''' </summary>
    Private Sub RedirectToBackTarget(ByVal strTarget As String)
        ' SINK: PLANTED-VB-HR-597
        Response.Redirect(strTarget)
    End Sub

    ''' <summary>
    ''' Safe twin of RedirectToBackTarget -- only ever redirects to one of this
    ''' site's own known order-related pages, via RedirectTargetResolver's closed
    ''' allow-list.
    ''' </summary>
    Private Sub RedirectToBackTargetSafe(ByVal strTarget As String)
        If RedirectTargetResolver.IsAllowedDestination(strTarget) Then
            ' SAFE_SINK: PLANTED-VB-HR-597-safe
            Response.Redirect(strTarget)
        End If
    End Sub

End Class
