namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    public interface IPathResolver
    {
        string GetApplicationPath(string applicationName);
    }
}
