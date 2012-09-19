using System;
using System.Security.Principal;

namespace SignalR
{
    /// <summary>
    /// The default connection id prefix generator.
    /// </summary>
    public class EmptyConnectionIdPrefixGenerator : IConnectionIdPrefixGenerator
    {
        /// <summary>
        /// Returns an empty connection id prefix.
        /// </summary>
        /// <param name="request">The <see cref="IRequest"/>.</param>
        /// <returns>An empty string.</returns>
        public string GenerateConnectionIdPrefix(IRequest request)
        {
            return String.Empty;
        }
    }
}
