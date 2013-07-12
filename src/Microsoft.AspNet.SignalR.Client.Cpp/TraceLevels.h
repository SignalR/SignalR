#pragma once

namespace MicrosoftAspNetSignalRClientCpp
{
    enum TraceLevel
    {
        None = 0,
        Messages = 1,
        Events = 2,
        StateChanges = 4,
        All = Messages | Events | StateChanges
    };

    class TraceLevelHelper
    {
    public:
        static bool HasFlag(TraceLevel level, TraceLevel flag)
        {
            return level & flag;
        }
    };
}