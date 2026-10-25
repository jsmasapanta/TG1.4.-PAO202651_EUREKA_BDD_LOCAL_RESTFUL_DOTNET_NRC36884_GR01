# 04. SERVIDOR — API REST EurekaBank (ASP.NET Web API 2 + SQL Server)

.NET Framework 4.7.2, IIS Express en `http://localhost:8088` (la página de inicio `index.html` lista las operaciones y tiene formularios de prueba).

```
EUREKA_RESTFUL_DOTNET_SERVIDOR/
├── ec/edu/monster/db/ConexionBD.cs           SINGLETON: lee "EurekaBank" de Web.config y abre conexiones
├── ec/edu/monster/modelo/Modelos.cs          Empleado, Cuenta, Movimiento, solicitudes, ResultadoOperacion, ErrorServicio (JSON)
├── ec/edu/monster/servicio/EurekaServicio.cs SINGLETON: login con 3 intentos y bloqueo de 2 min, validaciones, consultas y transacciones
├── ec/edu/monster/ws/EurekabankController.cs ApiController api/eurekabank: delega y convierte errores en HTTP 400/401/409/503
├── ec/edu/monster/ws/WebApiConfig.cs         rutas por atributo, solo JSON en camelCase, fechas yyyy-MM-ddTHH:mm:ss
├── Global.asax(.cs)                          registra Web API al arrancar
├── index.html                                página de prueba del API
├── packages.config / ..\nuget.config         Web API 5.2.9 y Newtonsoft.Json (paquetes en %LOCALAPPDATA%\EUREKA_REST_SERVIDOR\packages)
└── Web.config                                cadena de conexión (login SQL monster/monster)
```

| Verbo | Ruta | Cuerpo | Respuesta |
|---|---|---|---|
| POST | `api/eurekabank/login` | `{ usuario, clave }` | `{ codigo, usuario, nombre }` o 401 con intentos restantes |
| GET | `api/eurekabank/cuenta?numero=00100001` | — | Cuenta |
| GET | `api/eurekabank/movimientos?cuenta=00100001` | — | lista de Movimiento |
| POST | `api/eurekabank/deposito` | `{ cuenta, importe, codEmpleado }` | `{ cuenta, saldo }` |
| POST | `api/eurekabank/retiro` | `{ cuenta, importe, codEmpleado }` | `{ cuenta, saldo }` |
| POST | `api/eurekabank/transferencia` | `{ cuentaOrigen, cuentaDestino, importe, codEmpleado }` | `{ cuenta, saldo }` del origen |

Requisito: la base `EUREKABANK` creada con `03. BDD\crear_bdd.ps1`.
Publicar en red: `publicar_iis.ps1` (como administrador) o a mano desde Visual Studio (ver el README principal).