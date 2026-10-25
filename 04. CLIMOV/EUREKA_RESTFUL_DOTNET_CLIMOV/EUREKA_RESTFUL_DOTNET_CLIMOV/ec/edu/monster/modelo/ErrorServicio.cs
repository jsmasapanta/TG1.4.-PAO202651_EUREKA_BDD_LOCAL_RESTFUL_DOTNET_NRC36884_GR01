namespace ec.edu.monster.modelo;

/// <summary>Cuerpo de las respuestas de error del API (400, 401, 409, 503...). Solo datos (get/set).</summary>
public class ErrorServicio
{
    public string? Codigo { get; set; }
    public string? Mensaje { get; set; }
}
