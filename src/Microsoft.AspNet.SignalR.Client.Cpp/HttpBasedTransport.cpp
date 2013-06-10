#include "HttpBasedTransport.h"

HttpBasedTransport::HttpBasedTransport(shared_ptr<IHttpClient> httpClient, string_t transport)
{
    mHttpClient = httpClient;
    mTransportName = transport;

    mAbortResetEvent = unique_ptr<Concurrency::event>(new Concurrency::event());

    mStartedAbort = false;
    mDisposed = false;
}

HttpBasedTransport::~HttpBasedTransport(void)
{

}

shared_ptr<IHttpClient> HttpBasedTransport::GetHttpClient()
{
    return mHttpClient;
}

task<shared_ptr<NegotiationResponse>> HttpBasedTransport::Negotiate(shared_ptr<Connection> connection)
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

task<void> HttpBasedTransport::Start(shared_ptr<Connection> connection, string_t data, cancellation_token disconnectToken)
{
    task_completion_event<void> tce;
    

    function<void()> initializeCallback = [tce]()
    {
        tce.set();
    };

    exception ex;

    function<void()> errorCallback = [tce, &ex]()
    {
        tce.set_exception(ex);
    };

    OnStart(connection, data, disconnectToken, initializeCallback, errorCallback);
    return task<void>(tce);
}

task<void> HttpBasedTransport::Send(Connection* connection, string_t data)
{
    string_t uri = connection->GetUri() + U("send");
    string_t customQueryString = connection->GetQueryString().empty()? U("") : U("&") + connection->GetQueryString();
    uri += GetSendQueryString(mTransportName, connection->GetConnectionToken(), customQueryString);

    string_t encodedData = U("data=") + uri::encode_data_string(data);

    return mHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
    {
        connection->PrepareRequest(request);
    }, encodedData, false).then([connection](http_response response)
    {

    });
}

void HttpBasedTransport::Abort(shared_ptr<Connection> connection)
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

        mHttpClient->Post(uri, [connection](shared_ptr<HttpRequestWrapper> request)
        {
            connection->PrepareRequest(request);
        }, false).wait();

        OnAbort();
    }

    mAbortLock.unlock();
}

void HttpBasedTransport::CompleteAbort()
{
    //mDisposeLock.lock();

    if (!mDisposed)
    {
        mStartedAbort = true;
        //mAbortResetEvent->set();
    }

    //mDisposeLock.unlock();
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
        //mAbortResetEvent->set();
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
            mDisposed = true;
        }
        
        mDisposeLock.unlock();
        mAbortLock.unlock();
    }
}
