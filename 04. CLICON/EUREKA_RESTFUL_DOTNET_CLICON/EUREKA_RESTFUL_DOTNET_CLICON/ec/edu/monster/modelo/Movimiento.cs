using System;
using System.Runtime.Serialization;

namespace ec.edu.monster.modelo
{
    /// <summary>Un movimiento de la cuenta, tal como lo devuelve el API. Solo datos (get/set).</summary>
    [DataContract]
    public class Movimiento
    {
        [DataMember(Name = "numero")] public int Numero { get; set; }
        [DataMember(Name = "fecha")] public DateTime Fecha { get; set; }
        [DataMember(Name = "tipo")] public string Tipo { get; set; }
        /// <summary>INGRESO o SALIDA (lo decide el servidor según TipoMovimiento).</summary>
        [DataMember(Name = "accion")] public string Accion { get; set; }
        [DataMember(Name = "importe")] public decimal Importe { get; set; }
        [DataMember(Name = "empleado")] public string Empleado { get; set; }
        [DataMember(Name = "cuentaReferencia")] public string CuentaReferencia { get; set; }
    }
}
