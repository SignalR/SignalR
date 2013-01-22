using namespace std;

#include <iostream>
#include "Connection.h"
#include "MyConnectionHandler.h"
#include "FakeHttpClient.h"


void on_send_complete(Connection* connection, void* state) 
{
    connection->Stop();
}

int main() {

    IConnectionHandler* handler = new MyConnectionHandler();
    IHttpClient* client = new FakeHttpClient();

    auto connection = Connection("http://myendpoint", handler);
    
    connection.Start(client);
    
    bool completed_synchronously = connection.Send("hello", &on_send_complete);

    if(completed_synchronously)
    {	
        connection.Stop();
    }
}

