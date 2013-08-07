namespace Microsoft.AspNet.SignalR
{
    public interface IUserIdProvider
    {
        string GetUserId(IRequest request);
    }
}
