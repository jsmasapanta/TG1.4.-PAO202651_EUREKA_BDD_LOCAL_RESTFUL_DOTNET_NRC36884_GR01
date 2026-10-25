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

    /// <summary>El servidor manda la acción (INGRESO o SALIDA); aquí solo se elige el color y el signo.</summary>
    public static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";
}
