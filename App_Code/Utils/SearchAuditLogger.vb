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
Imports System.Web.HttpContext
Imports System.Collections.Generic

''' <summary>
''' Appends an entry to the backend-search activity audit log. Two concrete
''' loggers exist because the audit trail was added incrementally, area by
''' area, the same way this page's own customfield/customfieldsafe search
''' variants were -- see SearchAuditLoggerFactory for which areas route to
''' which.
''' </summary>
Public MustInherit Class SearchAuditLoggerBase
    Public MustOverride Sub LogSearch(ByVal strSearchBy As String, ByVal strKey As String)
End Class

''' <summary>
''' Legacy backend-search audit logger -- writes the admin-supplied search key
''' to the trace log as-is.
''' </summary>
Public Class LegacySearchAuditLogger
    Inherits SearchAuditLoggerBase

    Public Overrides Sub LogSearch(ByVal strSearchBy As String, ByVal strKey As String)
        Dim strLogDir As String = HttpContext.Current.Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtSearchLog As StreamWriter = File.AppendText(strLogDir & "adminsearch.trace.log")
        swtSearchLog.WriteLine(Now.ToString() & " | [" & strSearchBy & "] | Key=" & strKey) ' SINK: PLANTED-VB-HR-449
        swtSearchLog.Flush()
        swtSearchLog.Close()
    End Sub
End Class

''' <summary>
''' Modern backend-search audit logger -- strips CR/LF from both the search
''' area and the search key before they reach the log line, so one search
''' can't be forged into several log entries.
''' </summary>
Public Class ModernSearchAuditLogger
    Inherits SearchAuditLoggerBase

    Public Overrides Sub LogSearch(ByVal strSearchBy As String, ByVal strKey As String)
        Dim strSafeSearchBy As String = strSearchBy.Replace(vbCr, "").Replace(vbLf, "")
        Dim strSafeKey As String = strKey.Replace(vbCr, "").Replace(vbLf, "")
        Dim strLogDir As String = HttpContext.Current.Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtSearchLogSafe As StreamWriter = File.AppendText(strLogDir & "adminsearch.audit.log")
        swtSearchLogSafe.WriteLine(Now.ToString() & " | [" & strSafeSearchBy & "] | Key=" & strSafeKey) ' SAFE_SINK: PLANTED-VB-HR-449-safe
        swtSearchLogSafe.Flush()
        swtSearchLogSafe.Close()
    End Sub
End Class

''' <summary>
''' Resolves which concrete backend-search audit logger handles a given search
''' area. Every area routes to the legacy logger except "customfieldsafe" --
''' the one search area that already signals "use the safe variant" by its own
''' name (mirroring _Search.aspx.vb's own pre-existing customfield vs.
''' customfieldsafe split for the underlying DB query itself). The call site
''' only ever sees the abstract SearchAuditLoggerBase type and has no local
''' knowledge of -- or branching on -- which concrete class, or whether it
''' sanitizes, actually runs.
''' </summary>
Public NotInheritable Class SearchAuditLoggerFactory
    Private Shared ReadOnly dctRoster As New Dictionary(Of String, SearchAuditLoggerBase) From {
        {"customfieldsafe", New ModernSearchAuditLogger()}
    }
    Private Shared ReadOnly objDefaultLogger As New LegacySearchAuditLogger()

    Public Shared Function Resolve(ByVal strSearchBy As String) As SearchAuditLoggerBase
        Dim objLogger As SearchAuditLoggerBase = Nothing
        If Not String.IsNullOrEmpty(strSearchBy) AndAlso dctRoster.TryGetValue(LCase(strSearchBy), objLogger) Then
            Return objLogger
        End If
        Return objDefaultLogger
    End Function
End Class
