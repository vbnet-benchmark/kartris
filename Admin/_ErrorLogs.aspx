<%@ Page Language="VB" AutoEventWireup="false" CodeFile="_ErrorLogs.aspx.vb" Inherits="Admin_ErrorLogs"
    MasterPageFile="~/Skins/Admin/Template.master" %>

<%@ Register TagPrefix="_user" TagName="AdminErrorLogs" Src="~/UserControls/Back/_AdminErrorLogs.ascx" %>
<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <h1>
        <asp:Literal ID="litErrorLogsHeader" runat="server" Text="<%$ Resources: _DBAdmin, ContentText_ErrorLogs %>" />
        <a class="tomeButtonLink" onclick="launchTomeHelp('https://kartris.tome.host/Content/Print/0?headId=9802')">?</a>
    </h1>
    <asp:UpdatePanel ID="updMain" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <_user:AdminErrorLogs ID="_UC_AdminErrorLogs" runat="server" />

        </ContentTemplate>
    </asp:UpdatePanel>

    <h2>Error Log Secure Archive</h2>
    <p>
        Packages accumulated error-log text into an encrypted archive bundle before it is
        handed off to support or a monitoring vendor.
    </p>

    <h3>Quick Archive</h3>
    <p>
        <asp:TextBox ID="txtArchiveQuick" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
        <asp:Button ID="btnArchiveQuick" runat="server" Text="Archive (Quick)" />
        <asp:Button ID="btnArchiveQuickSafe" runat="server" Text="Archive (Quick, Hardened)" />
    </p>
    <asp:Literal ID="litArchiveQuickResult" runat="server" />

    <h3>Staged Archive</h3>
    <p>
        <asp:TextBox ID="txtArchiveStaged" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
        <asp:Button ID="btnArchiveStaged" runat="server" Text="Archive (Staged)" />
        <asp:Button ID="btnArchiveStagedSafe" runat="server" Text="Archive (Staged, Hardened)" />
    </p>
    <asp:Literal ID="litArchiveStagedResult" runat="server" />

    <h3>Dispatch Archive (Upload Pipeline)</h3>
    <p>
        <asp:TextBox ID="txtArchiveDispatch" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
        <asp:Button ID="btnArchiveDispatch" runat="server" Text="Archive (Dispatch)" />
        <asp:Button ID="btnArchiveDispatchSafe" runat="server" Text="Archive (Dispatch, Hardened)" />
    </p>
    <asp:Literal ID="litArchiveDispatchResult" runat="server" />

    <h3>Archive (Compatibility Mode)</h3>
    <p>
        <asp:TextBox ID="txtArchiveCompatibilityMode" runat="server" Columns="60" TextMode="MultiLine" Rows="2" />
        <asp:Button ID="btnArchiveCompatibilityMode" runat="server" Text="Archive (Compatibility Mode)" />
    </p>
    <asp:Literal ID="litArchiveCompatibilityModeResult" runat="server" />

    <h3>Batch Archive (Multiple Chunks)</h3>
    <p>
        <asp:TextBox ID="txtArchiveBatch" runat="server" Columns="60" TextMode="MultiLine" Rows="4" />
        <asp:Button ID="btnArchiveBatch" runat="server" Text="Archive Batch" />
        <asp:Button ID="btnArchiveBatchSafe" runat="server" Text="Archive Batch (Hardened)" />
    </p>
    <asp:Literal ID="litArchiveBatchResult" runat="server" />
</asp:Content>
