# 04. SERVIDOR — API REST EurekaBank (ASP.NET Web API 2 + SQL Server)

.NET Framework 4.7.2, IIS Express en `http://localhost:8088/` (panel) y `http://localhost:8088/api/eurekabank/` (API).

```
EUREKA_RESTFUL_DOTNET_SERVIDOR/
├── ec/edu/monster/db/ConexionBD.cs            SINGLETON: lee "EurekaBank" de Web.config y abre conexiones
├── ec/edu/monster/modelo/Modelos.cs           Empleado, Cuenta, Movimiento, solicitudes, ResultadoOperacion, ErrorServicio, EstadoServidor (JSON)
├── ec/edu/monster/servicio/EurekaServicio.cs  SINGLETON: login con 3 intentos y bloqueo de 2 min, validaciones, consultas y transacciones
├── ec/edu/monster/servicio/GestorSesiones.cs  SINGLETON: token → vencimiento (30 min), limpieza automática, ContarActivos()
├── ec/edu/monster/servicio/MonitorServidor.cs SINGLETON: peticiones, éxitos, conteo por cliente y operación, últimos 60 eventos (en memoria)
├── ec/edu/monster/ws/EurekabankController.cs  ApiController api/eurekabank: valida el token, delega, traduce errores a HTTP y registra en el monitor
├── ec/edu/monster/ws/ContextoPeticion.cs      lee los headers X-Cliente y Authorization: Bearer
├── ec/edu/monster/ws/WebApiConfig.cs          rutas por atributo, solo JSON camelCase, fechas yyyy-MM-ddTHH:mm:ss
├── ec/edu/monster/ws/PermitirOrigenes.cs      CORS (Content-Type, Authorization, X-Cliente)
├── index.html · panel.js · panel.css · monster.jpg   panel con login en la raíz del sitio
├── Global.asax(.cs)                           registra Web API al arrancar
├── packages.config · ..\nuget.config          Web API 5.2.9 y Newtonsoft.Json (paquetes en %LOCALAPPDATA%\EUREKA_REST_SERVIDOR\packages)
└── Web.config                                 cadena de conexión (login SQL monster/monster), documento predeterminado index.html
```

Requisito: la base `EUREKABANK` creada con `03. BDD\crear_bdd.ps1`.

## Endpoints (`api/eurekabank/…`)

| Verbo | Ruta | Token (`Authorization: Bearer`) |
|---|---|---|
| POST | `login` { usuario, clave } | No |
| GET | `estado` | No (no cuenta como petición) |
| POST | `logout` | Sí (el que se invalida) |
| GET | `cuenta?numero=` · `movimientos?cuenta=` | Sí |
| POST | `deposito` · `retiro` · `transferencia` | Sí |

Errores: `{ codigo, mensaje }` con 400 `VALOR_INVALIDO`, 401 `CREDENCIALES_INVALIDAS` / `SESION_INVALIDA`, 423 `USUARIO_BLOQUEADO`, 409 `REGLA_NEGOCIO`, 503 `ERROR_BD`. Sin token o vencido: **401 "Token inválido o expirado. Inicie sesión."** Cada petición se anota en el monitor con el cliente del header `X-Cliente`.

## Publicar en IIS (red local)

1. Visual Studio como administrador → clic derecho en el proyecto → **Publicar** → **Carpeta** `C:\inetpub\eureka_rest` (perfil `FolderProfile.pubxml`).
2. Administrador de IIS: grupo de aplicaciones **EUREKA_REST** (.NET CLR v4.0, Integrada) y sitio **EUREKA_REST** → `C:\inetpub\eureka_rest`, puerto **8088**.
   O todo de una vez, como administrador: `powershell -ExecutionPolicy Bypass -File .\publicar_iis.ps1` (también crea la regla de firewall "EUREKA REST 8088").
3. `ipconfig` → tomar la **Dirección IPv4** del adaptador **Wi-Fi** (por ejemplo `192.168.100.2`).
4. **Verificar con el panel:** abrir `http://IP:8088/` desde otro equipo o el celular → login → **Servicio activo**; al usar cualquier cliente aparece en *Peticiones en tiempo real* con su etiqueta (WEB, MOVIL, ESCRITORIO, CONSOLA).

Web API 2 corre en .NET Framework: no necesita el Hosting Bundle de ASP.NET Core. Emulador de Android: `http://10.0.2.2:8088`. Celular por cable: `adb reverse tcp:8088 tcp:8088`.
