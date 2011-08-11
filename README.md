# LICENSE
MIT License <http://www.opensource.org/licenses/mit-license.php>

# RAW Connection API

## Server
    // Server url : http://localhost/myconnection.ashx
    public class MyConnection : PersistentConnection {
        protected override void OnReceived(string clientId, string data) {
            // Broadcast data to all clients
            Connection.Broadcast(data);
        }
    }

## Client
### Javascript
    
    <script type="text/javascript">
    $(function () {
        var connection = $.connection('myconnection.ashx');

        connection.received(function (data) {
            $('<li/>').html(data).appendTo($('#messages'));
        });
        
        connection.start();
        
        $("#broadcast").click(function () {
            connection.send($('#msg').val());
        });
    });
    </script>

    <input type="text" id="msg" />
    <input type="button" id="broadcast" />

    <ul id="messages">
    </ul>
    
### C# (Events)
    
    var connection = new Connection("http://localhost/myconnection.ashx");
    connection.Received += data => {
        Console.WriteLine(data);
    };

    connection.Start().Wait();
    connection.Send("From C#");
    
### C# (IObservable)
    
    var connection = new Connection("http://localhost/myconnection.ashx");
    connection.AsObservable()
              .Subscribe(Console.WriteLine);
    
    connection.Start().Wait();
    connection.Send("From C#");
    
# Higher level API

## Server

    public class Chat : Hub {
        public void Send(string message) {
            // Call the addMessage method on all clients
            Clients.addMessage(message);
        }
    }
    
## Client
### Javascript

Import the magic script to generate the server side proxy
    <script src="/signalr/hubs" type="text/javascript"></script>
    
    <script type="text/javascript">
    $(function () {
        // Proxy created on the fly
        var chat = $.connection.chat;
        
        // Declare a function on the chat hub so the server can invoke it
        chat.addMessage = function(message) {
            $('<li/>').html(data).appendTo($('#messages'));
        };
        
        $("#broadcast").click(function () {
            // Call the chat method on the server
            chat.send($('#msg').val())
                .fail(function(e) { alert(e); }) // Supports jQuery deferred
        });
        
        // Start the connection
        $.connection.hub.start();
    });
    </script>
    
    <input type="text" id="msg" />
    <input type="button" id="broadcast" />

    <ul id="messages">
    </ul>