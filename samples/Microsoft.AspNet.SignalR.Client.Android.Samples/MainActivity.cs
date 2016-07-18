// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Samples;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Microsoft.AspNet.SignalR.Client.Android.Samples
{
	[Activity (Label = "Microsoft.AspNet.SignalR.Client.Android.Samples", MainLauncher = true)]
	public class MainActivity : Activity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);
			var textView = FindViewById<TextView>(Resource.Id.textView);
			var traceWriter = new TextViewWriter(SynchronizationContext.Current, textView);

			var client = new CommonClient(traceWriter);
			client.RunAsync("http://signalr-test1.cloudapp.net:83/");
		}
	}
}
