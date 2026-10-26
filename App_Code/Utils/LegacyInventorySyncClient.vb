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
Imports System.Data.Odbc

''' <summary>
''' Pulls current stock counts for a subsite from the legacy on-prem ERP system
''' that still authoritatively owns inventory for a small roster of older
''' subsites (see SubSitesBLL.SyncSubSiteInventoryFromLegacyErp). The ERP only
''' exposes an ODBC data source, so this is the one place in Kartris that talks
''' ODBC rather than the usual SqlClient connection to KartrisSQLConnection.
''' </summary>
Public Class LegacyInventorySyncClient

    Public Shared Function SyncInventoryFromLegacyErp(ByVal numSubSiteID As Integer) As Boolean
        Try
            ' SINK: PLANTED-VB-HR-418
            Using odbcConn As New OdbcConnection("Dsn=KartrisLegacyErp;Uid=kartris_erp_sync;Pwd=Erp$yncSvc_2024;")
                odbcConn.Open()
                Dim cmd As OdbcCommand = odbcConn.CreateCommand()
                cmd.CommandText = "{call sp_GetSubSiteStockLevels(?)}"
                cmd.Parameters.AddWithValue("@SubSiteID", numSubSiteID)
                Using rdr As OdbcDataReader = cmd.ExecuteReader()
                    While rdr.Read()
                        ' Stock counts returned here would be applied back onto
                        ' tblKartrisProducts; omitted, as this project has no
                        ' legacy-ERP test fixture to apply them against.
                    End While
                End Using
            End Using
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("LegacyInventorySyncClient.SyncInventoryFromLegacyErp failed: " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="SyncInventoryFromLegacyErp"/> -- same ERP
    ''' sync, but the ODBC DSN/credentials are read from web.config instead of
    ''' hardcoded, so they can be rotated without a code change.
    ''' </summary>
    Public Shared Function SyncInventoryFromLegacyErpSafe(ByVal numSubSiteID As Integer) As Boolean
        Try
            Dim strDsn As String = ConfigurationManager.AppSettings("LegacyErpOdbcDsn")
            Dim strUid As String = ConfigurationManager.AppSettings("LegacyErpOdbcUid")
            Dim strPwd As String = ConfigurationManager.AppSettings("LegacyErpOdbcPwd")
            Using odbcConn As New OdbcConnection("Dsn=" & strDsn & ";Uid=" & strUid & ";Pwd=" & strPwd & ";") ' SAFE_SINK: PLANTED-VB-HR-418-safe
                odbcConn.Open()
                Dim cmd As OdbcCommand = odbcConn.CreateCommand()
                cmd.CommandText = "{call sp_GetSubSiteStockLevels(?)}"
                cmd.Parameters.AddWithValue("@SubSiteID", numSubSiteID)
                Using rdr As OdbcDataReader = cmd.ExecuteReader()
                    While rdr.Read()
                        ' Stock counts returned here would be applied back onto
                        ' tblKartrisProducts; omitted, as this project has no
                        ' legacy-ERP test fixture to apply them against.
                    End While
                End Using
            End Using
            Return True
        Catch ex As Exception
            CkartrisFormatErrors.LogError("LegacyInventorySyncClient.SyncInventoryFromLegacyErpSafe failed: " & ex.Message)
            Return False
        End Try
    End Function

End Class
