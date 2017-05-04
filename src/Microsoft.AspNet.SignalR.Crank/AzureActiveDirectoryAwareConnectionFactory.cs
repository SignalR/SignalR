using System;
using System.Globalization;
using CmdLine;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.AspNet.SignalR.Crank
{
    public class AzureActiveDirectoryAwareConnectionFactory : IConnectionFactory
    {
        private readonly AuthenticationResult _authenticationResult;

        public AzureActiveDirectoryAwareConnectionFactory()
        {
            var arguments = EnsureAllArgumentsHaveBeenSet();

            var authority = string.Format(CultureInfo.InvariantCulture, arguments.AadInstance, arguments.Tenant);
            var authenticationContext = new AuthenticationContext(authority);
            Console.WriteLine("Starting Authentication...");
            try
            {
                if (string.IsNullOrWhiteSpace(arguments.Username) || string.IsNullOrWhiteSpace(arguments.Password))
                {
                    _authenticationResult = authenticationContext.AcquireTokenAsync(arguments.ResourceId, arguments.ClientId, new Uri(arguments.RedirectUri), new PlatformParameters(PromptBehavior.Auto)).Result;
                }
                else
                {
                    Console.WriteLine($"Trying to authenticate using username '{arguments.Username}' and provided password...");

                    _authenticationResult = authenticationContext.AcquireTokenAsync(arguments.ResourceId, arguments.ClientId, new UserPasswordCredential(arguments.Username, arguments.Password)).Result;

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred during authentication:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine("Note that silent login using username and password is not available for Microsoft Accounts or accounts using multi-factor-authentication. Use the interactive login for those.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            Console.WriteLine($"Authenticated as {_authenticationResult.UserInfo.DisplayableId} - Token acquired successfully.");
        }

        private static CrankArguments EnsureAllArgumentsHaveBeenSet()
        {
            var arguments = CommandLine.Parse<CrankArguments>();
            while (string.IsNullOrWhiteSpace(arguments.AadInstance))
            {
                Console.WriteLine("Please enter the AAD instance URI:");
                arguments.AadInstance = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(arguments.Tenant))
            {
                Console.WriteLine("Please enter the AAD Tenant name:");
                arguments.Tenant = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(arguments.ResourceId))
            {
                Console.WriteLine("Please enter the AAD ResourceId:");
                arguments.ResourceId = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(arguments.ClientId))
            {
                Console.WriteLine("Please enter the AAD ClientId:");
                arguments.ClientId = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(arguments.RedirectUri))
            {
                Console.WriteLine("Please enter the AAD Redirect URI:");
                arguments.RedirectUri = Console.ReadLine();
            }

            return arguments;
        }

        public Connection CreateConnection(string url)
        {
            var connection = new Connection(url);
            connection.Headers.Add("Authorization", $"Bearer {_authenticationResult.AccessToken}");
            return connection;
        }
    }
}
