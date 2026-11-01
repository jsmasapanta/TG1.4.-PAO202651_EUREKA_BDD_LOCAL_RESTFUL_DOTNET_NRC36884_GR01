using System.Runtime.Serialization;

namespace ec.edu.monster.modelo
{
    /// <summary>Empleado que inició sesión, tal como lo devuelve el API. Solo datos (get/set).</summary>
    [DataContract]
    public class Empleado
    {
        [DataMember(Name = "codigo")] public string Codigo { get; set; }
        [DataMember(Name = "usuario")] public string Usuario { get; set; }
        [DataMember(Name = "nombre")] public string Nombre { get; set; }
        /// <summary>Token de la sesión que entrega el login; va en el header Authorization: Bearer de las demás operaciones.</summary>
        [DataMember(Name = "token")] public string Token { get; set; }
    }
}
