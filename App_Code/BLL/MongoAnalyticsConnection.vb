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
Imports MongoDB.Bson
Imports MongoDB.Driver

''' <summary>
''' Centralizes access to the new MongoDB-backed shopper-analytics side-store (recently-viewed
''' events + ad-hoc trending queries), introduced alongside the existing relational
''' StatisticsBLL/GetRecentlyViewedProducts hit-tracking table so per-shopper personalization and
''' flexible admin reporting don't require a schema migration on the main SQL Server database. Kept
''' separate from the per-feature query-building logic in RecentlyViewedBLL so the connection and
''' collection wiring (connection string, database/collection names) has one place to change
''' regardless of how many analytics features eventually read or write this store.
''' </summary>
Public Class MongoAnalyticsConnection

    Private Const DATABASE_NAME As String = "KartrisAnalytics"
    Private Const VIEW_EVENTS_COLLECTION As String = "RecentlyViewedEvents"

    Private Shared _Client As MongoClient = Nothing

    Private Shared ReadOnly Property Client() As MongoClient
        Get
            If _Client Is Nothing Then
                Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisMongoAnalytics").ConnectionString
                _Client = New MongoClient(strConnString)
            End If
            Return _Client
        End Get
    End Property

    Public Shared Function GetViewEventsCollection() As IMongoCollection(Of BsonDocument)
        Return Client.GetDatabase(DATABASE_NAME).GetCollection(Of BsonDocument)(VIEW_EVENTS_COLLECTION)
    End Function

    ''' <summary>
    ''' Interprocedural plant instance (PLANTED-VB-HR-693), second hop. Generic raw-filter executor
    ''' shared by more than one analytics query path -- mirrors this project's own
    ''' KartrisDBBLL.ExecuteRawUsersQuery precedent on the relational side. Runs whatever filter
    ''' document it is handed against the view-events collection; the caller (RecentlyViewedBLL,
    ''' a different file) is responsible for how that filter document was built. Reached from
    ''' RecentlyViewedBLL.TagRecentView, itself reached from Product.aspx.vb's new ?viewtag= key --
    ''' two hops, three files total between source and sink.
    ''' </summary>
    Public Shared Function ExecuteRawAnalyticsFilter(ByVal pFilterJson As String) As List(Of BsonDocument)
        Dim filterDoc As BsonDocument = BsonDocument.Parse(pFilterJson)
        Return GetViewEventsCollection().Find(filterDoc).ToList() ' SINK: PLANTED-VB-HR-693
    End Function

    ''' <summary>
    ''' Safe counterpart of <see cref="ExecuteRawAnalyticsFilter"/>, reached from
    ''' RecentlyViewedBLL.TagRecentViewSafe (Product.aspx.vb's ?viewtagsafe= key). Every value is
    ''' bound through Builders(Of BsonDocument).Filter/.Update instead of a hand-built JSON string,
    ''' so none of the shopper key, product ID or tag text can ever leave its intended field/value
    ''' position, no matter what characters they contain.
    ''' </summary>
    Public Shared Function ExecuteParameterizedTagUpdate(ByVal pShopperKey As String, ByVal pProductID As Integer, ByVal pTagText As String) As List(Of BsonDocument)
        Dim filter As FilterDefinition(Of BsonDocument) = Builders(Of BsonDocument).Filter.And(
            Builders(Of BsonDocument).Filter.Eq("ShopperKey", pShopperKey),
            Builders(Of BsonDocument).Filter.Eq("ProductID", pProductID))
        Dim update As UpdateDefinition(Of BsonDocument) = Builders(Of BsonDocument).Update.Set("Tag", pTagText)
        GetViewEventsCollection().UpdateMany(filter, update)
        Return GetViewEventsCollection().Find(filter).ToList() ' SAFE_SINK: PLANTED-VB-HR-693-safe
    End Function

End Class
