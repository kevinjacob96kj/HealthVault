/*
    HealthVault database setup

    Run this file in SQL Server Management Studio while connected to the
    SQL Server instance you want to use. It is safe to run more than once:
    existing tables and mock users are preserved.
*/

USE [master];
GO

IF DB_ID(N'HealthVault') IS NULL
BEGIN
    PRINT N'Creating HealthVault database...';
    CREATE DATABASE [HealthVault];
END
ELSE
BEGIN
    PRINT N'HealthVault database already exists.';
END;
GO

USE [HealthVault];
GO

IF OBJECT_ID(N'dbo.People', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.People...';

    CREATE TABLE [dbo].[People]
    (
        [Id]        int IDENTITY(1,1) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName]  nvarchar(100) NOT NULL,
        [Email]     nvarchar(256) NOT NULL,
        [Status]    nvarchar(50) NOT NULL,
        [CreatedAt] datetime2 NOT NULL
            CONSTRAINT [DF_People_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_People] PRIMARY KEY ([Id])
    );
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

PRINT N'Adding mock people when missing...';

INSERT INTO [dbo].[People] ([FirstName], [LastName], [Email], [Status])
SELECT N'Ava', N'Chen', N'ava.chen@healthvault.test', N'Active'
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People]
    WHERE [Email] = N'ava.chen@healthvault.test'
);

INSERT INTO [dbo].[People] ([FirstName], [LastName], [Email], [Status])
SELECT N'Marco', N'Diaz', N'marco.diaz@healthvault.test', N'Active'
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People]
    WHERE [Email] = N'marco.diaz@healthvault.test'
);

INSERT INTO [dbo].[People] ([FirstName], [LastName], [Email], [Status])
SELECT N'Priya', N'Nair', N'priya.nair@healthvault.test', N'Active'
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People]
    WHERE [Email] = N'priya.nair@healthvault.test'
);

INSERT INTO [dbo].[People] ([FirstName], [LastName], [Email], [Status])
SELECT N'Jordan', N'Lee', N'jordan.lee@healthvault.test', N'Inactive'
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People]
    WHERE [Email] = N'jordan.lee@healthvault.test'
);
GO

PRINT N'Adding mock roles when missing...';

INSERT INTO [dbo].[UserClaims] ([PersonId], [Role])
SELECT p.[Id], roles.[Role]
FROM [dbo].[People] AS p
INNER JOIN
(
    VALUES
        (N'ava.chen@healthvault.test', N'Admin'),
        (N'ava.chen@healthvault.test', N'Staff'),
        (N'marco.diaz@healthvault.test', N'Doctor'),
        (N'priya.nair@healthvault.test', N'Nurse'),
        (N'priya.nair@healthvault.test', N'Staff'),
        (N'jordan.lee@healthvault.test', N'Patient')
) AS roles ([Email], [Role])
    ON roles.[Email] = p.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[UserClaims] AS existing
    WHERE existing.[PersonId] = p.[Id]
      AND existing.[Role] = roles.[Role]
);
GO

PRINT N'HealthVault setup complete.';

SELECT
    p.[Id],
    p.[FirstName],
    p.[LastName],
    p.[Email],
    p.[Status],
    STRING_AGG(c.[Role], N', ') WITHIN GROUP (ORDER BY c.[Role]) AS [Roles]
FROM [dbo].[People] AS p
LEFT JOIN [dbo].[UserClaims] AS c
    ON c.[PersonId] = p.[Id]
GROUP BY
    p.[Id],
    p.[FirstName],
    p.[LastName],
    p.[Email],
    p.[Status]
ORDER BY p.[Id];
GO
