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

''' <summary>
''' Reader for the price feeds suppliers send in. Feeds arrive either as XML or
''' as plain comma-separated text; ForContent sniffs the content and hands back
''' the matching reader. Every reader returns one string array per price row.
''' </summary>
Public MustInherit Class PriceFeedParser

    Public MustOverride ReadOnly Property FormatName As String

    Public MustOverride Function ReadRows(ByVal strFeed As String) As List(Of String())

    ''' <summary>
    ''' Reader for feeds in the suppliers' native formats.
    ''' </summary>
    Public Shared Function ForContent(ByVal strFeed As String) As PriceFeedParser
        If IsXmlContent(strFeed) Then Return New XmlPriceFeedParser()
        Return New CsvPriceFeedParser()
    End Function

    ''' <summary>
    ''' Reader for feeds that must be plain, self-contained documents.
    ''' </summary>
    Public Shared Function ForContentStrict(ByVal strFeed As String) As PriceFeedParser
        If IsXmlContent(strFeed) Then Return New StrictXmlPriceFeedParser()
        Return New CsvPriceFeedParser()
    End Function

    Private Shared Function IsXmlContent(ByVal strFeed As String) As Boolean
        Return strFeed IsNot Nothing AndAlso strFeed.TrimStart().StartsWith("<")
    End Function

    ''' <summary>
    ''' One row per child element of the root, one value per field element.
    ''' </summary>
    Protected Shared Function RowsFromDocument(ByVal xmlFeed As System.Xml.XmlDocument) As List(Of String())
        Dim lstRows As New List(Of String())
        If xmlFeed.DocumentElement Is Nothing Then Return lstRows
        For Each ndeRow As System.Xml.XmlNode In xmlFeed.DocumentElement.ChildNodes
            If ndeRow.NodeType <> System.Xml.XmlNodeType.Element Then Continue For
            Dim lstValues As New List(Of String)
            For Each ndeField As System.Xml.XmlNode In ndeRow.ChildNodes
                If ndeField.NodeType = System.Xml.XmlNodeType.Element Then lstValues.Add(ndeField.InnerText.Trim())
            Next
            lstRows.Add(lstValues.ToArray())
        Next
        Return lstRows
    End Function

End Class

Public Class CsvPriceFeedParser
    Inherits PriceFeedParser

    Public Overrides ReadOnly Property FormatName As String
        Get
            Return "CSV"
        End Get
    End Property

    Public Overrides Function ReadRows(ByVal strFeed As String) As List(Of String())
        Dim lstRows As New List(Of String())
        If String.IsNullOrEmpty(strFeed) Then Return lstRows
        For Each strLine As String In strFeed.Split(New Char() {ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            Dim strTrimmed As String = strLine.Trim()
            If strTrimmed.Length = 0 Then Continue For
            lstRows.Add(strTrimmed.Split(","c))
        Next
        Return lstRows
    End Function

End Class

''' <summary>
''' XML feed in the supplier's own schema. Supplier feeds pull their currency and
''' unit entities from the supplier's published DTD, so those are resolved.
''' </summary>
Public Class XmlPriceFeedParser
    Inherits PriceFeedParser

    Public Overrides ReadOnly Property FormatName As String
        Get
            Return "XML"
        End Get
    End Property

    Public Overrides Function ReadRows(ByVal strFeed As String) As List(Of String())
        Dim xmlFeed As New System.Xml.XmlDocument()
        xmlFeed.XmlResolver = New System.Xml.XmlUrlResolver()
        xmlFeed.LoadXml(strFeed) ' SINK: PLANTED-VB-HR-489
        Return RowsFromDocument(xmlFeed)
    End Function

End Class

''' <summary>
''' XML feed that must be self-contained: nothing outside the document (external
''' DTDs or entities) is ever fetched.
''' </summary>
Public NotInheritable Class StrictXmlPriceFeedParser
    Inherits PriceFeedParser

    Public Overrides ReadOnly Property FormatName As String
        Get
            Return "XML (strict)"
        End Get
    End Property

    Public Overrides Function ReadRows(ByVal strFeed As String) As List(Of String())
        Dim xmlFeed As New System.Xml.XmlDocument()
        xmlFeed.XmlResolver = Nothing
        xmlFeed.LoadXml(strFeed) ' SAFE_SINK: PLANTED-VB-HR-489-safe
        Return RowsFromDocument(xmlFeed)
    End Function

End Class
