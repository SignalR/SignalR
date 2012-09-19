using System.Security.Principal;

namespace SignalR
{
    /// <summary>
    /// Used to generate prefixes for connection ids.
    /// </summary>
    public interface IConnectionIdPrefixGenerator
    {
        /// <summary>
        /// Creates a prefix for a connection id generated in response to a negotiation request.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/>.</param>
        /// <returns>A connection id prefix</returns>
        string GenerateConnectionIdPrefix(IRequest request);
    }
}
