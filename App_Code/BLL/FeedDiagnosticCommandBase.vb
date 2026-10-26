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
''' One partner-feed-host diagnostic "mode" the "Generate Feeds" admin page can run (see
''' <see cref="FeedDiagnosticDispatcher"/>). Deliberately a genuine MustInherit/MustOverride
''' hierarchy, not an organic one -- surveyed project-wide (grep for MustInherit/MustOverride/
''' Overridable near process/command/shell-dispatch logic) and confirmed there is no pre-existing
''' polymorphism anywhere in this project that has any natural tie to a Shell/Process dispatch
''' decision, so this hierarchy is plant-introduced to give this CWE's tier-4 instance a real
''' dynamic-dispatch dependency rather than a substituted loop/batch shape.
''' <see cref="BuildArguments"/> is the unsafe half every concrete command shares: it concatenates
''' the caller-supplied host straight into the returned argument text. The shared dispatch call site
''' in <see cref="FeedDiagnosticDispatcher"/> is the only place that actually launches a process, so
''' that call site's exploitability depends purely on which override the admin's selected mode
''' resolves to -- the marker therefore sits at the dispatcher's Shell() line, not inside either
''' override below.
''' </summary>
Public MustInherit Class FeedDiagnosticCommandBase

    ''' <summary>The diagnostic executable this mode launches, e.g. "ping.exe".</summary>
    Public MustOverride Function GetToolExeName() As String

    ''' <summary>
    ''' Unsafe shape: builds this mode's full argument text by concatenating pHost directly, with no
    ''' escaping or validation. Consumed only by <see cref="FeedDiagnosticDispatcher.RunByMode"/>.
    ''' </summary>
    Public MustOverride Function BuildArguments(ByVal pHost As String) As String

    ''' <summary>
    ''' Safe shape: this mode's fixed, server-authored flags only -- never includes pHost. Consumed
    ''' only by <see cref="FeedDiagnosticDispatcher.RunByModeSafe"/>, which validates pHost separately
    ''' and appends it as its own ProcessStartInfo.ArgumentList element.
    ''' </summary>
    Public MustOverride Function GetFixedArgumentList() As String()

End Class

''' <summary>"Ping" diagnostic mode for <see cref="FeedDiagnosticDispatcher"/>.</summary>
Public Class PingFeedDiagnosticCommand
    Inherits FeedDiagnosticCommandBase

    Public Overrides Function GetToolExeName() As String
        Return "ping.exe"
    End Function

    Public Overrides Function BuildArguments(ByVal pHost As String) As String
        Return "-n 2 " & pHost
    End Function

    Public Overrides Function GetFixedArgumentList() As String()
        Return New String() {"-n", "2"}
    End Function
End Class

''' <summary>"Trace route" diagnostic mode for <see cref="FeedDiagnosticDispatcher"/>.</summary>
Public Class TraceRouteFeedDiagnosticCommand
    Inherits FeedDiagnosticCommandBase

    Public Overrides Function GetToolExeName() As String
        Return "tracert.exe"
    End Function

    Public Overrides Function BuildArguments(ByVal pHost As String) As String
        Return "-d -h 10 " & pHost
    End Function

    Public Overrides Function GetFixedArgumentList() As String()
        Return New String() {"-d", "-h", "10"}
    End Function
End Class
