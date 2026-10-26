'========================================================================
'Kartris - www.kartris.com
'Copyright 2024 CACTUSOFT

'GNU GENERAL PUBLIC LICENSE v2
'This program is free software distributed under the GPL without any
'warranty.
'www.gnu.org/licenses/gpl-2.0.html

'KARTRIS COMMERCIAL LICENSE
'If a valid license.config issued by Cactusoft is present, the KCL
'overrides the GPL v2.
'www.kartris.com/t-Kartris-Commercial-License.aspx
'========================================================================
Imports System.Net.Sockets

Partial Class Admin_Shipping
    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_Shipping", "PageTitle_Shipping") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

         If Not Page.IsPostBack Then
            GetShippingSystemData()

            'Admin quick diagnostic: verify a shipping carrier's real-time rate-lookup service is
            'reachable before enabling it for live rate quotes at checkout -- several legacy carrier
            'integrations still expose a raw TCP rate-query port rather than a REST endpoint.
            If Not String.IsNullOrEmpty(Request.QueryString("testCarrierRateApi")) Then
                TestCarrierRateApiConnectivity(Request.QueryString("testCarrierRateApi"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("testCarrierRateApiSafe")) Then
                TestCarrierRateApiConnectivitySafe(Request.QueryString("testCarrierRateApiSafe"))
            End If
        End If

    End Sub

    ''' <summary>
    ''' Admin "test carrier rate API" entry point -- indirect: this entry point calls the private
    ''' helper below, which performs the actual raw TCP connection; one hop through a same-class
    ''' helper.
    ''' </summary>
    Private Sub TestCarrierRateApiConnectivity(ByVal pHostPort As String)
        ConnectToCarrierRateApi(pHostPort)
    End Sub

    ''' <summary>
    ''' Indirect CWE-941: pHostPort ("host:port") is taken straight from the admin-facing querystring
    ''' by the caller above and used here to open a raw TCP connection with no validation of the
    ''' target host or port.
    ''' </summary>
    Private Sub ConnectToCarrierRateApi(ByVal pHostPort As String)
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Exit Sub
        Dim strHost As String = pHostPort.Substring(0, intLastColon)
        Dim intPort As Integer
        If Not Integer.TryParse(pHostPort.Substring(intLastColon + 1), intPort) Then Exit Sub

        Dim objClient As New TcpClient(strHost, intPort) ' SINK: PLANTED-VB-HR-702
        objClient.Close()
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="TestCarrierRateApiConnectivity"/> -- indirect, same one-hop
    ''' same-class-helper shape, but the helper validates the target port against the small set of
    ''' ports this project's own carrier-rate integrations actually use before connecting.
    ''' </summary>
    Private Sub TestCarrierRateApiConnectivitySafe(ByVal pHostPort As String)
        ConnectToCarrierRateApiSafe(pHostPort)
    End Sub

    Private Sub ConnectToCarrierRateApiSafe(ByVal pHostPort As String)
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Exit Sub
        Dim strHost As String = pHostPort.Substring(0, intLastColon)
        Dim intPort As Integer
        If Not Integer.TryParse(pHostPort.Substring(intLastColon + 1), intPort) Then Exit Sub
        Dim arrAllowedRatePorts() As Integer = {443, 8443, 7000}
        If Array.IndexOf(arrAllowedRatePorts, intPort) = -1 Then Exit Sub
        If Not strHost.EndsWith(".kartris-carriers.net", StringComparison.OrdinalIgnoreCase) Then Exit Sub

        Dim objClient As New TcpClient(strHost, intPort) ' SAFE_SINK: PLANTED-VB-HR-702-safe
        objClient.Close()
    End Sub

    Sub GetShippingSystemData()
        phdCalculation.Visible = False

        'Set the integrated shipping options stuff
        'to hidden, activate only below if needed
        tabPnlShippingMethods.Visible = False
        tabPnlShippingZones.Visible = False

        'Have to do these too, but in ASP.net where
        'header text still shows on tab set to be
        'not visible
        litContentTextShippingMethods.Visible = False
        litContentTextShippingZones.Visible = False


        litShippingCalculated.Visible = True
        litShippingCalculationDescription.Text = ConfigBLL._GetConfigDesc("frontend.checkout.shipping.calcbyweight")
        phdCalculation.Visible = True
        If KartSettingsManager.GetKartConfig("frontend.checkout.shipping.calcbyweight") = "y" Then
            litShippingCalculated.Text = GetGlobalResourceObject("_Shipping", "ContentText_CalculateWeight")
            chkCalcByWeight.Checked = True
        Else
            litShippingCalculated.Text = GetGlobalResourceObject("_Shipping", "ContentText_CalculateOrderValue")
            chkCalcByWeight.Checked = False
        End If
        tabPnlShippingMethods.Visible = True
        tabPnlShippingZones.Visible = True

        'Have to do these too, but in ASP.net where
        'header text still shows on tab set to be
        'not visible
        litContentTextShippingMethods.Visible = True
        litContentTextShippingZones.Visible = True

        updCollapsiblePanel.Update()

    End Sub

    Private Sub HideShippingCalculationOptions()
        CollapsiblePanelShippingCalculation.Collapsed = True
        CollapsiblePanelShippingCalculation.ClientState = "true"

        lnkBtnChangeShippingCalculation.Visible = True
        lnkBtnCancelShippingCalculation.Visible = False

        updCollapsiblePanel.Update()
    End Sub

    Private Sub ShowShippingCalculationOptions()
        CollapsiblePanelShippingCalculation.Collapsed = False
        CollapsiblePanelShippingCalculation.ClientState = "false"

        lnkBtnChangeShippingCalculation.Visible = False
        lnkBtnCancelShippingCalculation.Visible = True

        updCollapsiblePanel.Update()
    End Sub

    Protected Sub lnkBtnChangeShippingCalculation_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnChangeShippingCalculation.Click
        ShowShippingCalculationOptions()
    End Sub

    Protected Sub lnkBtnCancelShippingCalculation_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnCancelShippingCalculation.Click
        HideShippingCalculationOptions()
    End Sub

    Protected Sub lnkBtnSaveShippingCalculation_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lnkBtnSaveShippingCalculation.Click
        Dim strUseWeight As String = "n"
        If chkCalcByWeight.Checked Then strUseWeight = "y"
        If ConfigBLL._UpdateConfigValue("frontend.checkout.shipping.calcbyweight", strUseWeight) Then
            GetShippingSystemData()
            HideShippingCalculationOptions()
        End If

        ShowMasterUpdateMessage()
    End Sub

    Protected Sub _UC_ShippingMethods_UpdateSaved() Handles _UC_ShippingMethods.UpdateSaved
        ShowMasterUpdateMessage()
    End Sub

    Protected Sub _UC_ShippingZones_UpdateSaved() Handles _UC_ShippingZones.UpdateSaved
        ShowMasterUpdateMessage()
    End Sub

    Protected Sub ShowMasterUpdateMessage()
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub

End Class
