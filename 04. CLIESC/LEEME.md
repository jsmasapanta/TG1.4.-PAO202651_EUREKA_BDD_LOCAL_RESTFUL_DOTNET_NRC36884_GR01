# 04. CLIESC — Cliente escritorio (Windows Forms)

.NET Framework 4.7.2. MVC en `ec/edu/monster`:
- `vista/` FormLogin (con la imagen de Monsters Inc.), FormPrincipal (menú lateral), PanelMovimientos, PanelOperacion, Formato, Tema.
- `controlador/ControladorEureka.cs` solo pasa las llamadas (asíncronas) y guarda el empleado de la sesión.
- `servicio/ClienteEureka.cs` SINGLETON con un solo `HttpClient` (GET/POST JSON) y `servicio/Json.cs`.
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set) y TipoOperacion. Sin reglas del banco: las valida el servidor.
- `Recursos/monster.jpg` (recurso incrustado).

La URL del servidor está en `App.config` (`UrlServidor`).