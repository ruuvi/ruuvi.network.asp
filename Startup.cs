using Microsoft.Owin;
using Owin;
using RuuviTagApp.Models;

[assembly: OwinStartup(typeof(RuuviTagApp.Startup))]
namespace RuuviTagApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            ApiHelper.InitializeClient();
        }
    }
}
