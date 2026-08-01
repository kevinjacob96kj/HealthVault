/*
    HealthVault schema setup/upgrade
    =================================

    Run this file first in SQL Server Management Studio (or via sqlcmd). It
    creates the HealthVault database when missing, and upgrades an existing
    database to the current schema without deleting existing data.

    Target schema (all tables live in [dbo] - there is no Healthcare schema):
        dbo.People
        dbo.UserClaims
        dbo.Requests
        dbo.Patients
        dbo.Provider                    (formerly Healthcare.Provider / dbo.HealthcareProvider)
        dbo.Staff                       (formerly Healthcare.Staff / dbo.HealthcareStaff)
        dbo.PatientDoctorAssignments
        dbo.LoincCodes                  (LOINC reference codes)
        dbo.PatientData                 (patient observations keyed by LOINC)

    The script is fully idempotent - it can be run repeatedly against a
    database at any prior version and will converge on the target schema.
    All guards use COL_LENGTH / OBJECT_ID / catalog view lookups so steps
    that no longer apply are simply skipped.

    IMPORTANT: execute this file in one shot, top to bottom (e.g. F5 the
    whole file in SSMS, or run it with sqlcmd). A handful of steps below are
    implemented as local temporary procedures (names starting with "#"),
    which only live for the duration of the current connection - running a
    partial selection from a fresh session will fail because those helpers
    won't exist yet.
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

-- Required for filtered/dynamic index creation further down (Msg 1934 otherwise).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

/* ============================================================================
   Helper procedures
   ----------------------------------------------------------------------------
   Small, reusable building blocks for the idempotent guards used throughout
   this script. They are session-scoped local temp procedures, dropped
   automatically when the connection closes.
   ============================================================================ */

-- Adds [ColumnName] to [TableName] using @Definition (e.g. N'bit NOT NULL
-- CONSTRAINT [DF_X] DEFAULT (1)') only if the column does not already exist.
CREATE OR ALTER PROCEDURE #AddColumnIfMissing
    @TableName  sysname,
    @ColumnName sysname,
    @Definition nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    IF COL_LENGTH(@TableName, @ColumnName) IS NOT NULL
        RETURN;

    PRINT N'Adding ' + @TableName + N'.' + @ColumnName + N'...';
    EXEC (N'ALTER TABLE ' + @TableName + N' ADD [' + @ColumnName + N'] ' + @Definition + N';');
END;
GO

-- Drops [ColumnName] from [TableName] if present, first clearing out any
-- DEFAULT constraint, CHECK constraint, or non-PK index that references it
-- (SQL Server refuses to drop a column that is still bound to one of these).
CREATE OR ALTER PROCEDURE #DropColumnIfExists
    @TableName  sysname,
    @ColumnName sysname
AS
BEGIN
    SET NOCOUNT ON;
    IF COL_LENGTH(@TableName, @ColumnName) IS NULL
        RETURN;

    DECLARE @ObjectId int = OBJECT_ID(@TableName);
    DECLARE @ColumnId int = COLUMNPROPERTY(@ObjectId, @ColumnName, 'ColumnId');
    DECLARE @Sql nvarchar(max);

    SELECT TOP (1) @Sql = N'ALTER TABLE ' + @TableName + N' DROP CONSTRAINT [' + dc.[name] + N'];'
    FROM sys.default_constraints AS dc
    WHERE dc.[parent_object_id] = @ObjectId
      AND dc.[parent_column_id] = @ColumnId;
    IF @Sql IS NOT NULL
        EXEC (@Sql);

    -- Legacy/auto-named CHECK constraints may not be bound via parent_column_id,
    -- so also fall back to a definition text match.
    WHILE EXISTS
    (
        SELECT 1
        FROM sys.check_constraints AS cc
        WHERE cc.[parent_object_id] = @ObjectId
          AND (cc.[parent_column_id] = @ColumnId OR cc.[definition] LIKE N'%' + @ColumnName + N'%')
    )
    BEGIN
        SELECT TOP (1) @Sql = N'ALTER TABLE ' + @TableName + N' DROP CONSTRAINT [' + cc.[name] + N'];'
        FROM sys.check_constraints AS cc
        WHERE cc.[parent_object_id] = @ObjectId
          AND (cc.[parent_column_id] = @ColumnId OR cc.[definition] LIKE N'%' + @ColumnName + N'%');

        EXEC (@Sql);
    END;

    WHILE EXISTS
    (
        SELECT 1
        FROM sys.index_columns AS ic
        INNER JOIN sys.indexes AS ix
            ON ix.[object_id] = ic.[object_id] AND ix.[index_id] = ic.[index_id]
        WHERE ic.[object_id] = @ObjectId
          AND ic.[column_id] = @ColumnId
          AND ix.[is_primary_key] = 0
    )
    BEGIN
        SELECT TOP (1) @Sql = N'DROP INDEX [' + ix.[name] + N'] ON ' + @TableName + N';'
        FROM sys.index_columns AS ic
        INNER JOIN sys.indexes AS ix
            ON ix.[object_id] = ic.[object_id] AND ix.[index_id] = ic.[index_id]
        WHERE ic.[object_id] = @ObjectId
          AND ic.[column_id] = @ColumnId
          AND ix.[is_primary_key] = 0;

        EXEC (@Sql);
    END;

    PRINT N'Dropping ' + @TableName + N'.' + @ColumnName + N'...';
    EXEC (N'ALTER TABLE ' + @TableName + N' DROP COLUMN [' + @ColumnName + N'];');
END;
GO

-- Adds a simple FOREIGN KEY constraint if one from @ChildTable to @ParentTable
-- does not already exist under @ConstraintName.
CREATE OR ALTER PROCEDURE #AddForeignKeyIfMissing
    @ConstraintName  sysname,
    @ChildTable      sysname,
    @ChildColumn     sysname,
    @ParentTable     sysname,
    @ParentColumn    sysname,
    @OnDeleteCascade bit = 0
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = @ConstraintName)
        RETURN;

    DECLARE @Sql nvarchar(max) =
        N'ALTER TABLE ' + @ChildTable +
        N' ADD CONSTRAINT [' + @ConstraintName + N']' +
        N' FOREIGN KEY ([' + @ChildColumn + N'])' +
        N' REFERENCES ' + @ParentTable + N' ([' + @ParentColumn + N'])';
    IF @OnDeleteCascade = 1
        SET @Sql += N' ON DELETE CASCADE';
    SET @Sql += N';';

    PRINT N'Adding ' + @ConstraintName + N'...';
    EXEC (@Sql);
END;
GO

-- Creates an index by name if it does not already exist on @TableName.
-- @Columns / @Filter are raw SQL fragments, e.g. N'[PersonId]' or N'[IsActive] = 1'.
CREATE OR ALTER PROCEDURE #CreateIndexIfMissing
    @IndexName sysname,
    @TableName sysname,
    @Columns   nvarchar(max),
    @Unique    bit = 0,
    @Filter    nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = @IndexName AND [object_id] = OBJECT_ID(@TableName))
        RETURN;

    DECLARE @Sql nvarchar(max) =
        N'CREATE ' + CASE WHEN @Unique = 1 THEN N'UNIQUE ' ELSE N'' END +
        N'INDEX [' + @IndexName + N'] ON ' + @TableName + N' (' + @Columns + N')';
    IF @Filter IS NOT NULL
        SET @Sql += N' WHERE ' + @Filter;
    SET @Sql += N';';

    PRINT N'Creating ' + @IndexName + N'...';
    EXEC (@Sql);
END;
GO

-- Drops every foreign key that references @ReferencedObjectId. Used before
-- ALTER SCHEMA ... TRANSFER, which refuses to move a table that other tables
-- still point at.
CREATE OR ALTER PROCEDURE #DropInboundForeignKeys
    @ReferencedObjectId int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Sql nvarchar(max);

    WHILE EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = @ReferencedObjectId)
    BEGIN
        SELECT TOP (1) @Sql =
            N'ALTER TABLE [' + OBJECT_SCHEMA_NAME([parent_object_id]) + N'].[' + OBJECT_NAME([parent_object_id]) +
            N'] DROP CONSTRAINT [' + [name] + N'];'
        FROM sys.foreign_keys
        WHERE [referenced_object_id] = @ReferencedObjectId;

        EXEC (@Sql);
    END;
END;
GO

/* ============================================================================
   dbo.People
   ============================================================================ */

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
        [MustChangePassword] bit NOT NULL
            CONSTRAINT [DF_People_MustChangePassword] DEFAULT (1),
        [Gender]             nvarchar(20) NOT NULL
            CONSTRAINT [DF_People_Gender] DEFAULT N'Man',
        [CreatedAt]          datetime2 NOT NULL
            CONSTRAINT [DF_People_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_People] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_People_Gender]
            CHECK ([Gender] IN (N'Man', N'Women', N'Transgender'))
    );
END;
GO

-- Backfill columns that may be missing on older databases. Password/Gender
-- carry nvarchar defaults and are added directly to avoid nested-quote
-- escaping; the rest go through the generic helper.
IF COL_LENGTH(N'dbo.People', N'Password') IS NULL
BEGIN
    PRINT N'Adding dbo.People.Password...';
    ALTER TABLE [dbo].[People]
        ADD [Password] nvarchar(100) NOT NULL
            CONSTRAINT [DF_People_Password] DEFAULT N'Password@1';
END;
GO

IF COL_LENGTH(N'dbo.People', N'Gender') IS NULL
BEGIN
    PRINT N'Adding dbo.People.Gender...';
    ALTER TABLE [dbo].[People]
        ADD [Gender] nvarchar(20) NOT NULL
            CONSTRAINT [DF_People_Gender] DEFAULT N'Man';
END;
GO

EXEC #AddColumnIfMissing N'dbo.People', N'MustChangePassword',
    N'bit NOT NULL CONSTRAINT [DF_People_MustChangePassword] DEFAULT (1)';
GO

-- Gender values: Man, Women, Transgender (migrate legacy Male/Female/Other).
IF COL_LENGTH(N'dbo.People', N'Gender') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_People_Gender' AND [parent_object_id] = OBJECT_ID(N'dbo.People'))
        ALTER TABLE [dbo].[People] DROP CONSTRAINT [CK_People_Gender];

    UPDATE [dbo].[People]
    SET [Gender] = CASE [Gender]
        WHEN N'Male' THEN N'Man'
        WHEN N'Female' THEN N'Women'
        WHEN N'Other' THEN N'Transgender'
        WHEN N'Man' THEN N'Man'
        WHEN N'Women' THEN N'Women'
        WHEN N'Transgender' THEN N'Transgender'
        ELSE N'Man'
    END;

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [name] = N'DF_People_Gender' AND [parent_object_id] = OBJECT_ID(N'dbo.People'))
        ALTER TABLE [dbo].[People] DROP CONSTRAINT [DF_People_Gender];

    ALTER TABLE [dbo].[People]
        ADD CONSTRAINT [DF_People_Gender] DEFAULT N'Man' FOR [Gender];

    ALTER TABLE [dbo].[People]
        ADD CONSTRAINT [CK_People_Gender]
            CHECK ([Gender] IN (N'Man', N'Women', N'Transgender'));
END;
GO

-- Legacy People.Status has been superseded by dbo.Staff.IsActive.
EXEC #DropColumnIfExists N'dbo.People', N'Status';
GO

-- Roles live on UserClaims; IsHealthcareWorker is obsolete.
EXEC #DropColumnIfExists N'dbo.People', N'IsHealthcareWorker';
GO

-- Upgrade only the former application default; custom passwords are preserved.
UPDATE [dbo].[People]
SET [Password] = N'Password@1',
    [MustChangePassword] = 1
WHERE [Password] = N'password';
GO

EXEC #CreateIndexIfMissing N'IX_People_Email', N'dbo.People', N'[Email]', @Unique = 1;
GO

/* ============================================================================
   dbo.UserClaims
   ============================================================================ */

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
            CHECK ([Role] IN (N'Admin', N'CentralAdmin', N'Doctor', N'Nurse', N'Patient', N'Staff'))
    );
END;
GO

-- Keep the allowed-role list current even on pre-existing tables.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_UserClaims_Role' AND [parent_object_id] = OBJECT_ID(N'dbo.UserClaims'))
    ALTER TABLE [dbo].[UserClaims] DROP CONSTRAINT [CK_UserClaims_Role];
GO
ALTER TABLE [dbo].[UserClaims]
    ADD CONSTRAINT [CK_UserClaims_Role]
        CHECK ([Role] IN (N'Admin', N'CentralAdmin', N'Doctor', N'Nurse', N'Patient', N'Staff'));
GO

/* ============================================================================
   dbo.Requests
   ============================================================================ */

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

EXEC #CreateIndexIfMissing N'IX_Requests_RequestDateTime', N'dbo.Requests', N'[RequestDateTime]';
GO

/* ============================================================================
   dbo.Patients
   ============================================================================ */

IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.Patients...';

    CREATE TABLE [dbo].[Patients]
    (
        [Id]            int IDENTITY(1,1) NOT NULL,
        [PersonId]      int NOT NULL,
        [AbhaId]        nvarchar(20) NOT NULL,
        [AadhaarNumber] nvarchar(12) NULL,
        [DateOfBirth]   date NOT NULL,
        [MobileNumber]  nvarchar(20) NOT NULL,
        [IsActive]      bit NOT NULL
            CONSTRAINT [DF_Patients_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_People_PersonId]
            FOREIGN KEY ([PersonId])
            REFERENCES [dbo].[People] ([Id])
    );

    CREATE UNIQUE INDEX [IX_Patients_PersonId]
        ON [dbo].[Patients] ([PersonId]);

    CREATE UNIQUE INDEX [IX_Patients_AbhaId]
        ON [dbo].[Patients] ([AbhaId]);
END;
GO

-- Older databases may be missing PersonId (patients were keyed only by Email).
EXEC #AddColumnIfMissing N'dbo.Patients', N'PersonId', N'int NULL';
GO

-- Before Patients.Email can be dropped, every patient needs a linked People row.
IF COL_LENGTH(N'dbo.Patients', N'Email') IS NOT NULL
BEGIN
    PRINT N'Backfilling dbo.People from dbo.Patients by email...';

    DECLARE @HasPatientFirstName bit = IIF(COL_LENGTH(N'dbo.Patients', N'FirstName') IS NOT NULL, 1, 0);
    DECLARE @HasPatientLastName  bit = IIF(COL_LENGTH(N'dbo.Patients', N'LastName') IS NOT NULL, 1, 0);
    DECLARE @HasPatientGender    bit = IIF(COL_LENGTH(N'dbo.Patients', N'Gender') IS NOT NULL, 1, 0);

    DECLARE @PatientBackfillSql nvarchar(max) = N'
        INSERT INTO [dbo].[People]
            ([FirstName], [LastName], [Email], [Password], [MustChangePassword], [Gender])
        SELECT
            ' + IIF(@HasPatientFirstName = 1, N'ISNULL(patient.[FirstName], N''Patient'')', N'N''Patient''') + N',
            ' + IIF(@HasPatientLastName = 1, N'ISNULL(patient.[LastName], N''User'')', N'N''User''') + N',
            patient.[Email], N''Password@1'', 1,
            ' + IIF(@HasPatientGender = 1,
                N'CASE ISNULL(patient.[Gender], N''Man'')
                    WHEN N''Male'' THEN N''Man''
                    WHEN N''Female'' THEN N''Women''
                    WHEN N''Other'' THEN N''Transgender''
                    WHEN N''Man'' THEN N''Man''
                    WHEN N''Women'' THEN N''Women''
                    WHEN N''Transgender'' THEN N''Transgender''
                    ELSE N''Man''
                 END',
                N'N''Man''') + N'
        FROM [dbo].[Patients] AS patient
        WHERE patient.[PersonId] IS NULL
          AND NOT EXISTS
          (
              SELECT 1 FROM [dbo].[People] AS person WHERE person.[Email] = patient.[Email]
          );';
    EXEC (@PatientBackfillSql);

    EXEC (N'
        UPDATE patient
        SET patient.[PersonId] = person.[Id]
        FROM [dbo].[Patients] AS patient
        INNER JOIN [dbo].[People] AS person
            ON person.[Email] = patient.[Email]
        WHERE patient.[PersonId] IS NULL;
    ');
END;
GO

-- Patients.Gender predates People.Gender on some databases; sync it across before dropping it.
IF COL_LENGTH(N'dbo.Patients', N'Gender') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patients', N'PersonId') IS NOT NULL
BEGIN
    PRINT N'Backfilling dbo.People.Gender from dbo.Patients.Gender...';
    EXEC (N'
        UPDATE person
        SET person.[Gender] = CASE patient.[Gender]
            WHEN N''Male'' THEN N''Man''
            WHEN N''Female'' THEN N''Women''
            WHEN N''Other'' THEN N''Transgender''
            WHEN N''Man'' THEN N''Man''
            WHEN N''Women'' THEN N''Women''
            WHEN N''Transgender'' THEN N''Transgender''
            ELSE N''Man''
        END
        FROM [dbo].[People] AS person
        INNER JOIN [dbo].[Patients] AS patient
            ON patient.[PersonId] = person.[Id]
        WHERE patient.[Gender] IS NOT NULL;
    ');
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'dbo.Patients') AND [name] = N'PersonId' AND [is_nullable] = 1)
    ALTER TABLE [dbo].[Patients] ALTER COLUMN [PersonId] int NOT NULL;
GO

EXEC #AddForeignKeyIfMissing N'FK_Patients_People_PersonId',
    N'dbo.Patients', N'PersonId', N'dbo.People', N'Id';
GO

EXEC #CreateIndexIfMissing N'IX_Patients_PersonId', N'dbo.Patients', N'[PersonId]', @Unique = 1;
EXEC #CreateIndexIfMissing N'IX_Patients_AbhaId', N'dbo.Patients', N'[AbhaId]', @Unique = 1;
GO

EXEC #AddColumnIfMissing N'dbo.Patients', N'AadhaarNumber', N'nvarchar(12) NULL';
GO

EXEC #CreateIndexIfMissing N'IX_Patients_AadhaarNumber', N'dbo.Patients', N'[AadhaarNumber]',
    @Unique = 1, @Filter = N'[AadhaarNumber] IS NOT NULL';
GO

EXEC #AddColumnIfMissing N'dbo.Patients', N'IsActive',
    N'bit NOT NULL CONSTRAINT [DF_Patients_IsActive] DEFAULT (1)';
GO

IF OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patients', N'IsActive') IS NOT NULL
BEGIN
    UPDATE [dbo].[Patients]
    SET [IsActive] = 1
    WHERE [IsActive] <> 1;
END;
GO

-- Names, gender and login email now live only on dbo.People.
EXEC #DropColumnIfExists N'dbo.Patients', N'FirstName';
EXEC #DropColumnIfExists N'dbo.Patients', N'LastName';
EXEC #DropColumnIfExists N'dbo.Patients', N'Email';
EXEC #DropColumnIfExists N'dbo.Patients', N'Gender';
GO

-- Ensure PersonId is the 2nd column on dbo.Patients.
IF OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patients', N'PersonId') IS NOT NULL
   AND ISNULL(
   (
       SELECT cols.[name]
       FROM
       (
           SELECT [name], ROW_NUMBER() OVER (ORDER BY [column_id]) AS rn
           FROM sys.columns
           WHERE [object_id] = OBJECT_ID(N'dbo.Patients')
       ) cols
       WHERE cols.rn = 2
   ), N'') <> N'PersonId'
BEGIN
    PRINT N'Rebuilding dbo.Patients with PersonId as column 2...';

    IF OBJECT_ID(N'dbo.Patients_Rebuild', N'U') IS NOT NULL
        DROP TABLE [dbo].[Patients_Rebuild];

    CREATE TABLE [dbo].[Patients_Rebuild]
    (
        [Id]            int IDENTITY(1,1) NOT NULL,
        [PersonId]      int NOT NULL,
        [AbhaId]        nvarchar(20) NOT NULL,
        [AadhaarNumber] nvarchar(12) NULL,
        [DateOfBirth]   date NOT NULL,
        [MobileNumber]  nvarchar(20) NOT NULL,
        [IsActive]      bit NOT NULL
            CONSTRAINT [DF_Patients_Rebuild_IsActive] DEFAULT (1),

        CONSTRAINT [PK_Patients_Rebuild] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_Rebuild_People_PersonId]
            FOREIGN KEY ([PersonId])
            REFERENCES [dbo].[People] ([Id])
    );

    SET IDENTITY_INSERT [dbo].[Patients_Rebuild] ON;

    INSERT INTO [dbo].[Patients_Rebuild]
        ([Id], [PersonId], [AbhaId], [AadhaarNumber], [DateOfBirth], [MobileNumber], [IsActive])
    SELECT
        [Id], [PersonId], [AbhaId], [AadhaarNumber], [DateOfBirth], [MobileNumber], [IsActive]
    FROM [dbo].[Patients];

    SET IDENTITY_INSERT [dbo].[Patients_Rebuild] OFF;

    DECLARE @DropPatientsFk nvarchar(max);
    WHILE EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [referenced_object_id] = OBJECT_ID(N'dbo.Patients')
           OR [parent_object_id] = OBJECT_ID(N'dbo.Patients')
    )
    BEGIN
        SELECT TOP (1) @DropPatientsFk =
            N'ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.[parent_object_id]) + N'].[' +
            OBJECT_NAME(fk.[parent_object_id]) + N'] DROP CONSTRAINT [' + fk.[name] + N']'
        FROM sys.foreign_keys AS fk
        WHERE fk.[referenced_object_id] = OBJECT_ID(N'dbo.Patients')
           OR fk.[parent_object_id] = OBJECT_ID(N'dbo.Patients');
        EXEC sys.sp_executesql @DropPatientsFk;
    END;

    DROP TABLE [dbo].[Patients];
    EXEC sp_rename N'dbo.Patients_Rebuild', N'Patients';

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.Patients') AND [name] = N'PK_Patients_Rebuild')
        EXEC sp_rename N'dbo.PK_Patients_Rebuild', N'PK_Patients', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [parent_object_id] = OBJECT_ID(N'dbo.Patients') AND [name] = N'FK_Patients_Rebuild_People_PersonId')
        EXEC sp_rename N'dbo.FK_Patients_Rebuild_People_PersonId', N'FK_Patients_People_PersonId', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.Patients') AND [name] = N'DF_Patients_Rebuild_IsActive')
        EXEC sp_rename N'dbo.DF_Patients_Rebuild_IsActive', N'DF_Patients_IsActive', N'OBJECT';

    EXEC #CreateIndexIfMissing N'IX_Patients_PersonId', N'dbo.Patients', N'[PersonId]', @Unique = 1;
    EXEC #CreateIndexIfMissing N'IX_Patients_AbhaId', N'dbo.Patients', N'[AbhaId]', @Unique = 1;
    EXEC #CreateIndexIfMissing N'IX_Patients_AadhaarNumber', N'dbo.Patients', N'[AadhaarNumber]',
        @Unique = 1, @Filter = N'[AadhaarNumber] IS NOT NULL';

    IF OBJECT_ID(N'dbo.PatientDoctorAssignments', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PatientDoctorAssignments_Patients_PatientId')
    BEGIN
        ALTER TABLE [dbo].[PatientDoctorAssignments]
            ADD CONSTRAINT [FK_PatientDoctorAssignments_Patients_PatientId]
                FOREIGN KEY ([PatientId])
                REFERENCES [dbo].[Patients] ([Id])
                ON DELETE CASCADE;
    END;
END;
GO

/* ============================================================================
   Relocate legacy Healthcare.Provider / Healthcare.Staff (or leftover
   dbo.HealthcareProvider / dbo.HealthcareStaff) into dbo.Provider / dbo.Staff.

   This must run before the dbo.Provider / dbo.Staff "create if missing"
   blocks below, otherwise a fresh empty table would be created and the
   legacy data would never be relocated.
   ============================================================================ */

IF SCHEMA_ID(N'Healthcare') IS NOT NULL
   AND OBJECT_ID(N'dbo.Provider', N'U') IS NULL
   AND (OBJECT_ID(N'Healthcare.Provider', N'U') IS NOT NULL OR OBJECT_ID(N'Healthcare.HealthcareProvider', N'U') IS NOT NULL)
BEGIN
    PRINT N'Moving Healthcare provider table to dbo.Provider...';

    -- ALTER SCHEMA TRANSFER requires incoming foreign keys to be dropped first;
    -- they are recreated further down once dbo.Staff points at dbo.Provider.
    DECLARE @LegacyProviderObjectId int =
        COALESCE(OBJECT_ID(N'Healthcare.Provider', N'U'), OBJECT_ID(N'Healthcare.HealthcareProvider', N'U'));
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @LegacyProviderObjectId;

    IF OBJECT_ID(N'Healthcare.Provider', N'U') IS NOT NULL
    BEGIN
        ALTER SCHEMA [dbo] TRANSFER [Healthcare].[Provider];
    END
    ELSE
    BEGIN
        ALTER SCHEMA [dbo] TRANSFER [Healthcare].[HealthcareProvider];
        EXEC sp_rename N'dbo.HealthcareProvider', N'Provider';
    END;
END;
GO

IF OBJECT_ID(N'dbo.HealthcareProvider', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Provider', N'U') IS NULL
BEGIN
    PRINT N'Renaming dbo.HealthcareProvider to dbo.Provider...';
    EXEC sp_rename N'dbo.HealthcareProvider', N'Provider';
END;
GO

IF SCHEMA_ID(N'Healthcare') IS NOT NULL
   AND OBJECT_ID(N'dbo.Staff', N'U') IS NULL
   AND (OBJECT_ID(N'Healthcare.Staff', N'U') IS NOT NULL OR OBJECT_ID(N'Healthcare.HealthcareStaff', N'U') IS NOT NULL)
BEGIN
    PRINT N'Moving Healthcare staff table to dbo.Staff...';

    DECLARE @LegacyStaffObjectId int =
        COALESCE(OBJECT_ID(N'Healthcare.Staff', N'U'), OBJECT_ID(N'Healthcare.HealthcareStaff', N'U'));
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @LegacyStaffObjectId;

    IF OBJECT_ID(N'Healthcare.Staff', N'U') IS NOT NULL
    BEGIN
        ALTER SCHEMA [dbo] TRANSFER [Healthcare].[Staff];
    END
    ELSE
    BEGIN
        ALTER SCHEMA [dbo] TRANSFER [Healthcare].[HealthcareStaff];
        EXEC sp_rename N'dbo.HealthcareStaff', N'Staff';
    END;
END;
GO

IF OBJECT_ID(N'dbo.HealthcareStaff', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Staff', N'U') IS NULL
BEGIN
    PRINT N'Renaming dbo.HealthcareStaff to dbo.Staff...';
    EXEC sp_rename N'dbo.HealthcareStaff', N'Staff';
END;
GO

-- dbo.Provider / dbo.Staff already exist, but leftover Healthcare copies may remain
-- from an earlier create-then-seed path. Drop the duplicates once dbo owns the data.
IF OBJECT_ID(N'dbo.Provider', N'U') IS NOT NULL
   AND OBJECT_ID(N'Healthcare.Provider', N'U') IS NOT NULL
BEGIN
    PRINT N'Dropping leftover Healthcare.Provider (dbo.Provider already exists)...';

    DECLARE @DupProviderObjectId int = OBJECT_ID(N'Healthcare.Provider', N'U');
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @DupProviderObjectId;

    DECLARE @DropProviderFk nvarchar(max);
    WHILE EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [parent_object_id] = OBJECT_ID(N'Healthcare.Provider')
    )
    BEGIN
        SELECT TOP (1) @DropProviderFk =
            N'ALTER TABLE [Healthcare].[Provider] DROP CONSTRAINT [' + [name] + N'];'
        FROM sys.foreign_keys
        WHERE [parent_object_id] = OBJECT_ID(N'Healthcare.Provider');
        EXEC (@DropProviderFk);
    END;

    DROP TABLE [Healthcare].[Provider];
END;
GO

IF OBJECT_ID(N'dbo.Staff', N'U') IS NOT NULL
   AND OBJECT_ID(N'Healthcare.Staff', N'U') IS NOT NULL
BEGIN
    PRINT N'Dropping leftover Healthcare.Staff (dbo.Staff already exists)...';

    DECLARE @DupStaffObjectId int = OBJECT_ID(N'Healthcare.Staff', N'U');
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @DupStaffObjectId;

    DECLARE @DropStaffFk nvarchar(max);
    WHILE EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [parent_object_id] = OBJECT_ID(N'Healthcare.Staff')
    )
    BEGIN
        SELECT TOP (1) @DropStaffFk =
            N'ALTER TABLE [Healthcare].[Staff] DROP CONSTRAINT [' + [name] + N'];'
        FROM sys.foreign_keys
        WHERE [parent_object_id] = OBJECT_ID(N'Healthcare.Staff');
        EXEC (@DropStaffFk);
    END;

    DROP TABLE [Healthcare].[Staff];
END;
GO

IF OBJECT_ID(N'dbo.Provider', N'U') IS NOT NULL
   AND OBJECT_ID(N'Healthcare.HealthcareProvider', N'U') IS NOT NULL
BEGIN
    PRINT N'Dropping leftover Healthcare.HealthcareProvider...';
    DECLARE @DupHpObjectId int = OBJECT_ID(N'Healthcare.HealthcareProvider', N'U');
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @DupHpObjectId;
    DROP TABLE [Healthcare].[HealthcareProvider];
END;
GO

IF OBJECT_ID(N'dbo.Staff', N'U') IS NOT NULL
   AND OBJECT_ID(N'Healthcare.HealthcareStaff', N'U') IS NOT NULL
BEGIN
    PRINT N'Dropping leftover Healthcare.HealthcareStaff...';
    DECLARE @DupHsObjectId int = OBJECT_ID(N'Healthcare.HealthcareStaff', N'U');
    EXEC #DropInboundForeignKeys @ReferencedObjectId = @DupHsObjectId;
    DROP TABLE [Healthcare].[HealthcareStaff];
END;
GO

/* ============================================================================
   dbo.Provider
   ============================================================================ */

IF OBJECT_ID(N'dbo.Provider', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.Provider...';

    CREATE TABLE [dbo].[Provider]
    (
        [Id]           int IDENTITY(1,1) NOT NULL,
        [ProviderCode] nvarchar(20) NOT NULL,
        [Name]         nvarchar(200) NOT NULL,
        [ProviderType] nvarchar(50) NOT NULL,
        [Address]      nvarchar(256) NOT NULL,
        [City]         nvarchar(100) NOT NULL,
        [State]        nvarchar(50) NOT NULL,
        [PostalCode]   nvarchar(20) NOT NULL,
        [Phone]        nvarchar(30) NOT NULL,
        [Email]        nvarchar(256) NULL,
        [IsActive]     bit NOT NULL
            CONSTRAINT [DF_Provider_IsActive] DEFAULT (1),
        [CreatedAt]    datetime2 NOT NULL
            CONSTRAINT [DF_Provider_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Provider] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Provider_ProviderType]
            CHECK ([ProviderType] IN (N'Hospital', N'Clinic', N'Lab', N'Pharmacy'))
    );

    CREATE UNIQUE INDEX [IX_Provider_ProviderCode]
        ON [dbo].[Provider] ([ProviderCode]);
END;
GO

EXEC #AddColumnIfMissing N'dbo.Provider', N'IsActive',
    N'bit NOT NULL CONSTRAINT [DF_Provider_IsActive] DEFAULT (1)';
GO

EXEC #CreateIndexIfMissing N'IX_Provider_ProviderCode', N'dbo.Provider', N'[ProviderCode]', @Unique = 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Provider_ProviderType' AND [parent_object_id] = OBJECT_ID(N'dbo.Provider'))
BEGIN
    PRINT N'Adding CK_Provider_ProviderType...';
    ALTER TABLE [dbo].[Provider]
        ADD CONSTRAINT [CK_Provider_ProviderType]
            CHECK ([ProviderType] IN (N'Hospital', N'Clinic', N'Lab', N'Pharmacy'));
END;
GO

/* ============================================================================
   dbo.Staff
   ============================================================================ */

IF OBJECT_ID(N'dbo.Staff', N'U') IS NULL AND OBJECT_ID(N'dbo.Provider', N'U') IS NOT NULL
BEGIN
    PRINT N'Creating dbo.Staff...';

    CREATE TABLE [dbo].[Staff]
    (
        [Id]                   int IDENTITY(1,1) NOT NULL,
        [PersonId]             int NOT NULL,
        [StaffCode]            nvarchar(20) NOT NULL,
        [HealthcareProviderId] int NOT NULL,
        [Specialty]            nvarchar(100) NULL,
        [LicenseNumber]        nvarchar(50) NULL,
        [Phone]                nvarchar(30) NOT NULL,
        [IsActive]             bit NOT NULL
            CONSTRAINT [DF_Staff_IsActive] DEFAULT (1),
        [CreatedAt]            datetime2 NOT NULL
            CONSTRAINT [DF_Staff_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_Staff] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Staff_People_PersonId]
            FOREIGN KEY ([PersonId])
            REFERENCES [dbo].[People] ([Id]),
        CONSTRAINT [FK_Staff_Provider_HealthcareProviderId]
            FOREIGN KEY ([HealthcareProviderId])
            REFERENCES [dbo].[Provider] ([Id])
    );

    CREATE UNIQUE INDEX [IX_Staff_StaffCode]
        ON [dbo].[Staff] ([StaffCode]);

    CREATE UNIQUE INDEX [IX_Staff_PersonId]
        ON [dbo].[Staff] ([PersonId]);

    CREATE INDEX [IX_Staff_HealthcareProviderId]
        ON [dbo].[Staff] ([HealthcareProviderId]);
END;
GO

EXEC #AddColumnIfMissing N'dbo.Staff', N'IsActive',
    N'bit NOT NULL CONSTRAINT [DF_Staff_IsActive] DEFAULT (1)';
GO

-- Before Staff.Email can be dropped, every staff member needs a linked People row.
IF COL_LENGTH(N'dbo.Staff', N'Email') IS NOT NULL
BEGIN
    EXEC #AddColumnIfMissing N'dbo.Staff', N'PersonId', N'int NULL';

    PRINT N'Backfilling dbo.People from dbo.Staff by email...';

    DECLARE @HasStaffFirstName bit = IIF(COL_LENGTH(N'dbo.Staff', N'FirstName') IS NOT NULL, 1, 0);
    DECLARE @HasStaffLastName  bit = IIF(COL_LENGTH(N'dbo.Staff', N'LastName') IS NOT NULL, 1, 0);

    DECLARE @StaffBackfillSql nvarchar(max) = N'
        INSERT INTO [dbo].[People]
            ([FirstName], [LastName], [Email], [Password], [MustChangePassword], [Gender])
        SELECT
            ' + IIF(@HasStaffFirstName = 1, N'ISNULL(staff.[FirstName], N''Staff'')', N'N''Staff''') + N',
            ' + IIF(@HasStaffLastName = 1, N'ISNULL(staff.[LastName], N''User'')', N'N''User''') + N',
            staff.[Email], N''Password@1'', 1, N''Man''
        FROM [dbo].[Staff] AS staff
        WHERE staff.[PersonId] IS NULL
          AND NOT EXISTS
          (
              SELECT 1 FROM [dbo].[People] AS person WHERE person.[Email] = staff.[Email]
          );';
    EXEC (@StaffBackfillSql);

    EXEC (N'
        UPDATE staff
        SET staff.[PersonId] = person.[Id]
        FROM [dbo].[Staff] AS staff
        INNER JOIN [dbo].[People] AS person
            ON person.[Email] = staff.[Email]
        WHERE staff.[PersonId] IS NULL;
    ');
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(N'dbo.Staff') AND [name] = N'PersonId' AND [is_nullable] = 1)
    ALTER TABLE [dbo].[Staff] ALTER COLUMN [PersonId] int NOT NULL;
GO

EXEC #AddForeignKeyIfMissing N'FK_Staff_People_PersonId',
    N'dbo.Staff', N'PersonId', N'dbo.People', N'Id';
GO

IF OBJECT_ID(N'dbo.Provider', N'U') IS NOT NULL
    EXEC #AddForeignKeyIfMissing N'FK_Staff_Provider_HealthcareProviderId',
        N'dbo.Staff', N'HealthcareProviderId', N'dbo.Provider', N'Id';
GO

EXEC #CreateIndexIfMissing N'IX_Staff_PersonId', N'dbo.Staff', N'[PersonId]', @Unique = 1;
EXEC #CreateIndexIfMissing N'IX_Staff_StaffCode', N'dbo.Staff', N'[StaffCode]', @Unique = 1;
EXEC #CreateIndexIfMissing N'IX_Staff_HealthcareProviderId', N'dbo.Staff', N'[HealthcareProviderId]';
GO

-- Roles live on UserClaims; Staff.Role is obsolete.
IF COL_LENGTH(N'dbo.Staff', N'Role') IS NOT NULL
BEGIN
    DECLARE @StaffRoleCk sysname =
    (
        SELECT [name]
        FROM sys.check_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'dbo.Staff')
          AND [definition] LIKE N'%Role%'
    );
    IF @StaffRoleCk IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[Staff] DROP CONSTRAINT [' + @StaffRoleCk + N']');
END;
GO

EXEC #DropColumnIfExists N'dbo.Staff', N'Role';
GO

-- Names and login email now live only on dbo.People.
EXEC #DropColumnIfExists N'dbo.Staff', N'FirstName';
EXEC #DropColumnIfExists N'dbo.Staff', N'LastName';
EXEC #DropColumnIfExists N'dbo.Staff', N'Email';
EXEC #DropColumnIfExists N'dbo.Staff', N'Gender';
GO

IF OBJECT_ID(N'dbo.Staff', N'U') IS NOT NULL
BEGIN
    PRINT N'Setting all dbo.Staff.IsActive to 1...';
    UPDATE [dbo].[Staff]
    SET [IsActive] = 1
    WHERE [IsActive] <> 1;
END;
GO

/* ============================================================================
   dbo.PatientDoctorAssignments
   ============================================================================ */

IF OBJECT_ID(N'dbo.PatientDoctorAssignments', N'U') IS NULL
   AND OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Staff', N'U') IS NOT NULL
BEGIN
    PRINT N'Creating dbo.PatientDoctorAssignments...';

    CREATE TABLE [dbo].[PatientDoctorAssignments]
    (
        [Id]                int IDENTITY(1,1) NOT NULL,
        [PatientId]         int NOT NULL,
        [HealthcareStaffId] int NOT NULL,
        [IsActive]          bit NOT NULL
            CONSTRAINT [DF_PatientDoctorAssignments_IsActive] DEFAULT (1),
        [AssignedAt]        datetime2 NOT NULL
            CONSTRAINT [DF_PatientDoctorAssignments_AssignedAt] DEFAULT SYSUTCDATETIME(),
        [UnassignedAt]      datetime2 NULL,
        [Notes]             nvarchar(500) NULL,

        CONSTRAINT [PK_PatientDoctorAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientDoctorAssignments_Patients_PatientId]
            FOREIGN KEY ([PatientId])
            REFERENCES [dbo].[Patients] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_PatientDoctorAssignments_Staff_HealthcareStaffId]
            FOREIGN KEY ([HealthcareStaffId])
            REFERENCES [dbo].[Staff] ([Id])
    );

    CREATE INDEX [IX_PatientDoctorAssignments_PatientId]
        ON [dbo].[PatientDoctorAssignments] ([PatientId]);

    CREATE INDEX [IX_PatientDoctorAssignments_HealthcareStaffId]
        ON [dbo].[PatientDoctorAssignments] ([HealthcareStaffId]);
END;
GO

IF OBJECT_ID(N'dbo.PatientDoctorAssignments', N'U') IS NOT NULL
BEGIN
    EXEC #AddForeignKeyIfMissing N'FK_PatientDoctorAssignments_Patients_PatientId',
        N'dbo.PatientDoctorAssignments', N'PatientId', N'dbo.Patients', N'Id', @OnDeleteCascade = 1;

    EXEC #AddForeignKeyIfMissing N'FK_PatientDoctorAssignments_Staff_HealthcareStaffId',
        N'dbo.PatientDoctorAssignments', N'HealthcareStaffId', N'dbo.Staff', N'Id';

    EXEC #CreateIndexIfMissing N'IX_PatientDoctorAssignments_PatientId',
        N'dbo.PatientDoctorAssignments', N'[PatientId]';

    EXEC #CreateIndexIfMissing N'IX_PatientDoctorAssignments_HealthcareStaffId',
        N'dbo.PatientDoctorAssignments', N'[HealthcareStaffId]';

    EXEC #CreateIndexIfMissing N'UQ_PatientDoctorAssignments_Active',
        N'dbo.PatientDoctorAssignments', N'[PatientId], [HealthcareStaffId]',
        @Unique = 1, @Filter = N'[IsActive] = 1';
END;
GO

/* ============================================================================
   Drop the now-empty legacy Healthcare schema(s), if any remain.
   ============================================================================ */

IF SCHEMA_ID(N'Healthcare') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE [schema_id] = SCHEMA_ID(N'Healthcare'))
BEGIN
    PRINT N'Dropping empty Healthcare schema...';
    EXEC (N'DROP SCHEMA [Healthcare]');
END;
GO

IF SCHEMA_ID(N'Healthcare_rename_tmp') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.objects WHERE [schema_id] = SCHEMA_ID(N'Healthcare_rename_tmp'))
BEGIN
    PRINT N'Dropping empty Healthcare_rename_tmp schema...';
    EXEC (N'DROP SCHEMA [Healthcare_rename_tmp]');
END;
GO

/* ============================================================================
   dbo.LoincCodes — LOINC reference codes for observations / labs / vitals
   ============================================================================ */

IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NULL
BEGIN
    PRINT N'Creating dbo.LoincCodes...';

    CREATE TABLE [dbo].[LoincCodes]
    (
        [Id]               int IDENTITY(1,1) NOT NULL,
        [LoincNum]         nvarchar(20)  NOT NULL,
        [Component]        nvarchar(255) NOT NULL,
        [Property]         nvarchar(50)  NULL,
        [TimeAspct]        nvarchar(50)  NULL,
        [System]           nvarchar(100) NULL,
        [ScaleTyp]         nvarchar(30)  NULL,
        [MethodTyp]        nvarchar(100) NULL,
        [Class]            nvarchar(50)  NULL,
        [ShortName]        nvarchar(100) NULL,
        [LongCommonName]   nvarchar(255) NOT NULL,
        [Status]           nvarchar(20)  NOT NULL
            CONSTRAINT [DF_LoincCodes_Status] DEFAULT N'ACTIVE',
        [ClassType]        tinyint       NULL,
        [ExampleUnits]     nvarchar(50)  NULL,
        [CreatedAt]        datetime2     NOT NULL
            CONSTRAINT [DF_LoincCodes_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_LoincCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_LoincCodes_LoincNum] UNIQUE ([LoincNum]),
        CONSTRAINT [CK_LoincCodes_Status]
            CHECK ([Status] IN (N'ACTIVE', N'DEPRECATED', N'DISCOURAGED', N'TRIAL'))
    );

    CREATE INDEX [IX_LoincCodes_ShortName]
        ON [dbo].[LoincCodes] ([ShortName]);

    CREATE INDEX [IX_LoincCodes_Class]
        ON [dbo].[LoincCodes] ([Class]);

    CREATE INDEX [IX_LoincCodes_LongCommonName]
        ON [dbo].[LoincCodes] ([LongCommonName]);
END;
GO

-- Upgrade existing LoincCodes that used LoincNum as the primary key.
IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.LoincCodes', N'Id') IS NULL
BEGIN
    PRINT N'Adding dbo.LoincCodes.Id identity column...';

    ALTER TABLE [dbo].[LoincCodes]
        ADD [Id] int IDENTITY(1,1) NOT NULL;

    DECLARE @PkName sysname =
    (
        SELECT [name]
        FROM sys.key_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes')
          AND [type] = N'PK'
    );

    IF @PkName IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[LoincCodes] DROP CONSTRAINT [' + @PkName + N']');

    ALTER TABLE [dbo].[LoincCodes]
        ADD CONSTRAINT [PK_LoincCodes] PRIMARY KEY ([Id]);

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.key_constraints
        WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes')
          AND [name] = N'UQ_LoincCodes_LoincNum'
    )
    BEGIN
        ALTER TABLE [dbo].[LoincCodes]
            ADD CONSTRAINT [UQ_LoincCodes_LoincNum] UNIQUE ([LoincNum]);
    END;
END;
GO

EXEC #AddColumnIfMissing N'dbo.LoincCodes', N'ExampleUnits', N'nvarchar(50) NULL';
EXEC #AddColumnIfMissing N'dbo.LoincCodes', N'ClassType', N'tinyint NULL';
EXEC #AddColumnIfMissing N'dbo.LoincCodes', N'CreatedAt',
    N'datetime2 NOT NULL CONSTRAINT [DF_LoincCodes_CreatedAt] DEFAULT SYSUTCDATETIME()';
GO

EXEC #CreateIndexIfMissing N'IX_LoincCodes_ShortName', N'dbo.LoincCodes', N'[ShortName]';
EXEC #CreateIndexIfMissing N'IX_LoincCodes_Class', N'dbo.LoincCodes', N'[Class]';
EXEC #CreateIndexIfMissing N'IX_LoincCodes_LongCommonName', N'dbo.LoincCodes', N'[LongCommonName]';
GO

IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE [name] = N'UQ_LoincCodes_LoincNum' AND [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes'))
   AND COL_LENGTH(N'dbo.LoincCodes', N'LoincNum') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[LoincCodes]
        ADD CONSTRAINT [UQ_LoincCodes_LoincNum] UNIQUE ([LoincNum]);
END;
GO

IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_LoincCodes_Status')
BEGIN
    ALTER TABLE [dbo].[LoincCodes]
        ADD CONSTRAINT [CK_LoincCodes_Status]
            CHECK ([Status] IN (N'ACTIVE', N'DEPRECATED', N'DISCOURAGED', N'TRIAL'));
END;
GO

-- Ensure Id is the first column on dbo.LoincCodes.
IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.LoincCodes', N'Id') IS NOT NULL
   AND ISNULL(
   (
       SELECT cols.[name]
       FROM
       (
           SELECT [name], ROW_NUMBER() OVER (ORDER BY [column_id]) AS rn
           FROM sys.columns
           WHERE [object_id] = OBJECT_ID(N'dbo.LoincCodes')
       ) cols
       WHERE cols.rn = 1
   ), N'') <> N'Id'
BEGIN
    PRINT N'Rebuilding dbo.LoincCodes with Id as column 1...';

    IF OBJECT_ID(N'dbo.LoincCodes_Rebuild', N'U') IS NOT NULL
        DROP TABLE [dbo].[LoincCodes_Rebuild];

    CREATE TABLE [dbo].[LoincCodes_Rebuild]
    (
        [Id]               int IDENTITY(1,1) NOT NULL,
        [LoincNum]         nvarchar(20)  NOT NULL,
        [Component]        nvarchar(255) NOT NULL,
        [Property]         nvarchar(50)  NULL,
        [TimeAspct]        nvarchar(50)  NULL,
        [System]           nvarchar(100) NULL,
        [ScaleTyp]         nvarchar(30)  NULL,
        [MethodTyp]        nvarchar(100) NULL,
        [Class]            nvarchar(50)  NULL,
        [ShortName]        nvarchar(100) NULL,
        [LongCommonName]   nvarchar(255) NOT NULL,
        [Status]           nvarchar(20)  NOT NULL
            CONSTRAINT [DF_LoincCodes_Rebuild_Status] DEFAULT N'ACTIVE',
        [ClassType]        tinyint       NULL,
        [ExampleUnits]     nvarchar(50)  NULL,
        [CreatedAt]        datetime2     NOT NULL
            CONSTRAINT [DF_LoincCodes_Rebuild_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_LoincCodes_Rebuild] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_LoincCodes_Rebuild_LoincNum] UNIQUE ([LoincNum]),
        CONSTRAINT [CK_LoincCodes_Rebuild_Status]
            CHECK ([Status] IN (N'ACTIVE', N'DEPRECATED', N'DISCOURAGED', N'TRIAL'))
    );

    SET IDENTITY_INSERT [dbo].[LoincCodes_Rebuild] ON;

    INSERT INTO [dbo].[LoincCodes_Rebuild]
    (
        [Id], [LoincNum], [Component], [Property], [TimeAspct], [System], [ScaleTyp], [MethodTyp],
        [Class], [ShortName], [LongCommonName], [Status], [ClassType], [ExampleUnits], [CreatedAt]
    )
    SELECT
        [Id], [LoincNum], [Component], [Property], [TimeAspct], [System], [ScaleTyp], [MethodTyp],
        [Class], [ShortName], [LongCommonName], [Status], [ClassType], [ExampleUnits], [CreatedAt]
    FROM [dbo].[LoincCodes];

    SET IDENTITY_INSERT [dbo].[LoincCodes_Rebuild] OFF;

    DECLARE @DropLoincFk nvarchar(max);
    WHILE EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [referenced_object_id] = OBJECT_ID(N'dbo.LoincCodes')
           OR [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes')
    )
    BEGIN
        SELECT TOP (1) @DropLoincFk =
            N'ALTER TABLE [' + OBJECT_SCHEMA_NAME(fk.[parent_object_id]) + N'].[' +
            OBJECT_NAME(fk.[parent_object_id]) + N'] DROP CONSTRAINT [' + fk.[name] + N']'
        FROM sys.foreign_keys AS fk
        WHERE fk.[referenced_object_id] = OBJECT_ID(N'dbo.LoincCodes')
           OR fk.[parent_object_id] = OBJECT_ID(N'dbo.LoincCodes');
        EXEC sys.sp_executesql @DropLoincFk;
    END;

    DROP TABLE [dbo].[LoincCodes];
    EXEC sp_rename N'dbo.LoincCodes_Rebuild', N'LoincCodes';

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes') AND [name] = N'PK_LoincCodes_Rebuild')
        EXEC sp_rename N'dbo.PK_LoincCodes_Rebuild', N'PK_LoincCodes', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes') AND [name] = N'UQ_LoincCodes_Rebuild_LoincNum')
        EXEC sp_rename N'dbo.UQ_LoincCodes_Rebuild_LoincNum', N'UQ_LoincCodes_LoincNum', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes') AND [name] = N'CK_LoincCodes_Rebuild_Status')
        EXEC sp_rename N'dbo.CK_LoincCodes_Rebuild_Status', N'CK_LoincCodes_Status', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes') AND [name] = N'DF_LoincCodes_Rebuild_Status')
        EXEC sp_rename N'dbo.DF_LoincCodes_Rebuild_Status', N'DF_LoincCodes_Status', N'OBJECT';

    IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.LoincCodes') AND [name] = N'DF_LoincCodes_Rebuild_CreatedAt')
        EXEC sp_rename N'dbo.DF_LoincCodes_Rebuild_CreatedAt', N'DF_LoincCodes_CreatedAt', N'OBJECT';

    EXEC #CreateIndexIfMissing N'IX_LoincCodes_ShortName', N'dbo.LoincCodes', N'[ShortName]';
    EXEC #CreateIndexIfMissing N'IX_LoincCodes_Class', N'dbo.LoincCodes', N'[Class]';
    EXEC #CreateIndexIfMissing N'IX_LoincCodes_LongCommonName', N'dbo.LoincCodes', N'[LongCommonName]';

    IF OBJECT_ID(N'dbo.PatientData', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_PatientData_LoincCodes_LoincCodeId')
    BEGIN
        ALTER TABLE [dbo].[PatientData]
            ADD CONSTRAINT [FK_PatientData_LoincCodes_LoincCodeId]
                FOREIGN KEY ([LoincCodeId])
                REFERENCES [dbo].[LoincCodes] ([Id]);
    END;
END;
GO

/* ============================================================================
   dbo.PatientData — patient observations / results linked to LOINC codes
   ============================================================================ */

IF OBJECT_ID(N'dbo.PatientData', N'U') IS NULL
   AND OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.LoincCodes', N'U') IS NOT NULL
BEGIN
    PRINT N'Creating dbo.PatientData...';

    CREATE TABLE [dbo].[PatientData]
    (
        [Id]          int IDENTITY(1,1) NOT NULL,
        [PatientId]   int NOT NULL,
        [LoincCodeId] int NOT NULL,
        [Value]       nvarchar(100) NOT NULL,
        [Units]       nvarchar(50) NULL,
        [ObservedAt]  datetime2 NOT NULL
            CONSTRAINT [DF_PatientData_ObservedAt] DEFAULT SYSUTCDATETIME(),
        [Notes]       nvarchar(500) NULL,
        [CreatedAt]   datetime2 NOT NULL
            CONSTRAINT [DF_PatientData_CreatedAt] DEFAULT SYSUTCDATETIME(),

        CONSTRAINT [PK_PatientData] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientData_Patients_PatientId]
            FOREIGN KEY ([PatientId])
            REFERENCES [dbo].[Patients] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_PatientData_LoincCodes_LoincCodeId]
            FOREIGN KEY ([LoincCodeId])
            REFERENCES [dbo].[LoincCodes] ([Id])
            ON DELETE NO ACTION
    );

    CREATE INDEX [IX_PatientData_PatientId]
        ON [dbo].[PatientData] ([PatientId]);

    CREATE INDEX [IX_PatientData_LoincCodeId]
        ON [dbo].[PatientData] ([LoincCodeId]);

    CREATE INDEX [IX_PatientData_PatientId_ObservedAt]
        ON [dbo].[PatientData] ([PatientId], [ObservedAt]);
END;
GO

EXEC #AddForeignKeyIfMissing N'FK_PatientData_Patients_PatientId',
    N'dbo.PatientData', N'PatientId', N'dbo.Patients', N'Id', @OnDeleteCascade = 1;
EXEC #AddForeignKeyIfMissing N'FK_PatientData_LoincCodes_LoincCodeId',
    N'dbo.PatientData', N'LoincCodeId', N'dbo.LoincCodes', N'Id';
GO

EXEC #CreateIndexIfMissing N'IX_PatientData_PatientId', N'dbo.PatientData', N'[PatientId]';
EXEC #CreateIndexIfMissing N'IX_PatientData_LoincCodeId', N'dbo.PatientData', N'[LoincCodeId]';
EXEC #CreateIndexIfMissing N'IX_PatientData_PatientId_ObservedAt',
    N'dbo.PatientData', N'[PatientId], [ObservedAt]';
GO

PRINT N'HealthVault schema setup complete.';
GO
