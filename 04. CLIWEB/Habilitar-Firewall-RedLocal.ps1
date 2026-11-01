# ===================================================================
#  Abre en el firewall de Windows los puertos del proyecto, SOLO para
#  la red local (LocalSubnet):
#    TCP 8088  API REST (IIS)
#    TCP 5088  cliente web
#  En cualquier perfil (Privado o Público): la Wi-Fi suele quedar como Pública.
#
#  EJECUTAR COMO ADMINISTRADOR:
#    powershell -ExecutionPolicy Bypass -File ".\Habilitar-Firewall-RedLocal.ps1"
# ===================================================================

$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) { throw 'Abra la terminal COMO ADMINISTRADOR y vuelva a ejecutar el script.' }

$reglas = @(
    @{ Nombre = 'EUREKA RedLocal REST 8088'; Puerto = 8088 },
    @{ Nombre = 'EUREKA RedLocal REST WEB 5088';  Puerto = 5088 }
)

foreach ($r in $reglas) {
    Get-NetFirewallRule -DisplayName $r.Nombre -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $r.Nombre -Direction Inbound -Protocol TCP -LocalPort $r.Puerto `
        -Action Allow -Profile Any -RemoteAddress LocalSubnet | Out-Null
    Write-Host ("Regla creada: {0} (TCP {1}, solo red local, cualquier perfil)" -f $r.Nombre, $r.Puerto) -ForegroundColor Green
}

$perfiles = Get-NetConnectionProfile | Select-Object InterfaceAlias, NetworkCategory
Write-Host "`nPerfil de las redes conectadas:" -ForegroundColor Cyan
$perfiles | ForEach-Object { Write-Host ("   {0,-14} {1}" -f $_.InterfaceAlias, $_.NetworkCategory) }
