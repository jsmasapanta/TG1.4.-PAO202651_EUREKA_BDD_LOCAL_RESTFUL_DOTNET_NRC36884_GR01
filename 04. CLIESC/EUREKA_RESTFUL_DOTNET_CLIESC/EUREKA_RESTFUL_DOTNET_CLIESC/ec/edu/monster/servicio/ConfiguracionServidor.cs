using System;
using System.Configuration;

namespace ec.edu.monster.servicio
{
    /// <summary>
    /// Arma la URL base del API REST sin dejarla fija en el código. Primero mira las variables de entorno
    /// REST_HOST y REST_PORT; si no existen, usa RestHost y RestPuerto de App.config.
    /// Desde la pantalla se puede cambiar el host y el puerto (Guardar): se escriben en el .exe.config
    /// para que la próxima vez arranque con ellos. Si el host o el puerto no son válidos lanza un error claro.
    /// </summary>
    public static class ConfiguracionServidor
    {
        public static string Host => (Valor("REST_HOST", "RestHost") ?? string.Empty).Trim();

        public static string Puerto => (Valor("REST_PORT", "RestPuerto") ?? string.Empty).Trim();

        public static string LeerUrl() => ArmarUrl(Host, Puerto);

        /// <summary>
        /// Valida host y puerto y devuelve http://host:puerto (la base del API; las rutas son api/eurekabank/...).
        /// En "host" se acepta la IP sola (192.168.100.2), con puerto (192.168.100.2:8088) o la URL completa
        /// (http://192.168.100.2:8088/api/eurekabank/): de ahí se toman la IP y el puerto.
        /// </summary>
        public static string ArmarUrl(string host, string puerto)
        {
            host = (host ?? string.Empty).Trim();
            puerto = (puerto ?? string.Empty).Trim();
            Separar(ref host, ref puerto);

            if (host.Length == 0 || Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                throw new InvalidOperationException(
                    "Host del servidor inválido: \"" + host + "\". Escriba la IP de la PC del servidor (ejemplo: 192.168.1.10) "
                    + "en RestHost de App.config o en la variable REST_HOST.");
            }
            if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
            {
                throw new InvalidOperationException(
                    "Puerto del servidor inválido: \"" + puerto + "\". Debe ser un número entre 1 y 65535 (el servidor usa 8088).");
            }
            return new UriBuilder("http", host, numero).Uri.ToString().TrimEnd('/');
        }

        /// <summary>Si escribieron "IP:puerto" o la URL completa en el host, separa la IP y el puerto.</summary>
        private static void Separar(ref string host, ref string puerto)
        {
            if (host.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                if (Uri.TryCreate(host, UriKind.Absolute, out Uri url))
                {
                    host = url.Host;
                    puerto = url.Port.ToString();
                }
                return;
            }
            int dosPuntos = host.LastIndexOf(':');
            if (dosPuntos > 0 && host.IndexOf(':') == dosPuntos) // "192.168.100.2:8088" (no una IPv6)
            {
                puerto = host.Substring(dosPuntos + 1).Split('/')[0];
                host = host.Substring(0, dosPuntos);
            }
            host = host.Split('/')[0];
        }

        /// <summary>Guarda host y puerto en el .exe.config (appSettings RestHost y RestPuerto).</summary>
        public static void Guardar(string host, string puerto)
        {
            Configuration archivo = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Poner(archivo, "RestHost", host.Trim());
            Poner(archivo, "RestPuerto", puerto.Trim());
            archivo.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            // Si había variables de entorno, se quitan en este proceso para que manden los valores nuevos.
            Environment.SetEnvironmentVariable("REST_HOST", null);
            Environment.SetEnvironmentVariable("REST_PORT", null);
        }

        private static void Poner(Configuration archivo, string clave, string valor)
        {
            if (archivo.AppSettings.Settings[clave] == null)
            {
                archivo.AppSettings.Settings.Add(clave, valor);
            }
            else
            {
                archivo.AppSettings.Settings[clave].Value = valor;
            }
        }

        private static string Valor(string variable, string clave)
        {
            string valor = Environment.GetEnvironmentVariable(variable);
            return string.IsNullOrWhiteSpace(valor) ? ConfigurationManager.AppSettings[clave] : valor;
        }
    }
}
