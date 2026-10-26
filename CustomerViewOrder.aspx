<%@ Page Language="VB" MasterPageFile="~/Skins/Kartris/Template.master" AutoEventWireup="false"
    CodeFile="CustomerViewOrder.aspx.vb" Inherits="Customer_ViewOrder" %>

<%@ Register TagPrefix="user" TagName="CustomerOrder" Src="~/UserControls/Front/CustomerOrder.ascx" %>
<asp:Content ID="cntMain" ContentPlaceHolderID="cntMain" runat="Server">
    <user:PopupMessage ID="UC_PopUp" runat="server" />
    <asp:PlaceHolder ID="phdOrderStatus" runat="server">
        <div id="customer">
            <h1>
                <asp:Literal ID="litPageTitleOrderStatus" runat="server" Text='<%$ Resources: Kartris, PageTitle_OrderStatus %>' /></h1>
            <user:CustomerOrder ID="UC_CustomerOrder" runat="server" CustomerID="0" OrderID="0" />
            <p>
                <asp:LinkButton ID="lnkEnableQuickRecall" runat="server" CssClass="link2" OnClick="lnkEnableQuickRecall_Click"
                    Text="Enable Quick Order Recall On This Device" />
                <asp:LinkButton ID="lnkEnableQuickRecallSafe" runat="server" CssClass="link2"
                    Text="Enable Quick Order Recall On This Device (Hardened)" /></p>
            <p>
                <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/Customer.aspx" Text='<%$ Resources: Kartris, ContentText_GoBack %>'
                    CssClass="link2 icon_back"></asp:HyperLink></p>
        </div>
    </asp:PlaceHolder>
    <p>
        <asp:Literal ID="litOrderWorkspaceResult" runat="server" />
    </p>
</asp:Content>


