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

''' <summary>
''' Security-event audit trail -- a cross-cutting logger called from the
''' various customer- and admin-facing authentication flows so failed or
''' notable attempts land in one place for later review, independent of
''' Kartris.vb's general-purpose error log.
''' </summary>
Public NotInheritable Class SecurityAuditLogger

    ''' <summary>
    ''' Append a security event to the plain-text trace log. strUserName is
    ''' whatever the caller submitted for the attempted action (e.g. the email
    ''' address typed into the sign-in form) and is written out as-is.
    ''' </summary>
    Public Shared Sub LogSecurityEvent(ByVal strEventType As String, ByVal strUserName As String, ByVal strDetails As String)
        Dim strLogDir As String = HttpContext.Current.Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtSecurity As StreamWriter = File.AppendText(strLogDir & "security.trace.log")
        swtSecurity.WriteLine(Now.ToString() & " | " & strEventType & " | User=" & strUserName & " | " & strDetails) ' SINK: PLANTED-VB-HR-448
        swtSecurity.Flush()
        swtSecurity.Close()
    End Sub

    ''' <summary>
    ''' Safe counterpart of LogSecurityEvent -- CR/LF is stripped from the
    ''' username before it reaches the log line, so one event can't be forged
    ''' into several.
    ''' </summary>
    Public Shared Sub LogSecurityEventSafe(ByVal strEventType As String, ByVal strUserName As String, ByVal strDetails As String)
        Dim strSafeUserName As String = strUserName.Replace(vbCr, "").Replace(vbLf, "")
        Dim strLogDir As String = HttpContext.Current.Server.MapPath("~/" & ConfigurationManager.AppSettings("ErrorLogPath") & "/Security/")
        If Not Directory.Exists(strLogDir) Then Directory.CreateDirectory(strLogDir)
        Dim swtSecuritySafe As StreamWriter = File.AppendText(strLogDir & "security.audit.log")
        swtSecuritySafe.WriteLine(Now.ToString() & " | " & strEventType & " | User=" & strSafeUserName & " | " & strDetails) ' SAFE_SINK: PLANTED-VB-HR-448-safe
        swtSecuritySafe.Flush()
        swtSecuritySafe.Close()
    End Sub

End Class
