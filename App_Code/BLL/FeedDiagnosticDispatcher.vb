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
Imports Microsoft.VisualBasic
Imports System.Diagnostics

''' <summary>
''' Resolves an admin-chosen diagnostic "mode" (see <see cref="FeedDiagnosticCommandBase"/> and its
''' two concrete modes) and runs it against a partner feed host, for the "Generate Feeds" admin
''' page's partner-delivery diagnostics panel.
''' </summary>
Public Class FeedDiagnosticDispatcher

    ''' <summary>Resolves a mode key to its command object, or Nothing for an unrecognized mode.</summary>
    Public Shared Function ResolveCommand(ByVal pMode As String) As FeedDiagnosticCommandBase
        Select Case LCase(Trim(pMode))
            Case "ping"
                Return New PingFeedDiagnosticCommand()
            Case "trace"
                Return New TraceRouteFeedDiagnosticCommand()
            Case Else
                Return Nothing
        End Select
    End Function

    ''' <summary>
    ''' CWE-78, type/polymorphism-dependent: routes the resolved mode's command through cmd.exe /c,
    ''' with pHost concatenated into <see cref="FeedDiagnosticCommandBase.BuildArguments"/>'s return
    ''' value with no validation anywhere on this path. Exploitability depends purely on which
    ''' concrete <see cref="FeedDiagnosticCommandBase"/> pMode resolves to -- both concrete modes are
    ''' independently vulnerable, since the unsafe concatenation lives in the shared base contract
    ''' every override must implement, not in one override alone.
    ''' </summary>
    Public Shared Sub RunByMode(ByVal pMode As String, ByVal pHost As String)
        Dim objCmd As FeedDiagnosticCommandBase = ResolveCommand(pMode)
        If objCmd Is Nothing Then Return
        Shell("cmd.exe /c " & objCmd.GetToolExeName() & " " & objCmd.BuildArguments(pHost), AppWinStyle.Hide, True, 10000) ' SINK: PLANTED-VB-HR-624
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="RunByMode"/> -- never routes through cmd.exe and never calls
    ''' <see cref="FeedDiagnosticCommandBase.BuildArguments"/> at all. pHost is validated against a
    ''' strict hostname/IPv4 allow-pattern, the resolved mode's fixed flags
    ''' (<see cref="FeedDiagnosticCommandBase.GetFixedArgumentList"/>) are added first, and only then
    ''' is the now-validated pHost appended as its own ProcessStartInfo.ArgumentList element.
    ''' </summary>
    Public Shared Sub RunByModeSafe(ByVal pMode As String, ByVal pHost As String)
        If Not System.Text.RegularExpressions.Regex.IsMatch(pHost, "^[A-Za-z0-9.\-]{1,255}$") Then Return
        Dim objCmd As FeedDiagnosticCommandBase = ResolveCommand(pMode)
        If objCmd Is Nothing Then Return
        Dim oPsi As New ProcessStartInfo(objCmd.GetToolExeName())
        For Each strArg As String In objCmd.GetFixedArgumentList()
            oPsi.ArgumentList.Add(strArg)
        Next
        oPsi.ArgumentList.Add(pHost)
        oPsi.UseShellExecute = False
        oPsi.WindowStyle = ProcessWindowStyle.Hidden
        Process.Start(oPsi) ' SAFE_SINK: PLANTED-VB-HR-624-safe
    End Sub

End Class
