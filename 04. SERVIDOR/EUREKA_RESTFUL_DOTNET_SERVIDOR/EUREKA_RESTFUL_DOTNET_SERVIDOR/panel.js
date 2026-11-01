// Panel del servidor (index.html). Primero pide login (POST api/eurekabank/login con un empleado de la BD);
// después muestra las pestañas Servicio y Monitoreo. Habla con el API REST como cualquier cliente:
// JSON, header X-Cliente: PANEL y Authorization: Bearer <token>. GET estado no cuenta como petición.
const API = new URL("api/eurekabank/", location.href).href;
const OPERACIONES = {
  Deposito: ["Depósitos", "var(--verde)"], Retiro: ["Retiros", "var(--rojo)"], Transferencia: ["Transferencias", "var(--azul)"],
  Consulta: ["Consultas", "var(--cian)"], Movimientos: ["Movimientos", "var(--morado)"]
};
const COLOR_CLIENTE = { WEB: "var(--cian)", MOVIL: "var(--verde)", ESCRITORIO: "var(--morado)", CONSOLA: "var(--amarillo)", PANEL: "var(--azul)", OTRO: "#94a3b8" };
const CLAVE_SESION = "panel_eureka_rest";
let vistos = new Set(), sesion = null, reloj = null;

const $ = id => document.getElementById(id);
const esc = t => String(t ?? "").replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&apos;" }[c]));
const dinero = n => Number(n).toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Llama al API REST y devuelve el JSON; si el código no es 2xx lanza el "mensaje" de ErrorServicio. */
async function api(metodo, ruta, cuerpo = null, token = null) {
  const headers = { "X-Cliente": "PANEL" };
  if (cuerpo) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = "Bearer " + token;
  const r = await fetch(API + ruta, { method: metodo, cache: "no-store", headers, body: cuerpo ? JSON.stringify(cuerpo) : undefined });
  let json = null;
  try { json = await r.json(); } catch { }
  if (!r.ok) throw new Error(json && json.mensaje ? json.mensaje : `Respuesta inesperada (HTTP ${r.status})`);
  return json;
}

// ===================== Login =====================

function pintarIntentos(mensaje) {
  const m = /Intentos restantes:\s*(\d+)/.exec(mensaje || "");
  const restantes = m ? +m[1] : /bloquead/i.test(mensaje || "") ? 0 : 3;
  document.querySelectorAll(".intentos .carita").forEach((c, i) => c.className = "carita " + (i < restantes ? "viva" : "apagada"));
}

async function comprobarServidor(id) {
  const el = $(id);
  try {
    await api("GET", "estado");
    el.classList.remove("caido"); el.querySelector(".txt, #txtEstado").textContent = "Servicio activo";
    return true;
  } catch {
    el.classList.add("caido"); el.querySelector(".txt, #txtEstado").textContent = "Sin respuesta";
    return false;
  }
}

$("fLogin").onsubmit = async ev => {
  ev.preventDefault();
  $("btnLogin").disabled = true; $("errLogin").hidden = true;
  try {
    const r = await api("POST", "login", { usuario: $("usr").value, clave: $("clv").value });
    sesion = { token: r.token, codigo: r.codigo, nombre: r.nombre };
    try { sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion)); } catch { }
    $("clv").value = "";
    pintarIntentos("");
    mostrarPanel();
  } catch (err) {
    $("errLogin").textContent = "✖ " + err.message; $("errLogin").hidden = false;
    pintarIntentos(err.message);
  } finally { $("btnLogin").disabled = false; }
};

$("btnSalir").onclick = async () => {
  if (sesion) { try { await api("POST", "logout", null, sesion.token); } catch { } }
  salir("Sesión cerrada: el token ya no sirve.");
};

/** Vuelve al login (al cerrar sesión o si el servidor dice que el token venció). */
function salir(aviso) {
  sesion = null;
  try { sessionStorage.removeItem(CLAVE_SESION); } catch { }
  clearInterval(reloj);
  $("vistaApp").hidden = true; $("vistaLogin").hidden = false;
  if (aviso) { $("errLogin").textContent = aviso; $("errLogin").hidden = false; }
  comprobarServidor("estadoLogin");
}

// ===================== Panel =====================

function mostrarPanel() {
  $("vistaLogin").hidden = true; $("vistaApp").hidden = false;
  $("lblUsuario").textContent = `${sesion.nombre} (${sesion.codigo})`;
  $("base").textContent = API;
  const enlace = (id, url) => { $(id).href = url; $(id).textContent = url; };
  enlace("lnkApi", API);
  enlace("lnkEstado", API + "estado");
  enlace("lnkSinToken", API + "cuenta?numero=00100001");
  enlace("lnkWeb", `${location.protocol}//${location.hostname}:5088`);
  refrescar(); clearInterval(reloj); reloj = setInterval(refrescar, 3000);
}

document.querySelectorAll(".pestana").forEach(b => b.onclick = () => {
  document.querySelectorAll(".pestana").forEach(x => x.classList.toggle("activa", x === b));
  document.querySelectorAll(".contenido-pestana").forEach(p => p.hidden = p.id !== b.dataset.pestana);
  try { sessionStorage.setItem("panel_pestana", b.dataset.pestana); } catch { }
});

function contar(id, fin) {
  const el = $(id), ini = parseInt(el.textContent) || 0;
  if (ini === fin) return;
  const t0 = performance.now();
  (function paso(t) { const k = Math.min((t - t0) / 600, 1); el.textContent = Math.round(ini + (fin - ini) * k); if (k < 1) requestAnimationFrame(paso); })(t0);
}
function duracion(s) { const h = Math.floor(s / 3600), m = Math.floor(s % 3600 / 60); return h ? `${h} h ${m} min` : m ? `${m} min ${s % 60} s` : `${s} s`; }

function barras(id, datos, color) {
  const max = Math.max(1, ...datos.map(d => d[1]));
  $(id).innerHTML = datos.map(([k, v]) =>
    `<div class="barra"><div class="fila"><span>${esc(color(k)[0])}</span><b>${v}</b></div><div class="pista"><div class="relleno" data-w="${v / max * 100}" style="background:${color(k)[1]}"></div></div></div>`).join("");
  requestAnimationFrame(() => document.querySelectorAll(`#${id} .relleno`).forEach(b => b.style.width = b.dataset.w + "%"));
}

async function refrescar() {
  try {
    const e = await api("GET", "estado");
    $("estado").classList.remove("caido");
    $("txtEstado").textContent = "Servicio activo";
    $("uptime").textContent = duracion(e.segundosActivo);
    $("inicio").textContent = "Inicio: " + String(e.inicio).substring(0, 19).replace("T", " ");
    const porOperacion = e.porOperacion.map(c => [c.nombre, c.cantidad]);
    contar("sesiones", e.sesionesActivas);
    contar("operaciones", porOperacion.reduce((s, d) => s + d[1], 0));
    contar("peticiones", e.totalPeticiones);
    $("exitosas").textContent = `${e.totalExitosas} exitosas`;
    barras("barras", porOperacion, k => OPERACIONES[k] || [k, "var(--azul)"]);
    barras("clientes", e.porCliente.map(c => [c.nombre, c.cantidad]), k => [k, COLOR_CLIENTE[k] || "#94a3b8"]);

    if (e.eventos.length) {
      $("eventos").innerHTML = e.eventos.map(ev => {
        const h = String(ev.hora), cli = ev.cliente, op = ev.operacion, ok = ev.exito;
        const id = h + cli + op, nuevo = !vistos.has(id); vistos.add(id);
        return `<li class="evento ${ok ? "" : "error"}" style="${nuevo ? "" : "animation:none"}"><span class="hora">${esc(h.substring(11, 19))}</span>`
          + `<span class="badge b-${esc(cli)}">${esc(cli)}</span><span><b>${esc(op)}</b> · ${ok ? "éxito" : "error"}</span></li>`;
      }).join("");
    }
  } catch {
    $("estado").classList.add("caido");
    $("txtEstado").textContent = "Sin respuesta";
  }
}

// ===================== Prueba rápida (pestaña Servicio) =====================

function mostrar(html, ok) { const r = $("res"); r.className = "resultado"; void r.offsetWidth; r.className = "resultado " + (ok ? "ok" : "mal"); r.innerHTML = html; }

/** Si el token venció en el servidor, vuelve al login. */
function revisarSesion(err) {
  if (/Token inválido o expirado/i.test(err.message)) { salir(err.message); return true; }
  mostrar("❌ " + esc(err.message), false); return false;
}

$("fCuenta").onsubmit = async ev => {
  ev.preventDefault();
  const cuenta = $("cta").value.trim();
  try {
    const c = await api("GET", "cuenta?numero=" + encodeURIComponent(cuenta), null, sesion.token);
    const m = (await api("GET", "movimientos?cuenta=" + encodeURIComponent(cuenta), null, sesion.token)).slice(0, 5);
    const filas = m.map(x => { const ing = x.accion === "INGRESO";
      return `<tr><td>${esc(x.numero)}</td><td>${esc(String(x.fecha).substring(0, 10))}</td><td>${esc(x.tipo)}</td><td class="${ing ? "ing" : "sal"}">${ing ? "+" : "−"}${dinero(x.importe)}</td></tr>`; }).join("");
    mostrar(`✅ Cuenta <b>${esc(cuenta)}</b> · ${esc(c.cliente)} (${esc(c.estado)})<br>Saldo: <b style="font-size:20px">${dinero(c.saldo)}</b> ${esc(c.moneda)}`
      + (filas ? `<table class="movs"><tr><th>#</th><th>Fecha</th><th>Tipo</th><th>Importe</th></tr>${filas}</table>` : ""), true);
  } catch (err) { revisarSesion(err); }
};

$("btnDepositar").onclick = async () => {
  const cuenta = $("cta").value.trim(), texto = $("imp").value.trim().replace(",", ".");
  const importe = Number(texto);
  try {
    const r = await api("POST", "deposito", { cuenta, importe: isNaN(importe) ? texto : importe, codEmpleado: sesion.codigo }, sesion.token);
    mostrar(`✅ Depósito de <b>${dinero(importe)}</b> en ${esc(cuenta)}. Nuevo saldo: <b style="font-size:20px">${dinero(r.saldo)}</b>`, true);
  } catch (err) { revisarSesion(err); }
};

// ===================== Inicio =====================
// Si en esta pestaña del navegador ya había sesión, se reanuda (el servidor dirá si el token sigue vigente).
try { sesion = JSON.parse(sessionStorage.getItem(CLAVE_SESION) || "null"); } catch { sesion = null; }
if (sesion) {
  mostrarPanel();
  const guardada = (() => { try { return sessionStorage.getItem("panel_pestana"); } catch { return null; } })();
  if (guardada) document.querySelector(`.pestana[data-pestana="${guardada}"]`)?.click();
} else {
  comprobarServidor("estadoLogin");
}
