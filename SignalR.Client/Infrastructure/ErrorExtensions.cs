using System;
using System.IO;
using System.Net;

namespace SignalR.Client
{
    public static class ErrorExtensions
    {
        /// <summary>
        /// Simplifies error recognition by unwrapping complex exceptions.
        /// </summary>
        /// <param name="ex">The thrown exception.</param>
        /// <returns>An unwrapped exception in the form of a SignalRError.</returns>
        public static SignalRError GetError(this Exception ex)
        {
            ex = ex.Unwrap();
            var wex = ex as WebException;

            var error = new SignalRError(ex);           

            if (wex != null && wex.Response != null)
            {
                var response = wex.Response as HttpWebResponse;
                if (response != null)
                {
                    error.StatusCode = response.StatusCode;
                    Stream originStream = response.GetResponseStream();    
            
                    if (originStream.CanRead)
                    {
                        // We need to copy the stream over and not consume it all on "ReadToEnd".  If we consumed the entire stream GetError
                        // would only be able to be called once per Exception, otherwise you get inconsistent ResponseBody results.
                        Stream stream = Clone(originStream);
                                                
                        // Consume our copied stream
                        using (var sr = new StreamReader(stream))
                        {                            
                            error.ResponseBody = sr.ReadToEnd();
                        }
                    }
                }
            }

            return error;
        }

        private static Stream Clone(Stream source)
        {
            Stream cloned = new MemoryStream();
            byte[] buffer = new byte[2048];// Copy up to 2048 bytes at a time
            int copiedBytes;// Maintains how many bytes were read

            while ((copiedBytes = source.Read(buffer,0,buffer.Length)) > 0)// Read bytes and copy them into a buffer making sure not to trigger the dispose
            {
                cloned.Write(buffer, 0, copiedBytes);// Write the copied bytes from the buffer into the cloned stream
            }

            // Move the stream pointers back to the original start locations
            source.Seek(0, 0);
            cloned.Seek(0, 0);

            return cloned;
        }
    }
}
