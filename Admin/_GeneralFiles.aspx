<%@ Page Title="" Language="VB" MasterPageFile="~/Skins/Admin/Template.master" AutoEventWireup="false" CodeFile="_GeneralFiles.aspx.vb" Inherits="Admin_GeneralFiles" %>

<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <asp:UpdatePanel ID="updMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
    <div id="page_generalfiles">
    <h1>
        <asp:Literal ID="litPageTitleGeneralFiles" runat="server" Text="<%$ Resources: _Kartris, PageTitle_GeneralFiles %>" /></h1>
    <asp:MultiView ID="mvwGeneralFiles" runat="server" ActiveViewIndex="0">
        <asp:View ID="viwFiles" runat="server">
            <asp:UpdatePanel ID="updFiles" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <asp:GridView CssClass="kartristable" ID="gvwFiles" runat="server" AllowPaging="true"
                        AllowSorting="false" AutoGenerateColumns="False" DataKeyNames="F_Name" AutoGenerateEditButton="False"
                        GridLines="None" SelectedIndex="0" PageSize="15">
                        <Columns>
                            <asp:TemplateField>
                                <ItemStyle CssClass="column1" />
                                <ItemTemplate>
                                    <asp:Literal ID="litNumber" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemStyle CssClass="column2" />
                                <HeaderTemplate>
                                    <asp:Literal runat="server" ID="litContentFilename" Text='<%$ Resources:_Kartris, ContentText_Filename %>'></asp:Literal>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <asp:HyperLink ID="lnkFile" runat="server" Text='<%# Eval("F_Name") %>' NavigateUrl='<%# FormatFilePath(Eval("F_Name")) %>' Target="_blank"></asp:HyperLink>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="F_Type" HeaderText="<%$ Resources:_Kartris, ContentText_FileType %>"
                                ItemStyle-CssClass="column2" />
                            <asp:BoundField DataField="F_Size" HeaderText="<%$ Resources:_Kartris, ContentText_FileSize %>"
                                ItemStyle-CssClass="column2" />
                            <asp:BoundField DataField="F_LastUpdated" HeaderText="<%$ Resources:_Kartris, ContentText_LastUpdated %>"
                                ItemStyle-CssClass="column2" />
                            <asp:TemplateField ItemStyle-CssClass="selectfield">
                                <HeaderTemplate>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkBtnDelete" runat="server" CommandName="DeleteFile" CommandArgument='<%# Eval("F_Name") %>'
                                        Text='<%$ Resources: _Kartris, FormButton_Delete %>' CssClass="linkbutton icon_delete" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                    <asp:HiddenField ID="hidFileNameToDelete" runat="server"></asp:HiddenField>
                    <_user:PopupMessage ID="_UC_PopupMsg" runat="server" />
                    <asp:UpdateProgress ID="prgFiles" runat="server" AssociatedUpdatePanelID="updFiles">
                        <ProgressTemplate>
                            <div class="loadingimage">
                            </div>
                            <div class="updateprogress">
                            </div>
                        </ProgressTemplate>
                    </asp:UpdateProgress>

                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="viwNoFiles" runat="server">
            <asp:Panel ID="pnlNoItems" runat="server" CssClass="noresults">
                <asp:Literal ID="litNoItems" runat="server" Text="<%$ Resources: _Kartris, ContentText_NoItemsFound %>">
                </asp:Literal>
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
    <div id="updatebuttonbar" class="submitbuttons topsubmitbuttons">
        <asp:LinkButton ID="btnAdd" runat="server" CssClass="button addbutton" Text="<%$ Resources:_Kartris, FormButton_Add %>"
            ToolTip="<%$ Resources:_Kartris, FormButton_Add %>" />
    </div>
    <asp:Panel ID="pnlMessage" runat="server" Style="display: none" CssClass="popup">
        <asp:LinkButton ID="lnkExtenderCancel" runat="server" Text="" CssClass="closebutton" />
        <asp:LinkButton ID="lnkBtn" runat="server" CssClass="invisible"></asp:LinkButton>

        <script type="text/javascript">
            function onUpload() {
                var postBack = new Sys.WebForms.PostBackAction();
                postBack.set_target('lnkUpload');
                postBack.set_eventArgument('');
                postBack.performAction();
            }
        </script>

        <asp:LinkButton ID="lnkExtenderOk" runat="server" Text="" CssClass="invisible" />
        <h2>
            <asp:Literal ID="litContentTextUpload" runat="server" Text="<%$ Resources: _Kartris, ContentText_FileUpload %>"></asp:Literal></h2>
        <asp:UpdatePanel ID="updUploader" runat="server">
            <Triggers>
                <asp:PostBackTrigger ControlID="lnkUpload" />
                <asp:PostBackTrigger ControlID="lnkUploadSafe" />
                <asp:PostBackTrigger ControlID="lnkUploadTheme" />
                <asp:PostBackTrigger ControlID="lnkUploadThemeSafe" />
                <asp:PostBackTrigger ControlID="lnkImportTemplate" />
                <asp:PostBackTrigger ControlID="lnkBulkImport" />
                <asp:PostBackTrigger ControlID="lnkBulkImportSafe" />
                <asp:PostBackTrigger ControlID="lnkUploadRMA" />
                <asp:PostBackTrigger ControlID="lnkUploadRMASafe" />
                <asp:PostBackTrigger ControlID="lnkUploadBanner" />
                <asp:PostBackTrigger ControlID="lnkUploadBannerSafe" />
                <asp:PostBackTrigger ControlID="lnkUploadAgreement" />
                <asp:PostBackTrigger ControlID="lnkUploadAgreementSafe" />
                <asp:PostBackTrigger ControlID="lnkUploadCompliance" />
                <asp:PostBackTrigger ControlID="lnkBulkUploadInvoices" />
                <asp:PostBackTrigger ControlID="lnkBulkUploadInvoicesSafe" />
            </Triggers>
            <ContentTemplate>
                <asp:LinkButton ID="lnkUpload" runat="server" OnClick="lnkUpload_Click" Text="<%$ Resources: _Kartris, ContentText_Upload %>"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filUploader" runat="server" />
                <hr />
                <p>Upload (allow-listed extensions only):</p>
                <asp:LinkButton ID="lnkUploadSafe" runat="server" Text="Upload (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filUploaderSafe" runat="server" />
                <hr />
                <p>Upload Custom Skin File:</p>
                <asp:LinkButton ID="lnkUploadTheme" runat="server" Text="Upload Skin File"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filUploaderTheme" runat="server" />
                <asp:LinkButton ID="lnkUploadThemeSafe" runat="server" Text="Upload Skin File (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filUploaderThemeSafe" runat="server" />
                <hr />
                <p>Import Email Template:</p>
                <asp:DropDownList ID="ddlTemplateMode" runat="server">
                    <asp:ListItem Text="Legacy" Value="legacy" />
                    <asp:ListItem Text="Validated" Value="validated" Selected="True" />
                </asp:DropDownList>
                <asp:FileUpload ID="filUploaderTemplate" runat="server" />
                <asp:LinkButton ID="lnkImportTemplate" runat="server" Text="Import Template"
                    CssClass="linkbutton icon_upload" />
                <hr />
                <p>Bulk Import Attachments (select multiple files):</p>
                <input id="filBulkFiles" name="filBulkFiles" type="file" multiple="multiple" runat="server" />
                <asp:LinkButton ID="lnkBulkImport" runat="server" Text="Bulk Import"
                    CssClass="linkbutton icon_upload" />
                <input id="filBulkFilesSafe" name="filBulkFilesSafe" type="file" multiple="multiple" runat="server" />
                <asp:LinkButton ID="lnkBulkImportSafe" runat="server" Text="Bulk Import (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <hr />
                <p>Upload RMA Return Photo:</p>
                <asp:LinkButton ID="lnkUploadRMA" runat="server" OnClick="lnkUploadRMA_Click" Text="Upload RMA Photo"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filRMA" runat="server" />
                <asp:LinkButton ID="lnkUploadRMASafe" runat="server" Text="Upload RMA Photo (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filRMASafe" runat="server" />
                <hr />
                <p>Upload Vendor Banner Overlay:</p>
                <asp:LinkButton ID="lnkUploadBanner" runat="server" OnClick="lnkUploadBanner_Click" Text="Upload Banner Overlay"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filBanner" runat="server" />
                <asp:LinkButton ID="lnkUploadBannerSafe" runat="server" Text="Upload Banner Overlay (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filBannerSafe" runat="server" />
                <hr />
                <p>Upload Merchant Agreement Document:</p>
                <asp:LinkButton ID="lnkUploadAgreement" runat="server" OnClick="lnkUploadAgreement_Click" Text="Upload Agreement"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filAgreement" runat="server" />
                <asp:LinkButton ID="lnkUploadAgreementSafe" runat="server" Text="Upload Agreement (Filtered)"
                    CssClass="linkbutton icon_upload" />
                <asp:FileUpload ID="filAgreementSafe" runat="server" />
                <hr />
                <p>Upload Compliance Certificate:</p>
                <asp:DropDownList ID="ddlComplianceType" runat="server">
                    <asp:ListItem Text="ISO Certificate" Value="iso" Selected="True" />
                    <asp:ListItem Text="Insurance Certificate" Value="insurance" />
                </asp:DropDownList>
                <asp:FileUpload ID="filCompliance" runat="server" />
                <asp:LinkButton ID="lnkUploadCompliance" runat="server" Text="Upload Certificate"
                    CssClass="linkbutton icon_upload" />
                <hr />
                <p>Bulk Upload Supplier Invoice Scans (up to 3 files):</p>
                <input id="invScan1" name="invScan1" type="file" runat="server" />
                <input id="invScan2" name="invScan2" type="file" runat="server" />
                <input id="invScan3" name="invScan3" type="file" runat="server" />
                <asp:LinkButton ID="lnkBulkUploadInvoices" runat="server" OnClick="lnkBulkUploadInvoices_Click" Text="Bulk Upload Invoices"
                    CssClass="linkbutton icon_upload" />
                <input id="invScan1Safe" name="invScan1Safe" type="file" runat="server" />
                <input id="invScan2Safe" name="invScan2Safe" type="file" runat="server" />
                <input id="invScan3Safe" name="invScan3Safe" type="file" runat="server" />
                <asp:LinkButton ID="lnkBulkUploadInvoicesSafe" runat="server" Text="Bulk Upload Invoices (Filtered)"
                    CssClass="linkbutton icon_upload" />
            </ContentTemplate>
        </asp:UpdatePanel>
    <ajaxToolkit:ModalPopupExtender ID="popExtender" runat="server" TargetControlID="lnkBtn"
        PopupControlID="pnlMessage" OnOkScript="onUpload" BackgroundCssClass="popup_background"
        OkControlID="lnkExtenderOk" CancelControlID="lnkExtenderCancel" DropShadow="False"
        RepositionMode="None">
    </ajaxToolkit:ModalPopupExtender>
    </asp:Panel>
    <asp:UpdatePanel ID="updConfirmationMessage" runat="server">
        <ContentTemplate>
            <asp:Panel ID="pnlMessage2" runat="server" Style="display: none" CssClass="popup">
                <asp:LinkButton ID="lnkExtenderCancel2" runat="server" Text="" CssClass="closebutton" />
                <h2>
                    <asp:Literal ID="litTitle2" runat="server" Text="<%$ Resources:Kartris, ContentText_Error %>" /></h2>
                <p>
                    <asp:Literal ID="litStatus2" runat="server"></asp:Literal>
                </p>
                <asp:LinkButton ID="lnkBtn2" runat="server" CssClass="invisible"></asp:LinkButton>
                <asp:LinkButton ID="lnkExtenderOk2" runat="server" Text="" CssClass="invisible" />
            </asp:Panel>
            <ajaxToolkit:ModalPopupExtender ID="popExtender2" runat="server" TargetControlID="lnkBtn2"
                PopupControlID="pnlMessage2" OnOkScript="onYes()" BackgroundCssClass="popup_background"
                OkControlID="lnkExtenderOk2" CancelControlID="lnkExtenderCancel2" DropShadow="False"
                RepositionMode="None">
            </ajaxToolkit:ModalPopupExtender>
        </ContentTemplate>
    </asp:UpdatePanel>


    </div>
        </ContentTemplate></asp:UpdatePanel>
</asp:Content>

