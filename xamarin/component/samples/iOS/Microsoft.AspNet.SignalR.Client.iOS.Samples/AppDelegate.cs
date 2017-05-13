using System;
using System.Drawing;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Samples;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Microsoft.AspNet.SignalR.Client.iOS.Samples
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the 
	// User Interface of the application, as well as listening (and optionally responding) to 
	// application events from iOS.
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		// class-level declarations
		UIWindow window;
		UINavigationController navController;

		//
		// This method is invoked when the application has loaded and is ready to run. In this 
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			window = new UIWindow(UIScreen.MainScreen.Bounds);

			var controller = new UIViewController();
			var view = new UIView (UIScreen.MainScreen.Bounds);
			view.BackgroundColor = UIColor.White;
			controller.View = view;

			controller.NavigationItem.Title = "SignalR Client";

			var textView = new UITextView(new RectangleF(0, 0, 320, view.Frame.Height - 0));
			view.AddSubview (textView);


			navController = new UINavigationController (controller);

			window.RootViewController = navController;
			window.MakeKeyAndVisible();

			var traceWriter = new TextViewWriter(SynchronizationContext.Current, textView);

			var client = new CommonClient(traceWriter);
			client.RunAsync("http://signalr-test1.cloudapp.net:82/");

			return true;
		}
	}
}
