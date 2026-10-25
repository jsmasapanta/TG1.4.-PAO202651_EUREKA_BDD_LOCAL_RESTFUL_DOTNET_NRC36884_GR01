using System;
using System.Configuration;
using System.Data.SqlClient;

namespace ec.edu.monster.db
{
    /// <summary>
    /// Acceso a SQL Server. Patrón Singleton: una sola instancia lee la cadena de conexión
    /// "EurekaBank" de Web.config una vez y todas las peticiones la usan (Lazy la crea de forma segura entre hilos).
    /// Cada operación pide su propia SqlConnection con AbrirConexion(): una conexión compartida
    /// no es segura entre hilos, y ADO.NET ya reutiliza las conexiones físicas (pool).
    /// </summary>
    public sealed class ConexionBD
    {
        private const string NombreCadena = "EurekaBank";

        private static readonly Lazy<ConexionBD> _instancia =
            new Lazy<ConexionBD>(() => new ConexionBD());

        public static ConexionBD Instancia => _instancia.Value;

        private readonly string _cadenaConexion;

        // Constructor privado: nadie fuera de la clase puede hacer "new ConexionBD()".
        private ConexionBD()
        {
            ConnectionStringSettings cadena = ConfigurationManager.ConnectionStrings[NombreCadena];
            if (cadena == null || string.IsNullOrWhiteSpace(cadena.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Falta la cadena de conexión '" + NombreCadena + "' en Web.config (sección connectionStrings).");
            }
            _cadenaConexion = cadena.ConnectionString;

            var datos = new SqlConnectionStringBuilder(_cadenaConexion);
            Servidor = datos.DataSource;
            BaseDatos = datos.InitialCatalog;
        }

        public string Servidor { get; }

        public string BaseDatos { get; }

        /// <summary>Devuelve una conexión ya abierta. Quien la pide la cierra con "using".</summary>
        public SqlConnection AbrirConexion()
        {
            var conexion = new SqlConnection(_cadenaConexion);
            conexion.Open();
            return conexion;
        }
    }
}
