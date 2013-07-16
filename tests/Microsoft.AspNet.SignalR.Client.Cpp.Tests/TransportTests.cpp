//Copyright (c) Microsoft Corporation
//
//All rights reserved.
//
//THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY, OR NON-INFRINGEMENT.

#include "UnitTest++.h"
#include "Connection.h"
#include "ServerSentEventsTransport.h"
#include <array>

using namespace MicrosoftAspNetSignalRClientCpp;

SUITE(TransportTests)
{
    TEST(GetReceiveQueryStringAppendsConnectionQueryString)
    {
        // Arrange
        string_t connectionQS[] = {U("bob=12345"), U("bob=12345&foo=leet&baz=laskjdflsdk"), U("?foo=bar"), U("?foo=bar&baz=bear"), U("&foo=bar"), U("&foo=bar&baz=bear"), U(""), U("")};
        string_t expected[] = {U("&bob=12345"), U("&bob=12345&foo=leet&baz=laskjdflsdk"), U("?foo=bar"), U("?foo=bar&baz=bear"), U("&foo=bar"), U("&foo=bar&baz=bear"), U("?transport=&connectionToken="), U("")};
        int testScenarios = 8;


        for (int i = 0; i < testScenarios; i ++)
        {
            auto pConnection = shared_ptr<Connection>(new Connection(U("http://foo.com"), connectionQS[i]));
            pConnection->SetConnectionToken(U(""));

            // Act
            auto urlQs = TransportHelper::GetReceiveQueryString(pConnection, U(""), U(""));
            
            // Assert
            CHECK(StringHelper::EndsWith(urlQs, expected[i]));
        }
    }

    TEST(AppendCustomQueryStringAppendsConnectionQueryString)
    {
        // Arrange
        string_t connectionQS[] = {U("bob=12345"), U("bob=12345&foo=leet&baz=laskjdflsdk"), U("?foo=bar"), U("?foo=bar&baz=bear"), U("&foo=bar"), U("&foo=bar&baz=bear"), U("")};
        string_t expected[] = {U("?bob=12345"), U("?bob=12345&foo=leet&baz=laskjdflsdk"), U("?foo=bar"), U("?foo=bar&baz=bear"), U("&foo=bar"), U("&foo=bar&baz=bear"), U("")};
        int testScenarios = 7;


        for (int i = 0; i < testScenarios; i ++)
        {
            auto pConnection = shared_ptr<Connection>(new Connection(U("http://foo.com"), connectionQS[i]));
            pConnection->SetConnectionToken(U(""));

            // Act
            auto urlQs = TransportHelper::AppendCustomQueryString(pConnection, U("http://foo.com"));
            
            // Assert
            CHECK(urlQs.compare(expected[i]) == 0);
        }
    }

    TEST(OnInitializedFiresFromInitializeMessage)
    {
        // Arrange
        bool timedOut, disconnected, triggered = false;
        auto pConnection = shared_ptr<Connection>(new Connection(U("http://foo.com")));

        // Act
        TransportHelper::ProcessResponse(pConnection, U("{\"S\":1, \"M\":[]}"), &timedOut, &disconnected, [&triggered]()
        {
            triggered = true;
        });

        CHECK(triggered);
    }
}
