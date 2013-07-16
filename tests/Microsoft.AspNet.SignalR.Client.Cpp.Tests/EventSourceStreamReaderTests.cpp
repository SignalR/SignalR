//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "UnitTest++.h"
#include "EventSourceStreamReader.h"

using namespace MicrosoftAspNetSignalRClientCpp;

SUITE(EventSourceStreamReaderTests)
{
    TEST(ReadTriggersOpenedOnOpen)
    {
        //Arrange
        http_response response;
        response.set_body("data:somedata\n\n");
        auto pEvent = shared_ptr<Concurrency::event>(new Concurrency::event());
        pplx::task_completion_event<string_t> tce;
        pplx::task<string_t> task = pplx::create_task(tce);
        shared_ptr<Connection> pConnection;
        auto pEventSource = shared_ptr<EventSourceStreamReader>(new EventSourceStreamReader(pConnection, response.body()));

        //Act
        pEventSource->SetOpenedCallback([pEvent]()
        {
            pEvent->set();
        });

        pEventSource->SetMessageCallback([tce](shared_ptr<SseEvent> sseEvent)
        {
            tce.set(sseEvent->GetData());
        });
        pEventSource->Start();

        //Assert
        CHECK(pEvent->wait(5000) == 0);
        
        TaskAsyncHelper::Delay(seconds(5)).wait();
        bool taskIsDone = task.is_done();
        CHECK(taskIsDone);
        if(taskIsDone)
        {
            CHECK(task.get().compare(U("somedata")) == 0);
        }

        //Cleanup
        pEventSource->SetOpenedCallback([](){});
        pEventSource->SetMessageCallback([](shared_ptr<SseEvent>){});
    }
}