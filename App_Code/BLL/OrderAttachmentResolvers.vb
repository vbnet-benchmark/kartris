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
''' Base type for resolving an order attachment's on-disk path from a client-supplied file
''' reference. A CWE-73 call site built against this hierarchy has its exploitability
''' decided entirely by which concrete subclass the call site's variable was constructed
''' as -- ResolvePath itself is otherwise opaque to the caller.
''' </summary>
Public MustInherit Class OrderAttachmentResolverBase
    Public MustOverride Function ResolvePath(ByVal intOrderID As Integer, ByVal strFileRef As String) As String
End Class

''' <summary>
''' Legacy resolver kept so an older order-management client (one that never learned to
''' send a scoped order id with its delete requests) keeps working. Resolves the attachment
''' path directly from the raw file reference with NO check that the referenced attachment
''' actually belongs to intOrderID -- or to any order at all.
''' </summary>
Public Class LegacyOrderAttachmentResolver
    Inherits OrderAttachmentResolverBase

    Public Overrides Function ResolvePath(ByVal intOrderID As Integer, ByVal strFileRef As String) As String
        Return Current.Server.MapPath(GetKartConfig("general.uploadfolder") & "OrderAttachments/" & strFileRef)
    End Function
End Class

''' <summary>
''' Safe counterpart: the reference is reduced via Path.GetFileName ("."/".."/empty
''' explicitly rejected) and only resolved to a real path when
''' <see cref="OrderAttachmentsRegistry"/> confirms the resolved attachment's own order id
''' matches the order id the caller claims to be working on; otherwise returns Nothing so
''' the caller skips the deletion entirely.
''' </summary>
Public Class ScopedOrderAttachmentResolver
    Inherits OrderAttachmentResolverBase

    Public Overrides Function ResolvePath(ByVal intOrderID As Integer, ByVal strFileRef As String) As String
        Dim strSafeName As String = Path.GetFileName(strFileRef)
        If String.IsNullOrEmpty(strSafeName) OrElse strSafeName = "." OrElse strSafeName = ".." Then Return Nothing
        Dim intOwnerOrderID As Integer = OrderAttachmentsRegistry.GetOwnerOrderID(strSafeName)
        If intOwnerOrderID <= 0 OrElse intOwnerOrderID <> intOrderID Then Return Nothing
        Return Current.Server.MapPath(GetKartConfig("general.uploadfolder") & "OrderAttachments/" & strSafeName)
    End Function
End Class

''' <summary>
''' Server-controlled registry mapping a real order-attachment file name to the order id it
''' actually belongs to -- the authoritative scope check <see cref="ScopedOrderAttachmentResolver"/>
''' uses. A fixed mapping table, never derived from client input.
''' </summary>
Public Class OrderAttachmentsRegistry
    Private Shared ReadOnly Mapping As New Dictionary(Of String, Integer) From {
        {"po_confirmation_4471.pdf", 4471},
        {"po_confirmation_4488.pdf", 4488},
        {"proof_of_payment_4471.png", 4471}
    }

    Public Shared Function GetOwnerOrderID(ByVal strFileName As String) As Integer
        Dim intOwnerID As Integer = 0
        If Mapping.TryGetValue(strFileName, intOwnerID) Then Return intOwnerID
        Return 0
    End Function
End Class
