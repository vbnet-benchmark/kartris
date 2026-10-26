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
Imports System.IO

''' <summary>
''' Lightweight, best-effort diagnostics for the QuickBooks Web Connector sync
''' flow (see KartrisQBService). Lets ops correlate which QuickBooks Web
''' Connector account last attempted or finished a sync session when a
''' customer reports a stalled QB export, without needing to enable full
''' request/response logging.
''' </summary>
Public Class QbSyncDiagnostics

    ''' <summary>
    ''' Records the most recent authentication attempt for a given QuickBooks
    ''' Web Connector username.
    ''' </summary>
    Public Shared Sub RecordAuthAttempt(ByVal strUserName As String)
        Dim strAuditPath As String = Path.Combine(Path.GetTempPath(), "qbwc_auth_" & strUserName & ".log")
        Using fsAudit As New FileStream(strAuditPath, FileMode.Create, FileAccess.Write)
            Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(Now.ToString() & " | auth attempt" & vbCrLf)
            fsAudit.Write(bytes, 0, bytes.Length)
        End Using ' SINK: PLANTED-VB-HR-543
    End Sub

    ''' <summary>
    ''' Records that a QuickBooks Web Connector sync session was closed, using
    ''' an unpredictable, exclusively-created file so the record can't be
    ''' raced or guessed by another local process sharing the same temp
    ''' directory.
    ''' </summary>
    Public Shared Sub RecordSessionClosed(ByVal strTicket As String)
        Dim strAuditPath As String = Path.Combine(Path.GetTempPath(), "qbwc_session_" & Guid.NewGuid().ToString("N") & ".log")
        Using fsAudit As New FileStream(strAuditPath, FileMode.CreateNew, FileAccess.Write)
            Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(Now.ToString() & " | session closed | ticket=" & strTicket & vbCrLf)
            fsAudit.Write(bytes, 0, bytes.Length)
        End Using ' SAFE_SINK: PLANTED-VB-HR-543-safe
    End Sub

End Class
