using System.Runtime.Serialization;

namespace ec.edu.monster.modelo
{
    /// <summary>Cuerpo de las respuestas de error del API (400, 401, 409, 503...). Solo datos (get/set).</summary>
    [DataContract]
    public class ErrorServicio
    {
        [DataMember(Name = "codigo")] public string Codigo { get; set; }
        [DataMember(Name = "mensaje")] public string Mensaje { get; set; }
    }
}
