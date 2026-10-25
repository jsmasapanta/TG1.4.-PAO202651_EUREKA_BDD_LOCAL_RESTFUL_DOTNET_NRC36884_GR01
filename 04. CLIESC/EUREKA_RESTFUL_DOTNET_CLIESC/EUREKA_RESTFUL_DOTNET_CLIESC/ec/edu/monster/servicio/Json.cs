using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ec.edu.monster.servicio
{
    /// <summary>Convierte objetos a JSON y viceversa con el serializador que trae .NET Framework.</summary>
    internal static class Json
    {
        // El servidor envía las fechas como "2026-10-25T14:30:00".
        private static readonly DataContractJsonSerializerSettings Opciones = new DataContractJsonSerializerSettings
        {
            DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH:mm:ss")
        };

        public static string Serializar(object objeto)
        {
            using (var memoria = new MemoryStream())
            {
                new DataContractJsonSerializer(objeto.GetType(), Opciones).WriteObject(memoria, objeto);
                return Encoding.UTF8.GetString(memoria.ToArray());
            }
        }

        public static T Deserializar<T>(string json)
        {
            using (var memoria = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)new DataContractJsonSerializer(typeof(T), Opciones).ReadObject(memoria);
            }
        }
    }
}
