using System.Web.Cors;
using Microsoft.Owin.Cors;
using Owin;

namespace Microsoft.AspNet.SignalR.Samples
{
	public partial class Startup
	{
        public void ConfigureCors(IAppBuilder app)
        {
            var corsOptions = new CorsOptions
            {
                CorsPolicy = new CorsPolicy
                {
                    AllowAnyHeader = true,
                    AllowAnyMethod = true,
                    AllowAnyOrigin = true,
                    SupportsCredentials = true
                }
            };

            app.UseCors(corsOptions);
        }
	}
}