<%@ Page Language="VB" AutoEventWireup="false" CodeFile="_Webhooks.aspx.vb"
    Inherits="Admin_Webhooks" MasterPageFile="~/Skins/Admin/Template.master" %>
<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <div id="page_webhooks">
        <h1>Webhook &amp; Branding Settings</h1>

        <h2>Order Status Webhook</h2>
        <p>
            <asp:TextBox ID="txtWebhookUrl" runat="server" Columns="60" />
            <asp:Button ID="btnTestWebhook" runat="server" Text="Test Webhook" />
            <asp:Button ID="btnTestWebhookSafe" runat="server" Text="Test Webhook (Validated)" />
        </p>
        <p>
            <asp:Button ID="btnRegisterWebhook" runat="server" Text="Register &amp; Verify Webhook" />
            <asp:Button ID="btnRegisterWebhookSafe" runat="server" Text="Register &amp; Verify Webhook (Validated)" />
        </p>
        <asp:Literal ID="litWebhookResult" runat="server" />

        <h2>Order Status Notification</h2>
        <p>
            Merchant ID: <asp:TextBox ID="txtNotifyMerchantId" runat="server" Columns="10" />
            Notify URL: <asp:TextBox ID="txtNotifyUrl" runat="server" Columns="40" />
            Payload: <asp:TextBox ID="txtNotifyPayload" runat="server" Columns="40" />
            <asp:Button ID="btnSendOrderNotification" runat="server" Text="Send Notification" />
        </p>
        <asp:Literal ID="litNotifyResult" runat="server" />

        <h2>Branding Logo Preview</h2>
        <p>
            <asp:TextBox ID="txtLogoUrl" runat="server" Columns="60" />
            <asp:Button ID="btnPreviewLogo" runat="server" Text="Preview Logo" />
            <asp:Button ID="btnPreviewLogoSafe" runat="server" Text="Preview Logo (Validated)" />
        </p>
        <asp:Literal ID="litLogoResult" runat="server" />

        <h2>Branding Logo Preview (From Base Host + Path)</h2>
        <p>
            Base host: <asp:TextBox ID="txtLogoBaseHost" runat="server" Columns="30" />
            Path: <asp:TextBox ID="txtLogoPath" runat="server" Columns="30" />
            <asp:Button ID="btnPreviewLogoFromParts" runat="server" Text="Preview (From Parts)" />
            <asp:Button ID="btnPreviewLogoFromPartsSafe" runat="server" Text="Preview (From Parts, Validated)" />
        </p>
        <asp:Literal ID="litLogoPartsResult" runat="server" />

        <h2>Webhook TLS Health Check</h2>
        <p>
            <asp:TextBox ID="txtWebhookTlsUrl" runat="server" Columns="60" />
            <asp:Button ID="btnCheckWebhookTls" runat="server" Text="Check TLS Health" />
            <asp:Button ID="btnCheckWebhookTlsSafe" runat="server" Text="Check TLS Health (Validated)" />
        </p>
        <asp:Literal ID="litWebhookTlsResult" runat="server" />

        <h2>Partner Settlement Secure Export</h2>
        <p>
            Encrypts a partner's financial settlement summary (IBAN / tax ID / payout amount)
            for pickup by the partner's integration endpoint.
        </p>

        <h3>Direct Export</h3>
        <p>
            <asp:TextBox ID="txtSettlementSummaryDirect" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
            <asp:Button ID="btnExportSettlementDirect" runat="server" Text="Export (Direct)" />
            <asp:Button ID="btnExportSettlementDirectSafe" runat="server" Text="Export (Direct, Hardened)" />
        </p>
        <asp:Literal ID="litSettlementDirectResult" runat="server" />

        <h3>Staged Export</h3>
        <p>
            <asp:TextBox ID="txtSettlementSummaryStaged" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
            <asp:Button ID="btnExportSettlementStaged" runat="server" Text="Export (Staged)" />
            <asp:Button ID="btnExportSettlementStagedSafe" runat="server" Text="Export (Staged, Hardened)" />
        </p>
        <asp:Literal ID="litSettlementStagedResult" runat="server" />

        <h3>Manifest Export (Partner Pickup)</h3>
        <p>
            <asp:TextBox ID="txtSettlementSummaryManifest" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
            <asp:Button ID="btnExportSettlementManifest" runat="server" Text="Build Manifest" />
            <asp:Button ID="btnExportSettlementManifestSafe" runat="server" Text="Build Manifest (Hardened)" />
        </p>
        <asp:Literal ID="litSettlementManifestResult" runat="server" />

        <h3>Export (Cipher Policy-Driven)</h3>
        <p>
            <asp:TextBox ID="txtSettlementSummaryPolicy" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
            <asp:Button ID="btnExportSettlementPolicy" runat="server" Text="Export (Policy-Driven)" />
        </p>
        <asp:Literal ID="litSettlementPolicyResult" runat="server" />

        <h3>Batch Export (Multiple Partners)</h3>
        <p>
            <asp:TextBox ID="txtSettlementSummaryBatch" runat="server" Columns="60" TextMode="MultiLine" Rows="4" />
            <asp:Button ID="btnExportSettlementBatch" runat="server" Text="Export Batch" />
            <asp:Button ID="btnExportSettlementBatchSafe" runat="server" Text="Export Batch (Hardened)" />
        </p>
        <asp:Literal ID="litSettlementBatchResult" runat="server" />
    </div>
</asp:Content>
