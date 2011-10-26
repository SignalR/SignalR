namespace SignalR.Infrastructure
{
    public interface IJavaScriptMinifier
    {
        string Minify(string source);
    }
}
