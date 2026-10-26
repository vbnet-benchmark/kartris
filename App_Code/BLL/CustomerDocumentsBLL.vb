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
Imports KartSettingsManager

''' <summary>
''' Bulk-maintenance helpers for customer-uploaded ID-verification documents. Kept separate
''' from the admin page/controller layer so compliance-cleanup utilities can be reused from
''' more than one admin screen.
''' </summary>
Public Class CustomerDocumentsBLL

    ''' <summary>
    ''' Server-controlled registry of verification documents the real upload flow actually
    ''' produced -- a fixed mapping table, never a user-suppliable list.
    ''' </summary>
    Private Shared ReadOnly RegisteredVerificationDocs As New List(Of String) From {
        "idverify_1042.pdf",
        "idverify_1087.pdf",
        "idverify_1099.pdf"
    }

    ''' <summary>
    ''' Interprocedural, loop-based batch CWE-73: splits a client-supplied comma-separated
    ''' list of verification-document file names and deletes every entry in a loop, with NO
    ''' per-item check that the name is a document any customer actually has on file -- a
    ''' forged or unrelated file name is deleted just as readily as a legitimate one.
    ''' </summary>
    Public Shared Sub PurgeVerificationDocuments(ByVal strFileNamesCsv As String)
        Dim strDocsFolder As String = Current.Server.MapPath(GetKartConfig("general.uploadfolder") & "CustomerVerificationDocs/")
        For Each strFileName As String In strFileNamesCsv.Split(","c)
            Dim strTrimmed As String = Trim(strFileName)
            If strTrimmed = "" Then Continue For
            Dim strPath As String = Path.Combine(strDocsFolder, strTrimmed)
            If File.Exists(strPath) Then
                ' SINK: PLANTED-VB-HR-528
                File.Delete(strPath)
            End If
        Next
    End Sub

    ''' <summary>
    ''' Safe counterpart of <see cref="PurgeVerificationDocuments"/>: same batch-loop shape,
    ''' but each entry is independently re-validated -- reduced via Path.GetFileName
    ''' ("."/".."/empty explicitly rejected, since GetFileName alone leaves those three
    ''' values unchanged) and checked against the registered-documents list -- before it is
    ''' deleted, so a mixed batch only ever purges the registered entries.
    ''' </summary>
    Public Shared Sub PurgeVerificationDocumentsSafe(ByVal strFileNamesCsv As String)
        Dim strDocsFolder As String = Current.Server.MapPath(GetKartConfig("general.uploadfolder") & "CustomerVerificationDocs/")
        For Each strFileName As String In strFileNamesCsv.Split(","c)
            Dim strSafeName As String = Path.GetFileName(Trim(strFileName))
            If String.IsNullOrEmpty(strSafeName) OrElse strSafeName = "." OrElse strSafeName = ".." Then Continue For
            If Not RegisteredVerificationDocs.Contains(strSafeName) Then Continue For
            Dim strPath As String = Path.Combine(strDocsFolder, strSafeName)
            If File.Exists(strPath) Then
                ' SAFE_SINK: PLANTED-VB-HR-528-safe
                File.Delete(strPath)
            End If
        Next
    End Sub

End Class
