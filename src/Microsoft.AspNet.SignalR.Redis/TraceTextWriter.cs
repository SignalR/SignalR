using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.AspNet.SignalR.Redis
{
    internal class TraceTextWriter : TextWriter
    {
        private readonly string _prefix;
        private readonly TraceSource _trace;

        public TraceTextWriter(string prefix, TraceSource trace) : base(CultureInfo.CurrentCulture)
        {
            _prefix = prefix;
            _trace = trace;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {

        }

        public override void WriteLine(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _trace.TraceVerbose(_prefix + value);
            }
        }
    }
}