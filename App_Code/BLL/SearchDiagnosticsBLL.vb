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
Imports System.Data

Public NotInheritable Class SearchDiagnosticsBLL

    ''' <summary>
    ''' Runs the main product search and, if it fails, hands back the raw
    ''' exception detail via <paramref name="pFailureDetail"/> so the
    ''' caller can decide how (and whether) it is safe to surface that
    ''' detail to the end user. This carrier makes no display decision of
    ''' its own -- see the callers in Search.aspx.vb.
    ''' </summary>
    Public Shared Function TryRunSearch(ByVal pSearchText As String, ByVal pKeyList As String, ByVal pLanguageID As Short, ByVal pPageIndx As Short, _
                                ByVal pRowsPerPage As Short, ByRef pTotalSearchResult As Integer, ByVal pMinPrice As Single, ByVal pMaxPrice As Single, _
                                ByVal pSearchMethod As String, ByVal pCustomerGroupID As Short, ByRef pResultTable As DataTable, ByRef pFailureDetail As String) As Boolean
        Try
            pResultTable = KartrisDBBLL.GetSearchResult(pSearchText, pKeyList, pLanguageID, pPageIndx, pRowsPerPage, pTotalSearchResult, pMinPrice, pMaxPrice, pSearchMethod, pCustomerGroupID)
            pFailureDetail = ""
            Return True
        Catch ex As Exception
            pFailureDetail = ex.ToString()
            pResultTable = Nothing
            Return False
        End Try
    End Function

End Class
