# TG.1.4 · EurekaBank — RESTful .NET con base de datos (SQL Server)

**TG.1.4 PAO202651_EUREKABANK_BDDLOCAL_RESTFUL_DOTNET_NRC36884_GR01** · ESPE · Arquitectura de Software · NRC 36884 · Grupo 1 · Tutor: Ing. Mauricio Campaña
Repositorio: https://github.com/jsmasapanta/TG.1.4-PAO202651_EUREKABANK_BDDLOCAL_RESTFUL_DOTNET_NRC36884_GR01

Proyecto de **Arquitectura de Software (ESPE)**: un **API REST** en .NET (ASP.NET Web API 2, JSON) que maneja las cuentas de ahorro de **EurekaBank** sobre **SQL Server**, y **cuatro clientes** que lo consumen: **consola, escritorio, web y móvil**. Es la versión REST del TG.1.2 (SOAP): mismas operaciones, reglas, base de datos y presentación.

Operaciones: **consultar cuenta y movimientos, depósito, retiro y transferencia**. Cada operación queda grabada en la tabla `Movimiento` y actualiza el saldo de `Cuenta` dentro de una transacción.

| | |
|---|---|
| **Usuario de la app** | `monster` / `monster69` (tabla `Empleado`, código `0012`) |
| **Login de SQL Server** | `monster` / `monster` (lo usa el servidor para conectarse) |
| **Base de datos** | `EUREKABANK` en SQL Server local (`localhost`), la misma del TG.1.2 |
| **Panel del servidor** | `http://IP-DE-LA-PC:8088/` en IIS (login, estado, sesiones, peticiones por cliente y prueba rápida) |
| **API REST** | `http://IP-DE-LA-PC:8088/api/eurekabank/...` en IIS (sitio `EUREKA_REST`) · por defecto `192.168.100.2` |
| **Cliente web** | `http://IP-DE-LA-PC:5088` en IIS (sitio `EUREKA_REST_WEB`, se publica con `04. CLIWEB\publicar_web_iis.ps1` como administrador) |
| **IDE** | Visual Studio 2022 Community |
| **Paquetes** | `ec.edu.monster.db` · `modelo` · `servicio` · `ws` (servidor) y `modelo` · `vista` · `controlador` · `servicio` (clientes) |
| **Sesión** | Token de 30 minutos en el header `Authorization: Bearer <token>`; `POST logout` lo invalida |
| **Patrones** | Singleton (`ConexionBD`, `EurekaServicio`, `GestorSesiones`, `MonitorServidor`, `ClienteEureka`, `ClienteRest`) · MVC en los clientes |

Los puertos (8088 / 5088) son distintos a los del TG.1.2 SOAP (8087 / 5087): los dos proyectos pueden estar publicados a la vez.

---

## Inicio de sesión en cada cliente

Todos piden `monster` / `monster69`, muestran los intentos restantes que envía el servidor y si el servidor responde, y permiten cambiar la IP y el puerto del servidor desde la misma pantalla.

| Panel del servidor (`http://IP:8088/`) | Cliente web (`http://IP:5088/`) |
|:-:|:-:|
| <img src="05.%20DOCUMENTACI%C3%93N/capturas/SERVIDOR/01_panel_login.png" width="420"> | <img src="05.%20DOCUMENTACI%C3%93N/capturas/CLIWEB/01_login_intentos.png" width="420"> |
| **Escritorio (Windows Forms)** | **Consola** |
| <img src="05.%20DOCUMENTACI%C3%93N/capturas/CLIESC/01_login_intentos.png" width="420"> | <img src="05.%20DOCUMENTACI%C3%93N/capturas/CLICON/03_login_error.png" width="420"> |
| **Móvil (tablet Samsung SM-P610)** | **Móvil: intentos restantes** |
| <img src="05.%20DOCUMENTACI%C3%93N/capturas/CLIMOV/01_login.png" width="420"> | <img src="05.%20DOCUMENTACI%C3%93N/capturas/CLIMOV/03_login_intentos.png" width="420"> |

---

## 1. Arquitectura

```
                 X-Cliente          ┌───────────────────────────────────────────────┐
  CLICON (consola)   CONSOLA ────►  │ IIS :8088 (sitio EUREKA_REST)                 │
  CLIESC (escritorio) ESCRITORIO ►  │  /                 panel (index.html) PANEL   │
  CLIWEB  :5088      WEB ────────►  │  /api/eurekabank/* Web API 2 · JSON           │      ┌────────────────────┐
  CLIMOV (Android)   MOVIL ──────►  │  ec.edu.monster.ws       EurekabankController │      │ SQL Server         │
                                    │  ec.edu.monster.servicio EurekaServicio ───────────► │ EUREKABANK         │
    HTTP + JSON + Authorization:    │    GestorSesiones · MonitorServidor           │ ADO │ login monster      │
    Bearer <token>                  │  ec.edu.monster.db       ConexionBD           │ .NET└────────────────────┘
                                    └───────────────────────────────────────────────┘
```

- **Protocolo:** REST sobre HTTP con **JSON** (camelCase). GET para consultar y POST para el login y las operaciones.
- **Errores:** el servidor responde un **código HTTP** con `ErrorServicio { codigo, mensaje }`:

  | HTTP | `codigo` | Cuándo |
  |---|---|---|
  | 400 | `VALOR_INVALIDO` | Cuenta sin 8 dígitos, importe ≤ 0, con más de 2 decimales o que no es número, datos vacíos |
  | 401 | `CREDENCIALES_INVALIDAS` | Usuario o clave incorrectos (con los intentos restantes) |
  | 401 | `SESION_INVALIDA` | Sin token, token inventado, cerrado o vencido |
  | 423 | `USUARIO_BLOQUEADO` | Tercer intento fallido o usuario aún bloqueado |
  | 409 | `REGLA_NEGOCIO` | Cuenta inexistente o no activa, saldo insuficiente, monedas distintas |
  | 503 | `ERROR_BD` | SQL Server apagado |

  Cada cliente muestra el `mensaje` en rojo; si no llega al servidor dice *"No se pudo conectar a {url}. Verifica IP, puerto y red."*
- **Por qué Web API 2 en .NET Framework** (igual que el proyecto de referencia del otro grupo): se publica en IIS con el grupo **.NET CLR v4.0**, sin instalar el *Hosting Bundle* de ASP.NET Core.

## 2. Estructura del repositorio

```
01. UML/01. ERS         ERS_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.docx
01. UML/02. ECUD        ECUD_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.docx
01. UML/03. DIAGRAMAS   PowerDesigner 16.7 (.oom/.eam + .png): 01_CASO_USO, 02_ACTIVIDAD,
                        03_SECUENCIA, 04_CLASES, 05_ARQUITECTURA
02. MER/                modelo conceptual, lógico y físico (.puml + .png)
03. BDD/                01_Crea_BD.sql, 02_Carga_Datos.sql, 03_Crea_Login_monster.sql,
                        04_Consultas_Prueba.sql y crear_bdd.ps1 (ejecuta todo)
04. SERVIDOR/           EUREKA_RESTFUL_DOTNET_SERVIDOR (Web API 2 .NET Framework 4.7.2, panel index.html) + publicar_iis.ps1
04. CLICON/             EUREKA_RESTFUL_DOTNET_CLICON  (consola .NET Framework 4.7.2)
04. CLIESC/             EUREKA_RESTFUL_DOTNET_CLIESC  (Windows Forms .NET Framework 4.7.2)
04. CLIWEB/             EUREKA_RESTFUL_DOTNET_CLIWEB  (ASP.NET Core MVC .NET 8) + publicar_web_iis.ps1 y scripts de red local
                        (Iniciar-RedLocal.ps1, Detener-RedLocal.ps1, Habilitar-Firewall-RedLocal.ps1)
04. CLIMOV/             EUREKA_RESTFUL_DOTNET_CLIMOV  (.NET MAUI Android .NET 9, sucesor de Xamarin)
05. DOCUMENTACIÓN/      manual, presentación y capturas
Directory.Build.props   envía obj a %LOCALAPPDATA%\EUREKA_REST\obj (la ruta de la carpeta pasa de 260 caracteres)
```

## 3. Endpoints del API

Todas las rutas empiezan con `http://IP:8088/api/eurekabank/`.

| Verbo | Ruta | Cuerpo / parámetros | Token | Respuesta 200 | Qué hace en la BDD |
|---|---|---|:-:|---|---|
| POST | `login` | `{ usuario, clave }` | No | `{ codigo, usuario, nombre, token }` | Busca en `Empleado`; 3 intentos y bloqueo de 2 min |
| POST | `logout` | — | Bearer | `{ cerrada: true/false }` | Invalida el token (no toca la BDD) |
| GET | `estado` | — | No | `EstadoServidor` | Métricas del monitor; no cuenta como petición |
| GET | `cuenta` | `?numero=00100001` | Sí | `Cuenta` | `Cuenta` + `Cliente` + `Moneda` + `Sucursal` |
| GET | `movimientos` | `?cuenta=00100001` | Sí | lista de `Movimiento` | `Movimiento` + `TipoMovimiento`, el más reciente primero |
| POST | `deposito` | `{ cuenta, importe, codEmpleado }` | Sí | `{ cuenta, saldo }` | suma saldo, movimiento **003** |
| POST | `retiro` | `{ cuenta, importe, codEmpleado }` | Sí | `{ cuenta, saldo }` | valida saldo, resta, movimiento **004** |
| POST | `transferencia` | `{ cuentaOrigen, cuentaDestino, importe, codEmpleado }` | Sí | `{ cuenta, saldo }` del origen | **009** en el origen y **008** en el destino, cada uno con la otra cuenta como referencia |

**Reglas (todas en el servidor):** cuenta de 8 dígitos; importe numérico, mayor que cero, con máximo 2 decimales y hasta 1 000 000; la cuenta debe estar `ACTIVO`; el retiro y la transferencia exigen saldo suficiente; la transferencia es entre cuentas distintas y de la misma moneda. Las cuentas se leen con `UPDLOCK` dentro de la transacción, así dos operaciones simultáneas sobre la misma cuenta no se pisan. El número de movimiento es `int_cuencontmov + 1`.

### Sesión con token (30 minutos)

`POST login` devuelve un **token** (32 caracteres hexadecimales). `cuenta`, `movimientos`, `deposito`, `retiro` y `transferencia` lo exigen en el header HTTP:

```http
Authorization: Bearer 9f3c…
X-Cliente: CONSOLA
```

Sin token, con uno inventado o vencido responden **401** `{ "codigo": "SESION_INVALIDA", "mensaje": "Token inválido o expirado. Inicie sesión." }`. `GestorSesiones` (Singleton) guarda `token → vencimiento`, renueva los 30 minutos en cada uso, borra los vencidos cada minuto y cuenta las sesiones activas. `POST logout` lo invalida. Ningún cliente muestra el token.

### Monitor y panel (`http://IP:8088/`)

`MonitorServidor` (Singleton, seguro entre hilos) guarda **en memoria** la hora de inicio, el total de peticiones y de operaciones exitosas, el conteo por cliente (`WEB`, `MOVIL`, `ESCRITORIO`, `CONSOLA`, `PANEL`, `OTRO`), el conteo por operación (`Deposito`, `Retiro`, `Transferencia`, `Consulta`, `Movimientos`) y los **últimos 60 eventos** (hora, cliente, operación, éxito). **No guarda claves, tokens ni montos** y se reinicia al volver a desplegar. El cliente se toma del header **`X-Cliente`** que envía cada aplicación.

La raíz del sitio abre el **panel** (`index.html` + `panel.js` + `panel.css` + `monster.jpg`). Primero pide **login** (`POST login` con un empleado de la BD, `monster` / `monster69`) y después muestra dos pestañas: **Servicio** (tabla de endpoints con método, ruta, parámetros y si piden token; enlaces al API, al estado y al cliente web; prueba rápida de consulta y depósito) y **Monitoreo** (estado *Servicio activo / Sin respuesta* cada 3 s con `GET estado`, tarjetas de tiempo activo, sesiones, operaciones y peticiones, eventos en tiempo real y barras por operación y por cliente). El panel envía `X-Cliente: PANEL`, guarda la sesión en `sessionStorage`, el botón **Cerrar sesión** llama a `POST logout` y los textos se escapan antes de mostrarse. CORS está habilitado (`Authorization`, `X-Cliente`) para probar el API desde otra página.

## 4. Patrones: Singleton y MVC

### Singleton (constructor privado + `Lazy<T>`, seguro entre hilos)

| Dónde | Clase | Uso |
|---|---|---|
| Servidor | `ec.edu.monster.db.ConexionBD` | Lee la cadena `EurekaBank` de `Web.config` una vez; `ConexionBD.Instancia.AbrirConexion()` |
| Servidor | `ec.edu.monster.servicio.EurekaServicio` | `EurekaServicio.Instancia.RegistrarRetiro(...)`; guarda los intentos de login |
| Servidor | `ec.edu.monster.servicio.GestorSesiones` | Tokens y vencimientos |
| Servidor | `ec.edu.monster.servicio.MonitorServidor` | Métricas del panel |
| CLICON / CLIESC | `ec.edu.monster.servicio.ClienteEureka` | `ClienteEureka.Instancia` (un solo `HttpClient`) |
| CLIWEB | `ec.edu.monster.servicio.ClienteEureka` | `builder.Services.AddSingleton<ClienteEureka>()` |
| CLIMOV | `ClienteRest` y `ControladorEureka` | `ClienteRest.Instancia`, `ControladorEureka.Instancia` |

### MVC en los clientes (`ec/edu/monster/...`)

| Cliente | modelo | vista | controlador |
|---|---|---|---|
| CLICON | `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio`, `Solicitudes`, `TipoOperacion` | `VistaConsola` | `ControladorConsola` |
| CLIESC | ídem | `FormBienvenida`, `FormLogin`, `FormPrincipal`, `PanelMovimientos`, `PanelOperacion`, `FormComprobante`, `TarjetaCuenta`, `GraficoCuenta`, `IndicadorServidor` | `ControladorEureka` |
| CLIWEB | ídem + `ViewModels.cs` | `vista/Acceso/Login`, `vista/Banco/Movimientos`, `vista/Banco/Operacion` | `AccesoController`, `BancoController` |
| CLIMOV | ídem | `BienvenidaPage`, `LoginPage`, `MenuPage`, `MovimientosPage`, `OperacionPage`, `ComprobantePage` | `ControladorEureka` |

**Los clientes solo presentan; todo viene del servidor:**
- **`modelo/`**: `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio` y los cuerpos `SolicitudLogin`, `SolicitudMovimiento`, `SolicitudTransferencia`, `ResultadoOperacion`, `ResultadoCierre` con **solo get/set** (lo que viaja en JSON).
- **`servicio/`**: `ClienteEureka` (`ClienteRest` en el móvil) arma la petición HTTP con `X-Cliente` y `Authorization: Bearer`, envía y lee el JSON y, si el código no es 2xx, lanza `ErrorServicioException` con el `mensaje` del servidor. `ConfiguracionServidor` arma la URL desde la IP y el puerto.
- **`controlador/`**: solo pasa las llamadas de la vista al servicio y guarda el empleado de la sesión.
- **`vista/`**: las pantallas, el formato (`Formato`: dinero, títulos, colores) y la imagen de **Monsters Inc.** (`monster.jpg`).

**Todo lo demás está en el servidor:** el login y los **3 intentos** (al tercero el usuario queda bloqueado 2 minutos), el formato de la cuenta, el importe, las cuentas distintas, el saldo, la moneda y el estado. El cliente solo convierte el importe a número y muestra el mensaje del error.

## 5. Requisitos

| Herramienta | Para qué |
|---|---|
| **SQL Server** (2019/2022) en **modo de autenticación mixto** | Base `EUREKABANK` |
| `sqlcmd` o SQL Server Management Studio | Ejecutar los scripts |
| **Visual Studio 2022 Community** con *ASP.NET y web*, *Escritorio .NET* y *.NET MAUI* | Todo el código |
| IIS con ASP.NET 4.8 (API) y el **ASP.NET Core Hosting Bundle 8** (cliente web) | Publicar en red |
| Conexión a internet la primera vez | Visual Studio descarga los paquetes NuGet de Web API |
| Celular Android 7.0+ con depuración USB | Cliente móvil |

> **Regla de oro:** abre **cada solución en su propia ventana de Visual Studio**. Si abres otra solución en la ventana del servidor, se apaga IIS Express.

---

## Paso 0 — Base de datos (`03. BDD`)

Si ya creaste la base para el TG.1.2, **no hace falta repetirlo** (es la misma `EUREKABANK`).

**Opción A (script):** en PowerShell, dentro de `03. BDD`:
```powershell
powershell -ExecutionPolicy Bypass -File .\crear_bdd.ps1
```
**Opción B (SSMS):** abre y ejecuta en orden `01_Crea_BD.sql`, `02_Carga_Datos.sql`, `03_Crea_Login_monster.sql`.

**Cuentas de prueba:**

| Cuenta | Cliente | Moneda | Saldo inicial | Estado |
|---|---|---|---|---|
| 00100001 | Alan Aranda | Soles | 6 900.00 | ACTIVO |
| 00100002 | Alan Aranda | Dólares | 4 500.00 | ACTIVO |
| 00200001 | Rosa Flores | Soles | 7 000.00 | ACTIVO |
| 00200002 | Eric Coronel | Soles | 6 800.00 | ACTIVO |
| 00200003 | Edgar Chávez | Dólares | 6 000.00 | ACTIVO |
| 00300001 | Gabriel Gonzales | Soles | 0.00 | CANCELADO |

## Paso 1 — Servidor (`04. SERVIDOR`)

1. Abre `EUREKA_RESTFUL_DOTNET_SERVIDOR.sln`. La primera vez Visual Studio **restaura los paquetes NuGet** (Web API 5.2.9 y Newtonsoft.Json); `nuget.config` los guarda en `%LOCALAPPDATA%\EUREKA_REST_SERVIDOR\packages`.
2. **Depurar → Iniciar sin depurar**: abre el panel `http://localhost:8088/` (el API está en `/api/eurekabank/`).
3. La conexión está en `Web.config` (`Data Source=localhost;…;User ID=monster;Password=monster`). Con SQL Express cambia `Data Source=.\SQLEXPRESS`.

## Paso 2 — Consola (`04. CLICON`)

Abre `EUREKA_RESTFUL_DOTNET_CLICON.sln` en otra ventana → **Iniciar sin depurar** → `monster` / `monster69`. Menú: **[1] Consultar cuenta y movimientos, [2] Depósito, [3] Retiro, [4] Transferencia, [5] Estado del servidor, [0] Cerrar sesión** (llama a `logout`). El servidor se configura en el login con **S**, en `App.config` (`RestHost`, `RestPuerto` = 8088) o sin recompilar con las variables `REST_HOST` / `REST_PORT`:
```powershell
$env:REST_HOST = "192.168.100.2"; .\EUREKA_RESTFUL_DOTNET_CLICON.exe
```

## Paso 3 — Escritorio (`04. CLIESC`)

Abre `EUREKA_RESTFUL_DOTNET_CLIESC.sln` → **Iniciar sin depurar** → bienvenida y login. Menú lateral: **Movimientos, Depósito, Retiro, Transferencia**. Pide confirmación antes de registrar y muestra el comprobante. Servidor en la fila **Servidor [IP] : [puerto] → Cambiar** del login o en `App.config` (`RestHost`, `RestPuerto`).

## Paso 4 — Web (`04. CLIWEB`)

Abre `EUREKA_RESTFUL_DOTNET_CLIWEB.sln` → **Iniciar sin depurar** → `http://localhost:5088`. Servidor en **⚙ Servidor · cambiar** del login o en `appsettings.json`, sección `ServidorRest` (`Host`, `Puerto` = 8088). Para publicarla en IIS: `publicar_web_iis.ps1` como administrador (sitio `EUREKA_REST_WEB`, puerto 5088, conserva `appsettings.json`). Incluye filtro `[RequiereSesion]` y `ValidateAntiForgeryToken`. Es *responsive* y tiene modo oscuro.

## Paso 5 — Móvil (`04. CLIMOV`)

Celular con **Depuración USB**, abrir `EUREKA_RESTFUL_DOTNET_CLIMOV.sln`, elegir el celular y **Iniciar sin depurar**. La app se instala como **EurekaBank REST** (`ec.edu.monster.eureka.rest`), sin reemplazar la del TG.1.2. En el login, cuadro **⚙ Servidor REST (IIS)**: IP y puerto → **Guardar** (**Restablecer** vuelve al valor de `appsettings.json`).

- Por WiFi (lo normal): IP de la PC con IIS, puerto 8088
- Por cable: `adb reverse tcp:8088 tcp:8088` y servidor `localhost`, puerto 8088
- Emulador: `10.0.2.2`, puerto 8088

Para instalar con adb sin Visual Studio: compilar con `-p:EmbedAssembliesIntoApk=true -t:SignAndroidPackage` (si no, la app se cierra al abrir) y borrar antes los APK viejos de `bin`.
`Directory.Build.props` manda `obj` a `%LOCALAPPDATA%\EUREKA_REST_CLIMOV\obj` (Android falla con rutas de más de 260 caracteres).

## Configuración del servidor en cada cliente

El servidor corre en **IIS** y los clientes lo buscan por la **IP de la PC** (no `localhost`): por defecto `192.168.100.2:8088`. Como la IP cambia según la red, **cada cliente la puede cambiar desde su propia pantalla** sin recompilar. En el campo de IP se acepta la IP sola, `IP:puerto` o la URL completa (`http://192.168.100.2:8088/api/eurekabank/`). Si la configuración es inválida el cliente **no se cierra**: muestra el error y deja corregirla.

| Cliente | Cambiar desde la pantalla | Valor por defecto | Envía `X-Cliente` |
|---|---|---|---|
| Consola | En el login escribir **S** → IP y puerto (se guardan en el `.exe.config`) | `App.config` → `RestHost`, `RestPuerto` (o variables `REST_HOST`, `REST_PORT`) | `CONSOLA` |
| Escritorio | Login → fila **Servidor** `[IP] : [puerto]` → **Cambiar** | `App.config` → `RestHost`, `RestPuerto` | `ESCRITORIO` |
| Web | Login → **⚙ Servidor · cambiar** → **Guardar** (se guarda en `appsettings.json`) | `appsettings.json` → `ServidorRest:Host`, `Puerto` | `WEB` |
| Móvil | Login → **⚙ Servidor REST (IIS)** → **Guardar** (queda en el teléfono; **Restablecer** vuelve al valor por defecto) | `appsettings.json` incrustado (`EmbeddedResource`) | `MOVIL` |

## Presentación de los clientes (estilo Monsters Inc.)

| Mejora | Consola | Escritorio | Web | Móvil |
|---|:-:|:-:|:-:|:-:|
| Título **MONSTER** y **EUREKA BANK** | letras de bloque | marca en el menú | marca en el menú | login y menú |
| Bienvenida con Sulley y Boo (`monster.jpg`) | — | `FormBienvenida` | login con foto | `BienvenidaPage` |
| Intentos de login con 3 caritas 👾 (según "Intentos restantes") | ■ ■ □ | ✔ | ✔ | ✔ |
| Indicador **Servidor activo / Sin respuesta** y minutos de sesión | encabezado + opción 5 | barra de estado | arriba a la derecha | login y menú |
| Cuenta como **tarjeta bancaria** (0010 0001, titular, saldo) | — | ✔ | ✔ | ✔ |
| **Gráficos**: total por tipo y evolución del saldo | — | ✔ | ✔ | ✔ |
| **Comprobante** con N.º de movimiento y sello APROBADO | marco doble | ventana + Imprimir | ventana + Imprimir/PDF | ventana + Compartir |
| "Enviando…" mientras responde el servidor | spinner ⠹ | cursor de espera | — | indicador |
| Modo oscuro | — | — | ✔ (se recuerda) | ✔ |

El número de movimiento y el titular del comprobante se piden con `GET cuenta` justo después de la operación.

## Paso 6 — Trabajo en red (IIS)

| Integrante | Rol |
|---|---|
| 1 | Base de datos + servidor + consola |
| 2 | Web + escritorio (apuntando a la IP del integrante 1) |
| 3 | Móvil en celular real (misma WiFi) |

IIS Express solo acepta conexiones locales. El integrante 1 compila el servidor, cierra su Visual Studio y ejecuta **como administrador**:
```powershell
powershell -ExecutionPolicy Bypass -File "RUTA\04. SERVIDOR\publicar_iis.ps1"
powershell -ExecutionPolicy Bypass -File "RUTA\04. CLIWEB\publicar_web_iis.ps1"
```
El primero crea el sitio **EUREKA_REST** en el puerto **8088** (`C:\inetpub\eureka_rest`, con el panel) y la regla de firewall "EUREKA REST 8088"; el segundo publica la web en **EUREKA_REST_WEB :5088**. Como el API entra a SQL Server con el login `monster`, no hace falta dar permisos al usuario de IIS.

**Publicación manual (como la pide el ingeniero):**
1. Visual Studio **como administrador** → abrir el servidor → configuración **Release** → clic derecho en el proyecto → **Publicar** → **Carpeta** → `C:\inetpub\eureka_rest` (el perfil ya está en `Properties/PublishProfiles/FolderProfile.pubxml`).
2. Administrador de IIS → **Grupos de aplicaciones** → **Agregar**: nombre `EUREKA_REST`, **.NET CLR v4.0**, canalización **Integrada**.
3. **Sitios** → **Agregar sitio web**: nombre `EUREKA_REST`, grupo `EUREKA_REST`, ruta física `C:\inetpub\eureka_rest`, `http`, *Todas las no asignadas*, puerto `8088`.
4. Firewall: regla de entrada para el puerto TCP **8088**.
5. Probar desde otro equipo de la misma WiFi: `http://IP:8088/` (panel) y `http://IP:8088/api/eurekabank/estado` (JSON).

## Casos de prueba

| # | Caso | Entrada | Resultado esperado |
|---|---|---|---|
| 1 | Login correcto | monster / monster69 | Entra al menú |
| 2 | Login incorrecto | monster / otra | 401 "Credenciales inválidas. Intentos restantes: 2." (lo cuenta el servidor) |
| 2b | Tercer intento fallido | monster / otra (3 veces) | 423 "Cuenta bloqueada: se superaron los 3 intentos. Intentos restantes: 0. Espere 2 minutos." |
| 2c | Sin token | `GET api/eurekabank/cuenta?numero=00100001` sin `Authorization` | 401 "Token inválido o expirado. Inicie sesión." |
| 2d | Token cerrado | `POST logout` y reutilizar el token | 401 `SESION_INVALIDA` |
| 3 | Consultar cuenta | 00100001 | Tarjeta con el saldo y los movimientos |
| 4 | Cuenta inexistente | 99999999 | 409 "La cuenta 99999999 no existe." |
| 5 | Formato de cuenta | 123 | 400 "La cuenta debe tener 8 dígitos (ejemplo: 00100001)." |
| 6 | Depósito | 00100001, 100 | Comprobante con el N.º de movimiento (003) y el nuevo saldo |
| 7 | Saldo insuficiente | retiro 00100001, 50000 | 409 "Saldo insuficiente…" |
| 8 | Cuenta cancelada | retiro en 00300001 | 409 "La cuenta 00300001 no está activa (CANCELADO)." |
| 9 | Transferencia | 00100002 → 00200003, 25 | Movimientos 009 y 008 con referencia cruzada |
| 10 | Monedas distintas | 00100001 → 00100002 | 409 "Solo se puede transferir entre cuentas de la misma moneda." |
| 11 | Más de 2 decimales | 1.555 | 400 "El importe admite como máximo 2 decimales." |
| 12 | Importe no numérico (JSON) | `"importe": "abc"` | 400 "El importe debe ser un número…" (los clientes avisan antes de enviar) |
| 13 | SQL Server apagado | cualquier operación | 503 "No se pudo acceder a la base de datos EUREKABANK…" (no se probó: requiere detener SQL Server) |
| 14 | Servidor apagado | — | "No se pudo conectar a http://…:8088. Verifica IP, puerto y red." |
| 15 | Página protegida (web) | `/Banco/Movimientos` sin sesión | Redirección al login |
| 16 | Monitor por cliente | operar desde consola, escritorio, web y móvil | El panel cuenta `CONSOLA`, `ESCRITORIO`, `WEB` y `MOVIL` por separado |
| 17 | Cambiar servidor | `http://192.168.100.2:8088/api/eurekabank/` en el campo de IP | Se separan IP y puerto y el cliente conecta |

Para las capturas y pruebas se usaron las cuentas 00100001, 00100002 y 00200003. Para volver a los datos iniciales, ejecuta otra vez `03. BDD\crear_bdd.ps1` (afecta también al TG.1.2, que usa la misma base).

## Documentación

| Archivo | Contenido |
|---|---|
| `01. UML/01. ERS/ERS_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.docx` | Especificación de requisitos (IEEE 830): 19 RF y 9 RNF |
| `01. UML/02. ECUD/ECUD_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.docx` | Casos de uso CU01–CU08 con sus diagramas de casos de uso, actividad y secuencia |
| `01. UML/03. DIAGRAMAS/` | Diagramas en PowerDesigner 16.7: 5 de casos de uso, 4 de actividad, 8 de secuencia, clases y arquitectura |
| `05. DOCUMENTACIÓN/WS_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.docx` | Manual técnico paso a paso, con el código y las capturas |
| `05. DOCUMENTACIÓN/MANUAL_EUREKABANK_RESTFUL_DOTNET_BDD_GR01.pptx` | Presentación (17 diapositivas) |
| `05. DOCUMENTACIÓN/capturas/` | Capturas reales del servidor (panel y respuestas JSON), BDD, consola, escritorio, web y móvil |

Al abrir el manual en Word, si el índice aparece vacío: clic derecho sobre él → **Actualizar campo**.

Los documentos y diagramas se generan con `_herramientas/generador_documentos_eureka_rest` y `_herramientas/generador_powerdesigner_eureka_rest` (fuera de este repositorio). Las capturas de la consola son de la ventana real (conhost), las del escritorio de la app Windows Forms real, las web y del panel con Chrome automático, y las del móvil de una tablet Samsung SM-P610 con `adb exec-out screencap -p`.
