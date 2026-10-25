# 04. CLIWEB — Cliente web (ASP.NET Core MVC .NET 8)

`http://localhost:5088`. Todo dentro de `ec/edu/monster`:
- `controlador/` AccesoController (login/logout; los intentos los cuenta el servidor), BancoController (movimientos y operaciones), RequiereSesionAttribute.
- `vista/` Acceso/Login, Banco/Movimientos, Banco/Operacion, Shared/_Layout (Program.cs indica a Razor que busque aquí).
- `servicio/ClienteEureka.cs` registrado con `AddSingleton` (un solo `HttpClient`, System.Text.Json).
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set), TipoOperacion y ViewModels.cs. Sin reglas del banco: las valida el servidor.
- `wwwroot/img/monster.jpg` en el login y el menú.

URL del API REST en `appsettings.json` → `ServicioRest:Url` (por ejemplo `http://192.168.100.3:8088`).