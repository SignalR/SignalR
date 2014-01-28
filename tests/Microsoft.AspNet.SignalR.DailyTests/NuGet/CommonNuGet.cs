using System;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;

namespace Microsoft.AspNet.SignalR.FunctionalTests.NuGet
{
    public class CommonNuGet
    {
        public static string NuGetExe
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..", @".nuget\NuGet.exe"));
            }
        }

        public static void Run(string package, string name)
        {
            string paths = null;
            string wsr = @"\\wsr-teamcity\Drops\";

            if (name == "TeamCityDev")
            {
                if (!Directory.Exists(wsr))
                {
                    Debug.WriteLine(string.Format("Skipping because path is not available: {0}", wsr));
                    return;
                }

                paths = @"\\wsr-teamcity\Drops\SignalR.Main.Signed.AllLanguages\latest-successful\Signed\Dev\packages;\\wsr-teamcity\drops\Katana.Dev.Signed\latest\Release;https://nuget.org/api/v2/";
            }
            else if (name == "TeamCityRelease")
            {
                if (!Directory.Exists(wsr))
                {
                    Debug.WriteLine(string.Format("Skipping because path is not available: {0}", wsr));
                    return;
                }

                paths = @"\\wsr-teamcity\Drops\SignalR.Main.Signed.AllLanguages\latest-successful\Signed\Release\packages;\\wsr-teamcity\drops\Katana.Release.Signed\latest-successful\Release;https://nuget.org/api/v2/";
            }
            else if (name == "OwinTeamCityDev")
            {
                if (!Directory.Exists(wsr))
                {
                    Debug.WriteLine(string.Format("Skipping because path is not available: {0}", wsr));
                    return;
                }

                paths = @"\\wsr-teamcity\drops\Katana.Dev.Signed\latest\Release;\\wsr-teamcity\Drops\Main.Signed.AllLanguages\latest-successful\Signed\Packages\ENU;https://nuget.org/api/v2/";
            }
            else if (name == "OwinTeamCityRelease")
            {
                if (!Directory.Exists(wsr))
                {
                    Debug.WriteLine(string.Format("Skipping because path is not available: {0}", wsr));
                    return;
                }

                paths = @"\\wsr-teamcity\drops\Katana.Release.Signed\latest-successful\Release;\\wsr-teamcity\Drops\Main.Signed.AllLanguages\latest-successful\Signed\Packages\ENU;https://nuget.org/api/v2/";
            }
            else if (name == "aspnetwebstacknightly")
            {
                paths = @"http://myget.org/F/aspnetwebstacknightly/;https://nuget.org/api/v2/";
            }
            else if (name == "aspnetwebstacknightlyrelease")
            {
                paths = @"http://myget.org/F/aspnetwebstacknightlyrelease/;https://nuget.org/api/v2/";
            }
            else if (name == "Staging")
            {
                paths = @"http://staging.nuget.org/api/v2/;https://nuget.org/api/v2/";
            }
            else if (name == "Production")
            {
                paths = @"https://nuget.org/api/v2/";
            }

            if (Directory.Exists("packages"))
            {
                Directory.Delete("packages", recursive: true);
            }

            CommonCommandLine process = new CommonCommandLine();
            process.FileName = CommonNuGet.NuGetExe;
            process.Arguments = string.Format("install -Prerelease -NoCache -Source {0} -OutputDirectory packages {1}", paths, package);
            process.Run();
        }
    }
}
