using System;
using System.Configuration;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio
{
    /// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
    public class ErrorServicioException : Exception
    {
        public ErrorServicioException(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Llama al API REST de EurekaBank (URL en App.config, clave "UrlServidor"): GET para consultar y
    /// POST con JSON para el login y las operaciones. No tiene reglas del banco: si el servidor responde
    /// un error (400, 401, 409, 503...), su mensaje (ErrorServicio) se entrega a la vista.
    /// Patrón Singleton: toda la aplicación usa ClienteEureka.Instancia (un solo HttpClient).
    /// </summary>
    public sealed class ClienteEureka
    {
        // Clave de App.config (appSettings) con la URL base del servidor.
        private const string ClaveUrl = "UrlServidor";

        private static readonly Lazy<ClienteEureka> _instancia =
            new Lazy<ClienteEureka>(() => new ClienteEureka());

        public static ClienteEureka Instancia => _instancia.Value;

        private readonly HttpClient _http;

        // Constructor privado: solo la propia clase crea la instancia.
        private ClienteEureka()
        {
            string url = ConfigurationManager.AppSettings[ClaveUrl];
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ConfigurationErrorsException("Falta la clave '" + ClaveUrl + "' en App.config.");
            }
            UrlServidor = url.Trim().TrimEnd('/');
            _http = new HttpClient
            {
                BaseAddress = new Uri(UrlServidor + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public string UrlServidor { get; }

        /// <summary>POST api/eurekabank/login. Si falla, el servidor responde 401 con los intentos restantes o el bloqueo.</summary>
        public Empleado Login(string usuario, string clave) =>
            Post<Empleado>("api/eurekabank/login", new SolicitudLogin { Usuario = usuario, Clave = clave });

        /// <summary>GET api/eurekabank/cuenta?numero=...</summary>
        public Cuenta ConsultarCuenta(string cuenta) =>
            Get<Cuenta>("api/eurekabank/cuenta?numero=" + Uri.EscapeDataString(cuenta ?? string.Empty));

        /// <summary>GET api/eurekabank/movimientos?cuenta=...</summary>
        public Movimiento[] TraerMovimientos(string cuenta) =>
            Get<Movimiento[]>("api/eurekabank/movimientos?cuenta=" + Uri.EscapeDataString(cuenta ?? string.Empty)) ?? new Movimiento[0];

        public decimal RegistrarDeposito(string cuenta, decimal importe, string codEmpleado) =>
            Post<ResultadoOperacion>("api/eurekabank/deposito",
                new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado }).Saldo;

        public decimal RegistrarRetiro(string cuenta, decimal importe, string codEmpleado) =>
            Post<ResultadoOperacion>("api/eurekabank/retiro",
                new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado }).Saldo;

        public decimal RegistrarTransferencia(string origen, string destino, decimal importe, string codEmpleado) =>
            Post<ResultadoOperacion>("api/eurekabank/transferencia",
                new SolicitudTransferencia { CuentaOrigen = origen, CuentaDestino = destino, Importe = importe, CodEmpleado = codEmpleado }).Saldo;

        // ---------- HTTP ----------

        private T Get<T>(string ruta)
        {
            using (HttpResponseMessage respuesta = Enviar(() => _http.GetAsync(ruta)))
            {
                return Leer<T>(respuesta);
            }
        }

        private T Post<T>(string ruta, object cuerpo)
        {
            string json = Json.Serializar(cuerpo);
            using (HttpResponseMessage respuesta = Enviar(() =>
                _http.PostAsync(ruta, new StringContent(json, Encoding.UTF8, "application/json"))))
            {
                return Leer<T>(respuesta);
            }
        }

        private HttpResponseMessage Enviar(Func<Task<HttpResponseMessage>> peticion)
        {
            try
            {
                return peticion().GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                throw new ErrorServicioException("El servidor tardó demasiado en responder. Intente de nuevo.");
            }
            catch (HttpRequestException)
            {
                throw new ErrorServicioException(
                    "No se pudo conectar con el servidor en " + UrlServidor + ". Verifique que esté encendido y la URL en App.config.");
            }
        }

        /// <summary>Si la respuesta es 2xx devuelve el objeto; si no, lanza el mensaje de ErrorServicio.</summary>
        private T Leer<T>(HttpResponseMessage respuesta)
        {
            string json = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            try
            {
                if (respuesta.IsSuccessStatusCode)
                {
                    return Json.Deserializar<T>(json);
                }
                ErrorServicio error = Json.Deserializar<ErrorServicio>(json);
                if (!string.IsNullOrEmpty(error?.Mensaje))
                {
                    throw new ErrorServicioException(error.Mensaje);
                }
            }
            catch (SerializationException)
            {
                // El cuerpo no era el JSON esperado (por ejemplo, una página de error HTML).
            }
            throw new ErrorServicioException(
                "Respuesta inesperada del servidor (HTTP " + (int)respuesta.StatusCode + "). Revise la URL en App.config: " + UrlServidor);
        }
    }
}
