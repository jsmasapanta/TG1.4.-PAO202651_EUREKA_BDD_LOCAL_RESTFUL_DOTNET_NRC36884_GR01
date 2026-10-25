namespace ec.edu.monster.modelo;

// Cuerpos JSON que el cliente envía al API y respuesta de las operaciones. Solo datos (get/set).

/// <summary>POST api/eurekabank/login</summary>
public class SolicitudLogin
{
    public string Usuario { get; set; } = "";
    public string Clave { get; set; } = "";
}

/// <summary>POST api/eurekabank/deposito y api/eurekabank/retiro</summary>
public class SolicitudMovimiento
{
    public string Cuenta { get; set; } = "";
    public decimal Importe { get; set; }
    public string CodEmpleado { get; set; } = "";
}

/// <summary>POST api/eurekabank/transferencia</summary>
public class SolicitudTransferencia
{
    public string CuentaOrigen { get; set; } = "";
    public string CuentaDestino { get; set; } = "";
    public decimal Importe { get; set; }
    public string CodEmpleado { get; set; } = "";
}

/// <summary>Respuesta de depósito, retiro y transferencia: la cuenta y su nuevo saldo.</summary>
public class ResultadoOperacion
{
    public string Cuenta { get; set; } = "";
    public decimal Saldo { get; set; }
}
