#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(http_client* httpClient, string_t transport)
{
    mHttpClient = httpClient;
    mTransportName = transport;

    mStartedAbort = false;
    mDisposed = false;
}

HttpBasedTransport::~HttpBasedTransport(void)
{
    delete mHttpClient;
}

http_client* HttpBasedTransport::GetHttpClient()
{
    return mHttpClient;
}

task<NegotiationResponse*> HttpBasedTransport::Negotiate(Connection* connection)
{
    return TransportHelper::GetNegotiationResponse(mHttpClient, connection);
}

string_t HttpBasedTransport::GetSendQueryString(string_t transport, string_t connectionToken, string_t customQuery)
{
    return U("?transport=") + transport + U("&connectionToken=") + connectionToken + customQuery;
}

string_t HttpBasedTransport::GetReceiveQueryString(Connection* connection, string_t data)
{
    return TransportHelper::GetReceiveQueryString(connection, data, mTransportName);
}

task<void> HttpBasedTransport::Start(Connection* connection, string_t data, void* state)
{
    task_completion_event<void> tce;
    
    auto initializeCallback = new call<int>([tce](int)
    {
        tce.set();
    });

    exception ex;

    auto errorCallback = new call<int>([tce, &ex](int)
    {
        tce.set_exception(ex);
    });

    OnStart(connection, data, initializeCallback, errorCallback);
    return task<void>(tce).then([initializeCallback, errorCallback]()
    {
        delete initializeCallback;
        delete errorCallback;
    });
}

task<void> HttpBasedTransport::Send(Connection* connection, string_t data)
{
    string_t uri = connection->GetUri() + U("send");
    string_t customQueryString = connection->GetQueryString().empty()? U("") : U("&") + connection->GetQueryString();
    uri += GetSendQueryString(mTransportName, connection->GetConnectionToken(), customQueryString);

    http_request request(methods::POST);
    request.set_request_uri(uri);

    string_t encodedData = U("data=") + uri::encode_data_string(data);
    request.set_body(encodedData);

    return mHttpClient->request(request).then([connection](http_response response)
    {
        // check for errors, temporary solution
        if (response.status_code()/100 != 2)
        {
            throw exception("Sending message failed");
        }
    });
}

void HttpBasedTransport::Abort(Connection* connection)
{
    if (connection == NULL)
    {
        throw exception("ArgumentNullException: connection");
    }

    mAbortLock.lock();

    if (mDisposed)
    {
        mAbortLock.unlock();
        string name = typeid(this).name();
        throw exception(("ObjectDisposedException: " + name).c_str());
    }

    if (!mStartedAbort)
    {
        mStartedAbort = true;

        string_t uri = connection->GetUri() + U("abort") + GetSendQueryString(mTransportName, connection->GetConnectionToken(), U(""));
        uri += TransportHelper::AppendCustomQueryString(connection, uri);

        http_request request(methods::POST);
        request.set_request_uri(uri);

        mHttpClient->request(request);
    }

    mAbortLock.unlock();
}

void HttpBasedTransport::CompleteAbort()
{
    mDisposeLock.lock();

    if (!mDisposed)
    {
        mStartedAbort = true;
        mAbortResetEvent->set();
    }

    mDisposeLock.unlock();
}

bool HttpBasedTransport::TryCompleteAbort()
{
    mDisposeLock.lock();

    if (mDisposed)
    {
        mDisposeLock.unlock();
        return true;
    }
    else if (mStartedAbort)
    {
        mAbortResetEvent->set();
        mDisposeLock.unlock();
        return true;
    }
    else
    {
        mDisposeLock.unlock();
        return false;
    }
}

void HttpBasedTransport::Dispose()
{
    Dispose(true);
}

void HttpBasedTransport::Dispose(bool disposing)
{
    if (disposing)
    {
        mAbortLock.lock();
        mDisposeLock.lock();

        if (!mDisposed)
        {
            delete mAbortResetEvent;
            mDisposed = true;
        }
        
        mDisposeLock.lock();
        mAbortLock.lock();
    }
}
