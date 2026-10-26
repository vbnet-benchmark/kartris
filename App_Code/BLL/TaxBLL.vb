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
Imports kartrisTaxDataTableAdapters
Imports CkartrisFormatErrors
Imports KartSettingsManager
Public Class TaxBLL

    Private Shared _Adptr As TaxRatesTblAdptr = Nothing
    Protected Shared ReadOnly Property Adptr() As TaxRatesTblAdptr
        Get
            _Adptr = New TaxRatesTblAdptr
            Return _Adptr
        End Get
    End Property

    Public Shared Function GetTaxRate(ByVal numTaxID As Byte) As Double
        Try
            Return CDbl((GetTaxRateFromCache.Select("T_ID=" & numTaxID))(0)("T_Taxrate"))
        Catch ex As Exception
        End Try
        Return 0
    End Function
    Public Shared Function GetClosestRate(ByVal numComputedRate As Double) As Double
        Try
            Return CDbl(Adptr.GetClosestRate(numComputedRate))
        Catch ex As Exception
        End Try
        Return Nothing
    End Function
    Public Shared Function GetQBTaxRefCode(ByVal strVersionCodeNumber As String) As String
        Try
            Return Adptr.GetQBTaxRefCodeOfVerCode(strVersionCodeNumber)
        Catch ex As Exception
        End Try
        Return ""
    End Function
    Public Shared Function GetQBTaxRefCode(ByVal intTaxID As Integer) As String
        Try
            Return CStr((GetTaxRateFromCache.Select("T_ID=" & intTaxID))(0)("T_QBRefCode"))
        Catch ex As Exception
        End Try
        Return ""
    End Function
    Public Shared Function GetQBTaxRefCode(ByVal dblTaxRate As Double) As String
        Try
            Return CStr((GetTaxRateFromCache.Select("T_TaxRate=" & dblTaxRate))(0)("T_QBRefCode"))
        Catch ex As Exception
        End Try
        Return ""
    End Function
    Protected Friend Shared Function _GetTaxRatesForCache() As DataTable
        Return Adptr._GetTaxRates()
    End Function

    Public Shared Function _UpdateTaxRate(ByVal intTaxID As Byte, ByVal snglTaxRate As Single, ByVal strQBRefCode As String, ByVal strMsg As String) As Boolean
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateTax As SqlCommand = sqlConn.CreateCommand
            cmdUpdateTax.CommandText = "_spKartrisTaxRates_Update"
            Dim savePoint As SqlTransaction = Nothing
            cmdUpdateTax.CommandType = CommandType.StoredProcedure
            Try
                cmdUpdateTax.Parameters.AddWithValue("@T_ID", intTaxID)
                cmdUpdateTax.Parameters.AddWithValue("@T_Taxrate", snglTaxRate)
                cmdUpdateTax.Parameters.AddWithValue("@T_QBRefCode", strQBRefCode)

                sqlConn.Open()
                savePoint = sqlConn.BeginTransaction()
                cmdUpdateTax.Transaction = savePoint

                cmdUpdateTax.ExecuteNonQuery()
                savePoint.Commit()
                Return True
            Catch ex As Exception
                If Not savePoint Is Nothing Then savePoint.Rollback()
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close() : savePoint.Dispose()
            End Try
        End Using
        Return False
    End Function

#Region "TAX REGIME DIAGNOSTICS - XPath helpers"
    ''' <summary>
    ''' Builds an XPath 1.0 string literal for strValue that is safe to splice into a
    ''' predicate (e.g. [@Attr='value']). Chooses whichever quote character strValue does
    ''' not contain; if it contains both, falls back to concat(), splitting on every
    ''' single-quote and rejoining the pieces with a '-literal between them. XPath 1.0 has
    ''' no parameterized-query API (no SqlParameter equivalent for XPathNavigator.Select /
    ''' XmlDocument.SelectNodes), so this is the standard .NET-idiomatic way to neutralize
    ''' CWE-643 for a value used inside a predicate.
    ''' </summary>
    Public Shared Function XPathStringLiteral(ByVal strValue As String) As String
        If strValue Is Nothing Then strValue = ""
        If InStr(strValue, "'") = 0 Then
            Return "'" & strValue & "'"
        ElseIf InStr(strValue, """") = 0 Then
            Return """" & strValue & """"
        Else
            Dim arrParts() As String = Split(strValue, "'")
            Dim strConcatArgs As String = ""
            For i As Integer = 0 To arrParts.Length - 1
                If i > 0 Then strConcatArgs &= ", ""'"", "
                strConcatArgs &= "'" & arrParts(i) & "'"
            Next
            Return "concat(" & strConcatArgs & ")"
        End If
    End Function

    ''' <summary>
    ''' Strict allow-list for a candidate TaxRegime.Config node-name key (e.g. "US", "EU",
    ''' or a custom regime name an admin is testing before switching the live
    ''' general.tax.regime setting over to it). The key is used as a raw XML element-name
    ''' path segment, not inside a predicate, so it can't be quoted as a string literal --
    ''' it must instead be restricted to characters that are safe path-segment content.
    ''' Rejects anything containing XPath predicate/operator syntax.
    ''' </summary>
    Public Shared Function IsValidRegimeKey(ByVal strKey As String) As Boolean
        If String.IsNullOrEmpty(strKey) OrElse strKey.Length > 40 Then Return False
        For Each chrThis As Char In strKey
            If Not (Char.IsLetterOrDigit(chrThis) OrElse chrThis = "_"c) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' Strict allow-list for an ISO-country-code-shaped value used inside a Destination
    ''' lookup predicate.
    ''' </summary>
    Public Shared Function IsValidIsoCode(ByVal strIso As String) As Boolean
        If String.IsNullOrEmpty(strIso) OrElse strIso.Length > 3 Then Return False
        For Each chrThis As Char In strIso
            If Not Char.IsLetter(chrThis) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' Admin diagnostic: preview what DestinationsFilter node a candidate regime key +
    ''' key-field-value combination would resolve to in TaxRegime.Config, so an admin can
    ''' test a custom regime/filter combination before switching the live
    ''' general.tax.regime setting over to it. "Legacy" -- kept for older admin-skin builds
    ''' that still link to this endpoint directly. Vulnerable: splices strKeyFieldValue
    ''' straight into the predicate via String.Format with no escaping.
    ''' </summary>
    Public Shared Function PreviewDestinationFilter(ByVal strRegime As String, ByVal strKeyFieldValue As String) As String
        If Not IsValidRegimeKey(strRegime) Then Return "Invalid regime key."

        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        Dim strPath As String = String.Format("/configuration/{0}TaxRegime/DestinationsFilter[@KeyFieldValue='{1}']", strRegime, strKeyFieldValue)
        ' SINK: PLANTED-VB-HR-247
        Dim ndeFilter As XmlNode = docXML.SelectSingleNode(strPath)

        If ndeFilter Is Nothing Then Return "No matching DestinationsFilter found."
        Return "Matched filter, KeyFieldName=" & ndeFilter.Attributes.GetNamedItem("KeyFieldName").Value
    End Function

    ''' <summary>
    ''' Hardened twin of PreviewDestinationFilter -- strRegime is restricted by allow-list
    ''' (it's a raw path-segment, not quotable), and strKeyFieldValue is escaped as an
    ''' XPath string literal before being spliced into the predicate.
    ''' </summary>
    Public Shared Function PreviewDestinationFilterSafe(ByVal strRegime As String, ByVal strKeyFieldValue As String) As String
        If Not IsValidRegimeKey(strRegime) Then Return "Invalid regime key."

        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        Dim strPath As String = String.Format("/configuration/{0}TaxRegime/DestinationsFilter[@KeyFieldValue={1}]", strRegime, XPathStringLiteral(strKeyFieldValue))
        ' SAFE_SINK: PLANTED-VB-HR-247-safe
        Dim ndeFilter As XmlNode = docXML.SelectSingleNode(strPath)

        If ndeFilter Is Nothing Then Return "No matching DestinationsFilter found."
        Return "Matched filter, KeyFieldName=" & ndeFilter.Attributes.GetNamedItem("KeyFieldName").Value
    End Function

    ''' <summary>
    ''' Admin diagnostic: verify that a given ISO country code + display name both match
    ''' the same Destination entry under a given candidate regime, combining both
    ''' admin-supplied values into one 'and'-joined predicate (mirroring the classic
    ''' dual-field login-bypass shape for this CWE). Vulnerable: neither value is
    ''' escaped/validated before being spliced into the predicate.
    ''' </summary>
    Public Shared Function ValidateRegimeDestination(ByVal strRegime As String, ByVal strIsoCode As String, ByVal strCountryName As String) As Boolean
        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SINK: PLANTED-VB-HR-248
        Dim ndeDestination As XmlNode = docXML.SelectSingleNode("/configuration/" & strRegime & "TaxRegime/DestinationsFilter/Destination[@Key='" & strIsoCode & "' and @Name='" & strCountryName & "']")
        Return ndeDestination IsNot Nothing
    End Function

    ''' <summary>
    ''' Hardened twin of ValidateRegimeDestination -- the real failure mode to avoid here
    ''' is sanitizing only one of the two combined values. strRegime and strIsoCode both
    ''' have a narrow legitimate domain so they're allow-listed; strCountryName is a free
    ''' text display name (can legitimately contain spaces/apostrophes, e.g. "Cote
    ''' d'Ivoire") so it is escaped as an XPath string literal instead.
    ''' </summary>
    Public Shared Function ValidateRegimeDestinationSafe(ByVal strRegime As String, ByVal strIsoCode As String, ByVal strCountryName As String) As Boolean
        If Not IsValidRegimeKey(strRegime) Then Return False
        If Not IsValidIsoCode(strIsoCode) Then Return False

        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        ' SAFE_SINK: PLANTED-VB-HR-248-safe
        Dim ndeDestination As XmlNode = docXML.SelectSingleNode("/configuration/" & strRegime & "TaxRegime/DestinationsFilter/Destination[@Key='" & strIsoCode & "' and @Name=" & XPathStringLiteral(strCountryName) & "]")
        Return ndeDestination IsNot Nothing
    End Function

    ''' <summary>
    ''' Admin diagnostic: bulk-check a batch of candidate ISO destination codes (one per
    ''' line) against the active regime's Destination list in a single query, OR-joining
    ''' one predicate clause per code. No genuine polymorphism (Interface/MustInherit/
    ''' Overridable type hierarchy) exists near this tax/destination code -- confirmed by
    ''' grepping App_Code/BLL and the shipping plugins -- so this tier is substituted with
    ''' a loop/batch shape instead, per this round's planting convention. Vulnerable: each
    ''' code is spliced into its own clause unescaped.
    ''' </summary>
    Public Shared Function BulkCheckDestinationCodes(ByVal strRegime As String, ByVal strCandidateCodes As String) As List(Of String)
        Dim lstFound As New List(Of String)
        If Not IsValidRegimeKey(strRegime) Then Return lstFound
        If strCandidateCodes Is Nothing Then Return lstFound

        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        Dim arrCodes() As String = Split(strCandidateCodes, vbCrLf)
        Dim lstClauses As New List(Of String)
        For Each strCode As String In arrCodes
            strCode = Trim(strCode)
            If strCode <> "" Then lstClauses.Add("@Key='" & strCode & "'")
        Next
        If lstClauses.Count = 0 Then Return lstFound

        Dim strPath As String = "/configuration/" & strRegime & "TaxRegime/DestinationsFilter/Destination[" & Join(lstClauses.ToArray(), " or ") & "]"
        ' SINK: PLANTED-VB-HR-249
        Dim lstNodes As XmlNodeList = docXML.SelectNodes(strPath)
        For Each ndeFound As XmlNode In lstNodes
            lstFound.Add(ndeFound.Attributes.GetNamedItem("Key").Value)
        Next
        Return lstFound
    End Function

    ''' <summary>
    ''' Hardened twin of BulkCheckDestinationCodes -- each candidate code is allow-list
    ''' validated before being added as a clause; invalid codes are silently skipped
    ''' rather than ever reaching the query.
    ''' </summary>
    Public Shared Function BulkCheckDestinationCodesSafe(ByVal strRegime As String, ByVal strCandidateCodes As String) As List(Of String)
        Dim lstFound As New List(Of String)
        If Not IsValidRegimeKey(strRegime) Then Return lstFound
        If strCandidateCodes Is Nothing Then Return lstFound

        Dim docXML As XmlDocument = New XmlDocument
        docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

        Dim arrCodes() As String = Split(strCandidateCodes, vbCrLf)
        Dim lstClauses As New List(Of String)
        For Each strCode As String In arrCodes
            strCode = Trim(strCode)
            If strCode <> "" AndAlso IsValidIsoCode(strCode) Then lstClauses.Add("@Key='" & strCode & "'")
        Next
        If lstClauses.Count = 0 Then Return lstFound

        Dim strPath As String = "/configuration/" & strRegime & "TaxRegime/DestinationsFilter/Destination[" & Join(lstClauses.ToArray(), " or ") & "]"
        ' SAFE_SINK: PLANTED-VB-HR-249-safe
        Dim lstNodes As XmlNodeList = docXML.SelectNodes(strPath)
        For Each ndeFound As XmlNode In lstNodes
            lstFound.Add(ndeFound.Attributes.GetNamedItem("Key").Value)
        Next
        Return lstFound
    End Function
#End Region
End Class

Public Class TaxRegime
    Private Shared _Name As String
    Private Shared _DTax_Type As String
    Private Shared _DTax2_Type As String
    Private Shared _VTax_Type As String
    Private Shared _VTax2_Type As String
    Private Shared _Formulas As List(Of KartrisNameValuePair)
    Private Shared _DTaxNames As List(Of KartrisNameValuePair)
    Private Shared _DTax2Names As List(Of KartrisNameValuePair)
    Private Shared _CalculatePerItem As Boolean
    Private Shared _FormulaNameToFind As String
    ''' <summary>
    ''' Tax Regime Name
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property Name As String
        Get
            Return _Name
        End Get
    End Property
    Public Shared ReadOnly Property DTax_Type As String
        Get
            Return _DTax_Type
        End Get
    End Property
    Public Shared ReadOnly Property DTax_Type2 As String
        Get
            Return _DTax2_Type
        End Get
    End Property
    Public Shared ReadOnly Property VTax_Type As String
        Get
            Return _VTax_Type
        End Get
    End Property
    Public Shared ReadOnly Property VTax_Type2 As String
        Get
            Return _VTax2_Type
        End Get
    End Property
    ''' <summary>
    ''' Holds the list of Tax Rate Calculation Formulas for different D_TaxExtra values
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property Formulas As List(Of KartrisNameValuePair)
        Get
            Return _Formulas
        End Get
    End Property
    ''' <summary>
    ''' Holds the list of D_Tax Names for different languages
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property DTaxNames As List(Of KartrisNameValuePair)
        Get
            Return _DTaxNames
        End Get
    End Property
    ''' <summary>
    ''' Holds the list of D_Tax2 Names for different languages
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property DTax2Names As List(Of KartrisNameValuePair)
        Get
            Return _DTax2Names
        End Get
    End Property
    ''' <summary>
    ''' Determines if Regime calculate tax per item or against whole order value
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared ReadOnly Property CalculatePerItem As Boolean
        Get
            Return _CalculatePerItem
        End Get
    End Property
    Public Shared Sub LoadTaxConfigXML()
        Try
            Dim docXML As XmlDocument = New XmlDocument
            'Load the TaxScheme Config file file from web root
            docXML.Load(HttpContext.Current.Server.MapPath("~/TaxRegime.Config"))

            Dim lstNodes As XmlNodeList
            Dim ndeTaxRegime As XmlNode

            _Name = ConfigurationManager.AppSettings("TaxRegime").ToUpper
            Dim strRegimeNodePath As String = "/configuration/" & _Name & "TaxRegime"
            ndeTaxRegime = docXML.SelectSingleNode(strRegimeNodePath)

            'Get the regime's calculation mode - peritem(tax is calculated per item), perorder (tax is calculated against the whole order value)
            If ndeTaxRegime.Attributes.GetNamedItem("mode").Value = "PerItem" Then _CalculatePerItem = True Else _CalculatePerItem = False

            'get tax fields types *boolean or double(rate)* -> D_Tax, D_Tax2, V_Tax, V_Tax2
            ndeTaxRegime = docXML.SelectSingleNode(strRegimeNodePath & "/TaxFields/D_Tax")
            _DTax_Type = ndeTaxRegime.Attributes.GetNamedItem("type").Value

            ndeTaxRegime = docXML.SelectSingleNode(strRegimeNodePath & "/TaxFields/D_Tax2")
            If ndeTaxRegime IsNot Nothing Then
                _DTax2_Type = ndeTaxRegime.Attributes.GetNamedItem("type").Value
            Else
                _DTax2_Type = ""
            End If

            ndeTaxRegime = docXML.SelectSingleNode(strRegimeNodePath & "/TaxFields/V_Tax")
            If ndeTaxRegime IsNot Nothing Then _VTax_Type = ndeTaxRegime.Attributes.GetNamedItem("type").Value Else _VTax_Type = ""

            ndeTaxRegime = docXML.SelectSingleNode(strRegimeNodePath & "/TaxFields/V_Tax2")
            If ndeTaxRegime IsNot Nothing Then _VTax2_Type = ndeTaxRegime.Attributes.GetNamedItem("type").Value Else _VTax2_Type = ""

            'loop through TaxRateCalculation nodes and add them all to the Formulas property
            lstNodes = docXML.SelectNodes(strRegimeNodePath & "/TaxRateCalculation")
            _Formulas = New List(Of KartrisNameValuePair)
            For Each ndeTaxRegime In lstNodes
                Dim taxEquation As New KartrisNameValuePair
                'We only have a single formula (taxratecalculation node) if type attribute is empty so just name it as default
                If ndeTaxRegime.Attributes.GetNamedItem("type") IsNot Nothing Then taxEquation.Name = ndeTaxRegime.Attributes.GetNamedItem("type").Value Else taxEquation.Name = "default"
                taxEquation.Value = ndeTaxRegime.Attributes.GetNamedItem("value").Value

                _Formulas.Add(taxEquation)
            Next

        Catch ex As Exception
            'We want to write a log entry.
            CkartrisFormatErrors.LogError(ex.Message & vbCrLf & "This suggests the TaxRegime.config file on the root of the site is either corrupted, or permissions prevent it being read.")
        End Try


    End Sub

    ''' <summary>
    ''' Calculate the applicable tax rate
    ''' </summary>
    ''' <param name="V_Tax">Product Version Tax. The tax that is defined when creating or editting a single product version</param>
    ''' <param name="V_Tax2">Product Version Tax 2. Same use as V_Tax but used in countries with a 2 tier tax system (e.g. Canada)</param>
    ''' <param name="D_Tax">Destination Country Tax. The tax applicable in the destination country</param>
    ''' <param name="D_Tax2">Destination Country Tax 2. Same as D_Tax but used in countries with a 2 tier tax system (e.g. Canada)</param>
    ''' <param name="D_TaxExtra">The Tax Rate Calculation Type Name. View TaxRegime.Default and then for the country of interest look 
    ''' at the TaxRateCalculation node. If there is no node name then there is only one calculation, if there are multiple nodes then 
    ''' the name is used here to descriminate between them. From v3.0001, we also use to mark EU countries</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function CalculateTaxRate(ByVal V_Tax As Double, ByVal V_Tax2 As Double, ByVal D_Tax As Double,
                                         ByVal D_Tax2 As Double, ByVal D_TaxExtra As String) As Double

        Dim tblCalculate As New DataTable
        Dim formula As String = "({0})"
        Dim strCorrectFormula As String

        If Not String.IsNullOrEmpty(D_TaxExtra) Then
            If D_TaxExtra <> "EU" Then 'We now use this field to mark EU countries
                'D_TaxExtra has a value so must match it to the TaxRateCalculation Name to get the correct Formula
                _FormulaNameToFind = D_TaxExtra
                Dim result As KartrisNameValuePair = _Formulas.Find(AddressOf FindName)
                If result IsNot Nothing Then
                    strCorrectFormula = result.Value
                Else
                    Throw New Exception("Invalid D_TaxExtra Value: " & D_TaxExtra)
                End If
            Else
                'D_TaxExtra is empty so just get the "default" Formula
                strCorrectFormula = _Formulas.Item(0).Value
            End If

        Else
            'D_TaxExtra is empty so just get the "default" Formula
            strCorrectFormula = _Formulas.Item(0).Value
        End If


        'Boolean tax fields should only be either 1 or 0 
        If _DTax_Type.ToLower = "boolean" AndAlso D_Tax > 0 Then D_Tax = 1
        If _DTax2_Type.ToLower = "boolean" AndAlso D_Tax2 > 0 Then D_Tax2 = 1

        'V_Tax fields should always be converted to rate/percentage
        If _VTax_Type.ToLower = "boolean" AndAlso V_Tax > 0 Then V_Tax = 1 Else V_Tax = V_Tax / 100
        If _VTax2_Type.ToLower = "boolean" AndAlso V_Tax2 > 0 Then V_Tax2 = 1 Else V_Tax2 = V_Tax2 / 100

        'Replace the variables in the TaxRate Formula with the actual values
        strCorrectFormula = Replace(strCorrectFormula, "D_Tax2", D_Tax2)
        strCorrectFormula = Replace(strCorrectFormula, "V_Tax2", V_Tax2)
        strCorrectFormula = Replace(strCorrectFormula, "D_Tax", D_Tax)
        strCorrectFormula = Replace(strCorrectFormula, "V_Tax", V_Tax)


        Dim expr As String = String.Format(formula, strCorrectFormula)
        expr = expr.Replace(",", GetCurrenciesFromCache().Select("CUR_ID=" & HttpContext.Current.Session("CUR_ID"))(0)("CUR_DecimalPoint"))
        'this line does the actual computation
        Return CDec(tblCalculate.Compute(expr, ""))
    End Function

#Region "UTILITY CODE"
    ''' <summary>
    ''' generic Name Value Pair class
    ''' </summary>
    ''' <remarks></remarks>
    Class KartrisNameValuePair
        Public Property Name As String
        Public Property Value As String
    End Class
    ''' <summary>
    ''' Function to find a value in the Name property from a list
    ''' </summary>
    ''' <param name="Formula"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Shared Function FindName(ByVal Formula As KartrisNameValuePair) As Boolean
        If Formula.Name = _FormulaNameToFind Then
            Return True
        Else
            Return False
        End If
    End Function
#End Region
End Class