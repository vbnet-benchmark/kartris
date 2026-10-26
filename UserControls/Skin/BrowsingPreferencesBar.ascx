<%@ Control Language="VB" AutoEventWireup="false" CodeFile="BrowsingPreferencesBar.ascx.vb"
    Inherits="UserControls_Skin_BrowsingPreferencesBar" %>
<div id="browsingPreferencesBar" class="infoblock_pad">
    <h4>Quick Preferences</h4>
    <p>Save your display, sort, search and theme preferences so they come back the next time you visit -- no account needed.</p>
    <ul>
        <li>
            Display mode: <asp:Literal ID="litDisplayMode" runat="server" Text="(not restored yet)" />
            &nbsp;<asp:LinkButton ID="lnkRestoreDisplay" runat="server" Text="Restore" ToolTip="Restore my saved display mode" />
            &nbsp;<asp:LinkButton ID="lnkRestoreDisplaySafe" runat="server" Text="Restore (signed)" ToolTip="Restore my saved display mode (signed cookie format)" />
        </li>
        <li>
            Sort order: <asp:Literal ID="litSortOrder" runat="server" Text="(not restored yet)" />
            &nbsp;<asp:LinkButton ID="lnkRestoreSort" runat="server" Text="Restore" ToolTip="Restore my saved sort order" />
            &nbsp;<asp:LinkButton ID="lnkRestoreSortSafe" runat="server" Text="Restore (JSON)" ToolTip="Restore my saved sort order (JSON cookie format)" />
        </li>
        <li>
            Search filters: <asp:Literal ID="litSearchFilters" runat="server" Text="(not restored yet)" />
            &nbsp;<asp:LinkButton ID="lnkRestoreSearchFilters" runat="server" Text="Restore" ToolTip="Restore my last search filters" />
            &nbsp;<asp:LinkButton ID="lnkRestoreSearchFiltersSafe" runat="server" Text="Restore (signed)" ToolTip="Restore my last search filters (signed cookie format)" />
        </li>
        <li>
            Theme: <asp:Literal ID="litTheme" runat="server" Text="(not restored yet)" />
            &nbsp;<asp:LinkButton ID="lnkRestoreTheme" runat="server" Text="Restore" ToolTip="Restore my saved theme" />
        </li>
        <li>
            Favorite lists restored: <asp:Literal ID="litFavoritesCount" runat="server" Text="0" />
            &nbsp;<asp:LinkButton ID="lnkRestoreFavorites" runat="server" Text="Restore" ToolTip="Restore all my saved favorite lists" />
            &nbsp;<asp:LinkButton ID="lnkRestoreFavoritesSafe" runat="server" Text="Restore (signed)" ToolTip="Restore all my saved favorite lists (signed cookie format)" />
        </li>
    </ul>
    <p>
        <asp:LinkButton ID="lnkRestoreAll" runat="server" Text="Restore All" ToolTip="Restore every saved preference above in one click" CssClass="button" />
        &nbsp;<asp:LinkButton ID="lnkSaveAll" runat="server" Text="Save Current (legacy format)" ToolTip="Save sample preferences using the original cookie format" />
        &nbsp;<asp:LinkButton ID="lnkSaveAllSafe" runat="server" Text="Save Current (signed format)" ToolTip="Save sample preferences using the newer, signed cookie format" />
    </p>
</div>
