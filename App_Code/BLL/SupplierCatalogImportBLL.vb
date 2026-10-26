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
Imports System.Xml

''' <summary>
''' Supplier catalog import: turns a supplier's XML product catalog into a
''' preview table (code, name, price) so the admin can check it before the
''' products are created.
''' </summary>
Public NotInheritable Class SupplierCatalogImportBLL

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Parses a catalog in the supplier's own format. Supplier catalogs declare
    ''' their units, currencies and shared product fragments as entities in the
    ''' supplier's published DTD, so external definitions are resolved.
    ''' </summary>
    Public Shared Function PreviewCatalog(ByVal strCatalogXml As String) As DataTable
        Dim xmlCatalog As New XmlDocument()
        xmlCatalog.XmlResolver = New XmlUrlResolver()
        xmlCatalog.LoadXml(strCatalogXml) ' SINK: PLANTED-VB-HR-488
        Return CatalogToTable(xmlCatalog)
    End Function

    ''' <summary>
    ''' Parses a catalog that has already been normalised to plain XML
    ''' (no DTD, no external references).
    ''' </summary>
    Public Shared Function PreviewCatalogStrict(ByVal strCatalogXml As String) As DataTable
        Dim xmlCatalog As New XmlDocument()
        xmlCatalog.XmlResolver = Nothing
        xmlCatalog.LoadXml(strCatalogXml) ' SAFE_SINK: PLANTED-VB-HR-488-safe
        Return CatalogToTable(xmlCatalog)
    End Function

    Private Shared Function CatalogToTable(ByVal xmlCatalog As XmlDocument) As DataTable
        Dim tblPreview As New DataTable
        tblPreview.Columns.Add(New DataColumn("CodeNumber", GetType(String)))
        tblPreview.Columns.Add(New DataColumn("Name", GetType(String)))
        tblPreview.Columns.Add(New DataColumn("Price", GetType(String)))
        For Each ndeProduct As XmlNode In xmlCatalog.SelectNodes("//Product")
            Dim ndeCode As XmlNode = ndeProduct.SelectSingleNode("Code")
            Dim ndeName As XmlNode = ndeProduct.SelectSingleNode("Name")
            Dim ndePrice As XmlNode = ndeProduct.SelectSingleNode("Price")
            tblPreview.Rows.Add(If(ndeCode Is Nothing, "", ndeCode.InnerText),
                                If(ndeName Is Nothing, "", ndeName.InnerText),
                                If(ndePrice Is Nothing, "", ndePrice.InnerText))
        Next
        Return tblPreview
    End Function

End Class
