using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Hosting
{
    // TODO: Consider making it enumerable
    public interface INameValueCollection
    {
        string this[string key] { get; }
        IEnumerable<string> GetValues(string key);

        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "We're matching the name value collection API for compatibility")]
        string Get(string key);
    }
}
