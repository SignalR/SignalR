//using namespace std;

//#include <iostream>
#include "Connection.h"
#include "MyConnectionHandler.h"
#include "FakeHttpClient.h"
//
//
//int main() {
//
//    // Playing with API patterns
//
//    IConnectionHandler* handler = new MyConnectionHandler();
//    IHttpClient* client = new FakeHttpClient();
//
//    auto connection = Connection("http://myendpoint", handler);
//    
//    connection.Start(client);
//    
//    connection.Send("hello");
//    connection.Send("bar");
//
//    connection.Stop();
//}

#include <http_client.h>
#include <filestream.h>
#include <iostream>
#include <sstream>

using namespace utility;
using namespace web::http;
using namespace web::http::client;
using namespace std;
using namespace web::json;

int main () {
	IConnectionHandler* handler = new MyConnectionHandler();
    //IHttpClient* client = new FakeHttpClient();

    auto connection = Connection(U("http://localhost:40476/raw-connection"), handler);
    
    connection.Received = [](string_t message)
    {
        wcout << message << endl;
    };

    connection.Start().wait();
    
    cout << "connection started" << endl;

    web::json::value::field_map object;
    object.push_back(make_pair(value(U("type")), value(1)));
    object.push_back(make_pair(value(U("value")), value(U("hello"))));

    connection.Send(object).wait();

    //connection.Stop();

    char t;
	cin >> t;
}