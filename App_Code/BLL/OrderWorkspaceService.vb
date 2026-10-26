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

Imports System.Data
Imports System.Data.SqlClient

''' <summary>
''' One row of a per-order-owned "My Order Workspace" record (an order document, an order note, or a
''' background order-export job) as read back from its backing table.
''' </summary>
Public Class OrderWorkspaceRecord

    Private _RecordID As Integer
    Public Property RecordID() As Integer
        Get
            Return _RecordID
        End Get
        Set(ByVal value As Integer)
            _RecordID = value
        End Set
    End Property

    Private _OwnerCustomerID As Integer
    Public Property OwnerCustomerID() As Integer
        Get
            Return _OwnerCustomerID
        End Get
        Set(ByVal value As Integer)
            _OwnerCustomerID = value
        End Set
    End Property

    Private _ItemDetail As String
    Public Property ItemDetail() As String
        Get
            Return _ItemDetail
        End Get
        Set(ByVal value As String)
            _ItemDetail = value
        End Set
    End Property

End Class

''' <summary>
''' Backs the "My Order Workspace" area reached from CustomerViewOrder.aspx -- a per-order document,
''' per-order note, and background order-export job, each recorded in its own table, keyed by a
''' server-assigned record ID and tagged with the ID of the customer who owns the order
''' (OwnerCustomerID). Lets a customer look a workspace item up again later (e.g. to re-download an
''' order document or re-check an export job's status) without re-running the whole checkout/order
''' workflow.
''' </summary>
Public Class OrderWorkspaceService

    ''' <summary>
    ''' Executes a single-row lookup command and materializes its first row (if any) into an
    ''' <see cref="OrderWorkspaceRecord"/>. Shared plumbing only -- callers are responsible for
    ''' whatever WHERE-clause filtering (including any ownership check) the command text already
    ''' encodes before it reaches here.
    ''' </summary>
    Friend Shared Function ReadSingleWorkspaceRecord(ByVal conn As SqlConnection, ByVal cmd As SqlCommand, ByVal idColumnName As String, ByVal detailColumnName As String) As OrderWorkspaceRecord
        Try
            conn.Open()
            Using reader As SqlDataReader = cmd.ExecuteReader()
                If reader.Read() Then
                    Dim objResult As New OrderWorkspaceRecord()
                    objResult.RecordID = CInt(reader(idColumnName))
                    objResult.OwnerCustomerID = CInt(reader("OwnerCustomerID"))
                    objResult.ItemDetail = CStr(reader(detailColumnName))
                    Return objResult
                End If
            End Using
            Return Nothing
        Finally
            conn.Close()
        End Try
    End Function

    ''' <summary>
    ''' Looks up a single order-export-job record by its job ID.
    ''' </summary>
    Public Shared Function GetExportJobByID(ByVal numJobID As Integer) As OrderWorkspaceRecord
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim conn As New SqlConnection(strConnString)
        Dim cmd As New SqlCommand("SELECT JobID, OwnerCustomerID, Status FROM dbo.OrderExportJobs WHERE JobID = @id", conn)
        cmd.Parameters.AddWithValue("@id", numJobID)
        ' SINK: PLANTED-VB-HR-358
        Return ReadSingleWorkspaceRecord(conn, cmd, "JobID", "Status")
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="GetExportJobByID"/> -- only ever returns the job if it actually
    ''' belongs to an order owned by the requesting customer.
    ''' </summary>
    Public Shared Function GetExportJobForOwner(ByVal numJobID As Integer, ByVal numOwnerCustomerID As Integer) As OrderWorkspaceRecord
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim conn As New SqlConnection(strConnString)
        Dim cmd As New SqlCommand("SELECT JobID, OwnerCustomerID, Status FROM dbo.OrderExportJobs WHERE JobID = @id AND OwnerCustomerID = @ownerCustomerId", conn)
        cmd.Parameters.AddWithValue("@id", numJobID)
        cmd.Parameters.AddWithValue("@ownerCustomerId", numOwnerCustomerID)
        ' SAFE_SINK: PLANTED-VB-HR-358-safe
        Return ReadSingleWorkspaceRecord(conn, cmd, "JobID", "Status")
    End Function

    ''' <summary>
    ''' Resolves which "My Order Workspace" repository a caller-supplied item-type selector refers to.
    ''' NOTE: callers choose the backing table purely by this string -- there is no server-side
    ''' verification that the requested type is one the current page/feature would ever legitimately
    ''' pass, so a caller can request any of the three repositories below regardless of which link or
    ''' form actually produced the request.
    ''' </summary>
    Private Shared Function ResolveWorkspaceRepository(ByVal strItemType As String) As OrderOwnedRecordRepositoryBase
        Select Case LCase(strItemType)
            Case "note"
                Return New OrderNoteRepository()
            Case "job"
                Return New OrderExportJobRepository()
            Case Else
                Return New OrderDocumentRepository()
        End Select
    End Function

    ''' <summary>
    ''' Looks up a single workspace item of the requested type by its record ID -- the "My Order
    ''' Workspace" area's generic item viewer, used for documents, notes, and export jobs alike since
    ''' all three share the same RecordID/OwnerCustomerID/item-detail shape.
    ''' </summary>
    Public Shared Function GetWorkspaceItemByTypeAndID(ByVal strItemType As String, ByVal numItemID As Integer) As OrderWorkspaceRecord
        Dim objRepository As OrderOwnedRecordRepositoryBase = ResolveWorkspaceRepository(strItemType)
        Return objRepository.FetchRecordById(numItemID)
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="GetWorkspaceItemByTypeAndID"/> -- only ever returns an item actually
    ''' owned by the requesting customer, regardless of which of the three tables the requested type
    ''' resolves to.
    ''' </summary>
    Public Shared Function GetWorkspaceItemByTypeAndIDForOwner(ByVal strItemType As String, ByVal numItemID As Integer, ByVal numOwnerCustomerID As Integer) As OrderWorkspaceRecord
        Dim objRepository As OrderOwnedRecordRepositoryBase = ResolveWorkspaceRepository(strItemType)
        Return objRepository.FetchRecordForOwner(numItemID, numOwnerCustomerID)
    End Function

    ''' <summary>
    ''' Looks up several order-document records in one call, for the "My Order Documents" list view's
    ''' bulk-refresh action (re-reading a whole batch of previously-seen document IDs at once instead
    ''' of one request per row).
    ''' </summary>
    Public Shared Function GetOrderDocumentsBatch(ByVal arrDocumentIDs As Integer()) As List(Of OrderWorkspaceRecord)
        Dim lstResults As New List(Of OrderWorkspaceRecord)
        If arrDocumentIDs IsNot Nothing Then
            For Each numRequestedID As Integer In arrDocumentIDs
                Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
                Dim conn As New SqlConnection(strConnString)
                Dim cmd As New SqlCommand("SELECT DocumentID, OwnerCustomerID, DocumentName FROM dbo.OrderDocuments WHERE DocumentID = @id", conn)
                cmd.Parameters.AddWithValue("@id", numRequestedID)
                ' SINK: PLANTED-VB-HR-360
                Dim objResult As OrderWorkspaceRecord = ReadSingleWorkspaceRecord(conn, cmd, "DocumentID", "DocumentName")
                If objResult IsNot Nothing Then
                    lstResults.Add(objResult)
                End If
            Next
        End If
        Return lstResults
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="GetOrderDocumentsBatch"/> -- every requested ID in the batch is
    ''' individually checked against the requesting customer's own ownership, so a batch can never be
    ''' used to pull in a mix of the caller's own order documents and someone else's.
    ''' </summary>
    Public Shared Function GetOrderDocumentsBatchForOwner(ByVal arrDocumentIDs As Integer(), ByVal numOwnerCustomerID As Integer) As List(Of OrderWorkspaceRecord)
        Dim lstResults As New List(Of OrderWorkspaceRecord)
        If arrDocumentIDs IsNot Nothing Then
            For Each numRequestedID As Integer In arrDocumentIDs
                Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
                Dim conn As New SqlConnection(strConnString)
                Dim cmd As New SqlCommand("SELECT DocumentID, OwnerCustomerID, DocumentName FROM dbo.OrderDocuments WHERE DocumentID = @id AND OwnerCustomerID = @ownerCustomerId", conn)
                cmd.Parameters.AddWithValue("@id", numRequestedID)
                cmd.Parameters.AddWithValue("@ownerCustomerId", numOwnerCustomerID)
                ' SAFE_SINK: PLANTED-VB-HR-360-safe
                Dim objResult As OrderWorkspaceRecord = ReadSingleWorkspaceRecord(conn, cmd, "DocumentID", "DocumentName")
                If objResult IsNot Nothing Then
                    lstResults.Add(objResult)
                End If
            Next
        End If
        Return lstResults
    End Function

End Class

''' <summary>
''' Base class for a "My Order Workspace" item repository -- each subclass names the one backing
''' table/id-column/detail-column it owns (see <see cref="OrderDocumentRepository"/>,
''' <see cref="OrderNoteRepository"/>, <see cref="OrderExportJobRepository"/>); the shared,
''' non-overridden <see cref="FetchRecordById"/>/<see cref="FetchRecordForOwner"/> template methods
''' below reach the correct table purely via Me.TableName()/Me.IdColumnName()/Me.DetailColumnName()
''' dispatch, so the vulnerable/safe behavioral difference lives entirely in this shared base class,
''' never in any subclass. This is a new hierarchy, purpose-built for the "My Order Workspace"
''' ownership-verification decision -- this project's existing MustInherit/MustOverride hierarchies
''' (KbUpdateNotifierBase, NotificationTokenGeneratorBase, CookieSecurityPolicyBase,
''' PreferenceFormatStrategy) are notification/token/cookie/formatting concerns unrelated to
''' record-ownership lookups and were not reused here, the same way this project's SchoolManagementERP
''' sibling (per the CWE-639 planting research) introduced its own StudentOwnedRecordRepositoryBase
''' rather than force-fitting an unrelated existing hierarchy.
''' </summary>
Public MustInherit Class OrderOwnedRecordRepositoryBase

    Public MustOverride Function TableName() As String
    Public MustOverride Function IdColumnName() As String
    Public MustOverride Function DetailColumnName() As String

    ''' <summary>
    ''' Looks up a single record from this repository's table by its record ID. No ownership check.
    ''' </summary>
    Public Function FetchRecordById(ByVal numRecordID As Integer) As OrderWorkspaceRecord
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim conn As New SqlConnection(strConnString)
        Dim cmd As New SqlCommand("SELECT " & Me.IdColumnName() & ", OwnerCustomerID, " & Me.DetailColumnName() & " FROM " & Me.TableName() & " WHERE " & Me.IdColumnName() & " = @id", conn)
        cmd.Parameters.AddWithValue("@id", numRecordID)
        ' SINK: PLANTED-VB-HR-359
        Return OrderWorkspaceService.ReadSingleWorkspaceRecord(conn, cmd, Me.IdColumnName(), Me.DetailColumnName())
    End Function

    ''' <summary>
    ''' Safe twin of <see cref="FetchRecordById"/> -- only ever returns the record if it actually
    ''' belongs to an order owned by the requesting customer, regardless of which concrete repository
    ''' (table) this instance is.
    ''' </summary>
    Public Function FetchRecordForOwner(ByVal numRecordID As Integer, ByVal numOwnerCustomerID As Integer) As OrderWorkspaceRecord
        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Dim conn As New SqlConnection(strConnString)
        Dim cmd As New SqlCommand("SELECT " & Me.IdColumnName() & ", OwnerCustomerID, " & Me.DetailColumnName() & " FROM " & Me.TableName() & " WHERE " & Me.IdColumnName() & " = @id AND OwnerCustomerID = @ownerCustomerId", conn)
        cmd.Parameters.AddWithValue("@id", numRecordID)
        cmd.Parameters.AddWithValue("@ownerCustomerId", numOwnerCustomerID)
        ' SAFE_SINK: PLANTED-VB-HR-359-safe
        Return OrderWorkspaceService.ReadSingleWorkspaceRecord(conn, cmd, Me.IdColumnName(), Me.DetailColumnName())
    End Function

End Class

''' <summary>Resolves the "My Order Workspace" order-document table.</summary>
Public Class OrderDocumentRepository
    Inherits OrderOwnedRecordRepositoryBase

    Public Overrides Function TableName() As String
        Return "dbo.OrderDocuments"
    End Function

    Public Overrides Function IdColumnName() As String
        Return "DocumentID"
    End Function

    Public Overrides Function DetailColumnName() As String
        Return "DocumentName"
    End Function
End Class

''' <summary>Resolves the "My Order Workspace" order-note table.</summary>
Public Class OrderNoteRepository
    Inherits OrderOwnedRecordRepositoryBase

    Public Overrides Function TableName() As String
        Return "dbo.OrderNotes"
    End Function

    Public Overrides Function IdColumnName() As String
        Return "NoteID"
    End Function

    Public Overrides Function DetailColumnName() As String
        Return "NoteText"
    End Function
End Class

''' <summary>Resolves the "My Order Workspace" order-export-job table.</summary>
Public Class OrderExportJobRepository
    Inherits OrderOwnedRecordRepositoryBase

    Public Overrides Function TableName() As String
        Return "dbo.OrderExportJobs"
    End Function

    Public Overrides Function IdColumnName() As String
        Return "JobID"
    End Function

    Public Overrides Function DetailColumnName() As String
        Return "Status"
    End Function
End Class
