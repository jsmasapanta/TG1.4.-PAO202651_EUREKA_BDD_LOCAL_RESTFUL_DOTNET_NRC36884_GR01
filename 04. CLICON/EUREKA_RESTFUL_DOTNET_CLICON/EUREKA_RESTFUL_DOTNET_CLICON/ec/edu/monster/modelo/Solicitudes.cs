using System.Runtime.Serialization;

namespace ec.edu.monster.modelo
{
    // Cuerpos JSON que el cliente envía al API y respuesta de las operaciones. Solo datos (get/set).

    /// <summary>POST api/eurekabank/login</summary>
    [DataContract]
    public class SolicitudLogin
    {
        [DataMember(Name = "usuario")] public string Usuario { get; set; }
        [DataMember(Name = "clave")] public string Clave { get; set; }
    }

    /// <summary>POST api/eurekabank/deposito y api/eurekabank/retiro</summary>
    [DataContract]
    public class SolicitudMovimiento
    {
        [DataMember(Name = "cuenta")] public string Cuenta { get; set; }
        [DataMember(Name = "importe")] public decimal Importe { get; set; }
        [DataMember(Name = "codEmpleado")] public string CodEmpleado { get; set; }
    }

    /// <summary>POST api/eurekabank/transferencia</summary>
    [DataContract]
    public class SolicitudTransferencia
    {
        [DataMember(Name = "cuentaOrigen")] public string CuentaOrigen { get; set; }
        [DataMember(Name = "cuentaDestino")] public string CuentaDestino { get; set; }
        [DataMember(Name = "importe")] public decimal Importe { get; set; }
        [DataMember(Name = "codEmpleado")] public string CodEmpleado { get; set; }
    }

    /// <summary>Respuesta de depósito, retiro y transferencia: la cuenta y su nuevo saldo.</summary>
    [DataContract]
    public class ResultadoOperacion
    {
        [DataMember(Name = "cuenta")] public string Cuenta { get; set; }
        [DataMember(Name = "saldo")] public decimal Saldo { get; set; }
    }
}
