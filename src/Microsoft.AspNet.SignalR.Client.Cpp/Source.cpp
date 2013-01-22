using namespace std;

#include <iostream>
#include "Connection.h"
#include "MyConnectionHandler.h"
#include "FakeHttpClient.h"


int main() {

    // Playing with API patterns

    IConnectionHandler* handler = new MyConnectionHandler();
    IHttpClient* client = new FakeHttpClient();

    auto connection = Connection("http://myendpoint", handler);
    
    connection.Start(client);
    
    connection.Send("hello");
    connection.Send("bar");

    connection.Stop();
}

