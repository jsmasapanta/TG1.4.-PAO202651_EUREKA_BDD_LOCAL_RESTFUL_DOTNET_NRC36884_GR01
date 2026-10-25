using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ec.edu.monster.controlador;
using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.vista
{
    /// <summary>
    /// Pantallas de la consola: pide los datos, se los pasa al controlador y muestra lo que devuelve
    /// el servidor (datos o su mensaje de error). No tiene reglas del banco ni cuenta intentos de login.
    /// </summary>
    public class VistaConsola
    {
        private const int Ancho = 76;

        private readonly ControladorConsola _controlador = new ControladorConsola();

        public VistaConsola()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.Title = "EurekaBank - REST .NET";
        }

        // ================= Flujo de pantallas =================

        public void Iniciar()
        {
            if (!PantallaLogin())
            {
                return;
            }
            PantallaMenu();
            MostrarEncabezado("Sesión finalizada. ¡Hasta pronto!");
        }

        /// <summary>El servidor decide si el login es correcto, cuenta los intentos y bloquea al usuario.</summary>
        private bool PantallaLogin()
        {
            while (true)
            {
                MostrarEncabezado("INICIO DE SESIÓN");
                MostrarServidor(_controlador.UrlServidor);
                string usuario = PedirTexto("Usuario");
                string clave = PedirClave("Clave");
                try
                {
                    Empleado empleado = _controlador.IniciarSesion(usuario, clave);
                    MostrarExito($"Bienvenido, {empleado.Nombre} (empleado {empleado.Codigo}).");
                    Pausa();
                    return true;
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                }
                if (!PreguntarSiNo("¿Intentar de nuevo?"))
                {
                    return false;
                }
            }
        }

        private void PantallaMenu()
        {
            var opciones = new List<string> { "Consultar cuenta y movimientos", "Depósito", "Retiro", "Transferencia" };
            while (true)
            {
                MostrarEncabezado($"MENÚ PRINCIPAL · {_controlador.Empleado.Nombre}");
                switch (PedirOpcion(opciones, "Cerrar sesión"))
                {
                    case 0: _controlador.CerrarSesion(); return;
                    case 1: PantallaMovimientos(); break;
                    case 2: PantallaOperacion(TipoOperacion.Deposito); break;
                    case 3: PantallaOperacion(TipoOperacion.Retiro); break;
                    case 4: PantallaOperacion(TipoOperacion.Transferencia); break;
                }
            }
        }

        private void PantallaMovimientos()
        {
            do
            {
                MostrarEncabezado("CONSULTAR CUENTA Y MOVIMIENTOS");
                string cuenta = PedirCuenta("Número de cuenta");
                try
                {
                    MostrarCuenta(_controlador.ConsultarCuenta(cuenta));
                    MostrarMovimientos(_controlador.TraerMovimientos(cuenta));
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                }
            }
            while (PreguntarSiNo("¿Consultar otra cuenta?"));
        }

        private void PantallaOperacion(TipoOperacion tipo)
        {
            do
            {
                MostrarEncabezado(Titulo(tipo).ToUpperInvariant());
                string cuenta = PedirCuenta(tipo == TipoOperacion.Transferencia ? "Cuenta de origen" : "Número de cuenta");
                string destino = tipo == TipoOperacion.Transferencia ? PedirCuenta("Cuenta de destino") : null;
                decimal importe = PedirImporte("Importe");

                if (!PreguntarSiNo($"¿Confirma {Titulo(tipo).ToLowerInvariant()} de {Dinero(importe)}?"))
                {
                    MostrarError("Operación cancelada.");
                    continue;
                }
                try
                {
                    decimal saldo = tipo == TipoOperacion.Deposito ? _controlador.RegistrarDeposito(cuenta, importe)
                                  : tipo == TipoOperacion.Retiro ? _controlador.RegistrarRetiro(cuenta, importe)
                                  : _controlador.RegistrarTransferencia(cuenta, destino, importe);
                    MostrarOperacionExitosa(tipo, cuenta, destino, importe, saldo);
                }
                catch (ErrorServicioException ex)
                {
                    MostrarError(ex.Message);
                }
            }
            while (PreguntarSiNo("¿Realizar otra operación de este tipo?"));
        }

        // ================= Entrada y salida =================

        public void MostrarEncabezado(string subtitulo)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', Ancho));
            Console.WriteLine(Centrar("EUREKABANK · RESTFUL .NET + SQL SERVER"));
            Console.WriteLine(Centrar("ESPE · Arquitectura de Software"));
            Console.WriteLine(new string('═', Ancho));
            Console.ResetColor();
            if (!string.IsNullOrEmpty(subtitulo))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  " + subtitulo);
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        public void MostrarServidor(string url)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Servidor: " + url);
            Console.ResetColor();
            Console.WriteLine();
        }

        public string PedirTexto(string etiqueta)
        {
            Console.Write("  " + etiqueta + ": ");
            return Console.ReadLine() ?? string.Empty;
        }

        /// <summary>Lee la clave mostrando asteriscos.</summary>
        public string PedirClave(string etiqueta)
        {
            Console.Write("  " + etiqueta + ": ");
            if (Console.IsInputRedirected)
            {
                // Entrada desde archivo o pruebas automáticas: no hay teclado para leer tecla por tecla.
                string linea = Console.ReadLine() ?? string.Empty;
                Console.WriteLine();
                return linea;
            }
            var clave = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo tecla = Console.ReadKey(true);
                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return clave.ToString();
                }
                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (clave.Length > 0)
                    {
                        clave.Length--;
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(tecla.KeyChar))
                {
                    clave.Append(tecla.KeyChar);
                    Console.Write('*');
                }
            }
        }

        /// <summary>Muestra opciones numeradas y devuelve la elegida (0 = volver/salir).</summary>
        public int PedirOpcion(IList<string> opciones, string textoCero)
        {
            for (int i = 0; i < opciones.Count; i++)
            {
                Console.WriteLine($"   [{i + 1}] {opciones[i]}");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"   [0] {textoCero}");
            Console.ResetColor();
            Console.WriteLine();

            while (true)
            {
                string entrada = PedirTexto("Opción");
                if (int.TryParse(entrada, out int opcion) && opcion >= 0 && opcion <= opciones.Count)
                {
                    return opcion;
                }
                MostrarError($"Opción inválida. Escriba un número entre 0 y {opciones.Count}.");
            }
        }

        /// <summary>Pide un número de cuenta. Si no tiene el formato correcto lo dirá el servidor.</summary>
        public string PedirCuenta(string etiqueta)
        {
            while (true)
            {
                string cuenta = PedirTexto(etiqueta).Trim();
                if (cuenta.Length > 0)
                {
                    return cuenta;
                }
                MostrarError("Escriba el número de cuenta.");
            }
        }

        /// <summary>
        /// Pide el importe (punto o coma decimal). Solo se comprueba que sea un número, porque el servicio
        /// recibe un decimal; si es cero, negativo o tiene más de 2 decimales lo rechaza el servidor.
        /// </summary>
        public decimal PedirImporte(string etiqueta)
        {
            while (true)
            {
                string entrada = PedirTexto(etiqueta).Trim().Replace(',', '.');
                if (decimal.TryParse(entrada, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal importe))
                {
                    return importe;
                }
                MostrarError("Escriba el importe con números (ejemplo: 150 o 75.50).");
            }
        }

        public void MostrarCuenta(Cuenta cuenta)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ┌" + new string('─', Ancho - 4) + "┐");
            Console.WriteLine("  │" + Ajustar($" Cuenta   : {cuenta.Codigo}  ({cuenta.Estado})", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" Cliente  : {cuenta.Cliente}", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" Sucursal : {cuenta.Sucursal}   ·   Moneda: {cuenta.Moneda}", Ancho - 4) + "│");
            Console.WriteLine("  │" + Ajustar($" SALDO    : {Dinero(cuenta.Saldo)}", Ancho - 4) + "│");
            Console.WriteLine("  └" + new string('─', Ancho - 4) + "┘");
            Console.ResetColor();
        }

        public void MostrarMovimientos(IList<Movimiento> movimientos)
        {
            Console.WriteLine();
            Console.WriteLine($"  Movimientos ({movimientos.Count}), el más reciente primero:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  {0,4}  {1,-16}  {2,-20}  {3,14}  {4,-8}  {5,-8}", "Nro", "Fecha", "Tipo", "Importe", "Empleado", "Ref.");
            Console.WriteLine("  " + new string('─', Ancho - 4));
            Console.ResetColor();
            foreach (Movimiento m in movimientos)
            {
                Console.Write("  {0,4}  {1,-16}  {2,-20}  ", m.Numero, m.Fecha.ToString("dd/MM/yyyy HH:mm"), Recortar(m.Tipo, 20));
                Console.ForegroundColor = EsIngreso(m) ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write("{0,14}", (EsIngreso(m) ? "+" : "-") + Dinero(m.Importe));
                Console.ResetColor();
                Console.WriteLine("  {0,-8}  {1,-8}", m.Empleado, m.CuentaReferencia);
            }
            if (movimientos.Count == 0)
            {
                Console.WriteLine("  (la cuenta no tiene movimientos)");
            }
        }

        /// <summary>La acción (INGRESO o SALIDA) viene del servidor, de la tabla TipoMovimiento.</summary>
        private static bool EsIngreso(Movimiento m) => m.Accion == "INGRESO";

        public void MostrarOperacionExitosa(TipoOperacion tipo, string cuenta, string destino, decimal importe, decimal saldo)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ┌" + new string('─', Ancho - 4) + "┐");
            string registrado = tipo == TipoOperacion.Transferencia ? "registrada" : "registrado";
            Console.WriteLine("  │" + Centrar($"{Titulo(tipo)} {registrado} correctamente", Ancho - 4) + "│");
            string detalle = tipo == TipoOperacion.Transferencia
                ? $"{Dinero(importe)} de {cuenta} a {destino}"
                : $"{Dinero(importe)} en la cuenta {cuenta}";
            Console.WriteLine("  │" + Centrar(detalle, Ancho - 4) + "│");
            Console.WriteLine("  │" + Centrar($"Nuevo saldo de {cuenta}: {Dinero(saldo)}", Ancho - 4) + "│");
            Console.WriteLine("  └" + new string('─', Ancho - 4) + "┘");
            Console.ResetColor();
        }

        public void MostrarExito(string mensaje)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✔ " + mensaje);
            Console.ResetColor();
        }

        public void MostrarError(string mensaje)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✖ " + mensaje);
            Console.ResetColor();
        }

        public bool PreguntarSiNo(string pregunta)
        {
            Console.WriteLine();
            string r = PedirTexto(pregunta + " (s/n)").Trim().ToLowerInvariant();
            return r == "s" || r == "si" || r == "sí";
        }

        public void Pausa()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Presione una tecla para continuar...");
            Console.ResetColor();
            if (Console.IsInputRedirected)
            {
                Console.ReadLine();
                Console.WriteLine();
                return;
            }
            Console.ReadKey(true);
        }

        public static string Titulo(TipoOperacion tipo)
        {
            switch (tipo)
            {
                case TipoOperacion.Deposito: return "Depósito";
                case TipoOperacion.Retiro: return "Retiro";
                default: return "Transferencia";
            }
        }

        /// <summary>Importe con separador de miles y 2 decimales: 6,900.00</summary>
        public static string Dinero(decimal importe)
        {
            return importe.ToString("#,##0.00", CultureInfo.InvariantCulture);
        }

        private static string Recortar(string texto, int largo)
        {
            texto = texto ?? string.Empty;
            return texto.Length <= largo ? texto : texto.Substring(0, largo);
        }

        private static string Ajustar(string texto, int ancho)
        {
            return texto.Length >= ancho ? texto.Substring(0, ancho) : texto.PadRight(ancho);
        }

        private static string Centrar(string texto, int ancho = Ancho)
        {
            if (texto.Length >= ancho) return texto;
            int izquierda = (ancho - texto.Length) / 2;
            return new string(' ', izquierda) + texto + new string(' ', ancho - texto.Length - izquierda);
        }
    }
}
