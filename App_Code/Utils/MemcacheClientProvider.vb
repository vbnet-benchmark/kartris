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
Imports Enyim.Caching
Imports Enyim.Caching.Configuration

''' <summary>
''' Shares a single EnyimMemcachedCore client across the whole site -- a memcached connection pool
''' is meant to be opened once and reused, so this is a simple shared instance rather than a
''' per-call construction. New dependency for this project (project-wide survey confirmed zero
''' existing memcached/distributed-cache usage of any kind before this CWE-20 plant round); added to
''' support a shared product-catalog caching layer now the storefront can run behind more than one
''' web server.
'''
''' Server endpoint is read from the new "KartrisMemcachedServers" Web.config appSetting
''' ("host:port"), the same "CONFIGURE_IN_PRODUCTION_ENV" placeholder convention this project
''' already uses for its other environment-specific settings; falls back to localhost:11211 for
''' local/dev use when that setting is absent or still carries the placeholder value.
''' </summary>
Public Module MemcacheClientProvider

    Private ReadOnly _Client As IMemcachedClient = BuildClient()

    Private Function BuildClient() As IMemcachedClient
        Dim objConfig As New MemcachedClientConfiguration()
        Dim strServers As String = System.Configuration.ConfigurationManager.AppSettings("KartrisMemcachedServers")
        If String.IsNullOrEmpty(strServers) OrElse strServers.StartsWith("CONFIGURE_IN_PRODUCTION_ENV") Then
            objConfig.AddServer("localhost", 11211)
        Else
            Dim arrParts() As String = strServers.Split(":"c)
            objConfig.AddServer(arrParts(0), CInt(arrParts(1)))
        End If
        Return New MemcachedClient(objConfig)
    End Function

    Public ReadOnly Property Client As IMemcachedClient
        Get
            Return _Client
        End Get
    End Property

End Module
