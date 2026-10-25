using System;
using ec.edu.monster.vista;

namespace ec.edu.monster
{
    internal static class Program
    {
        private static void Main()
        {
            var vista = new VistaConsola();
            try
            {
                vista.Iniciar();
            }
            catch (Exception ex)
            {
                // Por ejemplo, App.config sin el endpoint "WSEureka".
                vista.MostrarError("Error inesperado: " + ex.Message);
                vista.Pausa();
            }
        }
    }
}
