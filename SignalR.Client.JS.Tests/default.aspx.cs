using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SignalR.Client.JS.Tests
{
    public partial class Default : System.Web.UI.Page
    {

        protected void Page_Load(object sender, EventArgs e)
        {
            dynamicJavascript.Controls.Clear();
            LoadJavascriptFiles("temp/resources"); // Load Javascript from other client directories
            LoadJavascriptFiles("temp/tests"); // Load Javascript tests.  These unit test the resource javascript files
        }

        private void LoadJavascriptFiles(String srcDirectory)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(HttpContext.Current.Request.PhysicalApplicationPath + "\\" + srcDirectory.Replace("/","\\"));
            FileInfo[] fileInfos = dirInfo.GetFiles("*.js", SearchOption.AllDirectories);
            
            foreach (FileInfo f in fileInfos)
            {
                System.Web.UI.HtmlControls.HtmlGenericControl script = new System.Web.UI.HtmlControls.HtmlGenericControl("script");
                script.Attributes.Add("src", srcDirectory+"/" + f.Name);
                script.Attributes.Add("type", "text/javascript");

                dynamicJavascript.Controls.Add(script);
            }
        }      
    }
}