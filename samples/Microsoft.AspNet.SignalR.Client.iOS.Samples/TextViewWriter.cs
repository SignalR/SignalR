using System;
using System.IO;
using System.Threading;
using MonoTouch.UIKit;

namespace Microsoft.AspNet.SignalR.Client.iOS.Samples
{
    public class TextViewWriter : TextWriter
    {
        private SynchronizationContext _context;
        private UITextView _textView;

        public TextViewWriter(SynchronizationContext context, UITextView textView)
        {
            _context = context;
            _textView = textView;
        }

        public override void WriteLine(string value)
        {
            _context.Post(delegate
            {
                _textView.Text = _textView.Text + value + Environment.NewLine;
            }, state: null);
        }

        public override void WriteLine(string format, object arg0)
        {
            this.WriteLine(string.Format(format, arg0));
        }

        public override void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            this.WriteLine(string.Format(format, arg0, arg1, arg2));
        }

        public override void WriteLine(string format, params object[] args)
        {
            _context.Post(delegate
            {
                _textView.Text = _textView.Text + string.Format(format, args) + Environment.NewLine;
            }, state: null);
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

