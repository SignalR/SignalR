namespace SignalR.Hubs
{
    /// <summary>
    /// Handles parsing incoming requests through the <see cref="HubDispatcher"/>.
    /// </summary>
    public interface IHubRequestParser
    {
        /// <summary>
        /// Parses the incoming hub payload into a <see cref="HubRequest"/>.
        /// </summary>
        /// <param name="data">The raw hub payload.</param>
        /// <returns>The resulting <see cref="HubRequest"/>.</returns>
        HubRequest Parse(string data);
    }
}
