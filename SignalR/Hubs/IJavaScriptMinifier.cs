namespace SignalR.Hubs
{
    public interface IJavaScriptMinifier
    {
        string Minify(string source);
    }
}
