USE [master]
RESTORE DATABASE [AdventureWorks] 
    FROM DISK = '/var/opt/mssql/backup/AdventureWorks2019.bak'
        WITH REPLACE,
        MOVE 'AdventureWorks2017' TO '/var/opt/mssql/data/AdventureWorks.mdf',
        MOVE 'AdventureWorks2017_log' TO '/var/opt/mssql/data/AdventureWorks_log.ldf'
GO

USE [AdventureWorks]
GO
CREATE SCHEMA [ProviderDbTypeTest]
GO

CREATE TABLE [ProviderDbTypeTest].[Test] (
  [ID] [INT] PRIMARY KEY,
  [LessPrecision] [DATETIME] NOT NULL,
  [MorePrecision] [DATETIME2](7) NOT NULL
)
GO