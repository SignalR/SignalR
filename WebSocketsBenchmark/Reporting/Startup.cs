using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Reporting.Startup))]
namespace Reporting
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
