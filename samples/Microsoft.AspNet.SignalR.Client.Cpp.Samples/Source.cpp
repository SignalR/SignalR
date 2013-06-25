#include <ctime>
#include <http_client.h>
#include "Connection.h"

// for testing only
#include "ExceptionHelper.h"
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
        shared_ptr<Connection> connection = shared_ptr<Connection>(new Connection(U("http://localhost:40476/raw-connection")));
    
        connection->Received = [](string_t message)
        {
            wcout << message << endl;
        };

        connection->Reconnected = []()
        {
            time_t now = time(0);
            struct tm nowStruct;
            localtime_s(&nowStruct, &now); // this only works in visual studios, should use localtime for linux

            wcout << "[" << (nowStruct.tm_mon + 1) << "-" << nowStruct.tm_mday << "-" << (nowStruct.tm_year + 1900) << " "
                << nowStruct.tm_hour << ":" << nowStruct.tm_min << ":" << nowStruct.tm_sec << "]: Connection restablished" << endl;
        };

        connection->StateChanged = [](StateChange stateChange)
        {
            wcout << stateChange.GetOldStateName() << " => " << stateChange.GetNewStateName() << endl;
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
    // pplx::create_delayed_task exist in the documentation but is yet to be released
    TaskAsyncHelper::Delay(seconds(1)).then([]()
    {
        cout << "I'm done!" << endl;
    }).wait();
}

static void RunExceptionSample()
{
    exception ex("baseException");
    OperationCanceledException canceled("someOperation");
    ExceptionNone none("none");

    bool isCanceled1 = ExceptionHelper::IsRequestAborted(ex);
    bool isCanceled2 = ExceptionHelper::IsRequestAborted(canceled);
    bool isCanceled3 = ExceptionHelper::IsRequestAborted(none);
    bool isNull1 = ExceptionHelper::IsNull(ex);
    bool isNull2 = ExceptionHelper::IsNull(canceled);
    bool isNull3 = ExceptionHelper::IsNull(none);
}

int main () 
{
    // Saving Memory State at the beginning of the program
    _CrtMemState s1, s2, s3;
    _CrtMemCheckpoint(&s1);

    //RunExceptionSample();
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