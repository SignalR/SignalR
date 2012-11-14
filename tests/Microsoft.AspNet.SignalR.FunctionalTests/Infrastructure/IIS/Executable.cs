using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure.IIS
{
    internal class Executable
    {
        public Executable(string path, string workingDirectory)
        {
            Path = path;
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; private set; }
        public string Path { get; private set; }

        public Tuple<string, string> Execute(string arguments, params object[] args)
        {

            var process = CreateProcess(arguments, args);
            process.Start();

            Func<StreamReader, string> reader = (StreamReader streamReader) => streamReader.ReadToEnd();

            IAsyncResult outputReader = reader.BeginInvoke(process.StandardOutput, null, null);
            IAsyncResult errorReader = reader.BeginInvoke(process.StandardError, null, null);

            process.StandardInput.Close();

            process.WaitForExit();

            string output = reader.EndInvoke(outputReader);
            string error = reader.EndInvoke(errorReader);

            // Sometimes, we get an exit code of 1 even when the command succeeds (e.g. with 'git reset .').
            // So also make sure there is an error string
            if (process.ExitCode != 0)
            {
                string text = String.IsNullOrEmpty(error) ? output : error;

                throw new Exception(text);
            }
            return Tuple.Create(output, error);

        }

        private Process CreateProcess(string arguments, object[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path,
                WorkingDirectory = WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                ErrorDialog = false,
                Arguments = String.Format(arguments, args)
            };

            var process = new Process()
            {
                StartInfo = psi
            };

            return process;
        }
    }
}
