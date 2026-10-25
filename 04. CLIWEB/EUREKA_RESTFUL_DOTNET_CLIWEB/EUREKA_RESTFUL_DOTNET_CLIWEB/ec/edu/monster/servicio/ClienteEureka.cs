using System.Net.Http.Json;
using System.Text.Json;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
public class ErrorServicioException : Exception
{
    public ErrorServicioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Llama al API REST de EurekaBank; la URL viene de appsettings.json ("ServicioRest:Url").
/// GET para consultar y POST con JSON para el login y las operaciones. No tiene reglas del banco:
/// si el servidor responde un error (400, 401, 409, 503...), su mensaje se entrega a la vista.
/// Se registra como Singleton en Program.cs: un solo HttpClient para toda la aplicación.
/// </summary>
public class ClienteEureka : IDisposable
{
    // camelCase y sin distinguir mayúsculas, igual que el JSON del servidor.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ClienteEureka(IConfiguration configuracion)
    {
        string url = configuracion["ServicioRest:Url"]
            ?? throw new InvalidOperationException("Falta 'ServicioRest:Url' en appsettings.json.");
        UrlServidor = url.Trim().TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(UrlServidor + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string UrlServidor { get; }

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

    private Task<T> GetAsync<T>(string ruta) => EnviarAsync<T>(() => _http.GetAsync(ruta));

    private Task<T> PostAsync<T>(string ruta, object cuerpo) =>
        EnviarAsync<T>(() => _http.PostAsync(ruta, JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json)));

    private async Task<T> EnviarAsync<T>(Func<Task<HttpResponseMessage>> peticion)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await peticion();
        }
        catch (TaskCanceledException)
        {
            throw new ErrorServicioException("El servidor tardó demasiado en responder. Intente de nuevo.");
        }
        catch (HttpRequestException)
        {
            throw new ErrorServicioException(
                $"No se pudo conectar con el servidor en {UrlServidor}. Verifique que esté encendido y la URL en appsettings.json.");
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
            throw new ErrorServicioException(
                $"Respuesta inesperada del servidor (HTTP {(int)respuesta.StatusCode}). Revise la URL en appsettings.json: {UrlServidor}");
        }
    }

    public void Dispose() => _http.Dispose();
}
