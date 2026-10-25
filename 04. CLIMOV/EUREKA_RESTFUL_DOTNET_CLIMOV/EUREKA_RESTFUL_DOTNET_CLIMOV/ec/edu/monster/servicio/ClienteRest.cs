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
/// Llama al API REST de EurekaBank con HttpClient: GET para consultar y POST con JSON para el login
/// y las operaciones, y copia la respuesta en los modelos (get/set). Si el servidor responde un error
/// (400, 401, 409, 503...), entrega su "mensaje". No tiene reglas del banco: todas las valida el servidor.
/// Patrón Singleton: toda la app usa ClienteRest.Instancia (un solo HttpClient).
/// </summary>
public sealed class ClienteRest
{
    private const string ClavePreferencia = "url_servidor_eureka_rest";

    // IP de la PC con IIS en la red WiFi; en el emulador sería http://10.0.2.2:8088.
    public const string UrlPorDefecto = "http://192.168.100.3:8088";

    // camelCase y sin distinguir mayúsculas, igual que el JSON del servidor.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<ClienteRest> _instancia = new(() => new ClienteRest());

    public static ClienteRest Instancia => _instancia.Value;

    private readonly HttpClient _http;

    // Constructor privado: solo la propia clase crea la instancia.
    private ClienteRest()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    /// <summary>URL base del API; se guarda en el teléfono para no escribirla cada vez.</summary>
    public string Url
    {
        get => Preferences.Default.Get(ClavePreferencia, UrlPorDefecto);
        set => Preferences.Default.Set(ClavePreferencia, value.Trim().TrimEnd('/'));
    }

    /// <summary>POST api/eurekabank/login. Si falla, el servidor responde 401 con los intentos restantes o el bloqueo.</summary>
    public Task<Empleado> LoginAsync(string usuario, string clave) =>
        PostAsync<Empleado>("api/eurekabank/login", new SolicitudLogin { Usuario = usuario, Clave = clave });

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
        EnviarAsync<T>(ruta, destino => _http.GetAsync(destino));

    private Task<T> PostAsync<T>(string ruta, object cuerpo) =>
        EnviarAsync<T>(ruta, destino => _http.PostAsync(destino, JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json)));

    private async Task<T> EnviarAsync<T>(string ruta, Func<Uri, Task<HttpResponseMessage>> peticion)
    {
        string url = Url;
        if (!Uri.TryCreate(url + "/" + ruta, UriKind.Absolute, out Uri? destino))
        {
            throw new ErrorServicioException($"La URL del servidor no es válida: {url}");
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await peticion(destino);
        }
        catch (TaskCanceledException)
        {
            throw new ErrorServicioException("El servidor tardó demasiado en responder. Intente de nuevo.");
        }
        catch (Exception e) when (e is HttpRequestException || e is SocketException || e is IOException)
        {
            throw new ErrorServicioException(
                $"No se pudo conectar con el servidor en {url}. Verifique que esté encendido, la IP y que estén en la misma red WiFi.");
        }

        using (respuesta)
        {
            string json = await respuesta.Content.ReadAsStringAsync();
            try
            {
                if (respuesta.IsSuccessStatusCode)
                {
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
            throw new ErrorServicioException($"Respuesta inesperada del servidor (HTTP {(int)respuesta.StatusCode}). Revise la URL: {url}");
        }
    }
}
