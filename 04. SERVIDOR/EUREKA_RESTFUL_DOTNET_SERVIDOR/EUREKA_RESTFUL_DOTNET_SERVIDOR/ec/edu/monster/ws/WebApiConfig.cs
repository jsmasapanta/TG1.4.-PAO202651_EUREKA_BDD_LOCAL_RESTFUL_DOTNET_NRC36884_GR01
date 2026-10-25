using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ec.edu.monster.ws
{
    /// <summary>Configuración de Web API: rutas por atributo ([Route]) y respuestas siempre en JSON.</summary>
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            // Solo JSON (sin XML), con nombres en camelCase y fechas "2026-10-25T14:30:00".
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            JsonSerializerSettings json = config.Formatters.JsonFormatter.SerializerSettings;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.Converters.Add(new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss" });

            // Los errores no controlados no muestran la pila de llamadas al cliente.
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Never;
        }
    }
}
