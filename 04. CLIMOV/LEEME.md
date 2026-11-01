# 04. CLIMOV — Cliente móvil (.NET MAUI Android, sucesor de Xamarin.Forms)

```
ec/edu/monster/modelo/                        Empleado, Cuenta, Movimiento, ErrorServicio, Solicitudes (solo get/set) y TipoOperacion
ec/edu/monster/servicio/ClienteRest.cs        HttpClient + System.Text.Json (GET/POST al API REST) · SINGLETON
ec/edu/monster/servicio/ConfiguracionServidor.cs  lee appsettings.json incrustado y arma http://IP:puerto
ec/edu/monster/controlador/ControladorEureka.cs  solo pasa las llamadas y guarda la sesión · SINGLETON
ec/edu/monster/vista/                         Bienvenida, Login, Menu, Movimientos, Operacion, Comprobante, GraficoCuenta, Formato, Tema
```

Servidor por defecto: `appsettings.json` del proyecto, incrustado como `EmbeddedResource`:
```json
"ServidorRest": { "Host": "192.168.100.2", "Puerto": 8088 }
```
- **Desde la pantalla:** login → **⚙ Servidor REST (IIS)** → IP y puerto → **Guardar** (queda guardado en el teléfono; **Restablecer** vuelve al de `appsettings.json`). No hace falta recompilar cuando cambia la red.
- Celular por WiFi: IP de la PC del servidor, misma red y firewall abierto. Emulador: `10.0.2.2`, puerto 8088.
- Pantallas: bienvenida, login con caritas de intentos y estado del servidor, menú con saludo y accesos de colores, cuenta como tarjeta con gráficos, comprobante (Compartir) y modo oscuro.
- El importe acepta punto decimal (filtro `DigitsKeyListener` en `OperacionPage.xaml.cs`; en la tablet Samsung el teclado solo envía el punto).

App **EurekaBank REST** (`ec.edu.monster.eureka.rest`): se instala junto a la del TG.1.2 sin reemplazarla. Para instalar con adb: compilar con `-p:EmbedAssembliesIntoApk=true -t:SignAndroidPackage` y `adb install -r bin\Debug\net9.0-android\ec.edu.monster.eureka.rest-Signed.apk`.

Envía `X-Cliente: MOVIL` y el token en `Authorization: Bearer`; "Salir" llama a `POST logout`. Verificación: en el panel `http://IP:8088/` las peticiones aparecen con la etiqueta MOVIL.
`Directory.Build.props` envía `obj` a `%LOCALAPPDATA%\EUREKA_REST_CLIMOV\obj` por el límite de 260 caracteres.
