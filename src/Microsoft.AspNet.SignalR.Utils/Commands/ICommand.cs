
namespace Microsoft.AspNet.SignalR.Utils
{
    internal interface ICommand
    {
        string DisplayName { get; }
        string Help { get; }
        string[] Names { get; }
        void Execute(string[] args);
    }
}
