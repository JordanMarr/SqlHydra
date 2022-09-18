USE [master]
RESTORE DATABASE [AdventureWorks] 
    FROM DISK = '/var/opt/mssql/backup/AdventureWorks2019.bak'
        WITH REPLACE,
        MOVE 'AdventureWorks2017' TO '/var/opt/mssql/data/AdventureWorks.mdf',
        MOVE 'AdventureWorks2017_log' TO '/var/opt/mssql/data/AdventureWorks_log.ldf'
GO

USE [AdventureWorks]
GO

-- SqlHydra custom "extensions" schema for bug fix reproductions, new features, etc
CREATE SCHEMA [ext]
GO

-- https://github.com/JordanMarr/SqlHydra/issues/30
-- https://github.com/JordanMarr/SqlHydra/pull/33
CREATE TABLE [ext].[DateTime2Support] (
  [ID] [INT] PRIMARY KEY,
  [LessPrecision] [DATETIME] NOT NULL,
  [MorePrecision] [DATETIME2](7) NOT NULL
)
GO

-- https://github.com/JordanMarr/SqlHydra/issues/38
CREATE TABLE [ext].[GetIdGuidRepro]
(
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    -- descriptive data
    [EmailAddress] NCHAR(50) NOT NULL
)
GO
