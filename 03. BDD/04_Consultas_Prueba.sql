/*
Script  :  04 - Consultas para verificar los datos (ejecutar en SSMS cuando se necesite)
*/

USE EUREKABANK;
GO

-- Empleado con el que se inicia sesion en los clientes
SELECT chr_emplcodigo, vch_emplnombre, vch_emplusuario FROM Empleado WHERE vch_emplusuario = 'monster';

-- Cuentas con su cliente y moneda
SELECT c.chr_cuencodigo AS Cuenta,
       cl.vch_clienombre + ' ' + cl.vch_cliepaterno AS Cliente,
       m.vch_monedescripcion AS Moneda,
       c.dec_cuensaldo AS Saldo,
       c.vch_cuenestado AS Estado,
       c.int_cuencontmov AS Movimientos
FROM Cuenta c
JOIN Cliente cl ON cl.chr_cliecodigo = c.chr_cliecodigo
JOIN Moneda m ON m.chr_monecodigo = c.chr_monecodigo
ORDER BY c.chr_cuencodigo;

-- Movimientos de una cuenta (el ultimo primero)
SELECT mv.int_movinumero AS Nro, mv.dtt_movifecha AS Fecha, t.vch_tipodescripcion AS Tipo,
       t.vch_tipoaccion AS Accion, mv.dec_moviimporte AS Importe, mv.chr_cuenreferencia AS Referencia
FROM Movimiento mv
JOIN TipoMovimiento t ON t.chr_tipocodigo = mv.chr_tipocodigo
WHERE mv.chr_cuencodigo = '00100001'
ORDER BY mv.int_movinumero DESC;

-- Comprobacion: el saldo de cada cuenta debe ser igual a ingresos - salidas
SELECT c.chr_cuencodigo, c.dec_cuensaldo,
       SUM(CASE t.vch_tipoaccion WHEN 'INGRESO' THEN mv.dec_moviimporte ELSE -mv.dec_moviimporte END) AS SaldoCalculado
FROM Cuenta c
JOIN Movimiento mv ON mv.chr_cuencodigo = c.chr_cuencodigo
JOIN TipoMovimiento t ON t.chr_tipocodigo = mv.chr_tipocodigo
GROUP BY c.chr_cuencodigo, c.dec_cuensaldo;
GO
