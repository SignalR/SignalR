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

using namespace web::http;
using namespace web::http::client;
using namespace std;

// Creates an HTTP request and prints the length of the response stream.
pplx::task<void> HTTPStreamingAsync()
{
    http_client client(L"http://localhost:40476/raw-connection/negotiate");

    // Make the request and asynchronously process the response. 
    return client.request(methods::GET).then([](http_response response)
    {
        // Print the status code.
        std::wostringstream ss;
        ss << L"Server returned returned status code " << response.status_code() << L'.' << std::endl;
        std::wcout << ss.str();

        // TODO: Perform actions here reading from the response stream.
        auto bodyStream = response.body();

        // In this example, we print the length of the response to the console.
        ss.str(std::wstring());
        ss << L"Content length is " << response.headers().content_length() << L" bytes." << std::endl;
        std::wcout << ss.str();

		ss.str(std::wstring());
		ss << response.extract_json().get() << endl;
        std::wcout << ss.str();

    });
}

int main () {
	//cout << "changed" << endl;

	//std::wcout << L"Calling HTTPStreamingAsync..." << std::endl;
 //   HTTPStreamingAsync().wait();


	IConnectionHandler* handler = new MyConnectionHandler();
    //IHttpClient* client = new FakeHttpClient();

    auto connection = Connection(U("http://localhost:40476/raw-connection"), handler);
    
    connection.Start();
    
    //connection.Send("hello");
    //connection.Send("bar");

    //connection.Stop();

    char t;
	cin >> t;
}