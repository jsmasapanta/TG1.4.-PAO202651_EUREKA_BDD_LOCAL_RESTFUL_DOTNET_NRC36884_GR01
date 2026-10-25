using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ec.edu.monster.controlador;

/// <summary>
/// Si no hay empleado en la sesión, redirige al login.
/// Así nadie entra a /Banco escribiendo la URL directamente.
/// </summary>
public class RequiereSesionAttribute : ActionFilterAttribute
{
    public const string ClaveEmpleado = "Empleado";
    public const string ClaveNombre = "NombreEmpleado";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(context.HttpContext.Session.GetString(ClaveEmpleado)))
        {
            context.Result = new RedirectToActionResult("Login", "Acceso", null);
        }
    }
}
