# 04. CLIMOV — Cliente móvil (.NET MAUI Android, sucesor de Xamarin.Forms)

```
ec/edu/monster/modelo/                        Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set) y TipoOperacion
ec/edu/monster/servicio/ClienteRest.cs        HttpClient + System.Text.Json (GET/POST al API REST) · SINGLETON
ec/edu/monster/controlador/ControladorEureka.cs  solo pasa las llamadas y guarda la sesión · SINGLETON
ec/edu/monster/vista/Formato.cs               dinero, títulos y colores
ec/edu/monster/vista/LoginPage                login (mensajes e intentos del servidor) + URL + imagen
ec/edu/monster/vista/MenuPage                 4 opciones + operaciones de la sesión
ec/edu/monster/vista/MovimientosPage          saldo y movimientos de una cuenta
ec/edu/monster/vista/OperacionPage            depósito, retiro o transferencia
```

URL en la app: `http://IP-DE-LA-PC:8088` (WiFi) o `http://localhost:8088` con `adb reverse tcp:8088 tcp:8088`.
ApplicationId `ec.edu.monster.eureka.rest` (se instala junto a la app SOAP del TG1.2 sin reemplazarla).
`Directory.Build.props` envía `obj` a `%LOCALAPPDATA%\EUREKA_REST_CLIMOV\obj` por el límite de 260 caracteres.