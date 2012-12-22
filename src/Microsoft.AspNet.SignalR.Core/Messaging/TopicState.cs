namespace Microsoft.AspNet.SignalR.Messaging
{
    internal class TopicState
    {
        public const int Created = 0;
        public const int HasSubscriptions = 1;
        public const int NoSubscriptions = 2;
        public const int Dead = 3;
    }
}
