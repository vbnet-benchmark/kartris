<%@ Page Language="VB" MasterPageFile="~/Skins/Kartris/Template.master" AutoEventWireup="false"
    CodeFile="RecentlyViewed.aspx.vb" Inherits="RecentlyViewed" %>

<asp:Content ID="cntMain" ContentPlaceHolderID="cntMain" runat="Server">
    <div id="page_recentlyviewed">
        <h1>
            <asp:Literal ID="litTitle" runat="server" Text="Recently Viewed Products" /></h1>
        <p>
            <asp:Literal ID="litIntro" runat="server" Text="Products you have looked at recently. Use the quick filter box to narrow the list by product name." />
        </p>
        <div class="inputform">
            <input type="text" id="txtQuickFilter" />
            <input type="button" value="Filter" onclick="location.href='RecentlyViewed.aspx?filter=' + encodeURIComponent(document.getElementById('txtQuickFilter').value);" />
        </div>
        <div class="spacer"></div>
        <asp:Repeater ID="rptRecentlyViewed" runat="server">
            <HeaderTemplate>
                <table class="baskettable">
                    <thead>
                        <tr class="headrow">
                            <th><asp:Literal ID="litColProduct" runat="server" Text="Product" /></th>
                            <th><asp:Literal ID="litColViewed" runat="server" Text="Viewed" /></th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Server.HtmlEncode(Container.DataItem.ToString()) %></td>
                    <td></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
        <asp:Label ID="lblNoResults" runat="server" CssClass="valsummary" Visible="false" />
    </div>
</asp:Content>
