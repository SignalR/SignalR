using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Reporting2.Startup))]
namespace Reporting2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
