using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ec.edu.monster.ws
{
    /// <summary>
    /// CORS: permite llamar al API desde otra página (por ejemplo, el panel abierto desde otro puerto o una prueba
    /// con fetch). Deja pasar los headers que usan los clientes: Content-Type, Authorization y X-Cliente.
    /// </summary>
    public class PermitirOrigenes : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage peticion, CancellationToken cancelacion)
        {
            HttpResponseMessage respuesta = peticion.Method == HttpMethod.Options
                ? new HttpResponseMessage(HttpStatusCode.OK)
                : await base.SendAsync(peticion, cancelacion);

            respuesta.Headers.Add("Access-Control-Allow-Origin", "*");
            respuesta.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            respuesta.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Cliente");
            return respuesta;
        }
    }
}
