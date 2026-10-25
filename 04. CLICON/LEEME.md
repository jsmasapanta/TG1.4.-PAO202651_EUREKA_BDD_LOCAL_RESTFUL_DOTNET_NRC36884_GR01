# 04. CLICON — Cliente consola

.NET Framework 4.7.2. MVC en `ec/edu/monster`: `modelo/` (Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes: solo get/set; TipoOperacion), `vista/VistaConsola.cs` (pantallas y formato), `controlador/ControladorConsola.cs` (solo pasa las llamadas), `servicio/ClienteEureka.cs` (SINGLETON, un solo `HttpClient`) y `servicio/Json.cs` (DataContractJsonSerializer). Sin reglas del banco: las valida el servidor.

Login `monster`/`monster69` (los 3 intentos los controla el servidor) → menú: consultar cuenta y movimientos, depósito, retiro, transferencia.
La URL del servidor está en `App.config` (`appSettings` → `UrlServidor`, por ejemplo `http://192.168.100.3:8088`).