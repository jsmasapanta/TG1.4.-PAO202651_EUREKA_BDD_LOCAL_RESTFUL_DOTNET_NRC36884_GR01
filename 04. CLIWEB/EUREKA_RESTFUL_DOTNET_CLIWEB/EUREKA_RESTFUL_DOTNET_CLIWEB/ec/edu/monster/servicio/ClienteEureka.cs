using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ec.edu.monster.modelo;

namespace ec.edu.monster.servicio;

/// <summary>Error que la vista puede mostrar tal cual al usuario (normalmente el mensaje que envió el servidor).</summary>
public class ErrorServicioException : Exception
{
    public ErrorServicioException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Llama al API REST de EurekaBank: GET para consultar y POST con JSON para el login y las operaciones.
/// La dirección se arma con la sección "ServidorRest" (Host, Puerto) de appsettings.json. Cada petición lleva
/// el header X-Cliente: WEB y el token de la sesión del usuario en Authorization: Bearer. No tiene reglas del
/// banco: si el servidor responde un error (400, 401, 409, 423, 503...), su mensaje se entrega a la vista.
/// Se registra como Singleton en Program.cs: un solo HttpClient para toda la aplicación.
/// El token no se guarda aquí (la web atiende a varios usuarios): cada controlador lo toma de la sesión.
/// </summary>
public class ClienteEureka : IDisposable
{
    public const string NombreCliente = "WEB";

    // camelCase y sin distinguir mayúsculas, igual que el JSON del servidor.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient? _http;

    /// <summary>Si Host o Puerto de appsettings.json no son válidos, este mensaje se muestra en cada pantalla.</summary>
    private string? _errorConfiguracion;

    private readonly string _archivoConfiguracion;
    private readonly object _candado = new();

    public ClienteEureka(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        _archivoConfiguracion = Path.Combine(entorno.ContentRootPath, "appsettings.json");
        _errorConfiguracion = Conectar(configuracion["ServidorRest:Host"] ?? "", configuracion["ServidorRest:Puerto"] ?? "");
    }

    public string UrlServidor { get; private set; } = "(configuración inválida)";

    public string HostServidor { get; private set; } = "";

    public string PuertoServidor { get; private set; } = "";

    /// <summary>
    /// Cambia el servidor desde la pantalla de login (la IP varía según la red). Si host y puerto son válidos,
    /// crea la conexión nueva y los guarda en appsettings.json; si no, devuelve el mensaje de error.
    /// Las sesiones abiertas con el servidor anterior dejan de servir: hay que volver a iniciar sesión.
    /// </summary>
    public string? CambiarServidor(string host, string puerto)
    {
        lock (_candado)
        {
            HttpClient? anterior = _http;
            string? error = Conectar(host, puerto);
            if (error != null)
            {
                return error;
            }
            _errorConfiguracion = null;
            anterior?.Dispose();
            Guardar();
            return null;
        }
    }

    /// <summary>Valida host y puerto y crea el HttpClient. Devuelve el mensaje de error o null si quedó conectado.</summary>
    private string? Conectar(string host, string puerto)
    {
        host = host.Trim();
        puerto = puerto.Trim();
        Separar(ref host, ref puerto);
        if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            return $"Host del servidor inválido: \"{host}\". Escriba la IP de la PC del servidor (ejemplo: 192.168.1.10).";
        }
        if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
        {
            return $"Puerto del servidor inválido: \"{puerto}\". Debe ser un número entre 1 y 65535 (el servidor usa 8088).";
        }
        HostServidor = host;
        PuertoServidor = numero.ToString();
        UrlServidor = new UriBuilder("http", host, numero).Uri.ToString().TrimEnd('/');
        var http = new HttpClient { BaseAddress = new Uri(UrlServidor + "/"), Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Add("X-Cliente", NombreCliente);
        _http = http;
        return null;
    }

    /// <summary>
    /// En "host" se acepta la IP sola (192.168.100.2), con puerto (192.168.100.2:8088) o la URL completa
    /// (http://192.168.100.2:8088/api/eurekabank/): de ahí se toman la IP y el puerto.
    /// </summary>
    private static void Separar(ref string host, ref string puerto)
    {
        if (host.Contains("://"))
        {
            if (Uri.TryCreate(host, UriKind.Absolute, out Uri? url))
            {
                host = url.Host;
                puerto = url.Port.ToString();
            }
            return;
        }
        int dosPuntos = host.LastIndexOf(':');
        if (dosPuntos > 0 && host.IndexOf(':') == dosPuntos) // "192.168.100.2:8088" (no una IPv6)
        {
            puerto = host[(dosPuntos + 1)..].Split('/')[0];
            host = host[..dosPuntos];
        }
        host = host.Split('/')[0];
    }

    /// <summary>Escribe Host y Puerto en la sección ServidorRest de appsettings.json (para el próximo arranque).</summary>
    private void Guardar()
    {
        try
        {
            JsonNode raiz = JsonNode.Parse(File.ReadAllText(_archivoConfiguracion)) ?? new JsonObject();
            JsonObject seccion = raiz["ServidorRest"] as JsonObject ?? new JsonObject();
            seccion["Host"] = HostServidor;
            seccion["Puerto"] = int.Parse(PuertoServidor);
            raiz["ServidorRest"] = seccion;
            File.WriteAllText(_archivoConfiguracion, raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is JsonException)
        {
            // Si no se puede escribir (por ejemplo publicada en una carpeta de solo lectura), el cambio vale hasta reiniciar.
        }
    }

    // Solo para el indicador "Servidor activo": GET api/eurekabank/estado con un tiempo de espera corto.
    private static readonly HttpClient _ping = new() { Timeout = TimeSpan.FromSeconds(3) };

    /// <summary>true si el API responde (se usa en el indicador de la barra lateral y del login).</summary>
    public async Task<bool> ServidorActivoAsync()
    {
        if (_http == null)
        {
            return false;
        }
        try
        {
            using HttpResponseMessage r = await _ping.GetAsync(UrlServidor + "/api/eurekabank/estado");
            return r.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>POST api/eurekabank/login. Si falla, el servidor responde 401 con los intentos restantes o 423 si está bloqueado.</summary>
    public Task<Empleado> LoginAsync(string usuario, string clave) =>
        PostAsync<Empleado>(null, "api/eurekabank/login", new SolicitudLogin { Usuario = usuario, Clave = clave });

    /// <summary>POST api/eurekabank/logout con el token de la sesión.</summary>
    public async Task<bool> CerrarSesionAsync(string token) =>
        (await PostAsync<ResultadoCierre>(token, "api/eurekabank/logout", null)).Cerrada;

    public Task<Cuenta> ConsultarCuentaAsync(string token, string cuenta) =>
        GetAsync<Cuenta>(token, "api/eurekabank/cuenta?numero=" + Uri.EscapeDataString(cuenta));

    public async Task<Movimiento[]> TraerMovimientosAsync(string token, string cuenta) =>
        await GetAsync<Movimiento[]>(token, "api/eurekabank/movimientos?cuenta=" + Uri.EscapeDataString(cuenta)) ?? Array.Empty<Movimiento>();

    public async Task<decimal> RegistrarDepositoAsync(string token, string cuenta, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>(token, "api/eurekabank/deposito",
            new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    public async Task<decimal> RegistrarRetiroAsync(string token, string cuenta, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>(token, "api/eurekabank/retiro",
            new SolicitudMovimiento { Cuenta = cuenta, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    public async Task<decimal> RegistrarTransferenciaAsync(string token, string origen, string destino, decimal importe, string codEmpleado) =>
        (await PostAsync<ResultadoOperacion>(token, "api/eurekabank/transferencia",
            new SolicitudTransferencia { CuentaOrigen = origen, CuentaDestino = destino, Importe = importe, CodEmpleado = codEmpleado })).Saldo;

    // ---------- HTTP ----------

    private Task<T> GetAsync<T>(string? token, string ruta) =>
        EnviarAsync<T>(token, new HttpRequestMessage(HttpMethod.Get, ruta));

    private Task<T> PostAsync<T>(string? token, string ruta, object? cuerpo) =>
        EnviarAsync<T>(token, new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = cuerpo == null ? new StringContent("") : JsonContent.Create(cuerpo, cuerpo.GetType(), options: Json)
        });

    private async Task<T> EnviarAsync<T>(string? token, HttpRequestMessage peticion)
    {
        HttpClient http = _http ?? throw new ErrorServicioException(_errorConfiguracion!);
        if (!string.IsNullOrEmpty(token))
        {
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.SendAsync(peticion);
        }
        catch (TaskCanceledException)
        {
            throw new ErrorServicioException($"El servidor en {UrlServidor} tardó demasiado en responder. Verifica IP, puerto y red.");
        }
        catch (HttpRequestException)
        {
            throw new ErrorServicioException($"No se pudo conectar a {UrlServidor}. Verifica IP, puerto y red.");
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
                $"Respuesta inesperada del servidor (HTTP {(int)respuesta.StatusCode}). Verifica IP y puerto: {UrlServidor}");
        }
    }

    public void Dispose() => _http?.Dispose();
}
