#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(shared_ptr<IHttpClient> httpClient, string_t transport)
{
    pHttpClient = httpClient;
    mTransportName = transport;

    pAbortResetEvent = unique_ptr<Concurrency::event>(new Concurrency::event());

    mStartedAbort = false;
    mDisposed = false;
}

HttpBasedTransport::~HttpBasedTransport(void)
{

}

shared_ptr<IHttpClient> HttpBasedTransport::GetHttpClient()
{
    return pHttpClient;
}

task<shared_ptr<NegotiationResponse>> HttpBasedTransport::Negotiate(shared_ptr<Connection> connection)
{
    return TransportHelper::GetNegotiationResponse(pHttpClient, connection);
}

string_t HttpBasedTransport::GetSendQueryString(string_t transport, string_t connectionToken, string_t customQuery)
{
    return U("?transport=") + transport + U("&connectionToken=") + connectionToken + customQuery;
}

string_t HttpBasedTransport::GetReceiveQueryString(shared_ptr<Connection> connection, string_t data)
{
    return TransportHelper::GetReceiveQueryString(connection, data, mTransportName);
}

task<void> HttpBasedTransport::Start(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken)
{
    task_completion_event<void> tce;
    
    function<void()> initializeCallback = [tce]()
    {
        tce.set();
    };

    function<void(exception)> errorCallback = [tce](exception& ex)
    {
        tce.set_exception(ex);
    };

    OnStart(connection, data, disconnectToken, initializeCallback, errorCallback);
    return task<void>(tce);
}

task<void> HttpBasedTransport::Send(shared_ptr<Connection> connection, string_t data)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    string_t uri = connection->GetUri() + U("send");
    string_t customQueryString = connection->GetQueryString().empty()? U("") : U("&") + connection->GetQueryString();
    uri += GetSendQueryString(mTransportName, connection->GetConnectionToken(), customQueryString);

    string_t encodedData = U("data=") + uri::encode_data_string(data);

    return pHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
    {
        connection->PrepareRequest(request);
    }, encodedData, false).then([connection](http_response response)
    {

    });
}

void HttpBasedTransport::Abort(shared_ptr<Connection> connection)
{
    if (connection == nullptr)
    {
        throw exception("ArgumentNullException: connection");
    }

    {
        lock_guard<mutex> lock(mAbortLock);
        
        if (mDisposed)
        {
            string name = typeid(this).name();
            throw exception(("ObjectDisposedException: " + name).c_str());
        }

        if (!mStartedAbort)
        {
            mStartedAbort = true;

            string_t uri = connection->GetUri() + U("abort") + GetSendQueryString(mTransportName, connection->GetConnectionToken(), U(""));
            uri += TransportHelper::AppendCustomQueryString(connection, uri);

            // this is not asynchronous yet
            pHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
            {
                connection->PrepareRequest(request);
            }, false).wait();

            OnAbort();
        }
    }
}

void HttpBasedTransport::CompleteAbort()
{
    lock_guard<mutex> lock(mDisposeLock);

    if (!mDisposed)
    {
        mStartedAbort = true;
        pAbortResetEvent->set();
    }
}

bool HttpBasedTransport::TryCompleteAbort()
{
    lock_guard<mutex> lock(mDisposeLock);

    if (mDisposed)
    {
        return true;
    }
    else if (mStartedAbort)
    {
        pAbortResetEvent->set();
        return true;
    }
    else
    {
        return false;
    }
}

void HttpBasedTransport::Dispose()
{
    Dispose(true);
}

// Is this function necessary for C++?
void HttpBasedTransport::Dispose(bool disposing)
{
    if (disposing)
    {
        lock_guard<mutex> lock(mAbortLock);
        
        {
            lock_guard<mutex> lock(mDisposeLock);

            // Wait for any ongoing aborts to complete
            // In practice, any aborts should have finished by the time Dispose is called
            if (!mDisposed)
            {
                //pAbortResetEvent.Dispose();
                mDisposed = true;
            }
        }
    }
}
