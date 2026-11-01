using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ec.edu.monster.ws
{
    /// <summary>
    /// Lee de la petición en curso lo que no viaja en el cuerpo: el header X-Cliente
    /// (WEB, MOVIL, ESCRITORIO, CONSOLA, PANEL) y el token del header Authorization: Bearer &lt;token&gt;.
    /// </summary>
    internal static class ContextoPeticion
    {
        public const string HeaderCliente = "X-Cliente";
        public const string EsquemaToken = "Bearer";

        public static string Cliente(HttpRequestMessage peticion)
        {
            return peticion != null && peticion.Headers.TryGetValues(HeaderCliente, out IEnumerable<string> valores)
                ? valores.FirstOrDefault()
                : null;
        }

        /// <summary>Token del header Authorization: Bearer, o null si no vino.</summary>
        public static string Token(HttpRequestMessage peticion)
        {
            var autorizacion = peticion?.Headers.Authorization;
            if (autorizacion == null || !string.Equals(autorizacion.Scheme, EsquemaToken, System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return autorizacion.Parameter?.Trim();
        }
    }
}
