# SignalR JavaScript Functional Tests

## Running Manually

1. Run `npm install` in this directory
2. Run `dotnet run` in this directory

The output of `dotnet run` will show the URL the server is listening on.

```
Now listening on http://localhost:8989/
Server started, press Ctrl-C to shut down.
```

Open a web browser to that URL to run the tests.

The tests can be directed to a different SignalR server using the `testUrl` query string parameter, so if you want to test the JavaScript client against a server running at `http://localhost:1234` you can use the URL `http://localhost:8989/?testUrl=http://localhost:1234`

## Running Automatically

1. Run `npm install` in this directory
2. Run `npm test` in this directory

## Running against Azure SignalR

1. One-time machine setup: Run `sn -Vr *,31bf3856ad364e35` from a .NET Developer Command Prompt
2. Open the `Directory.Build.props` file in the **root** of this repo.
3. Comment out the following lines:

```xml
    <DelaySign Condition="'$(CI)' == 'true' And '$(DelaySign)' == ''">true</DelaySign>
    <SignAssembly Condition="'$(CI)' == 'true' And '$(SignAssembly)' == ''">true</SignAssembly>
```

4. Uncomment the following lines:

```xml
    <DelaySign>true</DelaySign>
    <SignAssembly>true</SignAssembly>
```

5. Build this project with Azure SignalR Tests enabled:

```
dotnet build -p:AzureSignalRTests=true
```

6. Run the project, passing in your Azure SignalR connection string:

```
dotnet run --no-build -- --azure-signalr "Endpoint=https://myendpoint.service.signalr.net;AccessKey=accesskey"
```

7. Launch the browser to the URL specified on screen (usually `http://localhost:8989`)
8. When making changes to either the Server code or the SignalR JS Client, terminate the server process and re-run these steps from step 5 (`dotnet build`).

**REMEMBER TO REVERT THESE CHANGES WHEN YOU ARE DONE**