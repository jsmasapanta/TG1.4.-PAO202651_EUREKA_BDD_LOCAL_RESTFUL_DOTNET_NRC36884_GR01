namespace ec.edu.monster.modelo;

/// <summary>Datos de una cuenta, tal como los devuelve el servicio. Solo datos (get/set).</summary>
public class Cuenta
{
    public string Codigo { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string Moneda { get; set; } = "";
    public string Sucursal { get; set; } = "";
    public decimal Saldo { get; set; }
    public string Estado { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public int NumeroMovimientos { get; set; }
}
