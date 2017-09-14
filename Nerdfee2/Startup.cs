using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Nerdfee2.Startup))]
namespace Nerdfee2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
