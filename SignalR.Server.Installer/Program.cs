using System;
using System.Collections.Generic;
using System.Reflection;

namespace SignalR.Server.Installer
{
    class Program
    {
        static void Main(string[] args)
        {
            PrintBanner();

            if (args.Length != 1)
            {
                PrintHelp();
                return;
            }

            try
            {
                var cmd = args[0];

                if (cmd == "-i" || cmd == "-install")
                {
                    InstallPerformanceCounters();
                    return;
                }

                if (cmd == "-u" || cmd == "-uninstall")
                {
                    UninstallPerformanceCounters();
                    return;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Probably due to not running as admin, let's just stop here
                PrintWarning(ex.Message + " Try running as admin.");
                return;
            }
            catch (Exception ex)
            {
                ExitWithError(ex.ToString());
                return;
            }

            PrintHelp("Invalid arguments.");
        }

        private static void InstallPerformanceCounters()
        {
            Console.WriteLine("Installing performance counters...");

            var installer = new PerformanceCounterInstaller();
            var counters = installer.InstallCounters();
            
            foreach (var counter in counters)
            {
                Console.WriteLine("\t" + counter);
            }

            Console.WriteLine();
            Console.WriteLine("Performance counters installed!");
            Console.WriteLine();
        }

        private static void UninstallPerformanceCounters()
        {
            var installer = new PerformanceCounterInstaller();
            installer.UninstallCounters();

            Console.WriteLine("Performance counters uninstalled!");
            Console.WriteLine();
        }

        private static void ExitWithError(string error = null)
        {
            PrintError(error);
            Environment.Exit(1); // non-zero is all we need for error
        }

        private static void PrintBanner()
        {
            var banner =
@"-----------------------------------------------------------------
SignalR Installer Utility v{0}
-----------------------------------------------------------------";
            Console.WriteLine(String.Format(banner, Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine();
        }

        private static void PrintWarning(string info)
        {
            if (!String.IsNullOrWhiteSpace(info))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine("Warning: " + info);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void PrintError(string error)
        {
            if (!String.IsNullOrWhiteSpace(error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("Error: " + error);
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void PrintHelp(string error = null)
        {
            PrintError(error);

            var help =
@"Usage: signalr.exe [command]

Commands:

  -i, -install      Installs SignalR performance counters.
  -u, -uninstall    Uninstalls SignalR performance counters.";

            Console.WriteLine();
            Console.WriteLine(help);
            Console.WriteLine();
        }
    }
}
