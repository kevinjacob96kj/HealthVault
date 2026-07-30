/*
    Optional HealthVault sample users

    Run SetupHealthVault.sql first, then run this file. It is safe to run
    repeatedly: existing users and roles are not duplicated.

    Every sample user starts with Password@1 and must change it at first login.
*/

USE [HealthVault];
GO

DECLARE @People TABLE
(
    [FirstName] nvarchar(100),
    [LastName]  nvarchar(100),
    [Email]     nvarchar(256),
    [Status]    nvarchar(50)
);

INSERT INTO @People ([FirstName], [LastName], [Email], [Status])
VALUES
    (N'Ava', N'Chen', N'ava.chen@healthvault.test', N'Active'),
    (N'Marco', N'Diaz', N'marco.diaz@healthvault.test', N'Active'),
    (N'Priya', N'Nair', N'priya.nair@healthvault.test', N'Active'),
    (N'Jordan', N'Lee', N'jordan.lee@healthvault.test', N'Inactive');

INSERT INTO [dbo].[People]
(
    [FirstName],
    [LastName],
    [Email],
    [Password],
    [Status],
    [MustChangePassword]
)
SELECT
    source.[FirstName],
    source.[LastName],
    source.[Email],
    N'Password@1',
    source.[Status],
    1
FROM @People AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People] AS existing
    WHERE existing.[Email] = source.[Email]
);
GO

DECLARE @Roles TABLE
(
    [Email] nvarchar(256),
    [Role]  nvarchar(50)
);

INSERT INTO @Roles ([Email], [Role])
VALUES
    (N'ava.chen@healthvault.test', N'Admin'),
    (N'ava.chen@healthvault.test', N'Staff'),
    (N'marco.diaz@healthvault.test', N'Doctor'),
    (N'priya.nair@healthvault.test', N'Nurse'),
    (N'priya.nair@healthvault.test', N'Staff'),
    (N'jordan.lee@healthvault.test', N'Patient');

INSERT INTO [dbo].[UserClaims] ([PersonId], [Role])
SELECT person.[Id], source.[Role]
FROM @Roles AS source
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[UserClaims] AS existing
    WHERE existing.[PersonId] = person.[Id]
      AND existing.[Role] = source.[Role]
);
GO

PRINT N'HealthVault sample data setup complete.';

SELECT
    person.[Id],
    person.[FirstName],
    person.[LastName],
    person.[Email],
    person.[Status],
    person.[MustChangePassword],
    STRING_AGG(claim.[Role], N', ')
        WITHIN GROUP (ORDER BY claim.[Role]) AS [Roles]
FROM [dbo].[People] AS person
LEFT JOIN [dbo].[UserClaims] AS claim
    ON claim.[PersonId] = person.[Id]
GROUP BY
    person.[Id],
    person.[FirstName],
    person.[LastName],
    person.[Email],
    person.[Status],
    person.[MustChangePassword]
ORDER BY person.[Id];
GO
