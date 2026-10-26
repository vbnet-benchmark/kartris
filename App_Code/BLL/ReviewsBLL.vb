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
Imports kartrisReviewsData
Imports kartrisReviewsDataTableAdapters
Imports CkartrisDataManipulation
Imports CkartrisFormatErrors
Imports System.Web.HttpContext

Public Class ReviewsBLL

    Private Shared _Adptr As ReviewsTblAdptr = Nothing
    Protected Shared ReadOnly Property Adptr() As ReviewsTblAdptr
        Get
            _Adptr = New ReviewsTblAdptr
            Return _Adptr
        End Get
    End Property
    Public Shared Function _GetReviews() As DataTable
        Return Adptr._GetData()
    End Function
    Public Shared Function _GetReviewsByProductID(ByVal _ProductID As Integer, ByVal _LanguageID As Byte) As DataTable
        Return Adptr._GetReviewsByProductID(_ProductID, _LanguageID)
    End Function
    Public Shared Function GetReviewsByProductID(ByVal _ProductID As Integer, ByVal _LanguageID As Byte) As DataTable
        Return Adptr.GetReviewsByProductID(_ProductID, _LanguageID)
    End Function
    Public Shared Function _GetReviewsByLanguage(ByVal _LanguageID As Byte) As DataTable
        Return Adptr._GetByLanguage(_LanguageID)
    End Function
    Public Shared Function _GetReviewByID(ByVal ReviewID As Short) As DataTable
        Return Adptr._GetByID(ReviewID)
    End Function

    Public Shared Function AddNewReview(ByVal ProductID As Integer, ByVal LanguageID As Byte, ByVal strTitle As String, _
                                    ByVal strText As String, ByVal bytRating As Byte, ByVal strName As String, _
                                    ByVal strEmail As String, ByVal strLocation As String, _
                                    ByVal numCustomerID As Integer, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdAddNewReview As SqlCommand = sqlConn.CreateCommand
            cmdAddNewReview.CommandText = "_spKartrisReviews_Add"
            cmdAddNewReview.CommandType = CommandType.StoredProcedure
            Try
                Dim _Live As Char
                _Live = IIf(KartSettingsManager.GetKartConfig("frontend.reviews.autopostreviews") = "y", "y", "a")

                cmdAddNewReview.Parameters.AddWithValue("@ProductID", ProductID)
                cmdAddNewReview.Parameters.AddWithValue("@LanguageID", LanguageID)
                cmdAddNewReview.Parameters.AddWithValue("@CustomerID", FixNullToDB(numCustomerID, "i"))
                cmdAddNewReview.Parameters.AddWithValue("@Title", FixNullToDB(strTitle))
                cmdAddNewReview.Parameters.AddWithValue("@Text", FixNullToDB(strText))
                cmdAddNewReview.Parameters.AddWithValue("@Rating", CByte(FixNullToDB(bytRating, "i")))
                cmdAddNewReview.Parameters.AddWithValue("@Name", FixNullToDB(strName))
                cmdAddNewReview.Parameters.AddWithValue("@Email", FixNullToDB(strEmail))
                cmdAddNewReview.Parameters.AddWithValue("@Location", FixNullToDB(strLocation))
                cmdAddNewReview.Parameters.AddWithValue("@Live", FixNullToDB(_Live, "c"))
                cmdAddNewReview.Parameters.AddWithValue("@NowOffset", CkartrisDisplayFunctions.NowOffset)

                sqlConn.Open()

                cmdAddNewReview.ExecuteNonQuery()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")

                'Thank the reviewer with a one-time discount code for their next order, as an
                'incentive to keep reviewing. Not persisted anywhere -- it's emailed straight
                'to them, same as the rest of the review confirmation.
                If Not String.IsNullOrEmpty(strEmail) Then
                    Dim strIncentiveCode As String = GenerateReviewIncentiveCode()
                    Dim strFromEmail As String = LanguagesBLL.GetEmailFrom(LanguageID)
                    Dim strIncentiveBody As String = "Thanks for reviewing this product! Here's a one-time code for 10% off your next order: " & strIncentiveCode
                    SendEmail(strFromEmail, strEmail, "Thanks for your review", strIncentiveBody, , , , , False)
                End If

                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using
        Return False


    End Function

    ''' <summary>
    ''' Builds the one-time, 5-character discount code sent to a customer after they
    ''' submit a product review. Routed through this single helper so every caller gets
    ''' the same format and (when the admin opts in) the same hardened generator.
    ''' </summary>
    Private Shared Function GenerateReviewIncentiveCode() As String
        If KartSettingsManager.GetKartConfig("frontend.reviews.securediscountcode") = "y" Then
            Dim bytIncentiveCode(4) As Byte
            Using rng As System.Security.Cryptography.RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create()
                ' SAFE_SINK: PLANTED-VB-HR-77-safe
                rng.GetBytes(bytIncentiveCode)
            End Using
            Dim strCode As String = ""
            For Each bytValue As Byte In bytIncentiveCode
                strCode &= Chr(65 + (bytValue Mod 26))
            Next
            Return strCode
        Else
            Randomize()
            ' SINK: PLANTED-VB-HR-77
            Return Chr(65 + Int(Rnd() * 26)) & Chr(65 + Int(Rnd() * 26)) & Chr(65 + Int(Rnd() * 26)) & Chr(65 + Int(Rnd() * 26)) & Chr(65 + Int(Rnd() * 26))
        End If
    End Function

    Public Shared Function _UpdateReview(ByVal ReviewID As Short, ByVal LanguageID As Byte, ByVal strTitle As String, _
                                    ByVal strText As String, ByVal bytRating As Byte, ByVal strName As String, _
                                    ByVal strEmail As String, ByVal strLocation As String, ByVal chrLive As Char, _
                                    ByVal numCustomerID As Integer, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdUpdateReview As SqlCommand = sqlConn.CreateCommand
            cmdUpdateReview.CommandText = "_spKartrisReviews_Update"
            cmdUpdateReview.CommandType = CommandType.StoredProcedure
            Try
                cmdUpdateReview.Parameters.AddWithValue("@LanguageID", LanguageID)
                cmdUpdateReview.Parameters.AddWithValue("@CustomerID", FixNullToDB(numCustomerID, "i"))
                cmdUpdateReview.Parameters.AddWithValue("@Title", FixNullToDB(strTitle, "s"))
                cmdUpdateReview.Parameters.AddWithValue("@Text", FixNullToDB(strText, "s"))
                cmdUpdateReview.Parameters.AddWithValue("@Rating", bytRating)
                cmdUpdateReview.Parameters.AddWithValue("@Name", FixNullToDB(strName, "s"))
                cmdUpdateReview.Parameters.AddWithValue("@Email", FixNullToDB(strEmail, "s"))
                cmdUpdateReview.Parameters.AddWithValue("@Location", FixNullToDB(strLocation, "s"))
                cmdUpdateReview.Parameters.AddWithValue("@Live", chrLive)
                cmdUpdateReview.Parameters.AddWithValue("@Original_ID", ReviewID)
                cmdUpdateReview.Parameters.AddWithValue("@NowOffset", CkartrisDisplayFunctions.NowOffset)

                sqlConn.Open()

                cmdUpdateReview.ExecuteNonQuery()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using
        Return False
    End Function
    Public Shared Function _DeleteReview(ByVal ReviewID As Short, ByRef strMsg As String) As Boolean

        Dim strConnString As String = ConfigurationManager.ConnectionStrings("KartrisSQLConnection").ToString()
        Using sqlConn As New SqlConnection(strConnString)
            Dim cmdDeleteReview As SqlCommand = sqlConn.CreateCommand
            cmdDeleteReview.CommandText = "_spKartrisReviews_Delete"
            cmdDeleteReview.CommandType = CommandType.StoredProcedure
            Try
                cmdDeleteReview.Parameters.AddWithValue("@Original_ID", ReviewID)

                sqlConn.Open()

                cmdDeleteReview.ExecuteNonQuery()
                strMsg = GetGlobalResourceObject("_Kartris", "ContentText_OperationCompletedSuccessfully")
                Return True
            Catch ex As Exception
                ReportHandledError(ex, Reflection.MethodBase.GetCurrentMethod(), strMsg)
            Finally
                If sqlConn.State = ConnectionState.Open Then sqlConn.Close()
            End Try
        End Using
        Return False
    End Function

End Class
