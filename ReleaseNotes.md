# SignalR Release Notes

## v0.5.84 RC (Stable Prerelease)
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

## v0.5.63 (Stable Prerelease)

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

