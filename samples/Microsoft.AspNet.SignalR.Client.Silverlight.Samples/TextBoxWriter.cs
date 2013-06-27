using System;
using System.IO;
using System.Threading;
using System.Windows.Controls;

namespace Microsoft.AspNet.SignalR.Client.Silverlight.Samples
{
    public class TextBoxWriter : TextWriter
    {
        private SynchronizationContext _context;
        private TextBox _text;

        public TextBoxWriter(SynchronizationContext context, TextBox text)
        {
            _context = context;
            _text = text;
        }

        public override void WriteLine(string value)
        {
            _context.Post(delegate
            {
                _text.Text = _text.Text + value + Environment.NewLine;
            }, state: null);
        }

        public override void WriteLine(string format, object arg0)
        {
            this.WriteLine(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            this.WriteLine(string.Format(format, arg0, arg1));
        }

        public override void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(format, args));
        }

        #region implemented abstract members of TextWriter

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
