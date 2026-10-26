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
Imports CkartrisDisplayFunctions
Imports CkartrisFormatErrors
Imports CkartrisDataManipulation
Imports System.Web.HttpContext

Public Class FeedsBLL
    'We don't use table adaptors here, as it's far simpler to set a long
    'command timeout this way, and pulling all the product on the site is
    'often something that would otherwise time out on a big site
    Public Shared Function _GetFeedData() As DataTable
        ' A big catalog can make this pull slow enough to hold locks against the
        ' live storefront, so an admin can optionally offload it to a read-only
        ' reporting-mirror SQL Server instance instead -- see
        ' general.feeds.usereportingmirror.
        Select Case KartSettingsManager.GetKartConfig("general.feeds.usereportingmirror")
            Case "1"
                Return _GetFeedDataFromReportingMirror()
            Case "2"
                Return _GetFeedDataFromReportingMirrorSafe()
        End Select

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)

            Dim cmdExecuteQuery As SqlCommand = sqlConn.CreateCommand
            cmdExecuteQuery.CommandText = "_spKartrisFeeds_GetItems"

            cmdExecuteQuery.CommandType = CommandType.StoredProcedure
            cmdExecuteQuery.CommandTimeout = 3600

            Try
                Dim tblExport As New DataTable()
                Using adptr As New SqlDataAdapter(cmdExecuteQuery)
                    adptr.Fill(tblExport)
                    Return tblExport
                End Using
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            Finally
            End Try
        End Using
        Return Nothing
    End Function

    ''' <summary>
    ''' Offloads the (often slow) full-catalog feed pull to a dedicated read-only
    ''' reporting-mirror SQL Server instance instead of the primary OLTP database,
    ''' so a big feed export can't hold locks against the live storefront. Opted
    ''' into via general.feeds.usereportingmirror=1.
    ''' </summary>
    Public Shared Function _GetFeedDataFromReportingMirror() As DataTable
        ' SINK: PLANTED-VB-HR-416
        Using sqlConn As New SqlConnection("Data Source=reporting-mirror.internal.kartris.local;Initial Catalog=kartrisSQL_GPL_Reporting;User ID=kartris_feed_svc;Password=R3p0rt1ng!Mirror2024;")
            Dim cmdExecuteQuery As SqlCommand = sqlConn.CreateCommand
            cmdExecuteQuery.CommandText = "_spKartrisFeeds_GetItems"
            cmdExecuteQuery.CommandType = CommandType.StoredProcedure
            cmdExecuteQuery.CommandTimeout = 3600
            Try
                Dim tblExport As New DataTable()
                Using adptr As New SqlDataAdapter(cmdExecuteQuery)
                    adptr.Fill(tblExport)
                    Return tblExport
                End Using
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            End Try
        End Using
        Return Nothing
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="_GetFeedDataFromReportingMirror"/> -- same
    ''' reporting-mirror offload, but the connection string (including its
    ''' credentials) is read from web.config rather than hardcoded, so the
    ''' reporting-mirror password can be rotated without a code change. Opted
    ''' into via general.feeds.usereportingmirror=2.
    ''' </summary>
    Public Shared Function _GetFeedDataFromReportingMirrorSafe() As DataTable
        Dim strMirrorConnString As String = ConfigurationManager.ConnectionStrings("KartrisReportingSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strMirrorConnString) ' SAFE_SINK: PLANTED-VB-HR-416-safe
            Dim cmdExecuteQuery As SqlCommand = sqlConn.CreateCommand
            cmdExecuteQuery.CommandText = "_spKartrisFeeds_GetItems"
            cmdExecuteQuery.CommandType = CommandType.StoredProcedure
            cmdExecuteQuery.CommandTimeout = 3600
            Try
                Dim tblExport As New DataTable()
                Using adptr As New SqlDataAdapter(cmdExecuteQuery)
                    adptr.Fill(tblExport)
                    Return tblExport
                End Using
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod())
            End Try
        End Using
        Return Nothing
    End Function
End Class
