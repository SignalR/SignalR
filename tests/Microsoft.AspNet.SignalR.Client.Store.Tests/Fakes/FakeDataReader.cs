
using System;
using System.Collections.Generic;
using Windows.Storage.Streams;

namespace Microsoft.AspNet.SignalR.Client.Store.Tests.Fakes
{
    internal class FakeDataReader : IDataReader, IDisposable, IFake
    {
        private readonly FakeInvocationManager _invocationManager = new FakeInvocationManager();

        public ByteOrder ByteOrder { get; set; }

        public IBuffer DetachBuffer()
        {
            throw new NotImplementedException();
        }

        public IInputStream DetachStream()
        {
            throw new NotImplementedException();
        }

        public InputStreamOptions InputStreamOptions { get; set; }

        public DataReaderLoadOperation LoadAsync(uint count)
        {
            throw new NotImplementedException();
        }

        public bool ReadBoolean()
        {
            throw new NotImplementedException();
        }

        public IBuffer ReadBuffer(uint length)
        {
            throw new NotImplementedException();
        }

        public byte ReadByte()
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(byte[] value)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset ReadDateTime()
        {
            throw new NotImplementedException();
        }

        public double ReadDouble()
        {
            throw new NotImplementedException();
        }

        public Guid ReadGuid()
        {
            throw new NotImplementedException();
        }

        public short ReadInt16()
        {
            throw new NotImplementedException();
        }

        public int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public long ReadInt64()
        {
            throw new NotImplementedException();
        }

        public float ReadSingle()
        {
            throw new NotImplementedException();
        }

        public string ReadString(uint codeUnitCount)
        {
            _invocationManager.AddInvocation("ReadString", codeUnitCount);
            return _invocationManager.GetReturnValue<string>("ReadString");
        }

        public TimeSpan ReadTimeSpan()
        {
            throw new NotImplementedException();
        }

        public ushort ReadUInt16()
        {
            throw new NotImplementedException();
        }

        public uint ReadUInt32()
        {
            throw new NotImplementedException();
        }

        public ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public uint UnconsumedBufferLength { get; set; }

        public UnicodeEncoding UnicodeEncoding { get; set; }

        public void Dispose()
        {
        }

        public void Setup<T>(string methodName, Func<T> behavior)
        {
            _invocationManager.AddSetup(methodName, behavior);
        }

        public void Verify(string methodName, List<object[]> expectedParameters)
        {
            _invocationManager.Verify(methodName, expectedParameters);
        }

        IEnumerable<object[]> IFake.GetInvocations(string methodName)
        {
            throw new NotImplementedException();
        }
    }
}
