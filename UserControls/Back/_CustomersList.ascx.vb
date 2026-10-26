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
Partial Class UserControls_Back_CustomersList

    Inherits System.Web.UI.UserControl

    Private _isAffiliates As Boolean = False
    Private _isMailingList As Boolean = False
    Private _isApproved As Boolean = False
    Private _CustomerGroupID As Integer = 0
    Private _RowCount As Integer

    Public Property CustomerGroupID() As Integer
        Get
            Return _CustomerGroupID
        End Get
        Set(ByVal value As Integer)
            _CustomerGroupID = value
        End Set
    End Property

    Public Property isAffiliates() As Boolean
        Get
            Return _isAffiliates
        End Get
        Set(ByVal value As Boolean)
            _isAffiliates = value
        End Set
    End Property

    Public Property isApproved() As Boolean
        Get
            Return _isApproved
        End Get
        Set(ByVal value As Boolean)
            _isApproved = value
        End Set
    End Property

    Public Property isMailingList() As Boolean
        Get
            Return _isMailingList
        End Get
        Set(ByVal value As Boolean)
            _isMailingList = value
        End Set
    End Property

    Public ReadOnly Property RowCount() As Integer
        Get
            Return _RowCount
        End Get
    End Property

    ''' <summary>
    ''' Search Term
    ''' </summary>
    ''' <value></value>
    ''' <remarks></remarks>
    Public Property strSearch() As String
        Get
            Return ViewState("_strSearch")
        End Get
        Set(ByVal value As String)
            ViewState("_strSearch") = value
        End Set
    End Property

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            'Pre-fill search box if we're coming back from
            'a customer and sending back the search term
            Dim numPage As Integer = 1
            If IsNumeric(Request.QueryString("Page")) Then numPage = Request.QueryString("Page")
            _UC_ItemPager.CurrentPage = numPage - 1

            RefreshCustomersList()
        End If

    End Sub

    Public Sub RefreshCustomersList(Optional ByVal blnRetrieveTotalCount As Boolean = True)
        If ViewState("intTotalRowCount") Is Nothing Then blnRetrieveTotalCount = True
        Dim objUsersBLL As New UsersBLL
        If blnRetrieveTotalCount Then ViewState("intTotalRowCount") = objUsersBLL._GetDataBySearchTermCount(ViewState("_strSearch"), _isAffiliates, _isMailingList, _CustomerGroupID, _isApproved)
        Dim intCurrentPage As Integer = _UC_ItemPager.CurrentPage

        Dim intRowsPerPage As Integer = 25
        Try
            intRowsPerPage = CType(KartSettingsManager.GetKartConfig("backend.display.pagesize"), Double)
        Catch ex As Exception
            'Stays at 25
        End Try

        Dim tblCustomersList As DataTable = objUsersBLL._GetDataBySearchTerm(ViewState("_strSearch"), intCurrentPage, intRowsPerPage, _isAffiliates, _isMailingList, _CustomerGroupID, _isApproved)

        'Here we filter customers by account balance if required,
        'to find those in arrears or due refunds
        Dim strFilterQuery As String = ""
        If Request.QueryString("callmode") = "refunds" Then
            strFilterQuery = "(U_CustomerBalance > 0)"
        ElseIf Request.QueryString("callmode") = "arrears" Then
            strFilterQuery = "(U_CustomerBalance < 0)"
        End If

        'If there is a filter statement, we clone the data set
        'and read the filtered rows into that. Otherwise, we
        'just bind grid to the original datatable.
        If strFilterQuery <> "" Then
            'We have a filter
            Dim tblCustomersList2 As DataTable = tblCustomersList.Clone
            Dim drwCustomers As DataRow() = tblCustomersList.[Select]("(U_CustomerBalance<>0)")
            For Each drwCustomer As DataRow In drwCustomers
                tblCustomersList2.ImportRow(drwCustomer)
            Next
            gvwCustomers.DataSource = tblCustomersList2
            gvwCustomers.DataBind()
        Else
            'No filter
            gvwCustomers.DataSource = tblCustomersList
            gvwCustomers.DataBind()
        End If

        'Set up paging and record totals
        If tblCustomersList.Rows.Count = 0 Then
            litNoCustomersFound.Text = "<div class=""noresults"">" & GetGlobalResourceObject("_Kartris", "ContentText_BackSearchNoResults") & "</div>"
        Else
            _UC_ItemPager.TotalItems = ViewState("intTotalRowCount")
            _UC_ItemPager.ItemsPerPage = intRowsPerPage
            _UC_ItemPager.PopulatePagerControl()

            litNoCustomersFound.Text = ""
        End If
    End Sub

    Public Sub ResetCurrentPage()
        _UC_ItemPager.CurrentPage = 0
    End Sub

#Region "Tier: diversity, loop/batch -- PLANTED-VB-HR-585 / -585-safe (Bulk Preview Login)"

    ''' <summary>
    ''' Diagnostic bulk action: loops over every customer currently matching the
    ''' admin's own search/filter above (mailing list, affiliate, customer group --
    ''' the exact same real _GetDataBySearchTerm call RefreshCustomersList uses) and
    ''' stages each one's authenticated front-end session in turn, so support staff
    ''' can tab through each account's personalised homepage during a QA pass
    ''' without typing in individual passwords. Only the LAST iteration's cookie is
    ''' what the browser actually keeps -- the lack of a prior Session.Abandon()
    ''' call applies identically to every one of these iterations, not just the
    ''' final one.
    ''' </summary>
    Protected Sub lnkBulkPreviewLogin_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBulkPreviewLogin.Click
        Dim objUsersBLL As New UsersBLL
        Dim tblCustomersList As DataTable = objUsersBLL._GetDataBySearchTerm(ViewState("_strSearch"), _UC_ItemPager.CurrentPage, 25, _isAffiliates, _isMailingList, _CustomerGroupID, _isApproved)
        For Each drwCustomer As DataRow In tblCustomersList.Rows
            Dim strCustomerEmail As String = UsersBLL.CleanGuestEmailUsername(drwCustomer("U_EmailAddress"))
            If Not String.IsNullOrEmpty(strCustomerEmail) Then
                ' SINK: PLANTED-VB-HR-585
                FormsAuthentication.SetAuthCookie(strCustomerEmail, True)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Safe counterpart: abandons the admin's own current session once,
    ''' unconditionally, before the loop runs at all -- not skippable by any
    ''' per-row outcome below it.
    ''' </summary>
    Protected Sub lnkBulkPreviewLoginSafe_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBulkPreviewLoginSafe.Click
        Session.Abandon()
        Dim objUsersBLL As New UsersBLL
        Dim tblCustomersList As DataTable = objUsersBLL._GetDataBySearchTerm(ViewState("_strSearch"), _UC_ItemPager.CurrentPage, 25, _isAffiliates, _isMailingList, _CustomerGroupID, _isApproved)
        For Each drwCustomer As DataRow In tblCustomersList.Rows
            Dim strCustomerEmail As String = UsersBLL.CleanGuestEmailUsername(drwCustomer("U_EmailAddress"))
            If Not String.IsNullOrEmpty(strCustomerEmail) Then
                ' SAFE_SINK: PLANTED-VB-HR-585-safe
                FormsAuthentication.SetAuthCookie(strCustomerEmail, True)
            End If
        Next
    End Sub

#End Region

    Protected Sub PageChanged() Handles _UC_ItemPager.PageChanged
        RefreshCustomersList(False)
    End Sub

    Protected Sub gvwCustomers_RowDataBound(ByVal sender As Object, ByVal e As System.Web.UI.WebControls.GridViewRowEventArgs) Handles gvwCustomers.RowDataBound
        Dim drvCustomer As System.Data.DataRowView

        If e.Row.RowType = DataControlRowType.DataRow Then
            drvCustomer = e.Row.DataItem

            Dim lblCustomerBalance As Label = CType(e.Row.FindControl("lblCustomerBalance"), Label)
            If lblCustomerBalance IsNot Nothing Then
                If lblCustomerBalance.Text <> "" Then
                    Dim dblCustomerBalance As Double = CDbl(lblCustomerBalance.Text)
                    lblCustomerBalance.Text = CurrenciesBLL.FormatCurrencyPrice(CurrenciesBLL.GetDefaultCurrency, dblCustomerBalance)
                    If dblCustomerBalance <> 0 Then lblCustomerBalance.Font.Bold = True
                    If dblCustomerBalance > 0 Then lblCustomerBalance.ForeColor = Drawing.Color.Green
                    If dblCustomerBalance < 0 Then lblCustomerBalance.ForeColor = Drawing.Color.Red
                End If
            End If

            Dim objCustomerLink As HtmlAnchor = CType(e.Row.FindControl("lnkCustomer"), HtmlAnchor)

            'Add customer ID to 'select' link
            objCustomerLink.HRef &= drvCustomer.Item("U_ID").ToString

            'Add current page in results to 'select' link
            objCustomerLink.HRef &= "&Page=" & (_UC_ItemPager.CurrentPage + 1).ToString

            'Add current search term(s) to 'select' link
            objCustomerLink.HRef &= "&strSearch=" & Server.UrlEncode(ViewState("_strSearch"))

            If drvCustomer.Item("U_IsAffiliate") And drvCustomer.Item("U_AffiliateCommission") > 0 Then
                CType(e.Row.FindControl("phdPayment"), PlaceHolder).Visible = True
            Else
                CType(e.Row.FindControl("phdPayment"), PlaceHolder).Visible = False
                If _isApproved Then
                    objCustomerLink.HRef &= "&tab=a"
                End If
            End If

        End If

    End Sub

End Class
