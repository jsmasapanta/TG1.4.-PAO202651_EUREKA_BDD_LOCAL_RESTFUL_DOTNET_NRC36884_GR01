using System;
using System.Runtime.Serialization;

namespace ec.edu.monster.modelo
{
    /// <summary>Datos de una cuenta, tal como los devuelve el API (JSON en camelCase). Solo datos (get/set).</summary>
    [DataContract]
    public class Cuenta
    {
        [DataMember(Name = "codigo")] public string Codigo { get; set; }
        [DataMember(Name = "cliente")] public string Cliente { get; set; }
        [DataMember(Name = "moneda")] public string Moneda { get; set; }
        [DataMember(Name = "sucursal")] public string Sucursal { get; set; }
        [DataMember(Name = "saldo")] public decimal Saldo { get; set; }
        [DataMember(Name = "estado")] public string Estado { get; set; }
        [DataMember(Name = "fechaCreacion")] public DateTime FechaCreacion { get; set; }
        [DataMember(Name = "numeroMovimientos")] public int NumeroMovimientos { get; set; }
    }
}
