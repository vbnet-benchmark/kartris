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
Imports MongoDB.Bson

''' <summary>
''' Customer-facing "Recently Viewed Products" page, backed by the new MongoDB RecentlyViewedBLL
''' side-store. Linked from the My Account menu (web_menu.sitemap). Shows the shopper's own recent
''' views by default, and supports narrowing that list by product-name text via the new, dormant-
''' by-default ?filter=/?filtersafe= querystring keys (confirmed new via grep).
''' </summary>
Partial Class RecentlyViewed
    Inherits PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            Try
                Dim strShopperKey As String = If(User.Identity.IsAuthenticated, "cust-" & CurrentLoggedUser.ID.ToString(), "sess-" & Session.SessionID)

                Dim resultDocs As List(Of BsonDocument)
                Dim strFilter As String = Request.QueryString("filter")
                Dim strFilterSafe As String = Request.QueryString("filtersafe")

                If Not String.IsNullOrEmpty(strFilter) Then
                    resultDocs = RecentlyViewedBLL.SearchRecentlyViewedByProductName(strShopperKey, strFilter)
                ElseIf Not String.IsNullOrEmpty(strFilterSafe) Then
                    resultDocs = RecentlyViewedBLL.SearchRecentlyViewedByProductNameSafe(strShopperKey, strFilterSafe)
                Else
                    resultDocs = RecentlyViewedBLL.GetRecentlyViewedForShopper(strShopperKey)
                End If

                Dim displayLines As New List(Of String)
                For Each viewDoc As BsonDocument In resultDocs
                    Dim strProductName As String = If(viewDoc.Contains("ProductName"), viewDoc("ProductName").ToString(), "")
                    Dim strViewedUtc As String = If(viewDoc.Contains("ViewedUtc"), viewDoc("ViewedUtc").ToUniversalTime().ToString("u"), "")
                    displayLines.Add(strProductName & " (" & strViewedUtc & ")")
                Next

                If displayLines.Count = 0 Then
                    lblNoResults.Text = "No recently-viewed products found."
                    lblNoResults.Visible = True
                Else
                    rptRecentlyViewed.DataSource = displayLines
                    rptRecentlyViewed.DataBind()
                End If
            Catch ex As Exception
                lblNoResults.Text = "Your recently-viewed history is temporarily unavailable."
                lblNoResults.Visible = True
            End Try
        End If
    End Sub

End Class
