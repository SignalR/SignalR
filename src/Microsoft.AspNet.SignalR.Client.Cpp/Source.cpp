#include <http_client.h>
#include "Connection.h"
#include "MyConnectionHandler.h"


using namespace utility;
using namespace std;
using namespace web::json;

static void RunStreamingSample()
{
    wcout << U("Choose transport:") << endl;
    wcout << U("1. AutoTransport") << endl;
    wcout << U("2. ServerSentEventsTransport") << endl;
    wcout << U("Option: ");

    string_t key;
    getline(wcin, key);

    if (key == U("1") || key == U("2"))
    {
        Connection* connection = new Connection(U("http://localhost:40476/raw-connection"), new MyConnectionHandler());
    
        connection->Received = [](string_t message)
        {
            wcout << message << endl;
        };

        try
        {
            connection->Start().wait();

            wcout << U("Using ") << connection->GetTransport()->GetTransportName() << endl;
            wcout << U("- Broadcast by pressing <Enter> after entering a message or") << endl;
            wcout << U("- Exit by pressing <Enter>") << endl;
        }
        catch (exception& ex)
        {
            wcerr << U("========ERROR==========") << endl;
            wcerr << ex.what() << endl;
            wcerr << U("=======================") << endl;

            delete connection;
            return;
        }
        string_t line;
        getline(wcin, line);

        while (!line.empty())
        {
            value::field_map object;
            object.push_back(make_pair(value(U("type")), value(1)));
            object.push_back(make_pair(value(U("value")), value(line)));

            connection->Send(object).wait();

            getline(wcin, line);
        }

        delete connection;
    }
}

int main () 
{
    RunStreamingSample();

    wcout << U("Press Any Key to Exit ...") << endl;
    getwchar();
}