using System;
using Owin;

namespace SignalR.Hosting.Owin.Samples
{
    class Workaround
    {
#pragma warning disable 169
        static private ResultDelegate _workaround1;
        static private Action _workaround2;
#pragma warning restore 169

    }
}
