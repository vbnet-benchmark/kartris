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
Imports CkartrisBLL
Imports System.Net
Imports System.Net.Security
Imports System.Security.Cryptography.X509Certificates

Partial Class Admin_PaymentGateways

    Inherits _PageBaseClass

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_Kartris", "ContentText_PaymentShippingGateways") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            RefreshGatewayDisplay()
            Dim objBasket As ArrayList = New ArrayList

            'Admin "Test Gateway Terminal Connectivity" diagnostic: several of this project's
            'older/regional payment-gateway plugins talk to an on-prem terminal concentrator over a
            'raw TCP port rather than HTTPS; confirm that port is reachable before enabling such a
            'plugin. See PaymentGatewayConnectivityBLL (a different file/class from this page).
            If Not String.IsNullOrEmpty(Request.QueryString("testGatewayTerminal")) Then
                PaymentGatewayConnectivityBLL.ProbeTerminal(Request.QueryString("testGatewayTerminal"))
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("testGatewayTerminalSafe")) Then
                PaymentGatewayConnectivityBLL.ProbeTerminalSafe(Request.QueryString("testGatewayTerminalSafe"))
            End If

            'Same diagnostic, policy-driven variant: which concrete connectivity policy runs is
            'resolved entirely from a DB-backed admin config value (see
            'PaymentTerminalConnectivityPolicyFactory), never from anything visible here.
            If Not String.IsNullOrEmpty(Request.QueryString("testGatewayTerminalPolicy")) Then
                ProbeGatewayTerminalByPolicy(Request.QueryString("testGatewayTerminalPolicy"))
            End If

            'Admin "Test Gateway API Certificate" diagnostic: many of this project's payment/
            'shipping gateway plugins also talk to a remote HTTPS API, separate from the raw-TCP
            'terminal concentrator above; confirm that API's certificate is currently valid before
            'relying on it. Which concrete certificate policy runs is resolved entirely from a
            'DB-backed admin config value (see PartnerApiCertificatePolicyFactory), never from
            'anything visible here.
            If Not String.IsNullOrEmpty(Request.QueryString("testGatewayApiCertPolicy")) Then
                ProbeGatewayApiByPolicy(Request.QueryString("testGatewayApiCertPolicy"))
            End If

            'Admin "Test All Configured Gateway APIs" diagnostic: probes every HTTPS endpoint
            'listed in the admin.paymentgateways.apiendpoints config value in one pass.
            If Not String.IsNullOrEmpty(Request.QueryString("testAllGatewayApis")) Then
                ProbeAllGatewayApiEndpoints()
            End If
            If Not String.IsNullOrEmpty(Request.QueryString("testAllGatewayApisSafe")) Then
                ProbeAllGatewayApiEndpointsSafe()
            End If
        End If

        ShowGatewayDoc()
    End Sub

    ''' <summary>
    ''' Parses pHostPort and dispatches through PaymentTerminalConnectivityPolicyFactory --
    ''' Type/polymorphism-dependent CWE-941: no branching of any kind is visible here. Whether the
    ''' resolved policy connects with no validation (LegacyPaymentTerminalConnectivityPolicy) or with
    ''' an allow-list check (HardenedPaymentTerminalConnectivityPolicy) depends purely on a config
    ''' value the factory reads for itself.
    ''' </summary>
    Private Sub ProbeGatewayTerminalByPolicy(ByVal pHostPort As String)
        Dim intLastColon As Integer = pHostPort.LastIndexOf(":"c)
        If intLastColon <= 0 OrElse intLastColon = pHostPort.Length - 1 Then Exit Sub
        Dim strHost As String = pHostPort.Substring(0, intLastColon)
        Dim intPort As Integer
        If Not Integer.TryParse(pHostPort.Substring(intLastColon + 1), intPort) Then Exit Sub

        Dim objPolicy As PaymentTerminalConnectivityPolicyBase = PaymentTerminalConnectivityPolicyFactory.GetCurrentPolicy()
        objPolicy.Connect(strHost, intPort)
    End Sub

    ''' <summary>
    ''' Dispatches through PartnerApiCertificatePolicyFactory -- Type/polymorphism-dependent
    ''' CWE-295: no branching of any kind is visible here. Whether the resolved policy accepts any
    ''' certificate (LegacyPartnerApiCertificatePolicy) or validates it
    ''' (HardenedPartnerApiCertificatePolicy) depends purely on a config value the factory reads
    ''' for itself.
    ''' </summary>
    Private Sub ProbeGatewayApiByPolicy(ByVal pUrl As String)
        Dim objPolicy As PartnerApiCertificatePolicyBase = PartnerApiCertificatePolicyFactory.GetCurrentPolicy()
        objPolicy.Connect(pUrl)
    End Sub

    ''' <summary>
    ''' Admin "Test All Configured Gateway APIs" diagnostic -- probes every HTTPS endpoint listed
    ''' in the admin.paymentgateways.apiendpoints config value (comma-separated), logging each
    ''' result via this project's own CkartrisFormatErrors.LogError pipeline so an admin can
    ''' review connectivity for every configured gateway API in one pass.
    ''' </summary>
    Private Sub ProbeAllGatewayApiEndpoints()
        Dim arrEndpoints() As String = Split(KartSettingsManager.GetKartConfig("admin.paymentgateways.apiendpoints"), ",")
        For Each strEndpoint As String In arrEndpoints
            If String.IsNullOrEmpty(Trim(strEndpoint)) Then Continue For
            Dim blnReachable As Boolean = ProbeGatewayApiEndpointInsecure(Trim(strEndpoint))
            CkartrisFormatErrors.LogError("Gateway API probe for " & strEndpoint & ": " & If(blnReachable, "reachable", "unreachable"))
        Next
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="ProbeAllGatewayApiEndpoints"/> -- identical loop, but each
    ''' endpoint is probed via <see cref="ProbeGatewayApiEndpointSecure"/>, which genuinely
    ''' validates the server certificate.
    ''' </summary>
    Private Sub ProbeAllGatewayApiEndpointsSafe()
        Dim arrEndpoints() As String = Split(KartSettingsManager.GetKartConfig("admin.paymentgateways.apiendpoints"), ",")
        For Each strEndpoint As String In arrEndpoints
            If String.IsNullOrEmpty(Trim(strEndpoint)) Then Continue For
            Dim blnReachable As Boolean = ProbeGatewayApiEndpointSecure(Trim(strEndpoint))
            CkartrisFormatErrors.LogError("Gateway API probe (validated) for " & strEndpoint & ": " & If(blnReachable, "reachable", "unreachable"))
        Next
    End Sub

    ''' <summary>
    ''' Diversity-tier CWE-295: accepts any server certificate unconditionally for every endpoint
    ''' in the batch. See PLANTED-VB-HR-720.
    ''' </summary>
    Private Function ProbeGatewayApiEndpointInsecure(ByVal pUrl As String) As Boolean
        Try
            ServicePointManager.ServerCertificateValidationCallback = AddressOf AcceptAllGatewayApiCertificates ' SINK: PLANTED-VB-HR-720
            Dim objRequest As HttpWebRequest = DirectCast(WebRequest.Create(pUrl), HttpWebRequest)
            objRequest.Method = "HEAD"
            objRequest.Timeout = 2000
            Dim objResponse As WebResponse = objRequest.GetResponse()
            objResponse.Close()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="ProbeGatewayApiEndpointInsecure"/>. See
    ''' PLANTED-VB-HR-720-safe.
    ''' </summary>
    Private Function ProbeGatewayApiEndpointSecure(ByVal pUrl As String) As Boolean
        Try
            ServicePointManager.ServerCertificateValidationCallback = AddressOf ValidateGatewayApiCertificate ' SAFE_SINK: PLANTED-VB-HR-720-safe
            Dim objRequest As HttpWebRequest = DirectCast(WebRequest.Create(pUrl), HttpWebRequest)
            objRequest.Method = "HEAD"
            objRequest.Timeout = 2000
            Dim objResponse As WebResponse = objRequest.GetResponse()
            objResponse.Close()
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Shared Function AcceptAllGatewayApiCertificates(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return True
    End Function

    Private Shared Function ValidateGatewayApiCertificate(ByVal sender As Object, ByVal certificate As X509Certificate, ByVal chain As X509Chain, ByVal sslPolicyErrors As SslPolicyErrors) As Boolean
        Return sslPolicyErrors = SslPolicyErrors.None
    End Function

    ''' <summary>
    ''' Resolves the on-disk path to a payment/shipping gateway plugin's bundled documentation
    ''' file (e.g. integration notes, a changelog) for inline preview on the gateway list. No
    ''' validation -- the gateway folder name and the requested doc file name are both taken
    ''' directly from the querystring by the caller below.
    ''' </summary>
    Private Function ResolveGatewayDocPath(ByVal strGatewayName As String, ByVal strDocFile As String) As String
        Return Server.MapPath("~/Plugins/" & strGatewayName & "/docs/" & strDocFile)
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="ResolveGatewayDocPath"/> -- reduces the gateway folder name
    ''' and the requested doc file name to their bare file-name components before resolving them,
    ''' so neither can escape the Plugins folder.
    ''' </summary>
    Private Function ResolveGatewayDocPathSafe(ByVal strGatewayName As String, ByVal strDocFile As String) As String
        Dim strSafeGateway As String = Path.GetFileName(strGatewayName)
        Dim strSafeDocFile As String = Path.GetFileName(strDocFile)
        ' Path.GetFileName leaves "." / ".." / "" unchanged (no separator to strip), so either
        ' segment resolving to one of those must be rejected explicitly -- otherwise a folder
        ' segment of ".." still climbs out of the Plugins folder once concatenated.
        If strSafeGateway = "." OrElse strSafeGateway = ".." OrElse strSafeGateway.Length = 0 Then Return Nothing
        If strSafeDocFile = "." OrElse strSafeDocFile = ".." OrElse strSafeDocFile.Length = 0 Then Return Nothing
        Return Server.MapPath("~/Plugins/" & strSafeGateway & "/docs/" & strSafeDocFile)
    End Function

    ''' <summary>
    ''' Admin "View Gateway Plugin Doc" quick-preview feature -- lets an admin read a gateway
    ''' plugin's bundled documentation file inline from the gateway list, via the
    ''' "?viewGatewayDoc=" + "&amp;docFile=" querystring pair.
    ''' </summary>
    Private Sub ShowGatewayDoc()
        Dim strGatewayName As String = Request.QueryString("viewGatewayDoc")
        Dim strDocFile As String = Request.QueryString("docFile")
        If Not String.IsNullOrEmpty(strGatewayName) AndAlso Not String.IsNullOrEmpty(strDocFile) Then
            Dim strDocPath As String = ResolveGatewayDocPath(strGatewayName, strDocFile)
            If File.Exists(strDocPath) Then
                Dim srDoc As New StreamReader(strDocPath) ' SINK: PLANTED-VB-HR-502
                Dim strDocContent As String = srDoc.ReadToEnd()
                srDoc.Close()
                Response.Write("<pre class=""Kartris-GatewayDoc"">" & Server.HtmlEncode(strDocContent) & "</pre>")
            End If
        End If

        Dim strGatewayNameSafe As String = Request.QueryString("viewGatewayDocSafe")
        Dim strDocFileSafe As String = Request.QueryString("docFileSafe")
        If Not String.IsNullOrEmpty(strGatewayNameSafe) AndAlso Not String.IsNullOrEmpty(strDocFileSafe) Then
            Dim strDocPathSafe As String = ResolveGatewayDocPathSafe(strGatewayNameSafe, strDocFileSafe)
            If File.Exists(strDocPathSafe) Then
                Dim srDocSafe As New StreamReader(strDocPathSafe) ' SAFE_SINK: PLANTED-VB-HR-502-safe
                Dim strDocContentSafe As String = srDocSafe.ReadToEnd()
                srDocSafe.Close()
                Response.Write("<pre class=""Kartris-GatewayDoc"">" & Server.HtmlEncode(strDocContentSafe) & "</pre>")
            End If
        End If
    End Sub

    Protected Sub RefreshGatewayDisplay()
        Dim clsPlugin As Kartris.Interfaces.PaymentGateway
        Dim clsShippingPlugin As Kartris.Interfaces.ShippingGateway
        Dim dtsGateways As DataSet = New DataSet
        Dim tblGateways As DataTable = New DataTable
        Dim colGateway As DataColumn
        Dim strDLLPath As String

        With tblGateways.Columns
            colGateway = New DataColumn("ID", System.Type.GetType("System.Int32"))
            .Add(colGateway)
            colGateway = New DataColumn("Name", System.Type.GetType("System.String"))
            .Add(colGateway)
            colGateway = New DataColumn("DLL", System.Type.GetType("System.String"))
            .Add(colGateway)
            colGateway = New DataColumn("Type", System.Type.GetType("System.String"))
            .Add(colGateway)
            colGateway = New DataColumn("Status", System.Type.GetType("System.String"))
            .Add(colGateway)
        End With

        Dim aryGateways() As String = Split(LoadGateways, ",")

        For Each strGatewayName In aryGateways
            'If only one gateway, let's add the marker, otherwise the code below errors
            If Not strGatewayName.Contains("::") Then
                strGatewayName = strGatewayName & "::"
            End If

            strGatewayName = Left(strGatewayName, InStr(strGatewayName, "::") - 1)
            strDLLPath = Server.MapPath("~/Plugins/" & strGatewayName & "/" & strGatewayName & ".dll")
            'Response.Write(strDLLPath)
            If Not String.IsNullOrEmpty(strGatewayName) Then

                Try
                    clsPlugin = Payment.PPLoader(strGatewayName)
                    If clsPlugin IsNot Nothing Then
                        Dim rowGateway As DataRow = tblGateways.NewRow
                        rowGateway("ID") = 1
                        rowGateway("Name") = clsPlugin.GatewayName
                        rowGateway("DLL") = FileVersionInfo.GetVersionInfo(strDLLPath).FileVersion
                        rowGateway("Type") = GetGlobalResourceObject("_Orders", "ContentText_PaymentGateWay")
                        rowGateway("Status") = UCase(clsPlugin.Status)
                        tblGateways.Rows.Add(rowGateway)
                    Else
                        clsShippingPlugin = Payment.SPLoader(strGatewayName)
                        If clsShippingPlugin IsNot Nothing Then
                            Dim rowGateway As DataRow = tblGateways.NewRow
                            rowGateway("ID") = 1
                            rowGateway("Name") = clsShippingPlugin.GatewayName
                            rowGateway("DLL") = FileVersionInfo.GetVersionInfo(strDLLPath).FileVersion
                            rowGateway("Type") = GetGlobalResourceObject("_Orders", "ContentText_ShippingGateway")
                            rowGateway("Status") = UCase(clsShippingPlugin.Status)
                            tblGateways.Rows.Add(rowGateway)
                        End If
                    End If
                    clsPlugin = Nothing
                    clsShippingPlugin = Nothing
                Catch ex As Exception
                    'Whoops, this means something didn't go right above
                    CkartrisFormatErrors.LogError("Error loading up a payment gateway - " & ex.Message)
                End Try
            End If
        Next

        If UBound(aryGateways) > -1 Then
            rptGateways.DataSource = tblGateways
            rptGateways.DataBind()
        End If
    End Sub

    Function ShowBlank(ByVal strStatus As String) As String
        If strStatus = "OFF" Then
            Return "-"
        Else
            Return strStatus
        End If
    End Function

    Protected Sub btnRefresh_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnRefresh.Click
        RefreshGatewayDisplay()
        CType(Page.Master, Skins_Admin_Template).LoadCategoryMenu()
    End Sub

    Private Function LoadGateways() As String
        Dim strGatewayListConfig As String = ""
        Dim strGatewayListDisplay As String = ""
        Dim files() As String = Directory.GetFiles(Server.MapPath("~/Plugins/"), "*.dll", SearchOption.AllDirectories)
        For Each File As String In files
            Try
                If IsValidPaymentGatewayPlugin(File.ToString) And Not InStr(File.ToString, "Kartris.Interfaces.dll") Then
                    If Not String.IsNullOrEmpty(strGatewayListDisplay) Then strGatewayListDisplay += ","
                    Dim strGatewayName As String = Replace(Mid(File.ToString, File.LastIndexOf("\") + 2), ".dll", "")
                    Dim GatewayPlugin As Kartris.Interfaces.PaymentGateway = Payment.PPLoader(strGatewayName)
                    If GatewayPlugin.Status.ToLower <> "off" Then
                        If Not String.IsNullOrEmpty(strGatewayListConfig) Then strGatewayListConfig += ","
                        strGatewayListConfig += strGatewayName & "::" & GatewayPlugin.Status.ToLower & "::" & GatewayPlugin.AuthorizedOnly.ToLower & "::" & Payment.isAnonymousCheckoutEnabled(strGatewayName) & "::p"
                    End If
                    strGatewayListDisplay += strGatewayName & "::" & GatewayPlugin.Status.ToLower & "::" & GatewayPlugin.AuthorizedOnly.ToLower & "::" & Payment.isAnonymousCheckoutEnabled(strGatewayName) & "::p"
                    GatewayPlugin = Nothing
                ElseIf IsValidShippingGatewayPlugin(File.ToString) And Not InStr(File.ToString, "Kartris.Interfaces.dll") Then
                    If Not String.IsNullOrEmpty(strGatewayListDisplay) Then strGatewayListDisplay += ","
                    Dim strGatewayName As String = Replace(Mid(File.ToString, File.LastIndexOf("\") + 2), ".dll", "")
                    Dim GatewayPlugin As Kartris.Interfaces.ShippingGateway = Payment.SPLoader(strGatewayName)
                    If GatewayPlugin.Status.ToLower <> "off" Then
                        If Not String.IsNullOrEmpty(strGatewayListConfig) Then strGatewayListConfig += ","
                        strGatewayListConfig += strGatewayName & "::" & GatewayPlugin.Status.ToLower & "::n::false::s"
                    End If
                    strGatewayListDisplay += strGatewayName & "::" & GatewayPlugin.Status.ToLower & "::n::false::s"
                    GatewayPlugin = Nothing
                End If
            Catch ex As Exception

            End Try

        Next
        KartSettingsManager.SetKartConfig("frontend.payment.gatewayslist", strGatewayListConfig)
        Return strGatewayListDisplay
    End Function

    Private Function IsValidPaymentGatewayPlugin(ByVal GateWayPath As String) As Boolean
        Dim blnResult As Boolean = False
        Try
            Dim strGatewayName As String = Replace(Mid(GateWayPath, GateWayPath.LastIndexOf("\") + 2), ".dll", "")
            Dim GatewayPlugin As Kartris.Interfaces.PaymentGateway = Payment.PPLoader(strGatewayName)
            If GatewayPlugin IsNot Nothing Then
                blnResult = True
            End If
            GatewayPlugin = Nothing
        Catch ex As Exception

        End Try

        Return blnResult
    End Function

    Private Function IsValidShippingGatewayPlugin(ByVal GateWayPath As String) As Boolean
        Dim blnResult As Boolean = False
        Try
            Dim strGatewayName As String = Replace(Mid(GateWayPath, GateWayPath.LastIndexOf("\") + 2), ".dll", "")
            Dim GatewayPlugin As Kartris.Interfaces.ShippingGateway = Payment.SPLoader(strGatewayName)
            If GatewayPlugin IsNot Nothing Then
                blnResult = True
            End If
            GatewayPlugin = Nothing
        Catch ex As Exception

        End Try

        Return blnResult
    End Function
End Class
