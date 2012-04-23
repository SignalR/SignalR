using System.Security.Principal;
using SignalR.Hosting;

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
        /// <param name="request">The request</param>
        /// <param name="user">The current principal</param>
        /// <returns>A connection id</returns>
        string GenerateConnectionId(IRequest request, IPrincipal user);
    }
}
