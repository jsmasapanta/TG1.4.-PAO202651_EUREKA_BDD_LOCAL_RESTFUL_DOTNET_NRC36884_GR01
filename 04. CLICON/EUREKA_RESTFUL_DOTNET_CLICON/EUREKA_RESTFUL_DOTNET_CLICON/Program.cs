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
                // Por ejemplo, RestHost o RestPuerto inválidos en App.config.
                vista.MostrarError(ex.Message);
                vista.Pausa();
            }
        }
    }
}
