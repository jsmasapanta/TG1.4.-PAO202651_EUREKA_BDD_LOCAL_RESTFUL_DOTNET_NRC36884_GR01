# ===================================================================
#  Crea la base EUREKABANK en el SQL Server local y el login monster.
#  Ejecuta en orden: 01_Crea_BD.sql, 02_Carga_Datos.sql, 03_Crea_Login_monster.sql
#
#  Uso (PowerShell, no hace falta ser administrador si su usuario de
#  Windows es administrador de SQL Server):
#    powershell -ExecutionPolicy Bypass -File ".\crear_bdd.ps1"
#    powershell -ExecutionPolicy Bypass -File ".\crear_bdd.ps1" -Servidor ".\SQLEXPRESS"
#
#  OJO: borra y vuelve a crear EUREKABANK (se pierden los movimientos hechos).
# ===================================================================
param(
    [string]$Servidor = 'localhost'
)

$ErrorActionPreference = 'Stop'
$scripts = '01_Crea_BD.sql', '02_Carga_Datos.sql', '03_Crea_Login_monster.sql'

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'No se encontró sqlcmd. Ejecute los 3 scripts a mano en SQL Server Management Studio (en ese orden).'
}

foreach ($s in $scripts) {
    Write-Host ">> $s" -ForegroundColor Cyan
    # -E: usuario de Windows · -C: confiar en el certificado · -b: parar si hay error · -f 65001: UTF-8
    sqlcmd -S $Servidor -E -C -b -f 65001 -i (Join-Path $PSScriptRoot $s)
    if ($LASTEXITCODE -ne 0) { throw "Falló $s (código $LASTEXITCODE)." }
}

Write-Host "`nComprobando el login monster..." -ForegroundColor Cyan
sqlcmd -S $Servidor -U monster -P monster -C -d EUREKABANK -W -Q "SET NOCOUNT ON; SELECT COUNT(*) AS Cuentas FROM Cuenta; SELECT COUNT(*) AS Movimientos FROM Movimiento;"
if ($LASTEXITCODE -ne 0) {
    throw 'El login monster no pudo entrar. Revise que SQL Server tenga el modo de autenticación mixto (ver 03_Crea_Login_monster.sql).'
}
Write-Host "`nListo: EUREKABANK creada en $Servidor." -ForegroundColor Green
