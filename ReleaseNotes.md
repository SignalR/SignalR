# SignalR Release Notes

## v0.3.6

### SignalR.Server
* Added static events to PersistentConnection for profiling.
* Performance: Use single timer to keep track of timeouts on the server. (#51)

### SignalR.Client
* Added authentication support (#27)

## v0.3.5
* Added a SignalR client for WP7 (long polling)
* Introduced new programming model for integrating hubs in .NET/WP7 client
* Added group support to .NET client.
* Fixed issue with disconnect not working when long polling connection dies. (#42)
* Fixed issue with exceptions being thrown when parsing invalid JSON data for connections.
* Fixed reliability in .NET client.
