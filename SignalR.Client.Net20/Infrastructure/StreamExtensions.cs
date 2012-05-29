using System;
using System.IO;

namespace SignalR.Client.Net20.Infrastructure
{
	internal static class StreamExtensions
	{
		public static Task WriteAsync(Stream stream, byte[] buffer)
		{
			var signal = new Task();
			var state = new WriteStreamState { Stream = stream, Response = signal, Buffer = buffer };

			WriteAsyncInternal(state);
			return signal;
		}

		internal static void WriteAsyncInternal(WriteStreamState writeStreamState)
		{
			try
			{
				writeStreamState.Stream.BeginWrite(writeStreamState.Buffer, 0, writeStreamState.Buffer.Length, GetResponseWriteCallback, writeStreamState);
			}
			catch (Exception exception)
			{
				writeStreamState.Response.OnFinished(0, exception);
			}
		}

		public static Task<int> ReadAsync(Stream stream, byte[] buffer)
		{
			var signal = new Task<int>();
			var state = new ReadStreamState { Stream = stream, Response = signal, Buffer = buffer };

			ReadAsyncInternal(state);
			return signal;
		}

		internal static void ReadAsyncInternal(ReadStreamState readStreamState)
		{
			try
			{
				readStreamState.Stream.BeginRead(readStreamState.Buffer, 0, readStreamState.Buffer.Length, GetResponseCallback, readStreamState);
			}
			catch (Exception exception)
			{
				readStreamState.Response.OnFinished(0, exception);
			}
		}

		private static void GetResponseCallback(IAsyncResult asynchronousResult)
		{
			ReadStreamState readStreamState = (ReadStreamState)asynchronousResult.AsyncState;

			// End the operation
			try
			{
				var response = readStreamState.Stream.EndRead(asynchronousResult);
				readStreamState.Response.OnFinished(response,null);
			}
			catch (Exception ex)
			{
				try
				{
					ReadAsyncInternal(readStreamState);
				}
				catch (Exception)
				{
					readStreamState.Response.OnFinished(0, ex);
				}
			}
		}

		private static void GetResponseWriteCallback(IAsyncResult asynchronousResult)
		{
			WriteStreamState writeStreamState = (WriteStreamState)asynchronousResult.AsyncState;

			// End the operation
			try
			{
				writeStreamState.Stream.EndWrite(asynchronousResult);
				writeStreamState.Response.OnFinished(null, null);
			}
			catch (Exception ex)
			{
				try
				{
					WriteAsyncInternal(writeStreamState);
				}
				catch (Exception)
				{
					writeStreamState.Response.OnFinished(0, ex);
				}
			}
		}
	}
}