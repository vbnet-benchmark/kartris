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
Imports CkartrisEnumerations
Imports CkartrisDataManipulation
Imports System.Linq

Partial Class Admin_Destinations
    Inherits _PageBaseClass

    Private Shared ModifiedConfig As Configuration = Nothing

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        Page.Title = GetGlobalResourceObject("_RegionalWizard", "PageTitle_RegionalSetupWizard") & " | " & GetGlobalResourceObject("_Kartris", "ContentText_KartrisName")

        If Not Page.IsPostBack Then
            Dim strTaxRegime As String = TaxRegime.Name

            Try
                'Try to set tax regime dropdown
                ddlTaxRegime.SelectedValue = strTaxRegime
            Catch ex As Exception

            End Try

            'set default config settings for each region
            Select Case strTaxRegime
                Case "EU"
                    ddlPricesIncTaxConfig.SelectedValue = "y"
                    ddlShowTaxConfig.SelectedValue = "c"
                Case Else
                    ddlPricesIncTaxConfig.SelectedValue = "n"
                    ddlShowTaxConfig.SelectedValue = "c"
            End Select

            'Populate currency dropdown
            Dim dtbCurrencies As DataTable = KartSettingsManager.GetCurrenciesFromCache()
            Dim drwCurrencies As DataRow() = dtbCurrencies.Select()
            If drwCurrencies.Length > 0 Then
                ddlCurrency.Items.Clear()
                ddlCurrency.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))
                For i As Byte = 0 To drwCurrencies.Length - 1
                    ddlCurrency.Items.Add(New ListItem(drwCurrencies(i)("CUR_Symbol") & " " & drwCurrencies(i)("CUR_ISOCode"), drwCurrencies(i)("CUR_ID")))
                Next
            End If

            'Support-ticket convenience -- a bookmarkable diagnostic link support staff can
            'hand an admin to preview a candidate regime key directly, mirroring the
            'existing "NotifySubscriber" querystring convention in Admin_KnowledgeBase.Page_Load
            Dim strQsPreviewRegimeLegacy As String = Request.QueryString("PreviewRegimeLegacy")
            If Not String.IsNullOrEmpty(strQsPreviewRegimeLegacy) Then RunRegimePreviewLegacy(strQsPreviewRegimeLegacy)
            Dim strQsPreviewRegimeValidated As String = Request.QueryString("PreviewRegimeValidated")
            If Not String.IsNullOrEmpty(strQsPreviewRegimeValidated) Then RunRegimePreviewValidated(strQsPreviewRegimeValidated)

            'Same convenience for applying a previously-staged custom regime key
            Dim strQsApplyStagedRegime As String = Request.QueryString("ApplyStagedRegime")
            If strQsApplyStagedRegime = "legacy" Then
                ApplyStagedRegimeLegacy()
            ElseIf strQsApplyStagedRegime = "validated" Then
                ApplyStagedRegimeValidated()
            End If
        Else
            pnlSummary.Visible = False
        End If

    End Sub

    ''' <summary>
    ''' Reset tax regime
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlTaxRegime_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlTaxRegime.SelectedIndexChanged
        Try
            If ModifiedConfig Is Nothing Then
                ModifiedConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath)
            End If
        Catch ex As Exception
            'Hmmm....Kartris must be running under medium trust
        End Try

        Try
            Dim doc As New XmlDocument()
            Dim section As System.Configuration.ConfigurationSection = ModifiedConfig.GetSection("appSettings")
            Dim element As KeyValueConfigurationElement = CType(ModifiedConfig.AppSettings.Settings("TaxRegime"), KeyValueConfigurationElement)
            element.Value = ddlTaxRegime.SelectedValue

            If ModifiedConfig IsNot Nothing Then
                ModifiedConfig.Save()
                Response.Redirect("")
            End If
        Catch ex As Exception

        End Try

    End Sub

    Protected Sub ShowMasterUpdateMessage()
        CType(Me.Master, Skins_Admin_Template).DataUpdated()
    End Sub

    ''' <summary>
    ''' Handle currency selection dropdown
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlCurrency_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlCurrency.SelectedIndexChanged

        'If no currency, hide the rest of things
        If ddlCurrency.SelectedValue <> "noselection" Then
            'Currency selected
            mvwRegionalSetupWizard.Visible = True
        Else
            'No currency, hide bits below
            mvwRegionalSetupWizard.Visible = False
            pnlSummary.Visible = False
        End If

        Dim strActiveTaxRegime As String = TaxRegime.Name
        Select Case strActiveTaxRegime
            Case "VAT"
                mvwRegionalSetupWizard.SetActiveView(viwVAT)
                ddlCountries.Items.Clear()
                ddlCountries.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))

                Dim tblAllCountries As DataTable = ShippingBLL._GetDestinationsByLanguage(Session("_LANG"))
                For Each drwCountry As DataRow In tblAllCountries.Rows
                    ddlCountries.Items.Add(New ListItem(drwCountry("D_Name"), drwCountry("D_ID")))
                Next

            Case "EU"
                mvwRegionalSetupWizard.SetActiveView(viwEU)
            Case "US"
                mvwRegionalSetupWizard.SetActiveView(viwUS)
                ddlUSStates.Items.Clear()
                ddlUSStates.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))
                For Each objCountry In GetCountryListFromTaxConfig()
                    ddlUSStates.Items.Add(New ListItem(objCountry.Name, objCountry.CountryId))
                Next
                txtUSStateTaxRate.Text = ""
                phdUSStateTaxRate.Visible = False
            Case "CANADA"
                mvwRegionalSetupWizard.SetActiveView(viwCanada)
                ddlCanadaBaseProvince.Items.Clear()
                ddlCanadaBaseProvince.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))
                For Each objCountry In GetCountryListFromTaxConfig()
                    ddlCanadaBaseProvince.Items.Add(New ListItem(objCountry.Name, objCountry.CountryId))
                Next
                txtCanadaGST.Text = ""
                txtCanadaPST.Text = ""
                phdCanadaProvinceTax.Visible = False
            Case "SIMPLE"
                mvwRegionalSetupWizard.SetActiveView(viwOther)
                ddlSimpleBaseCountry.Items.Clear()
                ddlSimpleBaseCountry.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))

                Dim tblAllCountries As DataTable = ShippingBLL._GetDestinationsByLanguage(Session("_LANG"))
                For Each drwCountry As DataRow In tblAllCountries.Rows
                    ddlSimpleBaseCountry.Items.Add(New ListItem(drwCountry("D_Name"), drwCountry("D_ID")))
                Next

                txtSimpleTaxRate.Text = ""
                phdSimpleTaxRate.Visible = False
        End Select

    End Sub

    ''' <summary>
    ''' Handle 'VAT registered?' dropdown
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlVATRegistered_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlVatRegistered.SelectedIndexChanged
        If ddlVatRegistered.SelectedValue = "y" Then
            'Yes
            phdBaseCountry.Visible = True
        ElseIf ddlVatRegistered.SelectedValue = "n" Then
            'No
            phdBaseCountry.Visible = False
            phdVATRate.Visible = False
            pnlSummary.Visible = True
        Else
            'No selection
            phdBaseCountry.Visible = False
            phdVATRate.Visible = False
            pnlSummary.Visible = False
        End If
    End Sub

    ''' <summary>
    ''' Handle VAT country dropdown selection
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlCountries_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlCountries.SelectedIndexChanged
        If ddlCountries.SelectedValue = "noselection" Then
            'No country selected, hide bits below
            phdVATRate.Visible = False
            pnlSummary.Visible = False
        Else
            'Country selected, show VAT rate field
            txtVATRate.Text = ""
            phdVATRate.Visible = True
        End If
    End Sub

    ''' <summary>
    ''' Handle EU 'VAT registered?' dropdown
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlQVATRegistered_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlEUVatRegistered.SelectedIndexChanged
        If ddlEUVatRegistered.SelectedValue = "y" Then
            'Yes
            ddlEUCountries.Items.Clear()
            ddlEUCountries.Items.Add(New ListItem(GetGlobalResourceObject("_Kartris", "ContentText_DropDownSelect"), "noselection"))
            For Each objCountry In GetCountryListFromTaxConfig()
                Try
                    ddlEUCountries.Items.Add(New ListItem(objCountry.Name, objCountry.CountryId))
                Catch ex As Exception

                End Try
            Next
            phdEUBaseCountry.Visible = True
        ElseIf ddlEUVatRegistered.SelectedValue = "n" Then
            'No
            phdEUBaseCountry.Visible = False
            phdEUVATRate.Visible = False
            pnlSummary.Visible = True
        Else
            'No selection
            phdEUBaseCountry.Visible = False
            phdEUVATRate.Visible = False
            pnlSummary.Visible = False
        End If

    End Sub

    ''' <summary>
    ''' Handle EU country dropdown selection
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlEUCountries_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlEUCountries.SelectedIndexChanged
        If ddlEUCountries.SelectedValue = "noselection" Then
            'No country selected, hide bits below
            phdEUVATRate.Visible = False
            pnlSummary.Visible = False
        Else
            'Country selected, show VAT rate field
            txtQVatRate.Text = ""
            phdEUVATRate.Visible = True
        End If
    End Sub

    ''' <summary>
    ''' Handle US state dropdown selection
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlUSStates_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlUSStates.SelectedIndexChanged
        If ddlUSStates.SelectedValue = "noselection" Then
            'No state selected, hide bits below
            phdUSStateTaxRate.Visible = False
            pnlSummary.Visible = False
        Else
            'State selected, show tax rate field
            txtUSStateTaxRate.Text = ""
            phdUSStateTaxRate.Visible = True
        End If

    End Sub

    ''' <summary>
    ''' Handle Canada province/territory dropdown selection
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlCanadaBaseProvince_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlCanadaBaseProvince.SelectedIndexChanged
        If ddlCanadaBaseProvince.SelectedValue = "noselection" Then
            'No province/territory selected, hide bits below
            phdCanadaProvinceTax.Visible = False
            pnlSummary.Visible = False
        Else
            'Province/territory selected, show tax rate fields
            lblCanadaBaseProvince.Text = ddlCanadaBaseProvince.SelectedItem.Text
            txtCanadaGST.Text = ""
            txtCanadaPST.Text = ""
            phdCanadaProvinceTax.Visible = True
        End If

    End Sub

    ''' <summary>
    ''' Handle country dropdown selection
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub ddlSimpleBaseCountry_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ddlSimpleBaseCountry.SelectedIndexChanged
        If ddlCanadaBaseProvince.SelectedValue = "noselection" Then
            'No country selected, hide bits below
            phdSimpleTaxRate.Visible = False
            pnlSummary.Visible = False
        Else
            'Country selected, show tax rate fields
            txtSimpleTaxRate.Text = ""
            phdSimpleTaxRate.Visible = True
        End If

    End Sub

    ''' <summary>
    ''' Handle tax rate text box being changed
    ''' </summary>
    ''' <remarks></remarks>
    Protected Sub txtQVatRate_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtQVatRate.TextChanged, txtUSStateTaxRate.TextChanged,
                                                                                                        txtSimpleTaxRate.TextChanged, txtCanadaPST.TextChanged, txtVATRate.TextChanged

        'If tax rate is blank (we total all boxes
        'so we can do check on one line) then we
        'hide next section
        If txtQVatRate.Text & txtUSStateTaxRate.Text & txtSimpleTaxRate.Text & txtCanadaPST.Text & txtVATRate.Text = "" Then
            pnlSummary.Visible = False
        Else
            pnlSummary.Visible = True
        End If
    End Sub

    Protected Sub btnConfirmSetup_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnConfirmSetup.Click

        'CURRENCY
        Dim intSelectedCurrencyID As Integer = ddlCurrency.SelectedValue

        'Set default currency
        Dim strMessage As String = String.Empty
        CurrenciesBLL._SetDefault(intSelectedCurrencyID, strMessage)
        '/CURRENCY

        Dim dtbAllCountries As DataTable = ShippingBLL._GetDestinationsByLanguage(CkartrisBLL.GetLanguageIDfromSession)
        Dim lstRegionCountries As List(Of KartrisClasses.Country) = GetCountryListFromTaxConfig()

        'Store location ISO
        Dim strThisLocationISOCode As String = ""

        'Show link to reset currency rates
        lnkCurrencyLink.Visible = True

        Select Case TaxRegime.Name
            Case "VAT"
                'VAT Registered?
                If ddlVatRegistered.SelectedValue = "y" Then
                    'Yes - turn off tax for everything else, except this country

                    For Each drwCountry As DataRow In dtbAllCountries.Rows
                        Dim intCountryID As Integer = drwCountry("D_ID")

                        If ddlCountries.SelectedValue = intCountryID Then

                            'charge tax and set to live
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 1, FixNullFromDB(drwCountry("D_Tax2")),
                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                               FixNullFromDB(drwCountry("D_Region")), True, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        Else
                            'just don't charge tax
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                               FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        End If

                        'Special case for UK/GB
                        'Brexit is a mess. It seems that for UK companies trying to import or export to the EU,
                        'an EU VAT number really should be collected and show on the invoice, because apparently it
                        'makes shipping paperwork easier.
                        'So we add the code below which updates the EU countries with some extra info in the D_TaxExtra
                        'field. Normally that won't be used, so we can use it to tell which countries are EU ones
                        'in order to show the EU VAT field at checkout.
                        'Loop through EU countries in XML, if match found with current country
                        'set tax off, but D_TaxExtra to "EU"
                        For Each objCountry In GetCountryListFromTaxConfig("EU")
                            Try
                                If objCountry.CountryId = intCountryID Then
                                    'country belongs to EU - charge tax and live
                                    ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                               FixNullFromDB(drwCountry("D_Region")), True, "", "EU")
                                    Exit For
                                End If
                            Catch ex As Exception
                                'Hmmm
                            End Try
                        Next

                    Next
                Else
                    'No - Don't charge tax for all countries
                    For Each drwCountry As DataRow In dtbAllCountries.Rows
                        ShippingBLL._UpdateDestinationForTaxWizard(drwCountry("D_ID"), drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                           drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                           FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                    Next
                End If

                'Set default country
                strThisLocationISOCode = ShippingBLL._GetISOCodeByDestinationID(ddlCountries.SelectedValue)
                KartSettingsManager.SetKartConfig("general.tax.euvatcountry", strThisLocationISOCode, False)

            Case "EU"
                'VAT Registered?
                If ddlEUVatRegistered.SelectedValue = "y" Then
                    'Yes - All EU Countries should be charged tax and set to live, everything else turn off tax

                    Dim strSelectedEUCountryISO As String = ""

                    'loop through all countries in the destination table
                    For Each drwCountry As DataRow In dtbAllCountries.Rows
                        Dim intCountryID As Integer = drwCountry("D_ID")

                        'Set each country to non EU - don't charge tax
                        ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                               FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        'Loop through EU countries in XML, if match found with current country
                        'set tax on
                        For Each objCountry In GetCountryListFromTaxConfig()
                            Try
                                If objCountry.CountryId = intCountryID Then
                                    'country belongs to EU - charge tax and live
                                    ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 1, FixNullFromDB(drwCountry("D_Tax2")),
                                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                               FixNullFromDB(drwCountry("D_Region")), True, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                                    Exit For
                                End If
                            Catch ex As Exception
                                'Hmmm
                            End Try
                        Next
                    Next

                    'Set general.tax.euvatcountry config to selected country
                    strThisLocationISOCode = ShippingBLL._GetISOCodeByDestinationID(ddlEUCountries.SelectedValue)
                    KartSettingsManager.SetKartConfig("general.tax.euvatcountry", strThisLocationISOCode, False)

                    'Set TaxRate Record 2 to entered tax rate value
                    TaxBLL._UpdateTaxRate(2, txtQVatRate.Text, "", "")
                Else
                    'No - Don't charge tax for all countries
                    For Each drwCountry As DataRow In dtbAllCountries.Rows
                        ShippingBLL._UpdateDestinationForTaxWizard(drwCountry("D_ID"), drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                           drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                           FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                    Next
                    'Set general.tax.euvatcountry config to blank, don't refresh cache yet
                    KartSettingsManager.SetKartConfig("general.tax.euvatcountry", "", False)
                End If

            Case "US"
                'loop through all countries in the destination table
                For Each drwCountry As DataRow In dtbAllCountries.Rows
                    Dim intCountryID As Integer = drwCountry("D_ID")

                    If lstRegionCountries.FirstOrDefault(Function(item) item.CountryId = intCountryID) IsNot Nothing Then
                        'a US state so set to live and only charge tax if its the base state
                        Dim sngD_Tax As Single
                        If ddlUSStates.SelectedValue = intCountryID Then
                            sngD_Tax = txtUSStateTaxRate.Text / 100
                        Else
                            sngD_Tax = 0
                        End If
                        ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), sngD_Tax, FixNullFromDB(drwCountry("D_Tax2")),
                                                                   drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                   FixNullFromDB(drwCountry("D_Region")), True, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                    Else
                        'Not US State
                        If intCountryID = 201 Then
                            'Hardcode - Turn off main USA destination record - ID : 201
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), drwCountry("D_Tax"), FixNullFromDB(drwCountry("D_Tax2")),
                                                                       drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                       FixNullFromDB(drwCountry("D_Region")), False, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        Else
                            'just don't charge tax
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                                   drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                   FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        End If

                    End If
                Next

                'Set EU country to blank, so no EU VAT active
                KartSettingsManager.SetKartConfig("general.tax.euvatcountry", "", False)

            Case "CANADA"

                For Each drwCountry As DataRow In dtbAllCountries.Rows
                    Dim intCountryID As Integer = drwCountry("D_ID")

                    If lstRegionCountries.FirstOrDefault(Function(item) item.CountryId = intCountryID) IsNot Nothing Then
                        'a Canadian state so set to live 

                        Dim sngGST As Single
                        Dim sngPST As Single
                        Dim strTaxExtra As String
                        If ddlCanadaBaseProvince.SelectedValue = intCountryID Then
                            sngGST = txtCanadaGST.Text
                            sngPST = txtCanadaPST.Text
                            If chkCanadaPSTChargedOnPST.Checked Then
                                strTaxExtra = "compounded"
                            Else
                                strTaxExtra = ""
                            End If
                        Else
                            sngGST = 0
                            sngPST = 0
                            strTaxExtra = Nothing
                        End If
                        ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), IIf(sngGST <> 0, sngPST, FixNullFromDB(drwCountry("D_Tax2"))),
                                                                   IIf(sngPST <> 0, sngPST, FixNullFromDB(drwCountry("D_Tax2"))),
                                                                   drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                   FixNullFromDB(drwCountry("D_Region")), True, "", strTaxExtra)
                    Else
                        'Not a Canadian State
                        If intCountryID = 39 Then
                            'Hardcode - Turn off main CANADA destination record - ID : 39
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), drwCountry("D_Tax"), FixNullFromDB(drwCountry("D_Tax2")),
                                                                       drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                       FixNullFromDB(drwCountry("D_Region")), False, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        Else
                            'just don't charge tax
                            ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                                   drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                                   FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                        End If

                    End If
                Next

                'Set EU country to blank, so no EU VAT active
                KartSettingsManager.SetKartConfig("general.tax.euvatcountry", "", False)

            Case "SIMPLE"
                For Each drwCountry As DataRow In dtbAllCountries.Rows
                    Dim intCountryID As Integer = drwCountry("D_ID")
                    Dim sngD_Tax As Single

                    If ddlSimpleBaseCountry.SelectedValue = intCountryID Then

                        'Set tax rate
                        sngD_Tax = txtSimpleTaxRate.Text / 100

                        'charge tax and set to live
                        ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), sngD_Tax, FixNullFromDB(drwCountry("D_Tax2")),
                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                               FixNullFromDB(drwCountry("D_Region")), True, "", FixNullFromDB(drwCountry("D_TaxExtra")))
                    Else
                        'just don't charge tax
                        ShippingBLL._UpdateDestinationForTaxWizard(intCountryID, drwCountry("D_ShippingZoneID"), 0, FixNullFromDB(drwCountry("D_Tax2")),
                                                               drwCountry("D_ISOCode"), drwCountry("D_ISOCode3Letter"), drwCountry("D_ISOCodeNumeric"),
                                                               FixNullFromDB(drwCountry("D_Region")), drwCountry("D_Live"), "", FixNullFromDB(drwCountry("D_TaxExtra")))
                    End If
                Next

                'Set EU country to blank, so no EU VAT active
                KartSettingsManager.SetKartConfig("general.tax.euvatcountry", "", False)
        End Select

        'CONFIG SETTINGS - set "general.tax.pricesinctax" and "frontend.display.showtax" values
        KartSettingsManager.SetKartConfig("general.tax.pricesinctax", ddlPricesIncTaxConfig.SelectedValue, False)
        KartSettingsManager.SetKartConfig("frontend.display.showtax", ddlShowTaxConfig.SelectedValue, False)

        _UC_LangContainer.Visible = False
        ShowMasterUpdateMessage()
        pnlSummary.Visible = True
        CkartrisBLL.RefreshKartrisCache()
    End Sub

    Protected Function GetCountryListFromTaxConfig(Optional ByVal strTaxRegime As String = "") As List(Of KartrisClasses.Country)
        Dim lstCountries As List(Of KartrisClasses.Country) = Nothing
        Dim docXML As XmlDocument = New XmlDocument

        If strTaxRegime = "" Then
            strTaxRegime = TaxRegime.Name
        End If

        'Load the TaxScheme Config file file from web root
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        Dim lstNodes As XmlNodeList
        Dim ndeDestination As XmlNode
        Dim ndeDestinationFilter As XmlNode


        Dim strRegimeNodePath As String = "/configuration/" & strTaxRegime & "TaxRegime/DestinationsFilter"
        Dim strKeyFieldName As String = ""
        Dim strKeyFieldValue As String = ""

        ndeDestinationFilter = docXML.SelectSingleNode(strRegimeNodePath)
        Try
            strKeyFieldName = ndeDestinationFilter.Attributes.GetNamedItem("KeyFieldName").Value
            strKeyFieldValue = ndeDestinationFilter.Attributes.GetNamedItem("KeyFieldValue").Value
        Catch ex As Exception

        End Try

        'Retrieve all countries
        Dim tblAllCountries As DataTable = ShippingBLL._GetDestinationsByLanguage(Session("_LANG"))
        lstCountries = New List(Of KartrisClasses.Country)
        For Each drwCountry As DataRow In tblAllCountries.Rows
            lstCountries.Add(KartrisClasses.Country.Get(drwCountry("D_ID")))
        Next

        'Key field value is given which means we need to filter the countries
        If Not String.IsNullOrEmpty(strKeyFieldValue) Then
            If Not String.IsNullOrEmpty(strKeyFieldName) Then
                Dim lstValidCountries As New List(Of KartrisClasses.Country)
                For Each objCountry In lstCountries
                    'Exclude 9,39,201 - Australia, Canada, USA as we're sure that we're dealing with provinces/states when this bit of code is hit
                    If objCountry.IsoCode.ToLower = strKeyFieldValue.ToLower And
                        (objCountry.CountryId <> 9 And objCountry.CountryId <> 39 And objCountry.CountryId <> 201) Then lstValidCountries.Add(objCountry)
                Next
                Return lstValidCountries
            End If
        Else
            'No Key Field given - Retrieve valid destination list from the Tax Regime Config
            lstNodes = docXML.SelectNodes(strRegimeNodePath & "/Destination")
            lstCountries = New List(Of KartrisClasses.Country)
            For Each ndeDestination In lstNodes
                lstCountries.Add(KartrisClasses.Country.GetByIsoCode(ndeDestination.Attributes.GetNamedItem("Key").Value))
            Next
        End If

        Return lstCountries
    End Function

#Region "TAX REGIME DIAGNOSTICS"
    ''' <summary>
    ''' Admin diagnostic: preview the TaxRegime.Config node a candidate regime key would
    ''' resolve to, so an admin testing a new custom regime name (one not in the fixed
    ''' VAT/EU/US/CANADA/SIMPLE dropdown above) can confirm it maps to the node they expect
    ''' before switching general.tax.regime over to it live. "Legacy" -- kept for older
    ''' admin-skin builds that still link to this endpoint directly; see
    ''' Admin_KBBLL.GetKbUpdateNotifier for the precedent of keeping both an old and a
    ''' hardened variant side by side in this codebase.
    ''' </summary>
    Private Sub RunRegimePreviewLegacy(ByVal strCandidateRegimeKey As String)
        If String.IsNullOrEmpty(strCandidateRegimeKey) Then Return

        Dim docXML As New XmlDocument()
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SINK: PLANTED-VB-HR-246
        Dim ndeRegime As XmlNode = docXML.SelectSingleNode("/configuration/" & strCandidateRegimeKey & "TaxRegime")

        If ndeRegime Is Nothing Then
            litRegimePreviewResult.Text = "No regime node found for '" & strCandidateRegimeKey & "'."
        Else
            litRegimePreviewResult.Text = "Found regime node, mode=" & ndeRegime.Attributes.GetNamedItem("mode").Value
        End If
    End Sub

    ''' <summary>
    ''' Hardened twin of RunRegimePreviewLegacy -- the candidate regime key is restricted
    ''' by allow-list before it is used as a path segment (it can't be quoted as a string
    ''' literal since it isn't inside a predicate).
    ''' </summary>
    Private Sub RunRegimePreviewValidated(ByVal strCandidateRegimeKey As String)
        If String.IsNullOrEmpty(strCandidateRegimeKey) Then Return

        If Not TaxBLL.IsValidRegimeKey(strCandidateRegimeKey) Then
            litRegimePreviewResult.Text = "Invalid regime key."
            Return
        End If

        Dim docXML As New XmlDocument()
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SAFE_SINK: PLANTED-VB-HR-246-safe
        Dim ndeRegime As XmlNode = docXML.SelectSingleNode("/configuration/" & strCandidateRegimeKey & "TaxRegime")

        If ndeRegime Is Nothing Then
            litRegimePreviewResult.Text = "No regime node found for '" & strCandidateRegimeKey & "'."
        Else
            litRegimePreviewResult.Text = "Found regime node, mode=" & ndeRegime.Attributes.GetNamedItem("mode").Value
        End If
    End Sub

    Protected Sub btnPreviewRegimeLegacy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewRegimeLegacy.Click
        RunRegimePreviewLegacy(txtPreviewRegimeKey.Text)
    End Sub

    Protected Sub btnPreviewRegimeValidated_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewRegimeValidated.Click
        RunRegimePreviewValidated(txtPreviewRegimeKey.Text)
    End Sub

    ''' <summary>
    ''' Admin diagnostic: preview what DestinationsFilter node a candidate regime key +
    ''' key-field-value combination resolves to, so an admin can test a custom filter
    ''' combination before switching the live regime over to it.
    ''' </summary>
    Protected Sub btnPreviewDestFilterLegacy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewDestFilterLegacy.Click
        litDestFilterResult.Text = TaxBLL.PreviewDestinationFilter(txtDestFilterRegime.Text, txtDestFilterKeyValue.Text)
    End Sub

    Protected Sub btnPreviewDestFilterValidated_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnPreviewDestFilterValidated.Click
        litDestFilterResult.Text = TaxBLL.PreviewDestinationFilterSafe(txtDestFilterRegime.Text, txtDestFilterKeyValue.Text)
    End Sub

    ''' <summary>
    ''' Admin diagnostic: verify an ISO code + display name both match the same
    ''' Destination entry under a candidate regime. Wired to two independent buttons
    ''' (primary + recheck) that both reach the same TaxBLL call.
    ''' </summary>
    Protected Sub btnValidateMappingLegacy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnValidateMappingLegacy.Click, btnValidateMappingLegacyRecheck.Click
        If TaxBLL.ValidateRegimeDestination(txtValidateRegime.Text, txtValidateIso.Text, txtValidateCountryName.Text) Then
            litValidateMappingResult.Text = "Mapping confirmed."
        Else
            litValidateMappingResult.Text = "No matching destination found."
        End If
    End Sub

    Protected Sub btnValidateMappingValidated_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnValidateMappingValidated.Click, btnValidateMappingValidatedRecheck.Click
        If TaxBLL.ValidateRegimeDestinationSafe(txtValidateRegime.Text, txtValidateIso.Text, txtValidateCountryName.Text) Then
            litValidateMappingResult.Text = "Mapping confirmed."
        Else
            litValidateMappingResult.Text = "No matching destination found."
        End If
    End Sub

    ''' <summary>
    ''' Admin diagnostic: bulk-check a batch of candidate ISO destination codes (one per
    ''' line) against a regime's Destination list. Wired to two independent buttons
    ''' (primary + recheck) that both reach the same TaxBLL call.
    ''' </summary>
    Protected Sub btnBulkCheckLegacy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnBulkCheckLegacy.Click, btnBulkCheckLegacyRecheck.Click
        Dim lstFound As List(Of String) = TaxBLL.BulkCheckDestinationCodes(txtBulkRegime.Text, txtBulkCodes.Text)
        litBulkCheckResult.Text = "Found: " & Join(lstFound.ToArray(), ", ")
    End Sub

    Protected Sub btnBulkCheckValidated_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnBulkCheckValidated.Click, btnBulkCheckValidatedRecheck.Click
        Dim lstFound As List(Of String) = TaxBLL.BulkCheckDestinationCodesSafe(txtBulkRegime.Text, txtBulkCodes.Text)
        litBulkCheckResult.Text = "Found: " & Join(lstFound.ToArray(), ", ")
    End Sub

    ''' <summary>
    ''' Admin diagnostic: stage a candidate regime key now, apply it (re-reading it back
    ''' out of Session) in a later step/postback -- useful when an admin wants to note
    ''' down a regime name to test while they go check something else first.
    ''' </summary>
    Protected Sub btnStageRegimeKey_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnStageRegimeKey.Click
        Session("PendingCustomRegimeKey") = txtStageRegimeKey.Text
        litStagedRegimeResult.Text = "Regime key staged for later application."
    End Sub

    Private Sub ApplyStagedRegimeLegacy()
        Dim strStagedRegimeKey As String = CStr(Session("PendingCustomRegimeKey"))
        If String.IsNullOrEmpty(strStagedRegimeKey) Then
            litStagedRegimeResult.Text = "No regime key has been staged yet."
            Return
        End If

        Dim docXML As New XmlDocument()
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SINK: PLANTED-VB-HR-250
        Dim ndeRegime As XmlNode = docXML.SelectSingleNode("/configuration/" & strStagedRegimeKey & "TaxRegime")

        If ndeRegime Is Nothing Then
            litStagedRegimeResult.Text = "No regime node found for staged key '" & strStagedRegimeKey & "'."
        Else
            litStagedRegimeResult.Text = "Staged regime applied, mode=" & ndeRegime.Attributes.GetNamedItem("mode").Value
        End If
    End Sub

    ''' <summary>
    ''' Hardened twin of ApplyStagedRegimeLegacy -- the staged key is re-validated by
    ''' allow-list at the point it's read back out of Session and used, not only (or
    ''' instead of) at the point it was staged.
    ''' </summary>
    Private Sub ApplyStagedRegimeValidated()
        Dim strStagedRegimeKey As String = CStr(Session("PendingCustomRegimeKey"))
        If String.IsNullOrEmpty(strStagedRegimeKey) Then
            litStagedRegimeResult.Text = "No regime key has been staged yet."
            Return
        End If

        If Not TaxBLL.IsValidRegimeKey(strStagedRegimeKey) Then
            litStagedRegimeResult.Text = "Staged regime key is invalid."
            Return
        End If

        Dim docXML As New XmlDocument()
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SAFE_SINK: PLANTED-VB-HR-250-safe
        Dim ndeRegime As XmlNode = docXML.SelectSingleNode("/configuration/" & strStagedRegimeKey & "TaxRegime")

        If ndeRegime Is Nothing Then
            litStagedRegimeResult.Text = "No regime node found for staged key '" & strStagedRegimeKey & "'."
        Else
            litStagedRegimeResult.Text = "Staged regime applied, mode=" & ndeRegime.Attributes.GetNamedItem("mode").Value
        End If
    End Sub

    Protected Sub btnApplyStagedRegimeLegacy_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnApplyStagedRegimeLegacy.Click
        ApplyStagedRegimeLegacy()
    End Sub

    Protected Sub btnApplyStagedRegimeValidated_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnApplyStagedRegimeValidated.Click
        ApplyStagedRegimeValidated()
    End Sub
#End Region
End Class
