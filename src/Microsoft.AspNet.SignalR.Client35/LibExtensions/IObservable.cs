using System;

namespace Microsoft.AspNet.SignalR.Client.LibExtensions
{
    // Summary:
    //     Defines a provider for push-based notification.
    //
    // Type parameters:
    //   T:
    //     The object that provides notification information.This type parameter is
    //     covariant. That is, you can use either the type you specified or any type
    //     that is more derived. For more information about covariance and contravariance,
    //     see Covariance and Contravariance in Generics.
    public interface IObservable<out T>
    {
        // Summary:
        //     Notifies the provider that an observer is to receive notifications.
        //
        // Parameters:
        //   observer:
        //     The object that is to receive notifications.
        //
        // Returns:
        //     A reference to an interface that allows observers to stop receiving notifications
        //     before the provider has finished sending them.
        IDisposable Subscribe(IObserver<T> observer);
    }

    // Summary:
    //     Provides a mechanism for receiving push-based notifications.
    //
    // Type parameters:
    //   T:
    //     The object that provides notification information.This type parameter is
    //     contravariant. That is, you can use either the type you specified or any
    //     type that is less derived. For more information about covariance and contravariance,
    //     see Covariance and Contravariance in Generics.
    public interface IObserver<in T>
    {
        // Summary:
        //     Notifies the observer that the provider has finished sending push-based notifications.
        void OnCompleted();
        //
        // Summary:
        //     Notifies the observer that the provider has experienced an error condition.
        //
        // Parameters:
        //   error:
        //     An object that provides additional information about the error.
        void OnError(Exception error);
        //
        // Summary:
        //     Provides the observer with new data.
        //
        // Parameters:
        //   value:
        //     The current notification information.
        void OnNext(T value);
    }
}
