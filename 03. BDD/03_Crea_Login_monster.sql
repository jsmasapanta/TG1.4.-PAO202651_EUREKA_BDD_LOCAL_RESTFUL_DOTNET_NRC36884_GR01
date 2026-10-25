/*
Script  :  03 - Login de SQL Server para el API REST
Usuario :  monster   Clave: monster   (convencion del curso)

El servicio WCF se conecta con este login (autenticacion de SQL Server), asi funciona
igual en IIS Express (Visual Studio) y en IIS, sin depender del usuario de Windows.
Requisito: SQL Server en modo de autenticacion mixto
  (SSMS > clic derecho en el servidor > Propiedades > Seguridad > "Modo de autenticacion de SQL Server y Windows").
*/

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'monster')
BEGIN
	-- CHECK_POLICY = OFF: la clave "monster" no cumple la politica de complejidad de Windows.
	CREATE LOGIN monster WITH PASSWORD = 'monster', DEFAULT_DATABASE = EUREKABANK,
		CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
END;
GO

USE EUREKABANK;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'monster')
BEGIN
	CREATE USER monster FOR LOGIN monster;
END;
GO

-- Solo lectura y escritura de datos: el servicio no necesita crear ni borrar tablas.
ALTER ROLE db_datareader ADD MEMBER monster;
ALTER ROLE db_datawriter ADD MEMBER monster;
GO

PRINT 'Login monster listo (db_datareader + db_datawriter en EUREKABANK).';
GO
