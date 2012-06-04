using System.Security.Principal;

namespace SignalR
{
    /// <summary>
    /// Used to generate connection ids.
    /// </summary>
    public interface IConnectionIdGenerator
    {
        /// <summary>
        /// Creates a connection id for the current request.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/>.</param>
        /// <returns>A connection id</returns>
        string GenerateConnectionId(IRequest request);
    }
}
