using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Web.Http;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.ws
{
    /// <summary>
    /// API REST de EurekaBank (JSON). Solo recibe la petición, exige el token de sesión en las operaciones
    /// del banco (header Authorization: Bearer), delega en EurekaServicio, anota cada petición en el monitor
    /// (cliente según el header X-Cliente) y convierte los errores en un código HTTP con ErrorServicio { codigo, mensaje }:
    /// 400 VALOR_INVALIDO, 401 CREDENCIALES_INVALIDAS / SESION_INVALIDA, 423 USUARIO_BLOQUEADO,
    /// 409 REGLA_NEGOCIO, 503 ERROR_BD, 500 ERROR_SERVIDOR.
    /// </summary>
    [RoutePrefix("api/eurekabank")]
    public class EurekabankController : ApiController
    {
        private static EurekaServicio Servicio => EurekaServicio.Instancia;

        /// <summary>POST api/eurekabank/login  { "usuario": "monster", "clave": "monster69" }  →  { codigo, usuario, nombre, token }</summary>
        [HttpPost, Route("login")]
        public IHttpActionResult Login([FromBody] SolicitudLogin solicitud) =>
            Ejecutar("Login", false, () => Servicio.Login(solicitud?.Usuario, solicitud?.Clave));

        /// <summary>POST api/eurekabank/logout (con Authorization: Bearer)  →  { cerrada }</summary>
        [HttpPost, Route("logout")]
        public IHttpActionResult CerrarSesion() =>
            Ejecutar("CerrarSesion", false, () => new ResultadoCierre { Cerrada = GestorSesiones.Instancia.Cerrar(ContextoPeticion.Token(Request)) });

        /// <summary>
        /// GET api/eurekabank/estado  →  métricas del monitor para el panel. No pide login ni se anota en el monitor
        /// (el panel lo consulta cada 3 segundos y no debe inflar las peticiones).
        /// </summary>
        [HttpGet, Route("estado")]
        public EstadoServidor ObtenerEstado() =>
            MonitorServidor.Instancia.Estado(GestorSesiones.Instancia.ContarActivos());

        /// <summary>GET api/eurekabank/cuenta?numero=00100001  →  Cuenta</summary>
        [HttpGet, Route("cuenta")]
        public IHttpActionResult ConsultarCuenta(string numero = null) =>
            Ejecutar("Consulta", true, () => Servicio.ConsultarCuenta(numero));

        /// <summary>GET api/eurekabank/movimientos?cuenta=00100001  →  lista de Movimiento (el más reciente primero)</summary>
        [HttpGet, Route("movimientos")]
        public IHttpActionResult TraerMovimientos(string cuenta = null) =>
            Ejecutar<List<Movimiento>>("Movimientos", true, () => Servicio.TraerMovimientos(cuenta));

        /// <summary>POST api/eurekabank/deposito  { "cuenta", "importe", "codEmpleado" }  →  { cuenta, saldo }</summary>
        [HttpPost, Route("deposito")]
        public IHttpActionResult RegistrarDeposito([FromBody] SolicitudMovimiento s) =>
            Ejecutar("Deposito", true, () => Resultado(Requerido(s).Cuenta, Servicio.RegistrarDeposito(s.Cuenta, s.Importe, s.CodEmpleado)));

        /// <summary>POST api/eurekabank/retiro  { "cuenta", "importe", "codEmpleado" }  →  { cuenta, saldo }</summary>
        [HttpPost, Route("retiro")]
        public IHttpActionResult RegistrarRetiro([FromBody] SolicitudMovimiento s) =>
            Ejecutar("Retiro", true, () => Resultado(Requerido(s).Cuenta, Servicio.RegistrarRetiro(s.Cuenta, s.Importe, s.CodEmpleado)));

        /// <summary>POST api/eurekabank/transferencia  { "cuentaOrigen", "cuentaDestino", "importe", "codEmpleado" }  →  saldo del origen</summary>
        [HttpPost, Route("transferencia")]
        public IHttpActionResult RegistrarTransferencia([FromBody] SolicitudTransferencia s) =>
            Ejecutar("Transferencia", true, () => Resultado(Requerido(s).CuentaOrigen,
                Servicio.RegistrarTransferencia(s.CuentaOrigen, s.CuentaDestino, s.Importe, s.CodEmpleado)));

        // ---------- Apoyo ----------

        private static ResultadoOperacion Resultado(string cuenta, decimal saldo) =>
            new ResultadoOperacion { Cuenta = (cuenta ?? string.Empty).Trim(), Saldo = saldo };

        /// <summary>
        /// Comprueba que el cuerpo llegó y se pudo leer. Si un campo no tiene el tipo esperado
        /// (por ejemplo "importe": "abc"), Web API lo deja en cero y lo marca en ModelState.
        /// </summary>
        private T Requerido<T>(T solicitud) where T : class
        {
            if (solicitud == null)
            {
                throw new ArgumentException("Envíe los datos de la operación en JSON.");
            }
            if (!ModelState.IsValid)
            {
                throw new ArgumentException("El importe debe ser un número (ejemplo: 150 o 75.50).");
            }
            return solicitud;
        }

        /// <summary>
        /// Valida el token si la operación lo pide, ejecuta la acción, traduce los errores a un código HTTP
        /// y, pase lo que pase, anota la petición en el monitor con el cliente del header X-Cliente.
        /// </summary>
        private IHttpActionResult Ejecutar<T>(string operacion, bool requiereToken, Func<T> accion)
        {
            bool exito = false;
            try
            {
                if (requiereToken)
                {
                    GestorSesiones.Instancia.Validar(ContextoPeticion.Token(Request));
                }
                T resultado = accion();
                exito = true;
                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                return Error(HttpStatusCode.BadRequest, "VALOR_INVALIDO", ex.Message);
            }
            catch (ReglaNegocioException ex)
            {
                return Error(CodigoHttp(ex.Codigo), ex.Codigo, ex.Message);
            }
            catch (SqlException ex)
            {
                // El detalle técnico queda en la salida de depuración; al cliente le llega un mensaje entendible.
                Trace.TraceError("SQL Server: " + ex.Message);
                return Error(HttpStatusCode.ServiceUnavailable, "ERROR_BD",
                    "No se pudo acceder a la base de datos EUREKABANK. Verifique que SQL Server esté encendido.");
            }
            catch (InvalidOperationException ex)
            {
                return Error(HttpStatusCode.InternalServerError, "ERROR_SERVIDOR", ex.Message);
            }
            finally
            {
                MonitorServidor.Instancia.Registrar(ContextoPeticion.Cliente(Request), operacion, exito);
            }
        }

        /// <summary>Código HTTP de cada regla: credenciales y sesión 401, usuario bloqueado 423, el resto 409.</summary>
        private static HttpStatusCode CodigoHttp(string codigo)
        {
            switch (codigo)
            {
                case "CREDENCIALES_INVALIDAS":
                case "SESION_INVALIDA":
                    return HttpStatusCode.Unauthorized;
                case "USUARIO_BLOQUEADO":
                    return (HttpStatusCode)423;
                default:
                    return HttpStatusCode.Conflict;
            }
        }

        private IHttpActionResult Error(HttpStatusCode codigoHttp, string codigo, string mensaje)
        {
            // ArgumentException agrega "Nombre del parámetro: ..." al mensaje; nos quedamos con la primera línea.
            string limpio = mensaje.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
            return Content(codigoHttp, new ErrorServicio(codigo, limpio));
        }
    }
}
