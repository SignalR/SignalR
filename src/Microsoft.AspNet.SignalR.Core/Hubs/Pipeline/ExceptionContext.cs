using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Hubs
{
    public class ExceptionContext
    {
        private int _modified;
        private Exception _error;
        private object _result;

        public ExceptionContext(Exception error)
        {
            _error = error;
        }

        public Exception Error
        {
            get
            {
                return _error;
            }
            set
            {
                if (Interlocked.Exchange(ref _modified, 1) == 0)
                {
                    _error = value;
                }
                else
                {
                    throw new InvalidOperationException(Resources.Error_ExceptionContextCanOnlyBeModifiedOnce);
                }
            }
        }

        public object Result
        {
            get
            {
                return _result;
            }
            set
            {
                if (Interlocked.Exchange(ref _modified, 1) == 0)
                {
                    _error = null;
                    _result = value;
                }
                else
                {
                    throw new InvalidOperationException(Resources.Error_ExceptionContextCanOnlyBeModifiedOnce);
                }
            }
        }
    }
}
