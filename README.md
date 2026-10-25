# TG1.4 · EurekaBank — RESTful .NET con base de datos (SQL Server)

Proyecto de **Arquitectura de Software (ESPE)**: un **API REST** en .NET (ASP.NET Web API 2) que maneja las cuentas de ahorro de **EurekaBank** sobre **SQL Server**, y **cuatro clientes** que lo consumen: **consola, escritorio, web y móvil**.

Operaciones: **consultar cuenta y movimientos, depósito, retiro y transferencia**. Cada operación queda grabada en la tabla `Movimiento` y actualiza el saldo de `Cuenta` dentro de una transacción.

| | |
|---|---|
| **Usuario de la app** | `monster` / `monster69` (tabla `Empleado`, código `0012`) |
| **Login de SQL Server** | `monster` / `monster` (lo usa el servidor para conectarse) |
| **Base de datos** | `EUREKABANK` en SQL Server local (`localhost`), la misma del TG1.2 |
| **API REST** | `http://localhost:8088/api/eurekabank/...` (página de prueba en `http://localhost:8088`) |
| **Cliente web** | `http://localhost:5088` |
| **IDE** | Visual Studio 2022 Community |
| **Paquetes** | `ec.edu.monster.db` · `modelo` · `servicio` · `ws` (servidor) y `modelo` · `vista` · `controlador` · `servicio` (clientes) |
| **Patrones** | Singleton (`ConexionBD`, `EurekaServicio`, `ClienteEureka`, `ClienteRest`) · MVC en los clientes |

Los puertos (8088 / 5088) son distintos a los del TG1.2 SOAP (8087 / 5087), así los dos proyectos pueden estar publicados a la vez.

---

## 1. Arquitectura

```
  CLICON (consola)  ───►  ┌──────────────────────────────────────────┐
  CLIESC (escritorio) ─►  │ IIS / IIS Express :8088                  │
  CLIWEB  :5088  ──────►  │ api/eurekabank/* (Web API 2 · JSON)      │      ┌────────────────────┐
  CLIMOV (Android) ────►  │  ec.edu.monster.ws       EurekabankController │ │ SQL Server         │
                          │  ec.edu.monster.servicio EurekaServicio ──────►│ EUREKABANK         │
          HTTP + JSON     │  ec.edu.monster.db       ConexionBD      │ ADO │ login monster      │
                          └──────────────────────────────────────────┘ .NET └────────────────────┘
```

- **Protocolo:** REST sobre HTTP, cuerpos y respuestas en **JSON** (camelCase). GET para consultar, POST para el login y las operaciones.
- **Errores:** el servidor responde un código HTTP con `ErrorServicio { codigo, mensaje }`:
  `400 VALOR_INVALIDO` (formato), `401 REGLA_NEGOCIO` (login incorrecto o bloqueado), `409 REGLA_NEGOCIO` (saldo insuficiente, cuenta cancelada, monedas distintas…), `503 ERROR_BD` (SQL Server apagado). Cada cliente muestra el `mensaje` en rojo.
- **Por qué Web API 2 en .NET Framework** (igual que el proyecto de referencia): se publica en IIS con el grupo **.NET CLR v4.0**, sin instalar el *Hosting Bundle* de ASP.NET Core.

## 2. Estructura del repositorio

```
01. UML/03. DIAGRAMAS   casos de uso, actividad, secuencia, clases, arquitectura (.puml + .png)
02. MER/                modelo conceptual, lógico y físico (.puml + .png)
03. BDD/                01_Crea_BD.sql, 02_Carga_Datos.sql, 03_Crea_Login_monster.sql,
                        04_Consultas_Prueba.sql y crear_bdd.ps1 (ejecuta todo)
04. SERVIDOR/           EUREKA_RESTFUL_DOTNET_SERVIDOR (Web API 2, .NET Framework 4.7.2) + publicar_iis.ps1
04. CLICON/             EUREKA_RESTFUL_DOTNET_CLICON  (consola .NET Framework 4.7.2)
04. CLIESC/             EUREKA_RESTFUL_DOTNET_CLIESC  (Windows Forms .NET Framework 4.7.2)
04. CLIWEB/             EUREKA_RESTFUL_DOTNET_CLIWEB  (ASP.NET Core MVC .NET 8)
04. CLIMOV/             EUREKA_RESTFUL_DOTNET_CLIMOV  (.NET MAUI Android .NET 9, sucesor de Xamarin)
05. DOCUMENTACIÓN/      manual, presentación y capturas (pendiente)
```

## 3. Operaciones del API

| Verbo | Ruta | Cuerpo (JSON) | Respuesta 200 | Qué hace en la BDD |
|---|---|---|---|---|
| POST | `api/eurekabank/login` | `{ "usuario", "clave" }` | `{ codigo, usuario, nombre }` | Busca en `Empleado` (usuario sin distinguir mayúsculas, clave exacta) |
| GET | `api/eurekabank/cuenta?numero=…` | — | Cuenta | `Cuenta` + `Cliente` + `Moneda` + `Sucursal` |
| GET | `api/eurekabank/movimientos?cuenta=…` | — | lista de Movimiento | `Movimiento` + `TipoMovimiento`, el más reciente primero |
| POST | `api/eurekabank/deposito` | `{ "cuenta", "importe", "codEmpleado" }` | `{ cuenta, saldo }` | suma saldo, movimiento **003** |
| POST | `api/eurekabank/retiro` | `{ "cuenta", "importe", "codEmpleado" }` | `{ cuenta, saldo }` | valida saldo, resta, movimiento **004** |
| POST | `api/eurekabank/transferencia` | `{ "cuentaOrigen", "cuentaDestino", "importe", "codEmpleado" }` | `{ cuenta, saldo }` del origen | **009** en el origen y **008** en el destino, cada uno con la otra cuenta como referencia |

Ejemplo de respuesta de `GET api/eurekabank/cuenta?numero=00100001`:

```json
{"codigo":"00100001","cliente":"ALAN ALBERTO ARANDA LUNA","moneda":"Soles","sucursal":"Sipan",
 "saldo":7000.50,"estado":"ACTIVO","fechaCreacion":"2008-01-06T00:00:00","numeroMovimientos":8}
```

Ejemplo de error (`POST api/eurekabank/login` con clave incorrecta → **HTTP 401**):

```json
{"codigo":"REGLA_NEGOCIO","mensaje":"Usuario o clave incorrectos. Intentos restantes: 2."}
```

**Reglas (todas en el servidor):** cuenta de 8 dígitos; importe mayor que cero, numérico, con máximo 2 decimales y hasta 1 000 000; la cuenta debe estar `ACTIVO`; el retiro y la transferencia exigen saldo suficiente; la transferencia es entre cuentas distintas y de la misma moneda. Las cuentas se leen con `UPDLOCK` dentro de la transacción, así dos operaciones simultáneas sobre la misma cuenta no se pisan. El número de movimiento es `int_cuencontmov + 1`.

**Página de prueba:** al abrir `http://localhost:8088` aparece `index.html` con las 6 operaciones, la tabla de errores y un formulario para probar cada una desde el navegador (cumple el papel de la página del ASMX en el TG1.2).

## 4. Patrones: Singleton y MVC

### Singleton (constructor privado + `Lazy<T>`, seguro entre hilos)

| Dónde | Clase | Uso |
|---|---|---|
| Servidor | `ec.edu.monster.db.ConexionBD` | Lee la cadena `EurekaBank` de `Web.config` una vez; `ConexionBD.Instancia.AbrirConexion()` |
| Servidor | `ec.edu.monster.servicio.EurekaServicio` | `EurekaServicio.Instancia.RegistrarRetiro(...)`; guarda los intentos de login de todos los usuarios |
| CLICON / CLIESC | `ec.edu.monster.servicio.ClienteEureka` | `ClienteEureka.Instancia` (un solo `HttpClient`) |
| CLIWEB | `ec.edu.monster.servicio.ClienteEureka` | `builder.Services.AddSingleton<ClienteEureka>()` |
| CLIMOV | `ClienteRest` y `ControladorEureka` | `ClienteRest.Instancia`, `ControladorEureka.Instancia` |

### MVC en los clientes (`ec/edu/monster/...`)

| Cliente | modelo | vista | controlador |
|---|---|---|---|
| CLICON | `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio`, `Solicitudes`, `TipoOperacion` | `VistaConsola` | `ControladorConsola` |
| CLIESC | ídem | `FormLogin`, `FormPrincipal`, `PanelMovimientos`, `PanelOperacion` | `ControladorEureka` |
| CLIWEB | ídem + `ViewModels.cs` | `vista/Acceso/Login`, `vista/Banco/Movimientos`, `vista/Banco/Operacion` | `AccesoController`, `BancoController` |
| CLIMOV | ídem | `LoginPage`, `MenuPage`, `MovimientosPage`, `OperacionPage` | `ControladorEureka` |

**Los clientes solo presentan; todo viene del servidor** (igual que el GR05 y el TG1.2):
- **`modelo/`**: `Empleado`, `Cuenta`, `Movimiento`, `ErrorServicio` y los cuerpos `SolicitudLogin`, `SolicitudMovimiento`, `SolicitudTransferencia`, `ResultadoOperacion`, con **solo get/set** (lo que viaja en JSON).
- **`servicio/`**: `ClienteEureka` (o `ClienteRest` en el móvil) arma la petición HTTP, envía/lee el JSON y, si el código no es 2xx, lanza `ErrorServicioException` con el `mensaje` del servidor.
- **`controlador/`**: solo pasa las llamadas de la vista al servicio y guarda el empleado de la sesión.
- **`vista/`**: las pantallas y el formato (`Formato`: dinero, títulos, colores) y la imagen de **Monsters Inc.** (`monster.jpg`) en escritorio, web y móvil.

**Todo lo demás está en el servidor:** el login y los **3 intentos** (al tercero el usuario queda bloqueado 2 minutos), el formato de la cuenta, el importe, las cuentas distintas, el saldo, la moneda y el estado. El cliente solo convierte el importe a número y muestra el mensaje del error.

## 5. Requisitos

| Herramienta | Para qué |
|---|---|
| **SQL Server** (2019/2022) en **modo de autenticación mixto** | Base `EUREKABANK` |
| `sqlcmd` o SQL Server Management Studio | Ejecutar los scripts |
| **Visual Studio 2022 Community** con *ASP.NET y web*, *Escritorio .NET* y *.NET MAUI* | Todo el código |
| Conexión a internet la primera vez | Visual Studio descarga los paquetes NuGet de Web API |
| Celular Android 7.0+ con depuración USB | Cliente móvil |

> **Regla de oro:** abre **cada solución en su propia ventana de Visual Studio**. Si abres otra solución en la ventana del servidor, se apaga IIS Express.

---

## Paso 0 — Base de datos (`03. BDD`)

Si ya creaste la base para el TG1.2, **no hace falta repetirlo** (es la misma `EUREKABANK`).

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

1. Abre `EUREKA_RESTFUL_DOTNET_SERVIDOR.sln`. La primera vez Visual Studio **restaura los paquetes NuGet** (Web API 5.2.9 y Newtonsoft.Json); `nuget.config` los guarda en `%LOCALAPPDATA%\EUREKA_REST_SERVIDOR\packages` porque la ruta de la carpeta de entrega es muy larga.
2. **Depurar → Iniciar sin depurar**. Se abre `http://localhost:8088` con la página de prueba del API.
3. Prueba rápida en el navegador: `http://localhost:8088/api/eurekabank/cuenta?numero=00100001`.
4. La conexión está en `Web.config` (`Data Source=localhost;…;User ID=monster;Password=monster`). Con SQL Express cambia `Data Source=.\SQLEXPRESS`.

## Paso 2 — Consola (`04. CLICON`)

Abre `EUREKA_RESTFUL_DOTNET_CLICON.sln` en otra ventana → **Iniciar sin depurar** → `monster` / `monster69`. Menú: **[1] Consultar cuenta y movimientos, [2] Depósito, [3] Retiro, [4] Transferencia, [0] Cerrar sesión**. La URL está en `App.config` (`UrlServidor`; en la misma PC: `http://localhost:8088`).

## Paso 3 — Escritorio (`04. CLIESC`)

Abre `EUREKA_RESTFUL_DOTNET_CLIESC.sln` → **Iniciar sin depurar** → login con la imagen de Monsters Inc. Menú lateral: **Movimientos, Depósito, Retiro, Transferencia**. URL en `App.config`.

## Paso 4 — Web (`04. CLIWEB`)

Abre `EUREKA_RESTFUL_DOTNET_CLIWEB.sln` → perfil **EUREKA_RESTFUL_DOTNET_CLIWEB** → **Iniciar sin depurar** → `http://localhost:5088`. URL del API en `appsettings.json` (`ServicioRest:Url`). Incluye filtro `[RequiereSesion]` y `ValidateAntiForgeryToken`. Es *responsive*.

## Paso 5 — Móvil (`04. CLIMOV`)

Celular con **Depuración USB**, abrir `EUREKA_RESTFUL_DOTNET_CLIMOV.sln`, elegir el celular y **Iniciar sin depurar**. En el login, cuadro **Servidor REST**:

- Por WiFi: `http://IP-DE-LA-PC:8088` (servidor publicado en IIS, Paso 6)
- Por cable: `adb reverse tcp:8088 tcp:8088` y `http://localhost:8088`
- Emulador: `http://10.0.2.2:8088`

La app se instala como **EurekaBank REST** (`ec.edu.monster.eureka.rest`), así no reemplaza a la del TG1.2.

## Paso 6 — Trabajo en red (IIS)

| Integrante | Rol |
|---|---|
| 1 | Base de datos + servidor + consola |
| 2 | Web + escritorio (apuntando a la IP del integrante 1) |
| 3 | Móvil en celular real (misma WiFi) |

IIS Express solo acepta conexiones locales, por eso el servidor se publica en IIS.

**Publicación manual (como la pide el ingeniero):**
1. Visual Studio **como administrador** → abrir el servidor → configuración **Release** → clic derecho en el proyecto → **Publicar** → **Carpeta** → `C:\inetpub\eureka_rest` → **Publicar** (el perfil ya está en `Properties/PublishProfiles/FolderProfile.pubxml`).
2. Administrador de IIS → **Grupos de aplicaciones** → **Agregar**: nombre `EUREKA_REST`, **.NET CLR v4.0**, canalización **Integrada**.
3. **Sitios** → **Agregar sitio web**: nombre `EUREKA_REST`, grupo `EUREKA_REST`, ruta física `C:\inetpub\eureka_rest`, `http`, *Todas las no asignadas*, puerto `8088`.
4. Firewall: regla de entrada para el puerto TCP **8088**.
5. Probar en el navegador `http://IP:8088` y `http://IP:8088/api/eurekabank/cuenta?numero=00100001` **desde otro equipo** de la misma WiFi.
6. Después de publicar, no ejecutes el servidor en Visual Studio (el puerto 8088 ya lo usa IIS).

**Con script:** compila el servidor, cierra su Visual Studio y ejecuta **como administrador**:
```powershell
powershell -ExecutionPolicy Bypass -File "RUTA\04. SERVIDOR\publicar_iis.ps1"
```
Hace lo mismo (sitio, grupo, firewall "EUREKA REST 8088") y muestra la URL Wi-Fi.

Los clientes ya apuntan a `http://192.168.100.3:8088`. Si cambia la IP de la PC (`ipconfig` → IPv4 del Wi-Fi), cámbiala en `App.config` (consola y escritorio), `appsettings.json` (web) y en el login del móvil.

## Casos de prueba

| # | Caso | Entrada | Resultado esperado |
|---|---|---|---|
| 1 | Login correcto | monster / monster69 | Entra al menú |
| 2 | Login incorrecto | monster / otra | HTTP 401 "Usuario o clave incorrectos. Intentos restantes: 2." (lo cuenta el servidor) |
| 2b | Tercer intento fallido | monster / otra (3 veces) | "Se superaron los 3 intentos. El usuario queda bloqueado 2 minutos." |
| 3 | Consultar cuenta | 00100001 | Saldo y movimientos de la cuenta |
| 4 | Cuenta inexistente | 99999999 | HTTP 409 "La cuenta 99999999 no existe." |
| 5 | Formato de cuenta | 123 | HTTP 400 "La cuenta debe tener 8 dígitos (ejemplo: 00100001)." |
| 6 | Depósito | 00100001, 100.50 | Nuevo saldo, movimiento 003 |
| 7 | Retiro | 00100001, 0.50 | Nuevo saldo, movimiento 004 |
| 8 | Saldo insuficiente | 00100001, 50000 | HTTP 409 "Saldo insuficiente…" |
| 9 | Cuenta cancelada | retiro en 00300001 | HTTP 409 "La cuenta 00300001 no está activa (CANCELADO)." |
| 10 | Transferencia | 00100001 → 00200001, 100 | Movimientos 009 y 008 con referencia cruzada |
| 11 | Monedas distintas | 00100001 → 00100002 | HTTP 409 "Solo se puede transferir entre cuentas de la misma moneda." |
| 12 | Más de 2 decimales | 1.555 | HTTP 400 "El importe admite como máximo 2 decimales." |
| 13 | Importe no numérico (JSON) | `"importe": "abc"` | HTTP 400 "El importe debe ser un número…" (los clientes ya avisan antes de enviar) |
| 14 | SQL Server apagado | cualquier operación | HTTP 503 "No se pudo acceder a la base de datos EUREKABANK…" |
| 15 | Servidor apagado | — | "No se pudo conectar con el servidor en http://…:8088…" |
| 16 | Página protegida (web) | `/Banco/Movimientos` sin sesión | Redirección al login |

Para volver a los datos iniciales, ejecuta otra vez `03. BDD\crear_bdd.ps1` (afecta también al TG1.2, que usa la misma base).
