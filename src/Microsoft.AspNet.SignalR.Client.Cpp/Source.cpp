#include <ctime>
#include <http_client.h>
#include "Connection.h"
#include "TaskAsyncHelper.h"

#define _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#include <stdlib.h>

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
        key.~basic_string(); // Memory leak detector complains that key is lost

        shared_ptr<Connection> connection = shared_ptr<Connection>(new Connection(U("http://localhost:40476/raw-connection")));
    
        connection->Received = [](string_t message)
        {
            wcout << message << endl;
        };

        connection->Reconnected = []()
        {
            time_t now = time(0);
            struct tm* nowStruct = localtime(&now); // localtime is C++ ISO compliant, only MS mark it as deprecated

            wcout << "[" << (nowStruct->tm_mon + 1) << "-" << nowStruct->tm_mday << "-" << (nowStruct->tm_year + 1900) << " "
                << nowStruct->tm_hour << ":" << nowStruct->tm_min << ":" << nowStruct->tm_sec << "]: Connection restablished" << endl;
        };

        connection->StateChanged = [](shared_ptr<StateChange> stateChange)
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

            connection->Stop();
            return;
        }

        string_t line;
        getline(wcin, line);

        while (!line.empty())
        {
            // is there a better way to pass anonymous objects?
            value::field_map object;
            object.push_back(make_pair(value(U("type")), value(1)));
            object.push_back(make_pair(value(U("value")), value(line)));

            connection->Send(object).wait();

            getline(wcin, line);
        }

        connection->Stop();
    }  
}

static void RunDelaySample()
{
    TaskAsyncHelper::Delay(seconds(1)).then([]()
    {
        cout << "I'm done!" << endl;
    }).wait();
}

int main () 
{
    // Saving Memory State at the beginning of the program
    _CrtMemState s1, s2, s3;
    _CrtMemCheckpoint(&s1);

    RunStreamingSample();
    //RunDelaySample();

    wcout << U("Press <Enter> to Exit ...") << endl;
    getwchar();

    // Check for any leaks
    // CRT blocks are used by the CRT library and are not leaks
    _CrtMemCheckpoint(&s2);
    if (_CrtMemDifference(&s3, &s1, &s2))
    {
        _CrtMemDumpStatistics(&s3);
    }
}