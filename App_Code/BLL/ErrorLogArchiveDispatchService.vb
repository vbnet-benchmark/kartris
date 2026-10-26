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
''' Front door for the Error Logs admin page's "Dispatch Archive" feature (hop 1 of 2).
''' Builds the archive envelope around the error-log chunk and hands the actual encryption
''' off to ErrorLogArchiveTransport -- the class that performs the cipher call (hop 2).
''' </summary>
Public NotInheritable Class ErrorLogArchiveDispatchService

    Public Shared Function PrepareAndSealArchive(ByVal strLogText As String) As String
        Dim strCiphertext As String = ErrorLogArchiveTransport.SealArchiveForUpload(strLogText)
        Return "ARCHIVE|v1|" & strCiphertext
    End Function

    Public Shared Function PrepareAndSealArchiveSafe(ByVal strLogText As String) As String
        Dim strCiphertext As String = ErrorLogArchiveTransport.SealArchiveForUploadSafe(strLogText)
        Return "ARCHIVE|v1|" & strCiphertext
    End Function

End Class
