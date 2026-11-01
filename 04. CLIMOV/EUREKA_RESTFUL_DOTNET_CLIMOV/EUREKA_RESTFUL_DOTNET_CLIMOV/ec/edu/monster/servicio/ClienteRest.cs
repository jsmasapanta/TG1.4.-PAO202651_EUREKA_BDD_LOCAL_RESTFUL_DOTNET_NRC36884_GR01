using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
public class ErrorServicioException : Exception
{
    public ErrorServicioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Llama al API REST de EurekaBank con HttpClient: GET para consultar y POST con JSON para el login y las
/// operaciones. Cada petición lleva el header X-Cliente y, después del login, Authorization: Bearer &lt;token&gt;.
/// Copia la respuesta en los modelos (get/set); si el servidor responde un error (400, 401, 409, 423, 503...),
/// entrega su "mensaje". No tiene reglas del banco: todas las valida el servidor.
/// Patrón Singleton: toda la app usa ClienteRest.Instancia (un solo HttpClient).
/// </summary>
public sealed class ClienteRest
{
    /// <summary>MOVIL en Android; ESCRITORIO si la app corre en Windows.</summary>
    public static readonly string NombreCliente =
        DeviceInfo.Current.Platform == DevicePlatform.WinUI ? "ESCRITORIO" : "MOVIL";

    // camelCase y sin distinguir mayúsculas, igual que el JSON del servidor.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<ClienteRest> _instancia = new(() => new ClienteRest());

    public static ClienteRest Instancia => _instancia.Value;

    private readonly HttpClient _http;

    /// <summary>Token de la sesión; nunca se muestra en pantalla.</summary>
    private string? _token;

    /// <summary>Última respuesta correcta del servidor: el token vence 30 minutos después.</summary>
    private DateTime? _ultimaActividad;

    /// <summary>Minutos que le quedan a la sesión (null si no hay sesión).</summary>
    public int? MinutosSesion =>
        _token == null || _ultimaActividad == null
            ? null
            : Math.Max(0, 30 - (int)(DateTime.Now - _ultimaActividad.Value).TotalMinutes);

    /// <summary>
    /// true si el servidor responde GET api/eurekabank/estado en 3 segundos (no pide token ni cuenta como petición).
    /// Para el indicador "Servidor activo".
    /// </summary>
    public async Task<bool> ServidorActivoAsync()
    {
        if (!Uri.TryCreate(Url + "/api/eurekabank/estado", UriKind.Absolute, out Uri? destino))
        {
            return false;
        }
        try
        {
            using var espera = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using HttpResponseMessage r = await _http.GetAsync(destino, espera.Token);
            return r.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException || e is TaskCanceledException || e is SocketException || e is IOException)
        {
            return false;
        }
    }

    // Constructor privado: solo la propia clase crea la instancia.
    private ClienteRest()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    private const string ClavePreferencia = "servidor_eureka_rest";

    /// <summary>
    /// URL base del API. Por defecto la de appsettings.json (incrustado en la app); si en el login se cambia
    /// la IP o el puerto, la nueva se guarda en el teléfono y se usa hasta "Restablecer".
    /// </summary>
    public string Url
    {
        get => Preferences.Default.Get(ClavePreferencia, ConfiguracionServidor.Url);
        set => Preferences.Default.Set(ClavePreferencia, value);
    }

    /// <summary>Vuelve a la dirección de appsettings.json.</summary>
    public void RestablecerServidor()
    {
        Preferences.Default.Remove(ClavePreferencia);
        _token = null;
        _ultimaActividad = null;
    }

    /// <summary>POST api/eurekabank/login. Si falla, el servidor responde 401 con los intentos restantes o 423 si está bloqueado.</summary>
    public async Task<Empleado> LoginAsync(string usuario, string clave)
    {
        _token = null;
        Empleado empleado = await PostAsync<Empleado>("api/eurekabank/login", new SolicitudLogin { Usuario = usuario, Clave = clave });
        _token = empleado.Token;
        _ultimaActividad = DateTime.Now;
        return empleado;
    }

    /// <summary>POST api/eurekabank/logout: invalida el token en el servidor. Si no se puede avisar, igual se olvida aquí.</summary>
    public async Task CerrarSesionAsync()
    {
        if (string.IsNullOrEmpty(_token))
        {
            return;
        }
        try
        {
            await PostAsync<ResultadoCierre>("api/eurekabank/logout", null);
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

    public Task<Cuenta> ConsultarCuentaAsync(string cuenta) =>
        GetAsync<Cuenta>("api/eurekabank/cuenta?numero=" + Uri.EscapeDataString(cuenta));

    public async Task<Movimiento[]> TraerMovimientosAsync(string cuenta) =>
        await GetAsync<Movimiento[]>("api/eurekabank/movimientos?cuenta=" + Uri.EscapeDataString(cuenta)) ?? Array.Empty<Movimiento>();

    public async Task<decimal> RegistrarDepositoAsync(string cuenta, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>("api/eurekabank/deposito",
            new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    public async Task<decimal> RegistrarRetiroAsync(string cuenta, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>("api/eurekabank/retiro",
            new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    public async Task<decimal> RegistrarTransferenciaAsync(string origen, string destino, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>("api/eurekabank/transferencia",
            new SolicitudTransferencia { CuentaOrigen = origen, CuentaDestino = destino, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    // ---------- HTTP ----------

    private Task<T> GetAsync<T>(string ruta) =>
        EnviarAsync<T>(ruta, () => new HttpRequestMessage(HttpMethod.Get, ""));

    private Task<T> PostAsync<T>(string ruta, object? cuerpo) =>
        EnviarAsync<T>(ruta, () => new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = cuerpo == null ? new StringContent("") : JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json)
        });

    private async Task<T> EnviarAsync<T>(string ruta, Func<HttpRequestMessage> crear)
    {
        string url = Url;
        if (string.IsNullOrWhiteSpace(url) && ConfiguracionServidor.Error != null)
        {
            throw new ErrorServicioException(ConfiguracionServidor.Error);
        }
        if (!Uri.TryCreate(url + "/" + ruta, UriKind.Absolute, out Uri? destino) || (destino.Scheme != Uri.UriSchemeHttp && destino.Scheme != Uri.UriSchemeHttps))
        {
            throw new ErrorServicioException($"La URL del servidor no es válida: {url}");
        }

        HttpRequestMessage peticion = crear();
        peticion.RequestUri = destino;
        peticion.Headers.Add("X-Cliente", NombreCliente);
        if (!string.IsNullOrEmpty(_token))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await _http.SendAsync(peticion);
        }
        catch (TaskCanceledException)
        {
            throw new ErrorServicioException("El servidor tardó demasiado en responder. Intente de nuevo.");
        }
        catch (Exception e) when (e is HttpRequestException || e is SocketException || e is IOException)
        {
            throw new ErrorServicioException($"No se pudo conectar a {url}. Verifica IP, puerto y red.");
        }

        using (respuesta)
        {
            string json = await respuesta.Content.ReadAsStringAsync();
            try
            {
                if (respuesta.IsSuccessStatusCode)
                {
                    if (_token != null)
                    {
                        _ultimaActividad = DateTime.Now;
                    }
                    return JsonSerializer.Deserialize<T>(json, Json)!;
                }
                ErrorServicio? error = JsonSerializer.Deserialize<ErrorServicio>(json, Json);
                if (!string.IsNullOrEmpty(error?.Mensaje))
                {
                    throw new ErrorServicioException(error.Mensaje);
                }
            }
            catch (JsonException)
            {
                // El cuerpo no era el JSON esperado (por ejemplo, una página de error HTML).
            }
            throw new ErrorServicioException($"Respuesta inesperada del servidor (HTTP {(int)respuesta.StatusCode}). Verifica IP y puerto: {url}");
        }
    }
}
