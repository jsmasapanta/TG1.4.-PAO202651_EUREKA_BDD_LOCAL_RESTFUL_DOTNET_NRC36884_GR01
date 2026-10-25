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
    /// API REST de EurekaBank (JSON). Solo recibe la petición, delega en EurekaServicio y
    /// convierte los errores en un código HTTP con ErrorServicio { codigo, mensaje }:
    /// 400 VALOR_INVALIDO, 401 login rechazado, 409 REGLA_NEGOCIO, 503 ERROR_BD, 500 ERROR_SERVIDOR.
    /// </summary>
    [RoutePrefix("api/eurekabank")]
    public class EurekabankController : ApiController
    {
        private static EurekaServicio Servicio => EurekaServicio.Instancia;

        /// <summary>POST api/eurekabank/login  { "usuario": "monster", "clave": "monster69" }  →  Empleado</summary>
        [HttpPost, Route("login")]
        public IHttpActionResult Login([FromBody] SolicitudLogin solicitud) =>
            Ejecutar(() => Servicio.Login(solicitud?.Usuario, solicitud?.Clave), HttpStatusCode.Unauthorized);

        /// <summary>GET api/eurekabank/cuenta?numero=00100001  →  Cuenta</summary>
        [HttpGet, Route("cuenta")]
        public IHttpActionResult ConsultarCuenta(string numero = null) =>
            Ejecutar(() => Servicio.ConsultarCuenta(numero));

        /// <summary>GET api/eurekabank/movimientos?cuenta=00100001  →  lista de Movimiento (el más reciente primero)</summary>
        [HttpGet, Route("movimientos")]
        public IHttpActionResult TraerMovimientos(string cuenta = null) =>
            Ejecutar<List<Movimiento>>(() => Servicio.TraerMovimientos(cuenta));

        /// <summary>POST api/eurekabank/deposito  { "cuenta", "importe", "codEmpleado" }  →  { cuenta, saldo }</summary>
        [HttpPost, Route("deposito")]
        public IHttpActionResult RegistrarDeposito([FromBody] SolicitudMovimiento s) =>
            Ejecutar(() => Resultado(Requerido(s).Cuenta, Servicio.RegistrarDeposito(s.Cuenta, s.Importe, s.CodEmpleado)));

        /// <summary>POST api/eurekabank/retiro  { "cuenta", "importe", "codEmpleado" }  →  { cuenta, saldo }</summary>
        [HttpPost, Route("retiro")]
        public IHttpActionResult RegistrarRetiro([FromBody] SolicitudMovimiento s) =>
            Ejecutar(() => Resultado(Requerido(s).Cuenta, Servicio.RegistrarRetiro(s.Cuenta, s.Importe, s.CodEmpleado)));

        /// <summary>POST api/eurekabank/transferencia  { "cuentaOrigen", "cuentaDestino", "importe", "codEmpleado" }  →  saldo del origen</summary>
        [HttpPost, Route("transferencia")]
        public IHttpActionResult RegistrarTransferencia([FromBody] SolicitudTransferencia s) =>
            Ejecutar(() => Resultado(Requerido(s).CuentaOrigen,
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

        /// <param name="codigoRegla">Código HTTP para ReglaNegocioException: 401 en el login, 409 en lo demás.</param>
        private IHttpActionResult Ejecutar<T>(Func<T> operacion, HttpStatusCode codigoRegla = HttpStatusCode.Conflict)
        {
            try
            {
                return Ok(operacion());
            }
            catch (ArgumentException ex)
            {
                return Error(HttpStatusCode.BadRequest, "VALOR_INVALIDO", ex.Message);
            }
            catch (ReglaNegocioException ex)
            {
                return Error(codigoRegla, "REGLA_NEGOCIO", ex.Message);
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
        }

        private IHttpActionResult Error(HttpStatusCode codigoHttp, string codigo, string mensaje)
        {
            // ArgumentException agrega "Nombre del parámetro: ..." al mensaje; nos quedamos con la primera línea.
            string limpio = mensaje.Split(new[] { Environment.NewLine }, StringSplitOptions.None)[0];
            return Content(codigoHttp, new ErrorServicio(codigo, limpio));
        }
    }
}
