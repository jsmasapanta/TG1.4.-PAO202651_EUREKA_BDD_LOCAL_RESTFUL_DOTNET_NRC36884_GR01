# 04. CLICON — Cliente consola

.NET Framework 4.7.2. MVC en `ec/edu/monster`: `modelo/` (Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes: solo get/set; TipoOperacion), `vista/VistaConsola.cs` (pantallas, banner MONSTER · EUREKA BANK, comprobante y spinner), `controlador/ControladorConsola.cs` (solo pasa las llamadas), `servicio/ClienteEureka.cs` (SINGLETON, un solo `HttpClient`), `servicio/ConfiguracionServidor.cs` y `servicio/Json.cs` (DataContractJsonSerializer). Sin reglas del banco: las valida el servidor.

Login `monster`/`monster69` (los 3 intentos los controla el servidor) → menú: consultar cuenta y movimientos, depósito, retiro, transferencia y **[5] Estado del servidor**.

## Servidor REST

`App.config` → `appSettings`:
```xml
<add key="RestHost" value="192.168.100.2" />
<add key="RestPuerto" value="8088" />
```
Sin recompilar se pueden usar las variables de entorno (tienen prioridad):
```powershell
$env:REST_HOST = "192.168.100.2"; $env:REST_PORT = "8088"; .\EUREKA_RESTFUL_DOTNET_CLICON.exe
```
**Desde la pantalla:** en el login escriba **S** en "Usuario" → IP y puerto (se guardan en el `.exe.config`). Acepta la IP sola, `IP:puerto` o la URL completa.

Si el host o el puerto no son válidos se muestra el motivo y la consola no se cierra. Si no llega: *"No se pudo conectar a {url}. Verifica IP, puerto y red."* — revisar que el panel `http://IP:8088/` diga **Servicio activo**.

Envía `X-Cliente: CONSOLA` y el token en `Authorization: Bearer`; **[0] Cerrar sesión** llama a `POST logout`. La consola nunca imprime el token.
