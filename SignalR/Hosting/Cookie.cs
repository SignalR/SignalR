
namespace SignalR.Hosting
{
    public class Cookie
    {
        public Cookie(string name, string value, string domain = "", string path = "")
        {
            Name = name;
            Value = value;
            Domain = domain;
            Path = path;
        }

        public string Name { get; private set; }
        public string Domain { get; private set; }
        public string Path { get; private set; }
        public string Value { get; private set; }
    }
}
