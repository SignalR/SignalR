using System;
using System.Threading.Tasks;

namespace SignalR.Client.Hubs
{
    public interface IHubProxy
    {
        /// <summary>
        /// Gets or sets state on the hub.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <returns>The value of the field</returns>
        object this[string name] { get; set; }

        /// <summary>
        /// Executes a method on the server side <see cref="IHub"/> asynchronously.
        /// </summary>
        /// <param name="method">The name of the method.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task Invoke(string method, params object[] args);

        /// <summary>
        /// Executes a method on the server side <see cref="IHub"/> asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of result returned from the hub</typeparam>
        /// <param name="method">The name of the method.</param>
        /// <param name="args">The arguments</param>
        /// <returns>A task that represents when invocation returned.</returns>
        Task<T> Invoke<T>(string method, params object[] args);

        /// <summary>
        /// Registers an event for the <see cref="Hub"/>.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <returns>A <see cref="Subscription"/>.</returns>
        Subscription Subscribe(string eventName);
    }
}
