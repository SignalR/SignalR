How to Build the Xamarin Component
----------------------------------

The following steps will guide you through building a Xamarin Component release:

- Check `component.yaml` to ensure it has all the latest details including the new version number of the component
- Build the `Microsoft.AspNet.SignalR.Client.Portable` library
- Take the following built files:
   - `Microsoft.AspNet.SignalR.Client.dll`
   - `Newtonsoft.Json.dll`
   - `System.Net.Http.Extensions.dll`
- Copy them into the `\ios`, `\android`, `\wp8` folders inside of the `xamarin\component\lib` folder
- Make any necessary changes to the `Details.md`, `GettingStarted.md` and `License.md` files
- Run the `build.ps1` PowerShell script to generate the `signalr-x.y.z.xam` component file where `x.y.z` is the version number in your `components.yaml` file.



Testing your Component
----------------------

You should always test the built component before submitting it.  

You can extract the component by renaming the `signalr-x.y.z.xam` file to `signalr-x.y.z.zip` and extracting its contents.  

To test your component, you should check that all of the sample solutions open, build, and run on each platform!




Component Submission
-------------------------

Once your `.xam` file has been successfully generated you should be ready to upload it to the component store.

