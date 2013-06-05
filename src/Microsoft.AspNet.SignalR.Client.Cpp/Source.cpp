#include <ctime>
#include <http_client.h>
#include "Connection.h"

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
        Connection* connection = new Connection(U("http://localhost:40476/raw-connection"));
    
        connection->Received = [](string_t message)
        {
            wcout << message << endl;
        };

        connection->Reconnected = []()
        {
            time_t now = time(0);
            struct tm* nowStruct = localtime(&now);

            wcout << "[" << (nowStruct->tm_mon + 1) << "-" << nowStruct->tm_mday << "-" << (nowStruct->tm_year + 1900) << " "
                << nowStruct->tm_hour << ":" << nowStruct->tm_min << ":" << nowStruct->tm_sec << "]: Connection restablished" << endl;
        };

        connection->StateChanged = [](StateChange* stateChange)
        {
            wcout << stateChange->GetOldStateName() << " => " << stateChange->GetNewStateName() << endl;
        };

        connection->Error = [](exception& ex)
        {
            wcerr << U("========ERROR==========") << endl;
            wcerr << ex.what() << endl;
            wcerr << U("=======================") << endl;
        };

        try
        {
            connection->Start().wait();

            wcout << U("Using ") << connection->GetTransport()->GetTransportName() << endl;
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

    wcout << U("Press <Enter> to Exit ...") << endl;
    getwchar();
}