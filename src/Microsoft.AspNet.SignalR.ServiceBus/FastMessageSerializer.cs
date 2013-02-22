// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

namespace Microsoft.AspNet.SignalR.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;
    using Microsoft.AspNet.SignalR.Messaging;

    // This class provides binary stream represendation of Message instances
    // and allows to get Message instances back from such stream.
    sealed class FastMessageSerializer
    {
        const int DataSizeNull = -1;
        const int MessageMarker = 0;
        const int EndOfStreamMarker = -1;
        const int MaxDataSizeInBytes = 256 * 1024;

        public static Stream GetStream(IEnumerable<Message> messages)
        {
            return new MessagesStream(messages);
        }

        public static Message[] GetMessages(Stream stream)
        {
            return new MessagesStreamReader(stream).ReadToEnd();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Do not want to alter functionality.")]
        sealed class MessagesStreamReader
        {
            readonly BufferedStream stream;
            readonly BinaryReader reader;
            List<Message> messages;

            public MessagesStreamReader(Stream stream)
            {
                this.stream = new BufferedStream(stream);
                this.reader = new BinaryReader(this.stream);
            }

            public Message[] ReadToEnd()
            {
                if (this.messages != null)
                {
                    return this.messages.ToArray();
                }

                this.messages = new List<Message>();

                while (true)
                {
                    int marker = this.reader.ReadInt32();

                    if (marker == MessageMarker)
                    {
                        string source = this.GetDataItem();
                        string key = this.GetDataItem();
                        string value = this.GetDataItem();
                        string commandId = this.GetDataItem();
                        string waitForAcValue = this.GetDataItem();
                        string isAckValue = this.GetDataItem();
                        string filter = this.GetDataItem();

                        messages.Add(
                            new Message(source, key, value)
                            {
                                CommandId = commandId,
                                WaitForAck = Boolean.Parse(waitForAcValue),
                                IsAck = Boolean.Parse(isAckValue),
                                Filter = filter
                            });
                    }
                    else if (marker == EndOfStreamMarker)
                    {
                        break;
                    }
                    else
                    {
                        throw new SerializationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_MalformedDataStream));
                    }
                }

                return messages.ToArray();
            }

            string GetDataItem()
            {
                string data;

                int dataSize = this.reader.ReadInt32();

                if (dataSize < -1)
                {
                    throw new SerializationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_DataSizeSmallerThanNegOne));
                }

                if (dataSize >= MaxDataSizeInBytes)
                {
                    throw new SerializationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_DataSizeTooBig));
                }

                if (dataSize == DataSizeNull)
                {
                    data = null;
                }
                else if (dataSize == 0)
                {
                    data = string.Empty;
                }
                else
                {
                    byte[] buffer = this.reader.ReadBytes(dataSize);
                    data = Encoding.UTF8.GetString(buffer);
                }

                return data;
            }
        }

        sealed class MessagesStream : Stream
        {
            readonly IEnumerator<byte[]> iterator;

            byte[] sourceBuffer;
            int sourceBufferPosition;

            public MessagesStream(IEnumerable<Message> messages)
            {
                this.iterator = CreateIterator(messages);
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] destinationBuffer, int offset, int count)
            {
                int destinationIndex = offset;
                int totalCopied = 0;

                while (totalCopied < count)
                {
                    if (this.sourceBuffer == null || this.sourceBuffer.Length == this.sourceBufferPosition)
                    {
                        if (!this.iterator.MoveNext())
                        {
                            return totalCopied;
                        }

                        this.sourceBuffer = this.iterator.Current;
                        this.sourceBufferPosition = 0;
                    }

                    int bytesLeftInSourceBuffer = sourceBuffer.Length - sourceBufferPosition;
                    int bytesLeftInDestinationBuffer = count - destinationIndex;

                    int countToCopy = Math.Min(bytesLeftInDestinationBuffer, bytesLeftInSourceBuffer);
                    Array.Copy(sourceBuffer, sourceBufferPosition, destinationBuffer, destinationIndex, countToCopy);

                    this.sourceBufferPosition += countToCopy;
                    destinationIndex += countToCopy;
                    totalCopied += countToCopy;
                }

                return totalCopied;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override long Length
            {
                get { throw new NotSupportedException(); }
            }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            protected override void Dispose(bool disposing)
            {
                this.iterator.Dispose();
                base.Dispose(disposing);
            }

            static IEnumerator<byte[]> CreateIterator(IEnumerable<Message> messages)
            {
                foreach (Message message in messages)
                {
                    yield return BitConverter.GetBytes(MessageMarker);

                    string[] itemList = new string[] { message.Source, message.Key, message.GetString(), message.CommandId, message.WaitForAck.ToString(), message.IsAck.ToString(), message.Filter };

                    foreach (string item in itemList)
                    {
                        if (item == null)
                        {
                            yield return BitConverter.GetBytes(DataSizeNull);
                        }
                        else if (item == string.Empty)
                        {
                            yield return BitConverter.GetBytes(0);
                        }
                        else
                        {
                            byte[] buffer = Encoding.UTF8.GetBytes(item);
                            yield return BitConverter.GetBytes(buffer.Length);
                            yield return buffer;
                        }
                    }
                }

                yield return BitConverter.GetBytes(EndOfStreamMarker);
            }
        }
    }
}
