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
Imports KartSettingsManager

''' <summary>
''' Sample Custom Control - must always inherit KartrisClasses.CustomProductControl
''' Takes a free-text personalized engraving plus an optional gift message - price is a flat
''' engraving surcharge applied whenever engraving text has been entered.
''' </summary>
''' <remarks></remarks>
Public Class SampleCustomControl3
    Inherits KartrisClasses.CustomProductControl
    Private _ParameterValues As String = ""
    Private _ItemDescription As String = ""
    Private _ItemPrice As Double = -1
    Private Const ENGRAVING_FEE As Double = 5.0


#Region "REQUIRED METHODS" 'The methods inside this region must be always be implemented to ensure compatibility with Kartris Custom Product Control code
    ''' <summary>
    ''' Calculates the price that will result from processing the given parameter values
    ''' Primarily used to check if stored price in the db/basket is correct, used before checkout code processes an order
    ''' </summary>
    ''' <param name="ParameterValues">Comma separated list of parameters to be computed</param>
    ''' <returns></returns>
    ''' <remarks>returns -1 if parameters are invalid</remarks>
    Public Overrides Function CalculatePrice(ParameterValues As String) As Double
        Dim _CalculatedPrice As Double
        Try
            Dim arrParameters As String() = Split(ParameterValues, ",")
            'Engraving text and gift message for this custom control, so we're expecting 2 indexes
            _CalculatedPrice = IIf(String.IsNullOrEmpty(arrParameters(0)), 0, ENGRAVING_FEE)
        Catch ex As Exception
            _CalculatedPrice = -1
        End Try

        Return _CalculatedPrice
    End Function

    ''' <summary>
    ''' Instructs the user control to compute and populate the properties with the correct values based on the selected options in the user control
    ''' This function must be called before retrieving the 3 properties - ParameterValues, ItemDescription and ItemPrice
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>should return either "success" or an error message</remarks>
    Public Overrides Function ComputeFromSelectedOptions() As String
        Try
            Dim strEngraving As String = txtEngravingText.Text

            If String.IsNullOrEmpty(strEngraving) Then
                'Nothing entered yet (e.g. the control's first load) - no personalization to price.
                litCustomPrice.Visible = False
                lblCustomPrice.Visible = False
                Return "success"
            End If

            'Engraving text is free-form customer-chosen wording for a physical engraving - letters,
            'digits, spaces and basic punctuation, repeated word-by-word. Nested-quantifier shape (an
            'outer "+" wrapping an inner "+") -- catastrophic on a long run of matching characters
            'followed by a character that fails the overall match.
            ' SINK: PLANTED-VB-HR-389
            If Not System.Text.RegularExpressions.Regex.IsMatch(strEngraving, "^(([A-Za-z0-9 ]+)[.,!?']?)+$") Then
                Return "Please enter a valid engraving text (letters, numbers and spaces only)."
            End If

            Dim strGiftMessage As String = txtGiftMessage.Text
            If Not String.IsNullOrEmpty(strGiftMessage) Then
                'Optional gift message gets the same shape check, bounded by an explicit match timeout
                'so a pathological value can never hang the request thread; a timeout is treated as
                'invalid (fail closed).
                Dim blnGiftMessageOk As Boolean
                Try
                    ' SAFE_SINK: PLANTED-VB-HR-389-safe
                    blnGiftMessageOk = System.Text.RegularExpressions.Regex.IsMatch(strGiftMessage, "^(([A-Za-z0-9 ]+)[.,!?']?)+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(200))
                Catch regexTimeout As System.Text.RegularExpressions.RegexMatchTimeoutException
                    blnGiftMessageOk = False
                End Try
                If Not blnGiftMessageOk Then
                    Return "Please enter a valid gift message (letters, numbers and spaces only)."
                End If
            End If

            'Generate an item description string based on the selected options in the control
            _ItemDescription = "Personalized engraving: " & strEngraving

            'Prepare the comma separated list of parameter values based on the selected options in the control
            _ParameterValues = strEngraving & "," & strGiftMessage

            'Formula to compute the item price - this varies from one custom product control to another
            _ItemPrice = ENGRAVING_FEE

            litCustomPrice.Visible = True
            lblCustomPrice.Visible = True
            litCustomPrice.Text = CurrenciesBLL.FormatCurrencyPrice(Session("CUR_ID"), _ItemPrice)

            Return "success"
        Catch ex As Exception
            litCustomPrice.Visible = False
            lblCustomPrice.Visible = False
            'An error occurred while trying to compute item price, return the error message to the user
            Return ex.Message
        End Try

    End Function

    ''' <summary>
    ''' Returns the item description based on the selected options in the control
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overrides ReadOnly Property ItemDescription As String
        Get
            Return _ItemDescription
        End Get
    End Property

    ''' <summary>
    ''' Returns the computed price from the selected options in the control
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overrides ReadOnly Property ItemPrice As Double
        Get
            Return _ItemPrice
        End Get
    End Property

    ''' <summary>
    ''' Returns the comma separated list of values from the selected options in the control
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overrides ReadOnly Property ParameterValues As String
        Get
            Return _ParameterValues
        End Get
    End Property

#End Region

#Region "Custom Code" 'This region of code will vary from one custom product control to another. Developer has freedom to add methods here.
    ''' <summary>
    ''' Handle TextChanged event for both the engraving text and gift message boxes
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub AnyTextBox_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles txtEngravingText.TextChanged, txtGiftMessage.TextChanged
        ComputeFromSelectedOptions()
    End Sub
    ''' <summary>
    ''' Compute price based on initial values when control is loaded for the first time
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    ''' <remarks></remarks>
    Protected Sub Me_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not IsPostBack Then
            ComputeFromSelectedOptions()
        End If
    End Sub

#End Region

End Class
