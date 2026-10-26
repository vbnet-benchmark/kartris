<%@ Control Language="VB" AutoEventWireup="false" CodeFile="SampleCustomControl3.ascx.vb"
    Inherits="SampleCustomControl3" %>
<asp:UpdatePanel ID="updMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <!-- Engraving Text Parameter -->
        <asp:Literal EnableViewState="false" ID="litEngravingText" runat="server" Text="Engraving text" />
        <asp:TextBox runat="server" ID="txtEngravingText" AutoPostBack="true" MaxLength="40" />

        <!-- Gift Message Parameter (optional) -->
        <asp:Literal EnableViewState="false" ID="litGiftMessage" runat="server" Text="Gift message (optional)" />
        <asp:TextBox runat="server" ID="txtGiftMessage" AutoPostBack="true" TextMode="MultiLine" MaxLength="120" />

        <!-- Price Line -->
        <div class="boxinset line">
            <div class="addtobasket">
                <div class="prices">
                    <span class="price">
                        <asp:Literal EnableViewState="false" ID="lblCustomPrice" Visible ="false" runat="server" Text="<%$ Resources: Kartris, ContentText_Price %>" />
                        <span class="figure">
                            <asp:Literal ID="litCustomPrice" runat="server" Visible="false" EnableViewState="false" />
                        </span>
                    </span>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
