<%@ Page Language="VB" AutoEventWireup="false" CodeFile="_StaffDirectoryLookup.aspx.vb" Inherits="Admin_StaffDirectoryLookup"
    MasterPageFile="~/Skins/Admin/Template.master" %>

<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <h1>Corporate Staff Directory Lookup</h1>
    <asp:UpdatePanel ID="updMain" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <div id="page_staffdirectorylookup">
                <p>Back-office tool for verifying back-office/wholesale-contact identities against the
                    store's corporate Active Directory. Not exposed to customers.</p>

                <div class="Kartris-DetailsView">
                    <h2>Quick Search</h2>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblQuickSearch" runat="server" Text="Search term" AssociatedControlID="txtQuickSearch" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtQuickSearch" runat="server"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnQuickSearch" runat="server" CausesValidation="false" OnClick="btnQuickSearch_Click"
                            CssClass="button" Text="Search" />
                        <asp:LinkButton ID="btnQuickSearchSafe" runat="server" CausesValidation="false" OnClick="btnQuickSearchSafe_Click"
                            CssClass="button" Text="Search (Hardened)" />
                    </div>
                    <asp:Label ID="lblQuickSearchResult" runat="server" CssClass="valsummary" />
                </div>

                <div class="Kartris-DetailsView">
                    <h2>Policy-Controlled Search</h2>
                    <p>Escaping behaviour here is governed entirely by the
                        "general.security.strictldapescaping" config setting -- this action does not branch
                        on it directly.</p>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblPolicySearch" runat="server" Text="Search term" AssociatedControlID="txtPolicySearch" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtPolicySearch" runat="server"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnPolicySearch" runat="server" CausesValidation="false" OnClick="btnPolicySearch_Click"
                            CssClass="button" Text="Search" />
                    </div>
                    <asp:Label ID="lblPolicySearchResult" runat="server" CssClass="valsummary" />
                </div>

                <div class="Kartris-DetailsView">
                    <h2>Batch Verify</h2>
                    <p>Enter one corporate username per line to verify several accounts in a single
                        directory query.</p>
                    <div class="Kartris-DetailsView-Data">
                        <ul>
                            <li><span class="Kartris-DetailsView-Name">
                                <asp:Label ID="lblBatchUsernames" runat="server" Text="Usernames (one per line)" AssociatedControlID="txtBatchUsernames" /></span>
                                <span class="Kartris-DetailsView-Value">
                                    <asp:TextBox ID="txtBatchUsernames" runat="server" TextMode="MultiLine" Rows="6" Columns="40"></asp:TextBox>
                                </span></li>
                        </ul>
                    </div>
                    <div class="submitbuttons">
                        <asp:LinkButton ID="btnBatchVerify" runat="server" CausesValidation="false" OnClick="btnBatchVerify_Click"
                            CssClass="button" Text="Verify Batch" />
                        <asp:LinkButton ID="btnBatchVerifySafe" runat="server" CausesValidation="false" OnClick="btnBatchVerifySafe_Click"
                            CssClass="button" Text="Verify Batch (Hardened)" />
                    </div>
                    <asp:Label ID="lblBatchVerifyResult" runat="server" CssClass="valsummary" />
                </div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
