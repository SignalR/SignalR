// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.SignalR.Utils
{
    class Program
    {
        private static readonly ICommand[] _commands = new ICommand[]
        {
            new InstallPerformanceCountersCommand(PrintInfo, PrintSuccess, PrintWarning, PrintError),
            new UninstallPerformanceCountersCommand(PrintInfo, PrintSuccess, PrintWarning, PrintError),
            new GenerateHubProxyCommand(PrintInfo, PrintSuccess, PrintWarning, PrintError)
        };

        static void Main(string[] args)
        {
            PrintBanner();
            
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var command = ParseCommand(args);

            if (command == null)
            {
                // Unrecognized command
                PrintError(String.Format(CultureInfo.CurrentCulture, Resources.Error_UnrecognizedCommand, args[0]));
                PrintHelp();
                Environment.Exit(1); // non-zero is all we need for error
                return;
            }

            try
            {
                command.Execute(args); 
            }
            catch (Exception ex)
            {
                ExitWithError(ex.ToString());
                return;
            }
        }

        private static ICommand ParseCommand(string[] args)
        {
            var name = args[0];

            var command = _commands.SingleOrDefault(c => c.Names.Contains(name, StringComparer.OrdinalIgnoreCase));

            return command;
        }

        private static void ExitWithError(string error = null)
        {
            PrintError(error);
            Environment.Exit(1); // non-zero is all we need for error
        }

        private static void PrintBanner()
        {
            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.Notify_SignalRUtilityVersion, Assembly.GetExecutingAssembly().GetName().Version));
        }

        private static void PrintHelp(string error = null)
        {
            PrintError(error);

            var commands = String.Join(Environment.NewLine + "  ", _commands.Select(c => String.Join(", ", c.Names.Select(n => n)) + "    " + c.Help));

            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.Notify_Help, commands));
            Console.WriteLine();
        }

        private static void PrintInfo(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }
        }
        
        private static void PrintWarning(string info)
        {
            if (!String.IsNullOrWhiteSpace(info))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.Notify_Warning + info));
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
                Console.WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.Notify_Error + error));
                Console.WriteLine();
                Console.ResetColor();
            }
        }

        private static void PrintSuccess(string message)
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine(message);
                Console.WriteLine();
                Console.ResetColor();
            }
        }
    }
}
