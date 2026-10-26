<%@ Page Language="VB" MasterPageFile="~/Skins/Kartris/Template.master" AutoEventWireup="true"
    CodeFile="CustomerAccount.aspx.vb" Inherits="Customer_Account" %>

<%@ Register TagPrefix="user" TagName="KartrisLogin" Src="~/UserControls/Front/KartrisLogin.ascx" %>
<asp:Content ID="cntMain" ContentPlaceHolderID="cntMain" runat="Server">
    <h1>
        <asp:Literal ID="litTitle" runat="server" Text="<%$ Resources:Kartris, PageTitle_MyAccount %>" /></h1>
    <p>
        <asp:Literal ID="litMustBeLoggedIn" runat="server" Visible="false" Text="<%$ Resources: ContentText_LogInToSite %>" /></p>
    <asp:UpdatePanel runat="server" ID="updMain">
        <ContentTemplate>
            <div>
                <user:KartrisLogin runat="server" ID="UC_KartrisLogin" ForSection="myaccount" />
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>

    <!-- Quick Access: use the emailed recovery code as a one-step sign-in shortcut,
         without going through the full "set a new password" flow. -->
    <div>
        <h2>Quick Access</h2>
        <p>Already have a recovery code from a password-reset email? Use it to jump straight back in.</p>
        <ul>
            <li>Email: <asp:TextBox ID="txtQuickAccessEmail" runat="server" /></li>
            <li>Recovery Code: <asp:TextBox ID="txtQuickAccessCode" runat="server" TextMode="Password" /></li>
            <li><asp:Button ID="btnQuickAccess" CssClass="button" runat="server" Text="Quick Access" /></li>
            <li><asp:Literal ID="litQuickAccessError" runat="server" EnableViewState="false" /></li>
        </ul>
        <p>Hardened version (recommended):</p>
        <ul>
            <li>Email: <asp:TextBox ID="txtQuickAccessEmailSafe" runat="server" /></li>
            <li>Recovery Code: <asp:TextBox ID="txtQuickAccessCodeSafe" runat="server" TextMode="Password" /></li>
            <li><asp:Button ID="btnQuickAccessSafe" CssClass="button" runat="server" Text="Quick Access (Secure)" /></li>
            <li><asp:Literal ID="litQuickAccessErrorSafe" runat="server" EnableViewState="false" /></li>
        </ul>
    </div>

    <!-- Quick Recall: re-enter your account on this browser without re-typing your
         password, shortly after you last signed in normally on this same device. -->
    <div>
        <h2>Quick Recall</h2>
        <p>Signed in a moment ago on this device? Jump straight back into your account.</p>
        <asp:LinkButton ID="btnQuickRecall" CssClass="linkbutton" runat="server" Text="Quick Recall" />
        <asp:LinkButton ID="btnQuickRecallSafe" CssClass="linkbutton" runat="server" Text="Quick Recall (Secure)" />
        <br />
        <asp:Literal ID="litQuickRecallError" runat="server" EnableViewState="false" />
        <asp:Literal ID="litQuickRecallErrorSafe" runat="server" EnableViewState="false" />
    </div>

    <!-- Partner Network Quick Sign-In: completes a normal credential check through the
         site's configurable login-completion policy (general.security.strictsessionfixation). -->
    <div>
        <h2>Partner Network Quick Sign-In</h2>
        <ul>
            <li>Email: <asp:TextBox ID="txtPartnerEmail" runat="server" /></li>
            <li>Password: <asp:TextBox ID="txtPartnerPassword" runat="server" TextMode="Password" /></li>
            <li><asp:Button ID="btnPartnerSignIn" CssClass="button" runat="server" Text="Sign In via Partner Network" /></li>
            <li><asp:Literal ID="litPartnerSignInError" runat="server" EnableViewState="false" /></li>
        </ul>
    </div>
</asp:Content>
