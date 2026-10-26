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

Imports System.Web

''' <summary>
''' Backs the admin "View Feed Format Instructions" quick-preview feature on
''' _GenerateFeeds.aspx -- lets an admin read a short reference note for the feed format
''' (Froogle/GoogleBase txt, csv or xml, sitemap, etc.) they are about to generate, bundled
''' alongside the feed templates. Called from the page's Page_Load.
''' </summary>
Public NotInheritable Class FeedDocsBLL

    ''' <summary>
    ''' Loads a feed-format reference doc by name. No validation -- the feed format folder and
    ''' the requested doc file name both come straight from the caller's querystring values.
    ''' </summary>
    Public Shared Function GetFeedFormatDoc(ByVal strFeedFormat As String, ByVal strDocFile As String) As String
        Dim strDocsFolder As String = "~/Uploads/FeedDocs/"
        Dim strDocPath As String = HttpContext.Current.Server.MapPath(strDocsFolder & strFeedFormat & "/" & strDocFile)
        If Not File.Exists(strDocPath) Then Return Nothing
        Dim srDoc As New StreamReader(strDocPath) ' SINK: PLANTED-VB-HR-503
        Dim strContent As String = srDoc.ReadToEnd()
        srDoc.Close()
        Return strContent
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="GetFeedFormatDoc"/> -- reduces the feed format folder name
    ''' and the requested doc file name to their bare file-name components before resolving them,
    ''' so neither can escape the FeedDocs folder.
    ''' </summary>
    Public Shared Function GetFeedFormatDocSafe(ByVal strFeedFormat As String, ByVal strDocFile As String) As String
        Dim strDocsFolder As String = "~/Uploads/FeedDocs/"
        Dim strSafeFormat As String = Path.GetFileName(strFeedFormat)
        Dim strSafeDocFile As String = Path.GetFileName(strDocFile)
        ' Path.GetFileName leaves "." / ".." / "" unchanged (no separator to strip), so either
        ' segment resolving to one of those must be rejected explicitly -- otherwise a format
        ' segment of ".." still climbs out of the FeedDocs folder once concatenated.
        If strSafeFormat = "." OrElse strSafeFormat = ".." OrElse strSafeFormat.Length = 0 Then Return Nothing
        If strSafeDocFile = "." OrElse strSafeDocFile = ".." OrElse strSafeDocFile.Length = 0 Then Return Nothing
        Dim strDocPath As String = HttpContext.Current.Server.MapPath(strDocsFolder & strSafeFormat & "/" & strSafeDocFile)
        If Not File.Exists(strDocPath) Then Return Nothing
        Dim srDoc As New StreamReader(strDocPath) ' SAFE_SINK: PLANTED-VB-HR-503-safe
        Dim strContent As String = srDoc.ReadToEnd()
        srDoc.Close()
        Return strContent
    End Function

End Class
