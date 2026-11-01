# 04. CLIESC — Cliente escritorio (Windows Forms)

.NET Framework 4.7.2. MVC en `ec/edu/monster`:
- `vista/` FormBienvenida, FormLogin (con la imagen de Monsters Inc.), FormPrincipal (menú lateral), PanelMovimientos, PanelOperacion, FormComprobante, TarjetaCuenta, GraficoCuenta, IndicadorServidor, Formato, Tema.
- `controlador/ControladorEureka.cs` solo pasa las llamadas (asíncronas) y guarda el empleado de la sesión.
- `servicio/ClienteEureka.cs` SINGLETON con un solo `HttpClient`, `ConfiguracionServidor.cs` y `Json.cs` (iguales a la consola; solo cambia `X-Cliente: ESCRITORIO`).
- `modelo/` Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set) y TipoOperacion. Sin reglas del banco: las valida el servidor.
- `Recursos/monster.jpg` (recurso incrustado).

Servidor en `App.config` (`RestHost` = 192.168.100.2, `RestPuerto` = 8088) o variables `REST_HOST` / `REST_PORT`. **Desde la pantalla:** en el login, fila **Servidor** `[IP] : [puerto]` → **Cambiar** (se guarda en el `.exe.config`).

Pantallas: bienvenida con Sulley y Boo, login con 3 caritas de intentos e indicador del servidor, cuenta como tarjeta bancaria con gráficos, comprobante con sello e "Imprimir / PDF" y barra de estado con "Servidor activo" y minutos de sesión. Envía `X-Cliente: ESCRITORIO` y el token en `Authorization: Bearer`; "Cerrar sesión" llama a `POST logout`.
