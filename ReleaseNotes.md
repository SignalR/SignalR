# SignalR Release Notes

# 1.1.2

### Bugs Fixed

* Scaleout Message Bus Memory Leak ([#2070](https://github.com/SignalR/SignalR/issues/2070))

# 1.1.1

### Bugs Fixed

* SignalR Groups.Remove doesn't work ([#2040](https://github.com/SignalR/SignalR/issues/2040))
* Changing the number of streams/topics/tables crashes the process when using scaleout ([#2024](https://github.com/SignalR/SignalR/issues/2024))

# 1.1.0

### Features

* Add tracing to Service Bus message bus ([#1912](https://github.com/SignalR/SignalR/issues/1912))
* Add more scaleout performance counters ([#1833](https://github.com/SignalR/SignalR/issues/1833))
* Ensure fairness in scale-out message delivery using arrival time stamp for sorting ([#1807](https://github.com/SignalR/SignalR/issues/1807))
* Support generating an install script  ([#1793](https://github.com/SignalR/SignalR/issues/1793))

### Bugs Fixed

* [LP] - ConcurrentCalls causing leak on server side ([#1993](https://github.com/SignalR/SignalR/issues/1993))
* 'r' is undefined ([#1975](https://github.com/SignalR/SignalR/issues/1975))
* Duplicates messages received when using 2 machines ([#1973](https://github.com/SignalR/SignalR/issues/1973))
* Memory leak in connection tracking when using scaleout ([#1953](https://github.com/SignalR/SignalR/issues/1953))
* Missing messages when sending to groups ([#1948](https://github.com/SignalR/SignalR/issues/1948))
* ServiceBusMessageBus should delete subscriptions when the application shuts down ungracefully ([#1939](https://github.com/SignalR/SignalR/issues/1939))
* Only unwrap one level of exceptions when returning hub errors to client ([#1919](https://github.com/SignalR/SignalR/issues/1919))
* in SqlScaleoutConfiguration it should throw when TableCount < 1 ([#1888](https://github.com/SignalR/SignalR/issues/1888))
* Refactor SQL message bus to use events instead of delegate passing ([#1879](https://github.com/SignalR/SignalR/issues/1879))
* LP/SSE hang when using a reasonable value for ServicePoint DefaultConnectionLimit ([#1874](https://github.com/SignalR/SignalR/issues/1874))
* Unobserved ODE after manually stopping a connection on the .NET client ([#1848](https://github.com/SignalR/SignalR/issues/1848))
* Overwhelming the queue can cause issues sending to other clients ([#1847](https://github.com/SignalR/SignalR/issues/1847))
* The WebSocket instance cannot be used for communication because it has been transitioned into an invalid state. ([#1846](https://github.com/SignalR/SignalR/issues/1846))
* Validate ServiceBusScaleoutConfiguration members ([#1806](https://github.com/SignalR/SignalR/issues/1806))
* JS client webSockets /serverSendEvents /foreverFrame don't append /reconnect but append /poll in url when reconnect, however functional still work ([#1794](https://github.com/SignalR/SignalR/issues/1794))
* Update RedisMessageBus to use latest version of Booksleeve ([#1788](https://github.com/SignalR/SignalR/issues/1788))
* LongPollingTransport threw ObjectDisposedException in AbortResetEvent.Set() ([#1691](https://github.com/SignalR/SignalR/issues/1691))
* Redis message bus can result in missed messages due to messages received out of order from backplane ([#1676](https://github.com/SignalR/SignalR/issues/1676))


# 1.1.0beta

### General Changes
* Performance improvements in throughput and memory usage.
* New scaleout provider infrastructure and providers for Redis, SqlServer and Azure Service Bus.

### Known Issues
* When the backplane goes offline (redis, sql, service bus) you may miss messages.

### Features

* Add overload to MapHubs and MapConnection to allow adding OWIN middleware ([#1800](https://github.com/SignalR/SignalR/issues/1800))
* Removed Connection.Disconnect() ([#1798](https://github.com/SignalR/SignalR/issues/1798))
* Force <= IE8 to use LongPolling ([#1764](https://github.com/SignalR/SignalR/issues/1764))
* Support common scale-out message bus semantics in an abstract base class ([#1712](https://github.com/SignalR/SignalR/issues/1712))
* Support SQL Server scale-out message bus ([#1711](https://github.com/SignalR/SignalR/issues/1711))
* Make SQL message bus light up with query notifications only if service broker is available ([#1662](https://github.com/SignalR/SignalR/issues/1662))
* Allow specifiying the JsonSerializer in the .NET Client ([#1373](https://github.com/SignalR/SignalR/issues/1373))
* Allow specifying headers in the .NET client ([#1362](https://github.com/SignalR/SignalR/issues/1362))
* Allow specifying client certificates in the .NET Client ([#1303](https://github.com/SignalR/SignalR/issues/1303))
* Support client side keep alive for the .NET client ([#741](https://github.com/SignalR/SignalR/issues/741))
* Add tracing to the .NET client ([#135](https://github.com/SignalR/SignalR/issues/135))
* Add trace level to the .NET client ([#1746](https://github.com/SignalR/SignalR/issues/1746))

### Bugs Fixed

* ConnectDisconnect test failed due to a leak on w3wp.exe ([#1801](https://github.com/SignalR/SignalR/issues/1801))
* Update error messages based on customer feedback ([#1734](https://github.com/SignalR/SignalR/issues/1734))
* Hub script isn't minified ([#1710](https://github.com/SignalR/SignalR/issues/1710))
* Specifying the hubs url for a connection url should fail ([#1707](https://github.com/SignalR/SignalR/issues/1707))
* JS client webSockets and SSE transports only try request one time to connect during reconnect  ([#1684](https://github.com/SignalR/SignalR/issues/1684))
* PerformanceCounter.RemoveInstance throws on mono ([#1678](https://github.com/SignalR/SignalR/issues/1678))
* in HubContext Clients.Client(null).foo() doesn't throw ([#1660](https://github.com/SignalR/SignalR/issues/1660))
* .NET client: Calling Stop before connecting fails ([#1650](https://github.com/SignalR/SignalR/issues/1650))
* [WebSockets] AbortStopDisconnect scenario failing:  System.InvalidOperationException: Incorrect message type ([#1607](https://github.com/SignalR/SignalR/issues/1607))
* .NET client: OnError can be raised when stopping SSE connection  ([#1600](https://github.com/SignalR/SignalR/issues/1600))
* Get rid of all synchronous reads/writes on the client ([#1536](https://github.com/SignalR/SignalR/issues/1536))
* Group tokens are not verified against the current connection ID on reconnect ([#1506](https://github.com/SignalR/SignalR/issues/1506))
* Unhandled exceptions within service bus backplane ([#1504](https://github.com/SignalR/SignalR/issues/1504))
* Optimize hub method invocation ([#1486](https://github.com/SignalR/SignalR/issues/1486))
* Extending javascript array potentially breaks other code ([#1392](https://github.com/SignalR/SignalR/issues/1392))
* IE9 object serialization on persistant connection received ([#1388](https://github.com/SignalR/SignalR/issues/1388))
* Hub Clients.Client(null).foo() should throw same as PersistentConnection Connection.Send(null, message) ([#1265](https://github.com/SignalR/SignalR/issues/1265))
* Multiple connections don't work on the same page in some cases ([#1243](https://github.com/SignalR/SignalR/issues/1243))
* Client infinitely receive same messages from server when publish message with same Id ([#948](https://github.com/SignalR/SignalR/issues/948))
* jQuery $.ajaxSetup issue ([#947](https://github.com/SignalR/SignalR/issues/947))
* SqlServer - Use real schema instead of deprecated "dbo" ([#943](https://github.com/SignalR/SignalR/issues/943))
* SignalR Redis doesn't reconnect to Redis server after stop and re-start Redis server ([#916](https://github.com/SignalR/SignalR/issues/916))
* Scaleout Bus mapping grows infinitely ([#723](https://github.com/SignalR/SignalR/issues/723))
* Several browsers show infinite loading status (IE8) ([#215](https://github.com/SignalR/SignalR/issues/215))

# 1.0.1

### Bugs Fixed

* Connection Id not read properly when additional query string parameters are added ([#1556](https://github.com/SignalR/SignalR/issues/1556))
* WebSocket leaks if Close throws an exception ([#1554](https://github.com/SignalR/SignalR/issues/1554))
* CancellationTokenSource has been disposed ([#1549](https://github.com/SignalR/SignalR/issues/1549))
* Regression: State doesn't work when accessed as dictionary without casting ([#1545](https://github.com/SignalR/SignalR/issues/1545))
* Don't catch exceptions thrown from user-defined OnReceived handlers on the JS client ([#1530](https://github.com/SignalR/SignalR/issues/1530))
* Calling connection.stop() with the serverSentEvent transport on Opera raises a TypeError. ([#1519](https://github.com/SignalR/SignalR/issues/1519))
* Doc summary for BuildRejoiningGroups is wrong ([#1505](https://github.com/SignalR/SignalR/issues/1505))
* When converting JRaw to concrete type use default serialization settings that container a max depth ([#1484](https://github.com/SignalR/SignalR/issues/1484))
* AV in ForeverTransport<ProcessMessages> under stress ([#1479](https://github.com/SignalR/SignalR/issues/1479))
* JS client version property is incorrect ([#1476](https://github.com/SignalR/SignalR/issues/1476))
* Throw the "Not a websocket request" exception synchronously ([#1440](https://github.com/SignalR/SignalR/issues/1440))
* JS client with jQuery 1.9.0 raises connection error for all sends on persistent connection API ([#1437](https://github.com/SignalR/SignalR/issues/1437))
* Groups.Add.Wait fails, http.sys logs shows "Connection_Dropped" with no apparent reason. ([#1435](https://github.com/SignalR/SignalR/issues/1435))
* .NET client OnError is raised when calling connection.Stop and using WebSocket transport ([#1397](https://github.com/SignalR/SignalR/issues/1397))
* Fuzz test the cursor parsing logic. ([#1307](https://github.com/SignalR/SignalR/issues/1307))
* Improve the error message "Incompatible protocol version". ([#1176](https://github.com/SignalR/SignalR/issues/1176))
* PerformanceCounterCategory.Exists hangs ([#1158](https://github.com/SignalR/SignalR/issues/1158))
* SignalR .NET Client HubConnection looping between Reconnecting and Connected ([#1121](https://github.com/SignalR/SignalR/issues/1121))

# 1.0

### Breaking Changes

* Disable CORS by default ([#1306](https://github.com/SignalR/SignalR/issues/1306))

```csharp
var config = new HubConfiguration 
{ 
    EnableCrossDomain = true
} 
RouteTable.Routes.MapHubs(config);
```

* Don't return error text by default for hub errors ([#923](https://github.com/SignalR/SignalR/issues/923))
  - This means that exception messages are turned off by default

```csharp
var config = new HubConfiguration 
{ 
    EnableDetailedErrors = true
} 

RouteTable.Routes.MapHubs(config); 
```
* **EnableAutoRejoiningGroups** has been removed from HubPipeline. This feature is turned on by default. The groups payload is signed to the client
  cannot spoof groups anymore.


### Bugs Fixed

* Send nosniff header for all SignalR responses ([#1450](https://github.com/SignalR/SignalR/issues/1450))
* Hub state and args are placed into a dictionary directly from user input from the URL ([#1449](https://github.com/SignalR/SignalR/issues/1449))
  - JSON.NET 20 is the limit of recursion
  - 4096 KB is maxium size JSON payload
* HubDispatcher allows duplicate hub names in connectionData ([#1448](https://github.com/SignalR/SignalR/issues/1448))
  - If you have duplicate HubNames in your HubConnection, you will get an exception Duplicate hub names found
* ForeverFrame transport should validate frameId passed through the URL ([#1446](https://github.com/SignalR/SignalR/issues/1446))
* Route matching for the Owin hub dispatcher handler is too agressive ([#1445](https://github.com/SignalR/SignalR/issues/1445))
* JSONP callback method should be validated as valid JS identifier ([#1444](https://github.com/SignalR/SignalR/issues/1444))
* Hub method discovery includes methods it shouldn't ([#1443](https://github.com/SignalR/SignalR/issues/1443))
* Add CSRF protection for SignalR requests ([#1413](https://github.com/SignalR/SignalR/issues/1413))
* AV at Microsoft.Owin.Host.SystemWeb.OwinCallContext.StartOnce ([#1402](https://github.com/SignalR/SignalR/issues/1402))
* WebSocket leak HttpContext even though DefaultWebSocketHandler is closed ([#1387](https://github.com/SignalR/SignalR/issues/1387))
* Bug with same origin check behind reverse proxies/load balancers etc. ([#1363](https://github.com/SignalR/SignalR/issues/1363))
* Add summary in public AuthorizeAttribute class ([#1353](https://github.com/SignalR/SignalR/issues/1353))
* Infer hubs path from the url ([#1346](https://github.com/SignalR/SignalR/issues/1346))
* Sign the groups ([#1328](https://github.com/SignalR/SignalR/issues/1328))
* End the request, not connection as soon as cancellation token trips. ([#1327](https://github.com/SignalR/SignalR/issues/1327))
* Prefix for group from PersistentConnectionContext is not right ([#1326](https://github.com/SignalR/SignalR/issues/1326))
* Throw in ASP.NET 4 if the purpose isn't connection id and groups. ([#1325](https://github.com/SignalR/SignalR/issues/1325))
* Prevent connections from subscribing to a group that's actually a valid connection ID or Hub name ([#1320](https://github.com/SignalR/SignalR/issues/1320))
* Ensure that we don't allow clients to provide duplicate signals in cursors ([#1312](https://github.com/SignalR/SignalR/issues/1312))
* Add Authorize method to PersistentConnection. ([#1308](https://github.com/SignalR/SignalR/issues/1308))
  - Added Authorize and AuthorizeRequest method to PersistentConnection. 
  - This is the place in the pipeline to authorize requests. If a request fails authorization then a 403 is returned.
* Consider signing the connection id ([#1305](https://github.com/SignalR/SignalR/issues/1305))
* Change LongPollingTransport.Send to be non-virtual ([#1292](https://github.com/SignalR/SignalR/issues/1292))
* Change TopicMessageBus use of array to IList<T> ([#1291](https://github.com/SignalR/SignalR/issues/1291))
* Change Subscription.PerformWork to take a IList instead of List ([#1290](https://github.com/SignalR/SignalR/issues/1290))
* Change Linktionary to IndexedDictionary ([#1289](https://github.com/SignalR/SignalR/issues/1289))
* Investigate changing all uses of IEnumerable<T> in the API to IList<T> ([#1288](https://github.com/SignalR/SignalR/issues/1288))
* Change IHubIncomingInvokerContext.Args to IList<object> ([#1287](https://github.com/SignalR/SignalR/issues/1287))
* Change DefaultJavaScriptProxyGenerator.GetDescriptorName to non-vritual ([#1286](https://github.com/SignalR/SignalR/issues/1286))
* Change Subscription.Data to Received in .NET client ([#1285](https://github.com/SignalR/SignalR/issues/1285))
* Change .NET Client uses of arrays to IList ([#1284](https://github.com/SignalR/SignalR/issues/1284))
* Change IConnection.Groups to IList<T> ([#1282](https://github.com/SignalR/SignalR/issues/1282))
* Change ConnectionMessage.ExcludedSignals to IList<string> ([#1278](https://github.com/SignalR/SignalR/issues/1278))
* Add overloads for methods with params array on hot paths ([#1277](https://github.com/SignalR/SignalR/issues/1277))
* Client webSockts and SSE transports, after reconnected, Disconnect Command from server causes the reconnected connection to disconnect ([#1273](https://github.com/SignalR/SignalR/issues/1273))
* Loading Resources in Windows Ph8/Windows Store applications ([#1232](https://github.com/SignalR/SignalR/issues/1232))
* Long Polling leaking requests sometimes ([#1164](https://github.com/SignalR/SignalR/issues/1164))
* Signalr.exe to generate hub proxy only works for webapplication projects
* jquery.signalr.1.0.0.js file has the version specified as 1.0.0.rc1. The version is actually 1.0.0

* **ScaleOut with Redis/Service Bus**
  - Scale Out with SignalR using ServiceBus or Redis
  - Scaleout using Azure Service Bus or Redis is still under development. If you are using 1.0.0-RC2 versions of the ScaleOut packages then please upgrade to 1.0.0-RC3 if you want to use SignalR 1.0.0

### Known Issues
* JS client with jQuery 1.9.1 / 1.9.0 raises connection error for all sends on persistent connection API ([#1437](https://github.com/SignalR/SignalR/issues/1437)

# 1.0rc2

### Notable Commits
* Removed checks for the GAC and dynamic assemblies. ([2cb4491fc6](https://github.com/SignalR/SignalR/commit/2cb4491fc647c9816896093b394baf4eaa33df67))
* Modified KeepAlive to be an integer instead of a TimeSpan. ([a27f41f327](https://github.com/SignalR/SignalR/commit/a27f41f3271d67ce08fb6e82741d77e637bf7753))
* Added total topics performance counter. ([ece78b804c](https://github.com/SignalR/SignalR/commit/ece78b804cf44a7348560d403282e77832ae9164))
* Changing default message size to 1000. ([88f7134b8e](https://github.com/SignalR/SignalR/commit/88f7134b8e0a80758dc763116b5f25b36d977551))
* Exposed GetHttpContext extension method from SystemWeb assembly. ([6ea4b20e87](https://github.com/SignalR/SignalR/commit/6ea4b20e872d8116113791c15a52dbda68d27b07))
* Remove ServerVariables from IRequest. ([7f4969c6a8](https://github.com/SignalR/SignalR/commit/7f4969c6a8fb32b4c1216efec9d44550c5171f02))
* Fix some issues with SignalR's owin request impl on mono. ([b5c05bc6bf](https://github.com/SignalR/SignalR/commit/b5c05bc6bfee3ed590633a8f71c9a3e8038ab034))

### Features

* Added support for windows phone 8.
* Add websocket support for the .NET client. ([#985](https://github.com/SignalR/SignalR/issues/985))
* Ability to prevent auto generated Javascript proxies. ([#978](https://github.com/SignalR/SignalR/issues/978))
* Remove the Mode parameter from AuthorizeAttribute. ([#956](https://github.com/SignalR/SignalR/issues/956))

### Breaking Changes
* Moved several types into different namespaces. ([524e606e7f](https://github.com/SignalR/SignalR/commit/524e606e7f9f18e8a7b40d62d590c904a94eb30e))
* MapHubs and MapConnection no longer take route parameters. They are just prefixes. ([b7b1371a2a](https://github.com/SignalR/SignalR/commit/b7b1371a2aa0d002ff33138bb0d20115c7f49da6))

### Bugs Fixed

* Validate that connection IDs are in correct format in PersistentConnection on all requests. ([#1298](https://github.com/SignalR/SignalR/issues/1298))
* Remove "Async" from all member names. ([#1276](https://github.com/SignalR/SignalR/issues/1276))
* WebSocket transport: Unclean disconnects are being treated as clean disconnects. ([#1254](https://github.com/SignalR/SignalR/issues/1254))
* JS client longPolling can't reconnect when server process restarts except after the first rebuild. ([#1246](https://github.com/SignalR/SignalR/issues/1246))
* Registry exception. ([#1244](https://github.com/SignalR/SignalR/issues/1244))
* JS Client: LongPolling OnConnected message doesn't make it to client. ([#1241](https://github.com/SignalR/SignalR/issues/1241))
* In JS client, Group names are not encoded in the querystring when a transport reconnects. ([#1233](https://github.com/SignalR/SignalR/issues/1233))
* SL5 client is not working. it fails to load json.net. ([#1223](https://github.com/SignalR/SignalR/issues/1223))
* Interval between keep alive missed and notifying transport seems to small. ([#1211](https://github.com/SignalR/SignalR/issues/1211))
* "+" sign in a string gets transformed to a space. ([#1194](https://github.com/SignalR/SignalR/issues/1194))
* LP: Clients cannot receive messages on client if message is sent right after start. ([#1185](https://github.com/SignalR/SignalR/issues/1185))
* Fix issues with growing number of dead connections consuming too much memory. ([#1177](https://github.com/SignalR/SignalR/issues/1177))
* JS Client: Base events (reconnecting, connected, received etc.) are not unique to connection objects. ([#1173](https://github.com/SignalR/SignalR/issues/1173))
* PerformanceCounterCategory.Exists hangs. ([#1158](https://github.com/SignalR/SignalR/issues/1158))
* JS client function isCrossDomain returns true for same website /host url. ([#1156](https://github.com/SignalR/SignalR/issues/1156))
* Waiting on multiple commands in OnConnectedAsync causes a TaskCanceledException in ForeverTransports (SSE, FF, WS). ([#1155](https://github.com/SignalR/SignalR/issues/1155))
* JS client can't receive messages after reconnected for network disconnect and re-connected. ([#1144](https://github.com/SignalR/SignalR/issues/1144))
* .NET client fails auto-negotiation fallback. ([#1125](https://github.com/SignalR/SignalR/issues/1125))
* Deadlock in .NET client websocket stop logic. ([#1120](https://github.com/SignalR/SignalR/issues/1120))
* Remove MozWebSocket check in javascript websocket transport. ([#1119](https://github.com/SignalR/SignalR/issues/1119))
* OutOfMemoryException after sending a lot of huge messages. ([#1114](https://github.com/SignalR/SignalR/issues/1114))
* Don't create topics when publishing. ([#1071](https://github.com/SignalR/SignalR/issues/1071))
* Unseal AuthorizeAttribute. ([#1050](https://github.com/SignalR/SignalR/issues/1050))
* Topic objects remain in Active state and never clean up after all clients disconnected. ([#1001](https://github.com/SignalR/SignalR/issues/1001))
* Remove the Mode parameter from AuthorizeAttribute. ([#956](https://github.com/SignalR/SignalR/issues/956))
* on IE10/9 foreverFrame transport connection can't receive message after network disconnect time and network re-connect. ([#820](https://github.com/SignalR/SignalR/issues/820))


# 1.0rc1
* Drain pending writes before ending the request to avoid AVs. ([010c4f6750](https://github.com/SignalR/SignalR/commit/010c4f675039bcf06bde9b8e68f8178f86501de4))
* Bubble canceled tasks via hubs back to the client. ([c1e8e6834b](https://github.com/SignalR/SignalR/commit/c1e8e6834b9ddb182d751a04c5ea382d1e3c8668))
* Expose Request to the HubCallerContext. ([5ec61eb37c](https://github.com/SignalR/SignalR/commit/5ec61eb37c16369976ba66f4d5262a0bf345acfc))
* Removed Hosting.Common altogether. ([acebe530a4](https://github.com/SignalR/SignalR/commit/acebe530a4502303ffb6b669dce0a73bf2ef172d))
* Fixed deadlock in ForeverTransport. ([11da5b71a3](https://github.com/SignalR/SignalR/commit/11da5b71a3d89d5947d6797f7a0176d6d7e688e9))
* Drop silverlight4 package from build. ([33cc5e06d9](https://github.com/SignalR/SignalR/commit/33cc5e06d952d3d242de13f3728ea6843964dcd5))
* Change the default keep alive to 15 seconds. ([6741075e63](https://github.com/SignalR/SignalR/commit/6741075e630e96f8a4d96c336da19dd886b27f15))
* Server configured DisconnectTimeout is not honored by JavaScript client. ([#1086](https://github.com/SignalR/SignalR/issues/1086))
* RegisterHubs hangs for a while. ([#1063](https://github.com/SignalR/SignalR/issues/1063))
* Firefox 5 fails when stopping forever frame. ([#1060](https://github.com/SignalR/SignalR/issues/1060))
* Connection never disconnects if data is being sent to it. ([#1046](https://github.com/SignalR/SignalR/issues/1046))
* Unable to distribute to groups if transport is longpolling and HubName attribute is used. ([#1039](https://github.com/SignalR/SignalR/issues/1039))
* When an invalid transport is specified $.connection.hub.start() will auto negotiate. It should throw an error and stop the connection. ([#1037](https://github.com/SignalR/SignalR/issues/1037))
* When reconnecting, don't trigger on error for longpolling. ([#1036](https://github.com/SignalR/SignalR/issues/1036))
* Consider removing IRequestCookieCollection. ([#1034](https://github.com/SignalR/SignalR/issues/1034))
* Expose generic items bag on IRequest. ([#1033](https://github.com/SignalR/SignalR/issues/1033))
* The CancellationTokenSource has been disposed. ([#1020](https://github.com/SignalR/SignalR/issues/1020))
* MessageBusExtensions.cs disposer.Dispose() throws ObjectDisposedException. ([#1005](https://github.com/SignalR/SignalR/issues/1005))
* Remove dynamic support from .NET client. ([#996](https://github.com/SignalR/SignalR/issues/996))
* Add Version Property in JS Library. ([#994](https://github.com/SignalR/SignalR/issues/994))
* .Net Client Send() task hangs when server operation throws. ([#991](https://github.com/SignalR/SignalR/issues/991))
* Remove BOM encoding from output streams. ([#982](https://github.com/SignalR/SignalR/issues/982))
* Fix script escaping script files in forever frame. ([#950](https://github.com/SignalR/SignalR/issues/950))
* If no messages received after connection joins group and then disconnect, the connection won't be in the group. ([#938](https://github.com/SignalR/SignalR/issues/938))
* We do not escape the Message Id Parameter on the JS client. ([#937](https://github.com/SignalR/SignalR/issues/937))
* Joining group with hash in the name fails ([#935](https://github.com/SignalR/SignalR/issues/935))
* Optimized message format ([#887](https://github.com/SignalR/SignalR/issues/887))
* IE throws 'Access denied' on foreverFrame ([#873](https://github.com/SignalR/SignalR/issues/873))
* Native heap corruption while flushing from ForeverFrameTransport.KeepAlive ([#854](https://github.com/SignalR/SignalR/issues/854))
* First chance exception when webSockets connection closes. ([#821](https://github.com/SignalR/SignalR/issues/821))
* Use OWIN host exclusivly. ([9f9a18089f](https://github.com/SignalR/SignalR/commit/9f9a18089f8564926ace3a3a656efc4de51d0793))
* WebSocket closes ungracefully with no error when trying to call hub method with wrong name or parameters. ([#440](https://github.com/SignalR/SignalR/issues/440))

# 0.5.3 (Official Release)
* Improve logging for hubs in js client. ([#505](https://github.com/SignalR/SignalR/issues/505))
* Can't pass querystring when creating HubConnection. ([#581](https://github.com/SignalR/SignalR/issues/581))
* Improve errors for .NET client. ([#515](https://github.com/SignalR/SignalR/issues/515))
* Make http streaming work with Win8 client. ([a61ee958ed](https://github.com/SignalR/SignalR/commit/a61ee958edcc34a5b7c20ea0a1063f5170a14f03))
* Fix bugs related to forever loading indicators on various browsers ([#215](https://github.com/SignalR/SignalR/issues/215)), ([#383](https://github.com/SignalR/SignalR/issues/383)), ([#491](https://github.com/SignalR/SignalR/issues/491))
* Force shut down a client, server execution flow didn't go into IDisconnect.Disconnect () at all. ([#511](https://github.com/SignalR/SignalR/issues/511))
* Transport fallback should fail if onFailed called and there's no good fallback. ([#485](https://github.com/SignalR/SignalR/issues/485))
* Turn ReadStreamBuffering off for WP7 and Silverlight to enable SSE support. ([18cb087037](https://github.com/SignalR/SignalR/commit/18cb087037ca25def2e9c7afed1b510577e37beb))
* Connect/Disconnect events not firing in IE ([#501](https://github.com/SignalR/SignalR/issues/501))
* Make dictionaries in bus case in sensitive. ([5916a588f9](https://github.com/SignalR/SignalR/commit/5916a588f901160508f757dd386e57dea33410a3))
* Groups.Add within persistent connection fails under high crank load. ([#388](https://github.com/SignalR/SignalR/issues/388))
* Use the HttpClient stack in silverlight. ([b51a4144db](https://github.com/SignalR/SignalR/commit/b51a4144dba794e8f8ce3242d48799037055eb80))
* Made reconnect timeout configurable on SSE transport. ([6b869a4cd9](https://github.com/SignalR/SignalR/commit/6b869a4cd92ef5c93a1a16a087930026a6b3a6f4))
* Fix race condition when client sends immediately after connecting ([#552](https://github.com/SignalR/SignalR/issues/552))
* Make access to the state thread safe in the .NET client. ([8464514a8d](https://github.com/SignalR/SignalR/commit/8464514a8dab4a02d6134f4d94caf390d6fcb846))
* Abort the request before calling close on the request in the .NET client. ([50ee2b9b6c](https://github.com/SignalR/SignalR/commit/50ee2b9b6c05168473608742d42f2fc6ad03ac6b))
* Throw if CreateProxy called after the connection is started. ([89eb8e492c](https://github.com/SignalR/SignalR/commit/89eb8e492c6362f32b375845eb14fda229011a06))
* Fix caching negotiate response in Silverlight client ([#522](https://github.com/SignalR/SignalR/issues/522))
* Serve silverlight's cross domain policy file for self hosted server. ([eaec182fee](https://github.com/SignalR/SignalR/commit/eaec182fee09cbd038dd68081b0e1c500ac3106c))
* Expose underlying AuthenticationSchemes property ([52dbfbef12](https://github.com/SignalR/SignalR/commit/52dbfbef123c85d901cb955bf3f39b9467ab6657))
* Made exception handling fixes to SSE stream reader (C# client) ([7f0fd4ddc7](https://github.com/SignalR/SignalR/commit/7f0fd4ddc70e6fe2561a3eb6bfd0a9acf3ee4576))
* Make event names and proxy names case insensitive. ([#508](https://github.com/SignalR/SignalR/issues/508))
* Added hub events support to JavaScript client (using hubs without proxy) ([97c984754f](https://github.com/SignalR/SignalR/commit/97c984754f20bd6dda8c343983d679b8f65093c8))
* Made websocket implementation for self host on .NET 4.5. ([e94d091100](https://github.com/SignalR/SignalR/commit/e94d091100a86396d6efa81f72fca6198cbb57f1))
* LongPolling broken on ASP.NET 4.5 ([#496](https://github.com/SignalR/SignalR/issues/496)) 
* Prevented caching ajax requests on reconnect. ([fbfc65371d](https://github.com/SignalR/SignalR/commit/fbfc65371d0febbd822fcfff134edf5c083a78e1))
* Added consistent way to get a strongly typed representation of the connected clients ([fa6d0b533e](https://github.com/SignalR/SignalR/commit/fa6d0b533e7d0adb35374ce58d4cd74b7473537a))

# 0.5.2 (Official Release)
* ForeverFrame reconnect error: null frame reference ([#447](https://github.com/SignalR/SignalR/issues/447))
* Error when clicking link (IE only) ([#446](https://github.com/SignalR/SignalR/issues/446))
* Fixed zombie connections issue with forever transports ([7d5204e55d](https://github.com/SignalR/SignalR/commit/7d5204e55d6293881d6eabe85929d02808083027))
* ForeverFrame transport does not handle embedded ```</script>``` tags properly ([#413](https://github.com/SignalR/SignalR/issues/413))
* Made some modifications to js client for SSE and longpolling transports. ([5d782dc3b4](https://github.com/SignalR/SignalR/commit/5d782dc3b45b31183737ea054bb5b4897ed23b76))
* Duplicate connections on reconnect with SSE transport ([#452](https://github.com/SignalR/SignalR/issues/452))
* Added .NET 3.5 Client ([9e3a95c65f](https://github.com/SignalR/SignalR/commit/9e3a95c65fa64ee149e5929318dc6faacc37ef2e))
* Add server variables to the IRequest abstraction ([#438](https://github.com/SignalR/SignalR/issues/438))
* Forever frame slows down in IE after receiving many messages ([#458](https://github.com/SignalR/SignalR/issues/458))
* Reworked connection tracking logic ([8d27c97792](https://github.com/SignalR/SignalR/commit/8d27c977929ef6964a7926e97b3de6b463cd858f))
* Fix connect bug in IE6 with longpolling ([7ab434c02d](https://github.com/SignalR/SignalR/commit/7ab434c02d7d9015689f8988c9ed8456a91c741b))
* Optimized type conversation so we don't end up parsing JSON twice on hub calls  ([50cefbba42](https://github.com/SignalR/SignalR/commit/50cefbba422a46adb1345bd4f22e49be7ecc52d2))
* Clean up the way we use connection state in the .NET Client ([#474](https://github.com/SignalR/SignalR/issues/474))
* Added an overload to Send that allows passing an object (.NET Client) ([9c171bd8e7](https://github.com/SignalR/SignalR/commit/9c171bd8e77f79755aa43a14d0e5ab77e9e50e4a))
* Don't parse the message ID as a long (.NET client) ([#475](https://github.com/SignalR/SignalR/issues/475))
* Prevent hang on Connection.Start() ([5752d6007b](https://github.com/SignalR/SignalR/commit/5752d6007b8751a3bc723fea0405d7a0994ed82e))
* Stop EventSource before calling onFailed ([5c7536131b](https://github.com/SignalR/SignalR/commit/5c7536131bb2b501fff7d2f8a12194df5ac6705f))
* Fixed issues with cross domain websockets ([#461](https://github.com/SignalR/SignalR/issues/461))
* HeartBeat.MarkConnection(this) called twice per Send(..) in ForeverTransport ([#333](https://github.com/SignalR/SignalR/issues/333))

# 0.5.1 (Official Release)
* Windows RT client support ([#171](https://github.com/SignalR/SignalR/issues/171))
* Fixed Race condition in .NET SSE transport ([#341](https://github.com/SignalR/SignalR/issues/341))
* Added reconnect support for Websocket transport ([#395](https://github.com/SignalR/SignalR/issues/395))
* Implemented clean disconnect support on browser close ([#396](https://github.com/SignalR/SignalR/issues/396))
* Added async flush support for ASP.NET 4.5 ([#402](https://github.com/SignalR/SignalR/issues/402))
* Fixed websockets in WinJS client ([407](https://github.com/SignalR/SignalR/issues/407))
* Added connection state and new state changed event to js and .NET client ([431](https://github.com/SignalR/SignalR/issues/431))
* Check connection state before retrying in js client ([af2ae94133](https://github.com/SignalR/SignalR/commit/af2ae941339082ab21d8ea888e486a3c902e4b34))
* Turn keep alive on by default ([47e17b68ce](https://github.com/SignalR/SignalR/commit/47e17b68ce93d74a690dd9a7f918fddeb842fb02))
* Auto detect cross domain ([0a5c62438b](https://github.com/SignalR/SignalR/commit/0a5c62438bc928452bb61f75553635e5bf9e821e))
* Changed IResponse.Write and End to take ArraySegment<byte> instead of string ([f521fd2e6a](https://github.com/SignalR/SignalR/commit/f521fd2e6af967ce3603244bd7ed8606313f48a5))
* Added hubify.exe to generate the hubs file at build time ([e7672ebb60](https://github.com/SignalR/SignalR/commit/e7672ebb60e826d13528783967a88f9db7af56a2))
* Built WebSockets transport into the core.

## v0.5.0 (Official Release)
* Server Send Events connections not closing ([#369](https://github.com/SignalR/SignalR/issues/369))
* Allow HubConnection to specify hub url ([#368](https://github.com/SignalR/SignalR/issues/368))
* Added current IPrincipal to IRequest. ([e381ef1cb6](https://github.com/SignalR/SignalR/commit/e381ef1cb6c0c13087c221abdfa2833c0ae841a6))
* Remove implicit Send overload from PersistentConnection. ([44ff03aafa](https://github.com/SignalR/SignalR/commit/44ff03aafa5f5e2f3b73a5a19ac5a2437674891b))
* Regression: Method overloads no longer work in hubs because of caching. ([#362](https://github.com/SignalR/SignalR/issues/362))

## v0.5.0 RC (Stable Prerelease)
* Performance: Only register for disconnect for chunked requests on self host. (#352)
* Provide way to override default resolver in ASP.NET other than through routing. (#347)
* Performance: Only subscribe to hubs that have method subscriptions (#346)
* Don't create all hub instances on connect/reconnect/disconnect (#345)
* Hub names are case sensitive. (#344)
* Crappy error message when failing to create a hub (#343)
* Performance: InProcessMessageBus<T>.RemoveExpiredEntries takes N+1 locks to remove N entries (#335)
* Performance: Use DateTime.UtcNow instead of DateTime.Now. (7edef25411)
* Fixed incompatibility with jQuery.Validate (#328, #145).
* .NET Client Fixed race condition in Stop() after connection fails. (cd87d40583)
* ReflectedMethodDescriptor::TryGetMethod Executable Method Caching (#351)
* Disconnect is broken in webfarms (#69)

## v0.5.0 (Stable Prerelease)

### Bugs Fixed
* Missing messages in some cases (#307)
* Exception in .NET client when fiddler attached (#304)
* Calling Send multiple times fails (#260)
* Exception in .NET client SSE impl with large messages (#256)
* Invoking Hub methods with byte[] arguments fail (#245)
* Fixed encoding issues with self host (#244)
* Calling Stop then Start after Start fails doesn't restart .NET client (#226)
* Unobserved task exception on long polling transport when server is restarted during first request (#320)
* Hubs in inner classes don't work (#204)
* LongPolling raises IConnected.Reconnect too many times (#188)
* Issues with large message sizes (#163)
* Regression: Reconnect client after server restart (#24)
* Fixed Guids in Hub method parameters (#194)
* SelfHost Server not recognising remote disconnections (#214)
* Removed double deserialize from .NET client. (fbb3ea615d)

### Features
* Mono support (#58)
* Dynamic hubs implementation (#276)
* Improved serveral APIs to be more consistent (#20)
* Cross domain via jsonp (#6)
* Transports need to send "Keep Alives" (#168)
* Add Current User Identity to SignalR.Hosting.Request (#241)
* Added support fo Silverlight 5 Client (#264)

