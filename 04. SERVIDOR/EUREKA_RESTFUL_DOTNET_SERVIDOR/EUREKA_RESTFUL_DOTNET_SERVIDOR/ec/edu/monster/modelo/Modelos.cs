using System;

namespace ec.edu.monster.modelo
{
    public static class Constantes
    {
        /// <summary>Códigos de TipoMovimiento usados por el servicio.</summary>
        public const string TipoDeposito = "003";
        public const string TipoRetiro = "004";
        public const string TipoTransferenciaIngreso = "008";
        public const string TipoTransferenciaSalida = "009";
    }

    // Objetos que viajan en JSON. Web API los escribe en camelCase (codigo, cliente, saldo...): ver WebApiConfig.

    /// <summary>Empleado que inició sesión (tabla Empleado). Su código queda en cada movimiento.</summary>
    public class Empleado
    {
        public string Codigo { get; set; }
        public string Usuario { get; set; }
        public string Nombre { get; set; }
    }

    /// <summary>Datos de una cuenta con su cliente, moneda y sucursal (tablas Cuenta, Cliente, Moneda, Sucursal).</summary>
    public class Cuenta
    {
        public string Codigo { get; set; }
        public string Cliente { get; set; }
        public string Moneda { get; set; }
        public string Sucursal { get; set; }
        public decimal Saldo { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int NumeroMovimientos { get; set; }
    }

    /// <summary>Un movimiento de la cuenta (tabla Movimiento + TipoMovimiento).</summary>
    public class Movimiento
    {
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; }
        /// <summary>INGRESO o SALIDA.</summary>
        public string Accion { get; set; }
        public decimal Importe { get; set; }
        public string Empleado { get; set; }
        /// <summary>En transferencias, la otra cuenta; vacío en los demás.</summary>
        public string CuentaReferencia { get; set; }
    }

    // ---------- Cuerpos de las peticiones (POST) ----------

    /// <summary>POST api/eurekabank/login</summary>
    public class SolicitudLogin
    {
        public string Usuario { get; set; }
        public string Clave { get; set; }
    }

    /// <summary>POST api/eurekabank/deposito y api/eurekabank/retiro</summary>
    public class SolicitudMovimiento
    {
        public string Cuenta { get; set; }
        public decimal Importe { get; set; }
        public string CodEmpleado { get; set; }
    }

    /// <summary>POST api/eurekabank/transferencia</summary>
    public class SolicitudTransferencia
    {
        public string CuentaOrigen { get; set; }
        public string CuentaDestino { get; set; }
        public decimal Importe { get; set; }
        public string CodEmpleado { get; set; }
    }

    /// <summary>Respuesta de depósito, retiro y transferencia: la cuenta afectada y su nuevo saldo.</summary>
    public class ResultadoOperacion
    {
        public string Cuenta { get; set; }
        public decimal Saldo { get; set; }
    }

    /// <summary>
    /// Cuerpo de las respuestas de error (400, 401, 409, 500, 503).
    /// Los clientes muestran "mensaje" al usuario.
    /// </summary>
    public class ErrorServicio
    {
        public string Codigo { get; set; }
        public string Mensaje { get; set; }

        public ErrorServicio(string codigo, string mensaje)
        {
            Codigo = codigo;
            Mensaje = mensaje;
        }
    }
}
