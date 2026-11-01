# 04. CLIWEB — Cliente web (ASP.NET Core MVC .NET 8)

`http://IP-DE-LA-PC:5088` (en la misma PC también `http://localhost:5088`). Todo dentro de `ec/edu/monster`:
- `controlador/` AccesoController (login/logout y cambio de servidor; los intentos los cuenta el servidor), BancoController (movimientos y operaciones), RequiereSesionAttribute.
- `vista/` Acceso/Login, Banco/Movimientos, Banco/Operacion, Shared/_Layout (Program.cs indica a Razor que busque aquí).
- `servicio/ClienteEureka.cs` registrado con `AddSingleton` (un solo `HttpClient`, System.Text.Json); el token de cada usuario va en su sesión.
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set), TipoOperacion y ViewModels.cs. Sin reglas del banco: las valida el servidor.
- `wwwroot/img/monster.jpg` en el login y el menú; `wwwroot/js/estado.js` (indicador del servidor) y modo oscuro.

## Servidor REST

`appsettings.json`:
```json
"ServidorRest": { "Host": "192.168.100.2", "Puerto": 8088 }
```
`Host` = IP de la PC con el API en IIS. **Desde la pantalla de login**: **⚙ Servidor · cambiar** → IP y puerto → **Guardar** (valida y lo escribe en `appsettings.json`). Si `Host` o `Puerto` no son válidos, la pantalla de login lo dice. Cada petición envía `X-Cliente: WEB` y el token de la sesión del usuario en `Authorization: Bearer`; "Cerrar sesión" llama a `POST logout`.

## Red local

| Script | Qué hace |
|---|---|
| `publicar_web_iis.ps1` | **Como administrador**: publica la web en IIS (sitio `EUREKA_REST_WEB`, puerto 5088, `C:\inetpub\eureka_rest_web`), conserva `appsettings.json` y abre el 5088 en el firewall. Necesita el ASP.NET Core Hosting Bundle 8 |
| `Iniciar-RedLocal.ps1` | Inicia la web en `0.0.0.0:5088` y muestra las URL con las IP de la PC |
| `Detener-RedLocal.ps1` | Detiene la web iniciada por el script anterior |
| `Habilitar-Firewall-RedLocal.ps1` | **Como administrador**: abre TCP 8088 (API) y 5088 (web) solo para la red local (`LocalSubnet`) |

Desde el celular o otra laptop: `http://IP-DE-LA-PC:5088`. Para revisar que el API responde: `http://IP-DEL-SERVIDOR:8088/` (panel, **Servicio activo**).
