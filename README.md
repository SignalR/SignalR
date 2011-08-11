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