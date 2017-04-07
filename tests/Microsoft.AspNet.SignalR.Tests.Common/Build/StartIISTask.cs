﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using Microsoft.AspNet.SignalR.Tests.Common.Infrastructure;
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
                ITestHost myHost = new IISExpressTestHost(HostLocation[0].ToString(), "js");

                myHost.Initialize();
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
