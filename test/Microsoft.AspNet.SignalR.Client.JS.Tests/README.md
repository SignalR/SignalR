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

Coming soon.
