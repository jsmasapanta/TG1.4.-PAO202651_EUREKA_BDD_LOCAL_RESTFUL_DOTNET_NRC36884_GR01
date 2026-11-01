using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
    /// Llama al API REST de EurekaBank: GET para consultar y POST con JSON para el login y las operaciones.
    /// La dirección se arma con ConfiguracionServidor (App.config o variables REST_HOST/REST_PORT).
    /// Cada petición lleva el header X-Cliente: CONSOLA y, después del login, Authorization: Bearer &lt;token&gt;.
    /// No tiene reglas del banco: si el servidor responde un error (400, 401, 409, 423, 503...), su mensaje
    /// (ErrorServicio) se entrega a la vista.
    /// Patrón Singleton: toda la aplicación usa ClienteEureka.Instancia (un solo HttpClient por servidor).
    /// </summary>
    public sealed class ClienteEureka
    {
        public const string NombreCliente = "CONSOLA";

        private static readonly Lazy<ClienteEureka> _instancia =
            new Lazy<ClienteEureka>(() => new ClienteEureka());

        public static ClienteEureka Instancia => _instancia.Value;

        private HttpClient _http;

        /// <summary>Token de la sesión; nunca se muestra en pantalla.</summary>
        private string _token;

        /// <summary>Última respuesta correcta del servidor: el token vence 30 minutos después.</summary>
        private DateTime? _ultimaActividad;

        // Constructor privado: solo la propia clase crea la instancia.
        // Si la configuración no sirve, la aplicación igual abre para que se corrija desde la pantalla.
        private ClienteEureka()
        {
            try
            {
                Conectar(ConfiguracionServidor.LeerUrl());
            }
            catch (InvalidOperationException ex)
            {
                ErrorConfiguracion = ex.Message;
                UrlServidor = "(servidor sin configurar)";
            }
        }

        public string UrlServidor { get; private set; }

        /// <summary>Motivo por el que la dirección guardada no sirve (null si está bien).</summary>
        public string ErrorConfiguracion { get; private set; }

        /// <summary>
        /// Cambia el servidor desde la pantalla (la IP varía según la red). Valida host y puerto, lo guarda en el
        /// .exe.config y crea la conexión nueva. La sesión anterior se olvida: hay que volver a iniciar sesión.
        /// </summary>
        public void CambiarServidor(string host, string puerto)
        {
            string url = ConfiguracionServidor.ArmarUrl(host, puerto); // lanza InvalidOperationException si no es válido
            var direccion = new Uri(url);
            ConfiguracionServidor.Guardar(direccion.Host, direccion.Port.ToString()); // se guarda la IP sola
            _token = null;
            _ultimaActividad = null;
            _http?.Dispose();
            Conectar(url);
            ErrorConfiguracion = null;
        }

        private void Conectar(string url)
        {
            UrlServidor = url;
            _http = new HttpClient
            {
                BaseAddress = new Uri(url + "/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _http.DefaultRequestHeaders.Add("X-Cliente", NombreCliente);
        }

        /// <summary>Minutos que le quedan a la sesión (null si no hay sesión).</summary>
        public int? MinutosSesion =>
            _token == null || _ultimaActividad == null
                ? (int?)null
                : Math.Max(0, 30 - (int)(DateTime.Now - _ultimaActividad.Value).TotalMinutes);

        /// <summary>
        /// true si el servidor responde GET api/eurekabank/estado en 3 segundos (no pide token ni cuenta como petición).
        /// Para el indicador "Servidor activo".
        /// </summary>
        public bool ServidorActivo()
        {
            if (_http == null)
            {
                return false;
            }
            try
            {
                using (var prueba = new HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                using (HttpResponseMessage r = prueba.GetAsync(UrlServidor + "/api/eurekabank/estado").GetAwaiter().GetResult())
                {
                    return r.IsSuccessStatusCode;
                }
            }
            catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException)
            {
                return false;
            }
        }

        /// <summary>POST api/eurekabank/login. Si falla, el servidor responde 401 con los intentos restantes o 423 si está bloqueado.</summary>
        public Empleado Login(string usuario, string clave)
        {
            Empleado empleado = Post<Empleado>("api/eurekabank/login", new SolicitudLogin { Usuario = usuario, Clave = clave });
            _token = empleado.Token;
            _ultimaActividad = DateTime.Now;
            return empleado;
        }

        /// <summary>POST api/eurekabank/logout: invalida el token en el servidor. Si no se puede avisar, igual se olvida aquí.</summary>
        public void CerrarSesion()
        {
            string token = _token;
            if (token == null)
            {
                return;
            }
            try
            {
                Post<ResultadoCierre>("api/eurekabank/logout", null);
            }
            catch (ErrorServicioException)
            {
                // El token vence solo a los 30 minutos.
            }
            finally
            {
                _token = null;
                _ultimaActividad = null;
            }
        }

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

        private T Get<T>(string ruta) => Enviar<T>(new HttpRequestMessage(HttpMethod.Get, ruta));

        private T Post<T>(string ruta, object cuerpo)
        {
            string json = cuerpo == null ? string.Empty : Json.Serializar(cuerpo);
            return Enviar<T>(new HttpRequestMessage(HttpMethod.Post, ruta)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }

        private T Enviar<T>(HttpRequestMessage peticion)
        {
            if (_http == null)
            {
                throw new ErrorServicioException(ErrorConfiguracion);
            }
            if (_token != null)
            {
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            }

            HttpResponseMessage respuesta;
            try
            {
                respuesta = _http.SendAsync(peticion).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                throw new ErrorServicioException("El servidor en " + UrlServidor + " tardó demasiado en responder. Verifica IP, puerto y red.");
            }
            catch (HttpRequestException)
            {
                throw new ErrorServicioException("No se pudo conectar a " + UrlServidor + ". Verifica IP, puerto y red.");
            }

            using (respuesta)
            {
                string json = respuesta.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                try
                {
                    if (respuesta.IsSuccessStatusCode)
                    {
                        if (_token != null)
                        {
                            _ultimaActividad = DateTime.Now;
                        }
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
                    "Respuesta inesperada del servidor (HTTP " + (int)respuesta.StatusCode + "). Verifica IP y puerto: " + UrlServidor);
            }
        }
    }
}
