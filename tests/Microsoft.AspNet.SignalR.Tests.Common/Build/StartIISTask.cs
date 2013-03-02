using System;
using System.IO;
using System.Net;
using Microsoft.AspNet.SignalR.FunctionalTests.Infrastructure;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNet.SignalR.Tests.Common
{
    public class StartIISTask : Task
    {
        [Required]
        public ITaskItem[] HostLocation { get; set; }

        public override bool Execute()
        {
            try
            {
                var myHost = new IISExpressTestHost(HostLocation[0].ToString(), "js");

                myHost.Initialize(keepAlive: -1, // default
                                  connectionTimeout: 110,
                                  disconnectTimeout: 30,
                                  enableAutoRejoiningGroups: false);
            }
            catch (WebException ex)
            {
                var response = ex.Response;
                if (response == null)
                {
                    Log.LogError(ex.ToString());
                    throw;
                }

                using (response)
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        Log.LogError(sr.ReadToEnd());
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
                throw;
            }

            return true;
        }
    }
}
