using namespace std;

#include <iostream>
#include "Connection.h"
#include "MyConnectionHandler.h"
#include "FakeHttpClient.h"


void on_send_complete(Connection* connection, exception* error, void* state) 
{
    connection->Stop();
}

void OnReadLine(string line, exception* error, void* state)
{

}

int main() {

    // Playing with API patterns

    IConnectionHandler* handler = new MyConnectionHandler();
    IHttpClient* client = new FakeHttpClient();

    auto connection = Connection("http://myendpoint", handler);
    
    connection.Start(client);
    
    connection.Send("hello", &on_send_complete);
    connection.Send("bar", &on_send_complete);

    IHttpResponse* response = NULL;

    response->ReadLine(OnReadLine);
}

