using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.UI;

namespace SignalR.Client.JS.Tests
{
    public partial class Default : System.Web.UI.Page
    {
        private static string _jsResourceFolder, _tempFolder, _unitTestFolder;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                // These values are updated via the .csproj's before build events
                _jsResourceFolder = ConfigurationManager.AppSettings["JSResourceFolder"];
                _tempFolder = ConfigurationManager.AppSettings["TempFolder"];
                _unitTestFolder = ConfigurationManager.AppSettings["QUnitTestsFolder"];
            }

            // Make sure nothing is in our dynamic javascript panel
            dynamicJavascript.Controls.Clear();
            // Load core javascript files to test against
            LoadJavascriptFiles(_tempFolder + "/" + _jsResourceFolder);
            // Load unit tests.  These test the resource javascript files 
            LoadJavascriptFiles(_unitTestFolder);
        }

        /// <summary>
        /// Dynamically load javascript files that are in the rootDirectory
        /// </summary>
        /// <param name="rootDirectory">The directory to search for javascript files</param>
        private void LoadJavascriptFiles(String rootDirectory)
        {
            // Retrieve directory information regarding our rootDirectory
            DirectoryInfo dirInfo = new DirectoryInfo(HttpContext.Current.Request.PhysicalApplicationPath + "\\" + rootDirectory.Replace("/", "\\"));
            // Recursively find all of our javascript files within the rootDirectory.
            FileInfo[] fileInfos = dirInfo.GetFiles("*.js", SearchOption.AllDirectories);

            foreach (FileInfo f in fileInfos)
            {
                // Generate script tags and add them to our dynamic javascript panel
                System.Web.UI.HtmlControls.HtmlGenericControl script = new System.Web.UI.HtmlControls.HtmlGenericControl("script");
                script.Attributes.Add("src", MakeRelative(HttpContext.Current.Request.PhysicalApplicationPath, f.FullName));
                script.Attributes.Add("type", "text/javascript");

                dynamicJavascript.Controls.Add(script);
            }
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        private static String MakeRelative(String fromPath, String toPath)
        {
            Uri relativeUri = new Uri(fromPath).MakeRelativeUri(new Uri(toPath));
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }
    }
}