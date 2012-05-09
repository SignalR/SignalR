using System;
using System.Security.Principal;
using SignalR.Hosting;

namespace SignalR
{
    /// <summary>
    /// The default connection id generator.
    /// </summary>
    public class GuidConnectionIdGenerator : IConnectionIdGenerator
    {
        /// <summary>
        /// Generates a random guid as the connection id.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/>.</param>
        /// <returns>A guid that represents a connection id.</returns>
        public string GenerateConnectionId(IRequest request)
        {
            return Guid.NewGuid().ToString("d");
        }
    }
}
