using System.Threading.Tasks;

namespace SignalR.Abstractions
{
    public interface IResponse
    {
        bool Buffer { get; set; }
        bool IsClientConnected { get; }
        string ContentType { get; set; }

        Task WriteAsync(string data);
    }
}
