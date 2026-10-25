using ec.edu.monster.modelo;
using ec.edu.monster.servicio;

namespace ec.edu.monster.controlador
{
    /// <summary>
    /// Intermediario entre la vista y el servicio: solo pasa las llamadas.
    /// Guarda el empleado que inició sesión porque su código va en cada operación.
    /// </summary>
    public class ControladorConsola
    {
        private readonly ClienteEureka _servicio = ClienteEureka.Instancia;

        public string UrlServidor => _servicio.UrlServidor;

        public Empleado Empleado { get; private set; }

        public Empleado IniciarSesion(string usuario, string clave)
        {
            Empleado = _servicio.Login(usuario, clave);
            return Empleado;
        }

        public void CerrarSesion() => Empleado = null;

        public Cuenta ConsultarCuenta(string cuenta) => _servicio.ConsultarCuenta(cuenta);

        public Movimiento[] TraerMovimientos(string cuenta) => _servicio.TraerMovimientos(cuenta);

        public decimal RegistrarDeposito(string cuenta, decimal importe) =>
            _servicio.RegistrarDeposito(cuenta, importe, Empleado.Codigo);

        public decimal RegistrarRetiro(string cuenta, decimal importe) =>
            _servicio.RegistrarRetiro(cuenta, importe, Empleado.Codigo);

        public decimal RegistrarTransferencia(string origen, string destino, decimal importe) =>
            _servicio.RegistrarTransferencia(origen, destino, importe, Empleado.Codigo);
    }
}
