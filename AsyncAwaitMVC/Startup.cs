using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AsyncAwaitMVC.Startup))]
namespace AsyncAwaitMVC
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
