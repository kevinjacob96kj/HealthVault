/*
    HealthVault schema setup/upgrade

    Run this file first in SQL Server Management Studio. It creates the
    HealthVault database when missing and upgrades an older HealthVault
    database without deleting existing data.
*/

USE [master];
GO

IF DB_ID(N'HealthVault') IS NULL
BEGIN
    PRINT N'Creating HealthVault database...';
    CREATE DATABASE [HealthVault];
END
ELSE
    PRINT N'HealthVault database already exists.';
GO

USE [HealthVault];
GO

IF OBJECT_ID(N'dbo.People', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.People...';

    CREATE TABLE [dbo].[People]
    (
        [Id]                 int IDENTITY(1,1) NOT NULL,
        [FirstName]          nvarchar(100) NOT NULL,
        [LastName]           nvarchar(100) NOT NULL,
        [Email]              nvarchar(256) NOT NULL,
        [Password]           nvarchar(100) NOT NULL
            CONSTRAINT [DF_People_Password] DEFAULT N'Password@1',
        [Status]             nvarchar(50) NOT NULL,
        [MustChangePassword] bit NOT NULL
            CONSTRAINT [DF_People_MustChangePassword] DEFAULT (1),
        [CreatedAt]          datetime2 NOT NULL
            CONSTRAINT [DF_People_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_People] PRIMARY KEY ([Id])
    );
END;
GO

IF COL_LENGTH(N'dbo.People', N'Password') IS NULL
BEGIN
    PRINT N'Adding dbo.People.Password...';
    ALTER TABLE [dbo].[People]
        ADD [Password] nvarchar(100) NOT NULL
            CONSTRAINT [DF_People_Password] DEFAULT N'Password@1';
END;
GO

IF COL_LENGTH(N'dbo.People', N'MustChangePassword') IS NULL
BEGIN
    PRINT N'Adding dbo.People.MustChangePassword...';
    ALTER TABLE [dbo].[People]
        ADD [MustChangePassword] bit NOT NULL
            CONSTRAINT [DF_People_MustChangePassword] DEFAULT (1);
END;
GO

-- Upgrade only the former application default; custom passwords are preserved.
UPDATE [dbo].[People]
SET [Password] = N'Password@1',
    [MustChangePassword] = 1
WHERE [Password] = N'password';
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'dbo.People')
      AND [name] = N'IX_People_Email'
)
BEGIN
    CREATE UNIQUE INDEX [IX_People_Email]
        ON [dbo].[People] ([Email]);
END;
GO

IF OBJECT_ID(N'dbo.UserClaims', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.UserClaims...';

    CREATE TABLE [dbo].[UserClaims]
    (
        [Id]       int IDENTITY(1,1) NOT NULL,
        [PersonId] int NOT NULL,
        [Role]     nvarchar(50) NOT NULL,

        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_People_PersonId]
            FOREIGN KEY ([PersonId])
            REFERENCES [dbo].[People] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [UQ_UserClaims_PersonId_Role]
            UNIQUE ([PersonId], [Role]),
        CONSTRAINT [CK_UserClaims_Role]
            CHECK ([Role] IN (N'Admin', N'Doctor', N'Nurse', N'Patient', N'Staff'))
    );
END;
GO

IF OBJECT_ID(N'dbo.Requests', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.Requests...';

    CREATE TABLE [dbo].[Requests]
    (
        [Id]              bigint IDENTITY(1,1) NOT NULL,
        [RequestApi]      nvarchar(512) NOT NULL,
        [RequestDateTime] datetime2 NOT NULL
            CONSTRAINT [DF_Requests_RequestDateTime] DEFAULT SYSUTCDATETIME(),
        [WasSuccessful]   bit NOT NULL,
        [RequestedBy]     nvarchar(256) NOT NULL,

        CONSTRAINT [PK_Requests] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_Requests_RequestDateTime]
        ON [dbo].[Requests] ([RequestDateTime]);
END;
GO

PRINT N'HealthVault schema setup complete.';
GO
