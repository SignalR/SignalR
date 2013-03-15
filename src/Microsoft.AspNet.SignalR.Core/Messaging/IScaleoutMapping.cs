using System.Collections.Generic;

namespace Microsoft.AspNet.SignalR.Messaging
{
    public interface IScaleoutMapping
    {
        ulong Id { get; }
        IList<LocalEventKeyInfo> LocalKeyInfo { get; }
        IScaleoutMapping NextMapping();
    }
}
