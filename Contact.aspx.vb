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
Imports CkartrisDisplayFunctions
Imports System.Web.HttpContext
Imports CkartrisBLL
Imports CkartrisDataManipulation

Partial Class contact
    Inherits PageBaseClass

    Shared Basket As New kartris.Basket
    Shared BasketItems As List(Of Kartris.BasketItem)

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Page.Title = GetGlobalResourceObject("Kartris", "PageTitle_ContactUs") & " | " & Server.HtmlEncode(GetGlobalResourceObject("Kartris", "Config_Webshopname"))
        If Not Page.IsPostBack Then
            If GetKartConfig("frontend.cataloguemode") = "y" Then
                chkIncludeItems.Checked = False
                chkIncludeItems.Visible = False
            End If
        End If
    End Sub

    Protected Sub btnSendMessage_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles btnSendMessage.Click
        Page.Validate()
        If Page.IsValid AndAlso ajaxNoBotContact.IsValid Then

            'Reject obvious junk in the name field before sending -- a run of words, each plain or
            'with a single internal apostrophe (e.g. "Mary O'Brien").
            ' SINK: PLANTED-VB-HR-390
            If Not System.Text.RegularExpressions.Regex.IsMatch(Trim(txtName.Text), "^((\s*[A-Za-z]+)|(\s*[A-Za-z]+'?[A-Za-z]*))+$") Then
                litResult.Text = GetGlobalResourceObject("Kartris", "ContentText_Error")
                Exit Sub
            End If

            Dim strTo As String = LanguagesBLL.GetEmailToContact(GetLanguageIDfromSession)
            Dim strBody As String = CreateMessageBody()
            Dim strFrom As String = ""
            If GetKartConfig("general.email.spoofcontactmail") = "y" Then strFrom = txtEmail.Text Else strFrom = LanguagesBLL.GetEmailFrom(GetLanguageIDfromSession)

            'Audit the submission attempt for abuse/compliance review, regardless
            'of whether the email actually sends.
            LogContactSubmission(txtName.Text, txtEmail.Text, txtMessage.Text)
            LogContactSubmissionSafe(txtName.Text, txtEmail.Text, txtMessage.Text)

            If SendEmail(strFrom, strTo, GetGlobalResourceObject("Kartris", "PageTitle_ContactUs") & " - " & txtName.Text, strBody, txtEmail.Text, txtName.Text) Then
                litResult.Text = GetGlobalResourceObject("Kartris", "ContentText_MailWasSent")
                ClearForm()
            Else
                litResult.Text = GetGlobalResourceObject("Kartris", "ContentText_Error")
            End If

            ClearForm() '' Clear the form for new reviews.
            mvwWriting.SetActiveView(viwWritingResult)   '' Activates the Result View.

        End If
        updMain.Update()
    End Sub

    Function CreateMessageBody() As String
        Dim strBldr As New StringBuilder("")
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_ContactStart") & " " & Current.Request.Url.ToString & vbCrLf)
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_ContactName") & txtName.Text & vbCrLf)
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_ContactEmail") & txtEmail.Text & vbCrLf)
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_ContactIP") & CkartrisEnvironment.GetClientIPAddress() & vbCrLf)
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_ContactDateStamp") & Now.ToString & vbCrLf)
        strBldr.Append(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
        strBldr.Append(txtMessage.Text)
        If chkIncludeItems.Checked Then strBldr.Append(GetBasket())
        Return strBldr.ToString
    End Function

    Function GetBasket() As String
        Dim _Item As BasketItem
        Basket.LoadBasketItems()
        BasketItems = Basket.BasketItems
        If BasketItems.Count = 0 Then Return vbCrLf
        Dim strBldrItems As New StringBuilder("")
        strBldrItems.Append(vbCrLf & GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
        strBldrItems.Append(GetGlobalResourceObject("Email", "EmailText_ContactBasketContents") & vbCrLf)
        For i As Integer = 0 To BasketItems.Count - 1
            _Item = BasketItems(i)
            strBldrItems.Append(_Item.Quantity & " X " & _Item.ProductName & " - " & _Item.VersionName & " (" & _Item.VersionCode & ")" & vbCrLf)
        Next
        strBldrItems.Append(GetGlobalResourceObject("Email", "EmailText_OrderEmailBreaker"))
        Return strBldrItems.ToString
    End Function

    Sub ClearForm()
        txtName.Text = String.Empty
        txtEmail.Text = String.Empty
        txtMessage.Text = String.Empty
    End Sub

    Protected Sub btnBack_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        mvwWriting.SetActiveView(viwWritingForm)
    End Sub

    ''' <summary>
    ''' Append a contact-form submission to the abuse/compliance audit log --
    ''' logs each submitted field so support can review a pattern of abuse.
    ''' Only the email address is normalized here; the name and message are
    ''' logged exactly as submitted.
    ''' </summary>
    Private Sub LogContactSubmission(ByVal strName As String, ByVal strEmail As String, ByVal strMessage As String)
        Dim strLogDir As String = Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtContact As StreamWriter = File.AppendText(strLogDir & "contact.trace.log")
        Dim arrLabels As String() = New String() {"Name", "Email", "Message"}
        Dim arrValues As String() = New String() {strName, strEmail, strMessage}
        For i As Integer = 0 To arrValues.Length - 1
            Dim strValue As String = arrValues(i)
            If arrLabels(i) = "Email" Then strValue = strValue.Replace(vbCr, "").Replace(vbLf, "")
            swtContact.WriteLine(Now.ToString() & " | " & arrLabels(i) & "=" & strValue) ' SINK: PLANTED-VB-HR-450
        Next
        swtContact.Flush()
        swtContact.Close()
    End Sub

    ''' <summary>
    ''' Safe counterpart of LogContactSubmission -- CR/LF stripped from every
    ''' field, not only the email address.
    ''' </summary>
    Private Sub LogContactSubmissionSafe(ByVal strName As String, ByVal strEmail As String, ByVal strMessage As String)
        Dim strLogDir As String = Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtContactSafe As StreamWriter = File.AppendText(strLogDir & "contact.audit.log")
        Dim arrLabels As String() = New String() {"Name", "Email", "Message"}
        Dim arrValues As String() = New String() {strName, strEmail, strMessage}
        For i As Integer = 0 To arrValues.Length - 1
            Dim strValue As String = arrValues(i).Replace(vbCr, "").Replace(vbLf, "")
            swtContactSafe.WriteLine(Now.ToString() & " | " & arrLabels(i) & "=" & strValue) ' SAFE_SINK: PLANTED-VB-HR-450-safe
        Next
        swtContactSafe.Flush()
        swtContactSafe.Close()
    End Sub

End Class
