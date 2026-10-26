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
''' Server-controlled registry of supplier onboarding documents (contracts, W9 scans, etc.)
''' that were actually produced by the real supplier-onboarding upload flow. Used only to
''' confirm that a document-removal request names a file some supplier legitimately has on
''' file -- a fixed, server-controlled mapping table, never a user-suppliable list.
''' </summary>
Public Class SupplierDocumentsBLL

    Private Shared ReadOnly RegisteredDocuments As New List(Of String) From {
        "contract_acmesupplies.pdf",
        "w9_acmesupplies.pdf",
        "contract_globalparts.pdf"
    }

    Public Shared Function IsRegisteredDocument(ByVal strDocName As String) As Boolean
        Return RegisteredDocuments.Contains(strDocName)
    End Function

End Class
