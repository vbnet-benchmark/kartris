<%@ Page Language="VB" AutoEventWireup="false" CodeFile="_RecentlyViewedReport.aspx.vb" Inherits="Admin_RecentlyViewedReport"
    MasterPageFile="~/Skins/Admin/Template.master" %>

<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <h1>Recently Viewed Products Report</h1>
    <asp:UpdatePanel ID="updMain" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <div id="page_recentlyviewedreport">
                <p>Ad-hoc reporting against the new MongoDB "recently viewed" analytics side-store
                    (RecentlyViewedBLL), complementing the existing relational statistics report.</p>

                <div class="Kartris-DetailsView">
                    <h2>Trending Products Search</h2>
                    <p>Free-text search of view-events by product name.</p>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblNameSearch" runat="server" Text="Product name filter" AssociatedControlID="txtNameSearch" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtNameSearch" runat="server"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnNameSearch" runat="server" CausesValidation="false" OnClick="btnNameSearch_Click"
                            CssClass="button" Text="Search" />
                        <asp:LinkButton ID="btnNameSearchSafe" runat="server" CausesValidation="false" OnClick="btnNameSearchSafe_Click"
                            CssClass="button" Text="Search (Hardened)" />
                    </div>
                    <asp:Label ID="lblNameSearchResult" runat="server" CssClass="valsummary" />
                </div>

                <div class="Kartris-DetailsView">
                    <h2>Shopper History Lookup</h2>
                    <p>Escaping behaviour here is governed entirely by the
                        "general.analytics.securerecentlyviewedlookup" config setting -- this action does
                        not branch on it directly.</p>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblShopperLookup" runat="server" Text="Shopper key" AssociatedControlID="txtShopperLookup" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtShopperLookup" runat="server"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnShopperLookup" runat="server" CausesValidation="false" OnClick="btnShopperLookup_Click"
                            CssClass="button" Text="Look Up" />
                    </div>
                    <asp:Label ID="lblShopperLookupResult" runat="server" CssClass="valsummary" />
                </div>

                <div class="Kartris-DetailsView">
                    <h2>Compare Products</h2>
                    <p>Enter one product name per line to compare their view counts in a single pass.</p>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblCompareNames" runat="server" Text="Product names (one per line)" AssociatedControlID="txtCompareNames" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtCompareNames" runat="server" TextMode="MultiLine" Rows="6" Columns="40"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnCompareNames" runat="server" CausesValidation="false" OnClick="btnCompareNames_Click"
                            CssClass="button" Text="Compare Products" />
                        <asp:LinkButton ID="btnCompareNamesSafe" runat="server" CausesValidation="false" OnClick="btnCompareNamesSafe_Click"
                            CssClass="button" Text="Compare Products (Hardened)" />
                    </div>
                    <asp:Label ID="lblCompareResult" runat="server" CssClass="valsummary" />
                </div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
