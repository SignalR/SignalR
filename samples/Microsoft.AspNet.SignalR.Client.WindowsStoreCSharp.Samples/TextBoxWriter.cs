﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Windows.UI.Xaml.Controls;

namespace Microsoft.AspNet.SignalR.Client.Samples
{
    public class TextBoxWriter : TextWriter
    {
        private SynchronizationContext _context;
        private TextBox _textView;

        public TextBoxWriter(SynchronizationContext context, TextBox textView)
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

        public override void Write(char value)
        {
            throw new NotImplementedException();
        }
    }
}
