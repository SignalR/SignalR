IF EXISTS (SELECT * FROM sys.databases WHERE name='Stress') DROP DATABASE Stress;
CREATE DATABASE Stress;
ALTER DATABASE Stress SET ENABLE_BROKER;

IF EXISTS (SELECT * FROM sys.database_principals WHERE name='StressUser') DROP USER StressUser;
IF EXISTS (SELECT * FROM sys.server_principals WHERE name='StressUser') DROP LOGIN StressUser;
CREATE LOGIN StressUser WITH PASSWORD='Stre55Pa55';
CREATE USER StressUser FOR LOGIN StressUser;

GRANT ALL TO StressUser;