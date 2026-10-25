using ec.edu.monster.servicio;
using Microsoft.AspNetCore.Mvc.Razor;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Las vistas viven en ec/edu/monster/vista (no en la carpeta Views por defecto):
// {1} = nombre del controlador, {0} = nombre de la vista.
builder.Services.Configure<RazorViewEngineOptions>(opciones =>
{
    opciones.ViewLocationFormats.Clear();
    opciones.ViewLocationFormats.Add("/ec/edu/monster/vista/{1}/{0}.cshtml");
    opciones.ViewLocationFormats.Add("/ec/edu/monster/vista/Shared/{0}.cshtml");
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});
// Patrón Singleton: el contenedor crea un solo ClienteEureka y lo inyecta en todos los controladores.
builder.Services.AddSingleton<ClienteEureka>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Acceso/Login");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Index}/{id?}");

app.Run();
