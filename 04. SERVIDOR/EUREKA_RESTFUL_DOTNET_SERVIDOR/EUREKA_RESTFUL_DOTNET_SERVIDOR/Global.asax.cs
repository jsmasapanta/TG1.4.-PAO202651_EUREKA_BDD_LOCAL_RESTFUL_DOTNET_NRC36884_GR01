using System.Web;
using System.Web.Http;
using ec.edu.monster.ws;

namespace ec.edu.monster
{
    /// <summary>Arranque de la aplicación en IIS: registra las rutas del API REST.</summary>
    public class Global : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
