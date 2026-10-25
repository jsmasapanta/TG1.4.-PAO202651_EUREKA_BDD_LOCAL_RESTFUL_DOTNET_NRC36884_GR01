namespace ec.edu.monster.modelo;

/// <summary>Un movimiento de la cuenta, tal como lo devuelve el servicio. Solo datos (get/set).</summary>
public class Movimiento
{
    public int Numero { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    /// <summary>INGRESO o SALIDA (lo decide el servidor según TipoMovimiento).</summary>
    public string Accion { get; set; } = "";
    public decimal Importe { get; set; }
    public string Empleado { get; set; } = "";
    public string CuentaReferencia { get; set; } = "";
}
