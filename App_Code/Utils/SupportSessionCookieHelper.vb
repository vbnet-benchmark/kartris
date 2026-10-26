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
''' Shared helper for the "remember my support session on this device" and
''' "pin my open tickets to this device" features on CustomerTickets.aspx --
''' builds the cookie(s) carrying a per-customer, per-ticket continuation
''' token so a returning customer can jump back into the support-ticket area
''' without a fresh sign-in prompt. Neither function here adds anything to
''' Response.Cookies or touches .HttpOnly -- that is the caller's job, exactly
''' like the pre-existing, unrelated HttpSecureCookie.ForceLogout() pattern
''' elsewhere in this project where construction and dispatch are kept apart.
''' </summary>
Public NotInheritable Class SupportSessionCookieHelper
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Builds (but does not add to the response) a single cookie that lets a
    ''' returning customer reopen the support-ticket list on this device
    ''' without a fresh sign-in prompt, carrying their customer ID and the
    ''' time the token was issued.
    ''' </summary>
    Public Shared Function BuildSupportSessionCookie(ByVal numCustomerID As Integer) As HttpCookie
        Dim strToken As String = numCustomerID.ToString() & "|" & CkartrisDisplayFunctions.NowOffset.Ticks.ToString()
        Dim cokSession As New HttpCookie(HttpSecureCookie.GetCookieName("SupportSession"), strToken)
        cokSession.Expires = CkartrisDisplayFunctions.NowOffset.AddDays(7)
        Return cokSession
    End Function

    ''' <summary>
    ''' Builds (but does not add to the response) one per-ticket "quick access"
    ''' cookie for each open ticket ID supplied, so a returning customer can
    ''' jump straight back into any of several open tickets from this device
    ''' without a fresh sign-in prompt.
    ''' </summary>
    Public Shared Function BuildPinnedTicketCookies(ByVal numCustomerID As Integer, ByVal arrTicketIDs As List(Of Integer)) As List(Of HttpCookie)
        Dim lstCookies As New List(Of HttpCookie)
        For Each numTicketID As Integer In arrTicketIDs
            Dim strToken As String = numCustomerID.ToString() & "|" & numTicketID.ToString() & "|" & CkartrisDisplayFunctions.NowOffset.Ticks.ToString()
            Dim cokTicket As New HttpCookie(HttpSecureCookie.GetCookieName("PinnedTicket" & numTicketID.ToString()), strToken)
            cokTicket.Expires = CkartrisDisplayFunctions.NowOffset.AddDays(7)
            lstCookies.Add(cokTicket)
        Next
        Return lstCookies
    End Function
End Class
