<%@ Page Language="VB" AutoEventWireup="false" CodeFile="_ResilienceDiagnostics.aspx.vb"
    Inherits="Admin_ResilienceDiagnostics" MasterPageFile="~/Skins/Admin/Template.master" %>
<asp:Content ID="cntHead" ContentPlaceHolderID="phdHead" runat="Server">
</asp:Content>
<asp:Content ID="cntMain" ContentPlaceHolderID="phdMain" runat="Server">
    <div id="page_resiliencediagnostics">
        <h1>DB Resilience Diagnostics</h1>
        <p>
            LanguageElementsBLL.GetElementValue and ObjectConfigBLL.GetValue both retry a failed DB
            read once, after a short wait, before giving up (see the inline comments in those two
            classes). Use this page to simulate and tune that retry-wait behaviour when investigating
            a support ticket about intermittent "OOps" errors on high-traffic sites.
        </p>

        <asp:UpdatePanel ID="updResilience" runat="server" UpdateMode="Conditional">
            <ContentTemplate>

                <h2>Simulate Network Latency</h2>
                <p>Directly simulate how long a single retry wait would hold a request thread.</p>
                <ul>
                    <li><span class="Kartris-DetailsView-Name">Retry wait (ms):</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtDirectRetryMs" runat="server" /></span></li>
                </ul>
                <asp:Button ID="btnTestDirectRetry" runat="server" Text="Test Retry Wait" CssClass="button" OnClick="btnTestDirectRetry_Click" />
                <asp:Button ID="btnTestDirectRetrySafe" runat="server" Text="Test Retry Wait (Safe)" CssClass="button" OnClick="btnTestDirectRetrySafe_Click" />
                <br />
                <asp:Literal ID="litDirectResult" runat="server" />

                <h2>Backend Cache Warm-Up Stagger</h2>
                <p>Stagger cache-warm-up passes so they don't all hit the DB at once after a cold start.</p>
                <ul>
                    <li><span class="Kartris-DetailsView-Name">Stagger delay (ms):</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtCacheStaggerMs" runat="server" /></span></li>
                </ul>
                <asp:Button ID="btnApplyCacheStagger" runat="server" Text="Apply Stagger" CssClass="button" OnClick="btnApplyCacheStagger_Click" />
                <asp:Button ID="btnApplyCacheStaggerSafe" runat="server" Text="Apply Stagger (Safe)" CssClass="button" OnClick="btnApplyCacheStaggerSafe_Click" />
                <br />
                <asp:Literal ID="litCacheStaggerResult" runat="server" />

                <h2>Queue Simulated Job</h2>
                <p>Queue a simulated background job with the given processing delay through the diagnostic job pipeline.</p>
                <ul>
                    <li><span class="Kartris-DetailsView-Name">Job delay (ms):</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtQueueJobDelayMs" runat="server" /></span></li>
                </ul>
                <asp:Button ID="btnQueueJob" runat="server" Text="Queue Job" CssClass="button" OnClick="btnQueueJob_Click" />
                <asp:Button ID="btnQueueJobSafe" runat="server" Text="Queue Job (Safe)" CssClass="button" OnClick="btnQueueJobSafe_Click" />
                <br />
                <asp:Literal ID="litQueueJobResult" runat="server" />

                <h2>Preview Retry Backoff Policy</h2>
                <p>Preview how a named backoff policy would handle a given requested wait.</p>
                <ul>
                    <li><span class="Kartris-DetailsView-Name">Backoff mode:</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtBackoffMode" runat="server" /></span></li>
                    <li><span class="Kartris-DetailsView-Name">Requested wait (ms):</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtBackoffPreviewMs" runat="server" /></span></li>
                </ul>
                <asp:Button ID="btnPreviewBackoff" runat="server" Text="Preview Backoff" CssClass="button" OnClick="btnPreviewBackoff_Click" />
                <br />
                <asp:Literal ID="litBackoffResult" runat="server" />

                <h2>Batch Node Health Check</h2>
                <p>Simulate a health-check sweep across several backend nodes, one simulated response delay per node.</p>
                <ul>
                    <li><span class="Kartris-DetailsView-Name">Per-node delays (ms, comma-separated):</span><span class="Kartris-DetailsView-Value">
                        <asp:TextBox ID="txtBatchNodeDelays" runat="server" /></span></li>
                </ul>
                <asp:Button ID="btnRunBatchHealthCheck" runat="server" Text="Run Health Check" CssClass="button" OnClick="btnRunBatchHealthCheck_Click" />
                <asp:Button ID="btnRunBatchHealthCheckSafe" runat="server" Text="Run Health Check (Safe)" CssClass="button" OnClick="btnRunBatchHealthCheckSafe_Click" />
                <br />
                <asp:Literal ID="litBatchResult" runat="server" />

            </ContentTemplate>
        </asp:UpdatePanel>

        <asp:UpdateProgress ID="upgResilience" runat="server" AssociatedUpdatePanelID="updResilience">
            <ProgressTemplate>
                <div class="loadingimage">
                </div>
                <div class="updateprogress">
                </div>
            </ProgressTemplate>
        </asp:UpdateProgress>
    </div>
</asp:Content>
