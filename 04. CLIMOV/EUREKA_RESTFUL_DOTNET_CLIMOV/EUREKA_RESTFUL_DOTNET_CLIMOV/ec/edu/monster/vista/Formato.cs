using System.Globalization;
using ec.edu.monster.modelo;

namespace ec.edu.monster.vista;

/// <summary>Cómo se muestran los datos en pantalla. No tiene reglas del banco.</summary>
public static class Formato
{
    /// <summary>Importe con separador de miles y 2 decimales: 6,900.00</summary>
    public static string Dinero(decimal importe) => importe.ToString("#,##0.00", CultureInfo.InvariantCulture);

    public static string Titulo(TipoOperacion tipo) => tipo switch
    {
        TipoOperacion.Deposito => "Depósito",
        TipoOperacion.Retiro => "Retiro",
        _ => "Transferencia"
    };

    public static Color ColorDe(TipoOperacion tipo) => tipo switch
    {
        TipoOperacion.Deposito => Color.FromArgb("#198754"),
        TipoOperacion.Retiro => Color.FromArgb("#C0392B"),
        _ => Color.FromArgb("#6F42C1")
    };

    /// <summary>El servidor manda la acción (INGRESO o SALIDA); aquí solo se elige el color y el signo.</summary>
    public static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

    /// <summary>
    /// Convierte lo escrito (punto o coma decimal) al decimal que recibe el servicio; null si no es un número.
    /// Si el importe es cero, negativo o tiene más de 2 decimales lo rechaza el servidor.
    /// </summary>
    public static decimal? LeerImporte(string? texto)
    {
        string limpio = (texto ?? "").Trim().Replace(',', '.');
        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe) ? importe : null;
    }
}
