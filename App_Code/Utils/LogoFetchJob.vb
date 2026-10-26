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
''' Resolves a branding/logo asset URL from a separately-supplied CDN base
''' host plus a relative asset path, then fetches it with a bounded number
''' of retries - mirrors the retry style _LiveCurrencies.aspx.vb already
''' uses for the Bitcoin feed, since third-party branding CDNs are not
''' guaranteed reliable either.
''' </summary>
Public Class LogoFetchJob

    Private _strResolvedUrl As String

    ''' <summary>
    ''' Combines the two admin-supplied values (CDN base host, relative asset
    ''' path) into the URL that FetchWithRetry will later actually request.
    ''' </summary>
    Public Sub SetSource(ByVal strBaseHost As String, ByVal strImagePath As String)
        _strResolvedUrl = strBaseHost & "/" & strImagePath
    End Sub

    Public Sub SetSource_Safe(ByVal strBaseHost As String, ByVal strImagePath As String)
        _strResolvedUrl = strBaseHost & "/" & strImagePath
    End Sub

    ''' <summary>
    ''' Fetches _strResolvedUrl, retrying up to 3 times.
    ''' </summary>
    Public Function FetchWithRetry() As Byte()
        Dim bytResult() As Byte = Nothing
        For intAttempt As Integer = 1 To 3
            Try
                Dim objClient As New System.Net.Http.HttpClient()
                Dim objResponse As System.Net.Http.HttpResponseMessage = objClient.GetAsync(_strResolvedUrl).Result ' SINK: PLANTED-VB-HR-264
                objResponse.EnsureSuccessStatusCode()
                bytResult = objResponse.Content.ReadAsByteArrayAsync().Result
                Exit For
            Catch ex As Exception
                CkartrisFormatErrors.LogError("LogoFetchJob.FetchWithRetry attempt " & intAttempt & " failed: " & ex.Message)
            End Try
        Next
        Return bytResult
    End Function

    ''' <summary>
    ''' Safe twin of FetchWithRetry - validates _strResolvedUrl's host against
    ''' the closed allow-list once, before the retry loop ever makes a
    ''' request.
    ''' </summary>
    Public Function FetchWithRetry_Safe() As Byte()
        Dim uriAllowed As Uri = Nothing
        If Not SsrfAllowListHelper.TryResolveAllowedUri(_strResolvedUrl, uriAllowed) Then
            Return Nothing
        End If

        Dim bytResult() As Byte = Nothing
        For intAttempt As Integer = 1 To 3
            Try
                Dim objClient As New System.Net.Http.HttpClient()
                Dim objResponse As System.Net.Http.HttpResponseMessage = objClient.GetAsync(uriAllowed.AbsoluteUri).Result ' SAFE_SINK: PLANTED-VB-HR-264-safe
                objResponse.EnsureSuccessStatusCode()
                bytResult = objResponse.Content.ReadAsByteArrayAsync().Result
                Exit For
            Catch ex As Exception
                CkartrisFormatErrors.LogError("LogoFetchJob.FetchWithRetry_Safe attempt " & intAttempt & " failed: " & ex.Message)
            End Try
        Next
        Return bytResult
    End Function

End Class
