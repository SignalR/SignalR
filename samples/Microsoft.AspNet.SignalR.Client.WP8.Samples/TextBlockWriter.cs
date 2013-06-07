using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.AspNet.SignalR.Client.WP8.Samples
{
    public class TextBlockWriter : TextWriter
    {
        private SynchronizationContext _context;
        private StackPanel _stackPanel;
        private TextBlock _text;

        public TextBlockWriter(SynchronizationContext context, StackPanel stackPanel)
        {
            _context = context;
            _stackPanel = stackPanel;
            _text = CreateNew();
            _stackPanel.Children.Add(_text);
        }

        private TextBlock CreateNew()
        {
            TextBlock text = new TextBlock();
            text.FontSize = 10;
            text.TextWrapping = TextWrapping.Wrap;

            return text;
        }

        public override void WriteLine(string value)
        {            
            _context.Post(delegate
            {
                if (_text.Text.Length > 500)
                {
                    _text = CreateNew();
                    _stackPanel.Children.Add(_text);
                }

                if (_text.Text.Length > 0)
                {
                    _text.Text = _text.Text + Environment.NewLine + value;
                }
                else
                {
                    _text.Text = value;
                }                
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
