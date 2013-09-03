namespace Microsoft.AspNet.SignalR.Tests.Common.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class CommonCommandLine
    {
        //inputs
        public string FileName { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }

        //outputs
        public int ExitCode { get; private set; }
        public string LogFileName { get; private set; }

        //configuration
        public int Timeout { get; set; }
        public bool ShowOutputInTestLog { get; set; }
        public List<int> IgnoreExitCodes { get; private set; }

        private Process process;

        public CommonCommandLine()
        {
            this.process = new Process();
            this.process.StartInfo.UseShellExecute = false;
            this.process.StartInfo.CreateNoWindow = true;
            this.Timeout = 30000;
            this.ShowOutputInTestLog = true;
            this.IgnoreExitCodes = new List<int>();
        }

        public void Run()
        {
            this.LogFileName = string.Format("{0}.txt", Guid.NewGuid());
            this.process.StartInfo.Arguments = string.Format("/c ( \"{0}\" {1} ) > {2} 2>&1", this.FileName, this.Arguments, this.LogFileName);
            this.process.StartInfo.FileName = "cmd.exe";

            if (string.IsNullOrEmpty(this.WorkingDirectory))
            {
                this.WorkingDirectory = Environment.CurrentDirectory;
            }

            this.process.StartInfo.WorkingDirectory = this.WorkingDirectory;

            if (!Directory.Exists(this.process.StartInfo.WorkingDirectory))
            {
                Directory.CreateDirectory(this.process.StartInfo.WorkingDirectory);
            }

            CommonLog.WriteLine("Executing: {0} {1}", this.process.StartInfo.FileName, this.process.StartInfo.Arguments);
            CommonLog.WriteLine("WorkingDirectory: {0}", this.process.StartInfo.WorkingDirectory);

            bool started = this.process.Start();
            if (!started)
            {
                throw new Exception("Failed to start process");
            }

            bool exited = this.process.WaitForExit(this.Timeout);
            if (!exited)
            {
                this.process.Kill();
            }

            this.LogFileName = Path.Combine(this.process.StartInfo.WorkingDirectory, this.LogFileName);
            if (this.ShowOutputInTestLog)
            {
                CommonLog.WriteLine(Environment.NewLine + File.ReadAllText(this.LogFileName));
                File.Delete(this.LogFileName);
            }
            else
            {
                CommonLog.WriteLine("LogFile: {0}", this.LogFileName);
            }

            if (!exited)
            {
                throw new Exception("Process timeout");
            }

            if (this.process.ExitCode != 0 && !this.IgnoreExitCodes.Contains(this.process.ExitCode))
            {
                throw new Exception(string.Format("Process Exit Code is {0}", this.process.ExitCode));
            }
        }
    }
}
