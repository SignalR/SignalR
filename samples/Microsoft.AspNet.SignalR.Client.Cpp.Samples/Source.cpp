//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.


#include <ctime>
#include <http_client.h>
#include "Connection.h"

// for testing only
#include "TaskAsyncHelper.h"

#define _CRTDBG_MAP_ALLOC
#include <crtdbg.h>
#include <stdlib.h>

using namespace utility;
using namespace std;
using namespace web::json;
using namespace MicrosoftAspNetSignalRClientCpp;

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
        auto connection = shared_ptr<MicrosoftAspNetSignalRClientCpp::Connection>(new MicrosoftAspNetSignalRClientCpp::Connection(U("http://localhost:40476/raw-connection")));
    
        connection->SetReceivedCallback([](string_t message)
        {
            wcout << message << endl;
        });

        connection->SetReconnectedCallback([]()
        {
            time_t now = time(0);
            struct tm nowStruct;
            localtime_s(&nowStruct, &now); // this only works in visual studios, should use localtime for linux

            wcout << "[" << (nowStruct.tm_mon + 1) << "-" << nowStruct.tm_mday << "-" << (nowStruct.tm_year + 1900) << " "
                << nowStruct.tm_hour << ":" << nowStruct.tm_min << ":" << nowStruct.tm_sec << "]: Connection restablished" << endl;
        });

        connection->SetStateChangedCallback([](StateChange stateChange)
        {
            wcout << ConnectionStateString::ToString(stateChange.GetOldState()) << " => " << ConnectionStateString::ToString(stateChange.GetNewState()) << endl;
        });

        connection->SetErrorCallback([](exception& ex)
        {
            wcerr << U("========ERROR==========") << endl;
            wcerr << ex.what() << endl;
            wcerr << U("=======================") << endl;
        });

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

        connection->SetTraceLevel(TraceLevel::All);
        connection->SetTraceWriter(cout);

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
    pplx::cancellation_token_source ctsOne, ctsTwo;
    
    TaskAsyncHelper::Delay(seconds(5), ctsTwo.get_token()).then([]()
    {
        cout << "Cancel me!" << endl;
    });

    TaskAsyncHelper::Delay(seconds(1), ctsOne.get_token()).then([]()
    {
        cout << "I'm done!" << endl;
    }).wait();

    ctsTwo.cancel();
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