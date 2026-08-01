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
    [Gender]    nvarchar(20)
);

INSERT INTO @People ([FirstName], [LastName], [Email], [Gender])
VALUES
    (N'Morgan', N'Blake', N'morgan.blake@healthvault.test', N'Transgender'),
    (N'Ava', N'Chen', N'ava.chen@healthvault.test', N'Women'),
    (N'Marco', N'Diaz', N'marco.diaz@healthvault.test', N'Man'),
    (N'Priya', N'Nair', N'priya.nair@healthvault.test', N'Women'),
    (N'Elena', N'Singh', N'elena.singh@healthvault.test', N'Women'),
    (N'Chris', N'Owens', N'chris.owens@healthvault.test', N'Transgender'),
    (N'Taylor', N'Brooks', N'taylor.brooks@healthvault.test', N'Transgender'),
    (N'Quinn', N'Reed', N'quinn.reed@healthvault.test', N'Transgender'),
    (N'Jordan', N'Lee', N'healthtest797@gmail.com', N'Man'),
    -- Additional doctors
    (N'Sofia', N'Martinez', N'sofia.martinez@healthvault.test', N'Women'),
    (N'James', N'Kim', N'james.kim@healthvault.test', N'Man'),
    (N'Amara', N'Okoye', N'amara.okoye@healthvault.test', N'Women'),
    (N'Noah', N'Bennett', N'noah.bennett@healthvault.test', N'Man'),
    (N'Isla', N'Rahman', N'isla.rahman@healthvault.test', N'Women'),
    (N'Lucas', N'Nguyen', N'lucas.nguyen@healthvault.test', N'Man'),
    (N'Mia', N'Patel', N'mia.patel@healthvault.test', N'Women'),
    (N'Daniel', N'Costa', N'daniel.costa@healthvault.test', N'Man'),
    -- Additional nurses / staff
    (N'Hannah', N'Walsh', N'hannah.walsh@healthvault.test', N'Women'),
    (N'Omar', N'Hassan', N'omar.hassan@healthvault.test', N'Man'),
    (N'Grace', N'Thornton', N'grace.thornton@healthvault.test', N'Women'),
    (N'Leo', N'Morrison', N'leo.morrison@healthvault.test', N'Man'),
    (N'Nina', N'Park', N'nina.park@healthvault.test', N'Women'),
    (N'Victor', N'Almeida', N'victor.almeida@healthvault.test', N'Man');

INSERT INTO [dbo].[People]
(
    [FirstName],
    [LastName],
    [Email],
    [Password],
    [MustChangePassword],
    [Gender]
)
SELECT
    source.[FirstName],
    source.[LastName],
    source.[Email],
    N'Password@1',
    1,
    source.[Gender]
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
    (N'morgan.blake@healthvault.test', N'CentralAdmin'),
    (N'ava.chen@healthvault.test', N'Admin'),
    (N'ava.chen@healthvault.test', N'Staff'),
    (N'marco.diaz@healthvault.test', N'Doctor'),
    (N'priya.nair@healthvault.test', N'Nurse'),
    (N'priya.nair@healthvault.test', N'Staff'),
    (N'elena.singh@healthvault.test', N'Admin'),
    (N'elena.singh@healthvault.test', N'Doctor'),
    (N'chris.owens@healthvault.test', N'Nurse'),
    (N'chris.owens@healthvault.test', N'Staff'),
    (N'taylor.brooks@healthvault.test', N'Admin'),
    (N'taylor.brooks@healthvault.test', N'Staff'),
    (N'quinn.reed@healthvault.test', N'Admin'),
    (N'quinn.reed@healthvault.test', N'Staff'),
    (N'healthtest797@gmail.com', N'Patient'),
    (N'sofia.martinez@healthvault.test', N'Doctor'),
    (N'james.kim@healthvault.test', N'Doctor'),
    (N'amara.okoye@healthvault.test', N'Doctor'),
    (N'noah.bennett@healthvault.test', N'Doctor'),
    (N'isla.rahman@healthvault.test', N'Doctor'),
    (N'lucas.nguyen@healthvault.test', N'Doctor'),
    (N'mia.patel@healthvault.test', N'Doctor'),
    (N'daniel.costa@healthvault.test', N'Doctor'),
    (N'hannah.walsh@healthvault.test', N'Nurse'),
    (N'hannah.walsh@healthvault.test', N'Staff'),
    (N'omar.hassan@healthvault.test', N'Nurse'),
    (N'omar.hassan@healthvault.test', N'Staff'),
    (N'grace.thornton@healthvault.test', N'Staff'),
    (N'leo.morrison@healthvault.test', N'Staff'),
    (N'nina.park@healthvault.test', N'Nurse'),
    (N'nina.park@healthvault.test', N'Staff'),
    (N'victor.almeida@healthvault.test', N'Staff');

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

DECLARE @Patients TABLE
(
    [AbhaId]       nvarchar(20),
    [FirstName]    nvarchar(100),
    [LastName]     nvarchar(100),
    [DateOfBirth]  date,
    [Gender]       nvarchar(20),
    [Email]        nvarchar(256),
    [MobileNumber] nvarchar(20)
);

INSERT INTO @Patients
    ([AbhaId], [FirstName], [LastName], [DateOfBirth], [Gender], [Email], [MobileNumber])
VALUES
    (N'12-3456-7890-1234', N'Jordan', N'Lee', '1992-04-18', N'Man', N'healthtest797@gmail.com', N'+1-555-0104'),
    (N'98-7654-3210-9876', N'Sam', N'Patel', '1988-11-02', N'Women', N'sam.patel@healthvault.test', N'+1-555-0148'),
    (N'45-6123-7890-4567', N'Riley', N'Nguyen', '2001-07-25', N'Transgender', N'riley.nguyen@healthvault.test', N'+1-555-0199');

-- Ensure patient emails have People login rows before inserting Patients.PersonId.
INSERT INTO [dbo].[People]
(
    [FirstName], [LastName], [Email], [Password], [MustChangePassword], [Gender]
)
SELECT
    source.[FirstName],
    source.[LastName],
    source.[Email],
    N'Password@1',
    1,
    source.[Gender]
FROM @Patients AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People] AS existing
    WHERE existing.[Email] = source.[Email]
);

INSERT INTO [dbo].[UserClaims] ([PersonId], [Role])
SELECT person.[Id], N'Patient'
FROM @Patients AS source
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[UserClaims] AS existing
    WHERE existing.[PersonId] = person.[Id]
      AND existing.[Role] = N'Patient'
);

INSERT INTO [dbo].[Patients]
(
    [PersonId],
    [AbhaId],
    [DateOfBirth],
    [MobileNumber]
)
SELECT
    person.[Id],
    source.[AbhaId],
    source.[DateOfBirth],
    source.[MobileNumber]
FROM @Patients AS source
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Patients] AS existing
    WHERE existing.[PersonId] = person.[Id]
       OR existing.[AbhaId] = source.[AbhaId]
);
GO

-- 20 additional demo patients (demographics only unless already present).
DECLARE @MorePatients TABLE
(
    [AbhaId]       nvarchar(20),
    [FirstName]    nvarchar(100),
    [LastName]     nvarchar(100),
    [DateOfBirth]  date,
    [Gender]       nvarchar(20),
    [Email]        nvarchar(256),
    [MobileNumber] nvarchar(20)
);

INSERT INTO @MorePatients
    ([AbhaId], [FirstName], [LastName], [DateOfBirth], [Gender], [Email], [MobileNumber])
VALUES
    (N'11-1001-2001-3001', N'Aiden', N'Brooks', '1990-01-12', N'Man', N'aiden.brooks@healthvault.test', N'+1-555-1001'),
    (N'11-1002-2002-3002', N'Bella', N'Carter', '1987-03-22', N'Women', N'bella.carter@healthvault.test', N'+1-555-1002'),
    (N'11-1003-2003-3003', N'Caleb', N'Davis', '1995-07-08', N'Man', N'caleb.davis@healthvault.test', N'+1-555-1003'),
    (N'11-1004-2004-3004', N'Diana', N'Evans', '1993-11-15', N'Women', N'diana.evans@healthvault.test', N'+1-555-1004'),
    (N'11-1005-2005-3005', N'Ethan', N'Foster', '1985-05-30', N'Man', N'ethan.foster@healthvault.test', N'+1-555-1005'),
    (N'11-1006-2006-3006', N'Fiona', N'Garcia', '1998-09-03', N'Women', N'fiona.garcia@healthvault.test', N'+1-555-1006'),
    (N'11-1007-2007-3007', N'Gavin', N'Hayes', '1991-02-19', N'Man', N'gavin.hayes@healthvault.test', N'+1-555-1007'),
    (N'11-1008-2008-3008', N'Hannah', N'Iyer', '1996-12-01', N'Women', N'hannah.iyer@healthvault.test', N'+1-555-1008'),
    (N'11-1009-2009-3009', N'Isaac', N'Jones', '1989-06-27', N'Man', N'isaac.jones@healthvault.test', N'+1-555-1009'),
    (N'11-1010-2010-3010', N'Julia', N'Khan', '1994-04-14', N'Women', N'julia.khan@healthvault.test', N'+1-555-1010'),
    (N'11-1011-2011-3011', N'Kevin', N'Lopez', '1992-08-21', N'Man', N'kevin.lopez@healthvault.test', N'+1-555-1011'),
    (N'11-1012-2012-3012', N'Lila', N'Morris', '1997-10-09', N'Women', N'lila.morris@healthvault.test', N'+1-555-1012'),
    (N'11-1013-2013-3013', N'Mason', N'Nguyen', '1986-01-25', N'Man', N'mason.nguyen@healthvault.test', N'+1-555-1013'),
    (N'11-1014-2014-3014', N'Nora', N'Ortiz', '1999-03-17', N'Women', N'nora.ortiz@healthvault.test', N'+1-555-1014'),
    (N'11-1015-2015-3015', N'Owen', N'Patel', '1990-07-05', N'Man', N'owen.patel@healthvault.test', N'+1-555-1015'),
    (N'11-1016-2016-3016', N'Priya', N'Quinn', '1988-12-29', N'Women', N'priya.quinn@healthvault.test', N'+1-555-1016'),
    (N'11-1017-2017-3017', N'Ryan', N'Reed', '1993-05-11', N'Man', N'ryan.reed@healthvault.test', N'+1-555-1017'),
    (N'11-1018-2018-3018', N'Sara', N'Singh', '1995-09-23', N'Women', N'sara.singh@healthvault.test', N'+1-555-1018'),
    (N'11-1019-2019-3019', N'Tyler', N'Turner', '1991-11-07', N'Man', N'tyler.turner@healthvault.test', N'+1-555-1019'),
    (N'11-1020-2020-3020', N'Uma', N'Vargas', '1996-02-28', N'Transgender', N'uma.vargas@healthvault.test', N'+1-555-1020');

INSERT INTO [dbo].[People]
(
    [FirstName], [LastName], [Email], [Password], [MustChangePassword], [Gender]
)
SELECT
    source.[FirstName],
    source.[LastName],
    source.[Email],
    N'Password@1',
    1,
    source.[Gender]
FROM @MorePatients AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[People] AS existing
    WHERE existing.[Email] = source.[Email]
);

INSERT INTO [dbo].[UserClaims] ([PersonId], [Role])
SELECT person.[Id], N'Patient'
FROM @MorePatients AS source
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[UserClaims] AS existing
    WHERE existing.[PersonId] = person.[Id]
      AND existing.[Role] = N'Patient'
);

INSERT INTO [dbo].[Patients]
(
    [PersonId], [AbhaId], [DateOfBirth], [MobileNumber]
)
SELECT
    person.[Id],
    source.[AbhaId],
    source.[DateOfBirth],
    source.[MobileNumber]
FROM @MorePatients AS source
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Patients] AS existing
    WHERE existing.[PersonId] = person.[Id]
       OR existing.[AbhaId] = source.[AbhaId]
);
GO

DECLARE @Providers TABLE
(
    [ProviderCode] nvarchar(20),
    [Name]         nvarchar(200),
    [ProviderType] nvarchar(50),
    [Address]      nvarchar(256),
    [City]         nvarchar(100),
    [State]        nvarchar(50),
    [PostalCode]   nvarchar(20),
    [Phone]        nvarchar(30),
    [Email]        nvarchar(256),
    [IsActive]     bit
);

INSERT INTO @Providers
(
    [ProviderCode], [Name], [ProviderType], [Address], [City], [State],
    [PostalCode], [Phone], [Email], [IsActive]
)
VALUES
    (N'HV-MAIN', N'HealthVault General Hospital', N'Hospital', N'100 Care Way', N'Austin', N'TX', N'78701', N'+1-512-555-0100', N'info@hvgeneral.test', 1),
    (N'HV-NORTH', N'HealthVault North Clinic', N'Clinic', N'250 Wellness Blvd', N'Austin', N'TX', N'78758', N'+1-512-555-0110', N'north@hvclinic.test', 1),
    (N'HV-LAB', N'HealthVault Diagnostics Lab', N'Lab', N'88 Sample Street', N'Round Rock', N'TX', N'78664', N'+1-512-555-0120', N'lab@hvdiag.test', 1),
    (N'HV-RX', N'HealthVault Pharmacy', N'Pharmacy', N'12 Remedy Road', N'Austin', N'TX', N'78702', N'+1-512-555-0130', N'pharmacy@hvrx.test', 1);

INSERT INTO [dbo].[Provider]
(
    [ProviderCode], [Name], [ProviderType], [Address], [City], [State],
    [PostalCode], [Phone], [Email], [IsActive]
)
SELECT
    source.[ProviderCode],
    source.[Name],
    source.[ProviderType],
    source.[Address],
    source.[City],
    source.[State],
    source.[PostalCode],
    source.[Phone],
    source.[Email],
    source.[IsActive]
FROM @Providers AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Provider] AS existing
    WHERE existing.[ProviderCode] = source.[ProviderCode]
);
GO

DECLARE @Staff TABLE
(
    [StaffCode]    nvarchar(20),
    [ProviderCode] nvarchar(20),
    [FirstName]    nvarchar(100),
    [LastName]     nvarchar(100),
    [Email]        nvarchar(256),
    [Role]         nvarchar(50),
    [Specialty]    nvarchar(100),
    [LicenseNumber] nvarchar(50),
    [Phone]        nvarchar(30),
    [IsActive]     bit
);

INSERT INTO @Staff
(
    [StaffCode], [ProviderCode], [FirstName], [LastName], [Email],
    [Role], [Specialty], [LicenseNumber], [Phone], [IsActive]
)
VALUES
    (N'DOC-001', N'HV-MAIN', N'Marco', N'Diaz', N'marco.diaz@healthvault.test', N'Doctor', N'Internal Medicine', N'TX-MD-10021', N'+1-512-555-0201', 1),
    (N'DOC-002', N'HV-NORTH', N'Elena', N'Singh', N'elena.singh@healthvault.test', N'Doctor', N'Pediatrics', N'TX-MD-10045', N'+1-512-555-0202', 1),
    (N'DOC-003', N'HV-MAIN', N'Sofia', N'Martinez', N'sofia.martinez@healthvault.test', N'Doctor', N'Cardiology', N'TX-MD-10110', N'+1-512-555-0203', 1),
    (N'DOC-004', N'HV-MAIN', N'James', N'Kim', N'james.kim@healthvault.test', N'Doctor', N'Orthopedics', N'TX-MD-10122', N'+1-512-555-0204', 1),
    (N'DOC-005', N'HV-MAIN', N'Amara', N'Okoye', N'amara.okoye@healthvault.test', N'Doctor', N'Emergency Medicine', N'TX-MD-10133', N'+1-512-555-0205', 1),
    (N'DOC-006', N'HV-NORTH', N'Noah', N'Bennett', N'noah.bennett@healthvault.test', N'Doctor', N'Family Medicine', N'TX-MD-10144', N'+1-512-555-0206', 1),
    (N'DOC-007', N'HV-NORTH', N'Isla', N'Rahman', N'isla.rahman@healthvault.test', N'Doctor', N'Dermatology', N'TX-MD-10155', N'+1-512-555-0207', 1),
    (N'DOC-008', N'HV-LAB', N'Lucas', N'Nguyen', N'lucas.nguyen@healthvault.test', N'Doctor', N'Pathology', N'TX-MD-10166', N'+1-512-555-0208', 1),
    (N'DOC-009', N'HV-MAIN', N'Mia', N'Patel', N'mia.patel@healthvault.test', N'Doctor', N'Neurology', N'TX-MD-10177', N'+1-512-555-0209', 1),
    (N'DOC-010', N'HV-RX', N'Daniel', N'Costa', N'daniel.costa@healthvault.test', N'Doctor', N'Clinical Pharmacy', N'TX-MD-10188', N'+1-512-555-0210', 1),
    (N'NUR-001', N'HV-MAIN', N'Priya', N'Nair', N'priya.nair@healthvault.test', N'Nurse', N'Emergency Care', N'TX-RN-22011', N'+1-512-555-0211', 1),
    (N'NUR-002', N'HV-NORTH', N'Chris', N'Owens', N'chris.owens@healthvault.test', N'Nurse', N'Outpatient Care', N'TX-RN-22033', N'+1-512-555-0212', 1),
    (N'NUR-003', N'HV-MAIN', N'Hannah', N'Walsh', N'hannah.walsh@healthvault.test', N'Nurse', N'Medical-Surgical', N'TX-RN-22044', N'+1-512-555-0213', 1),
    (N'NUR-004', N'HV-MAIN', N'Omar', N'Hassan', N'omar.hassan@healthvault.test', N'Nurse', N'ICU', N'TX-RN-22055', N'+1-512-555-0214', 1),
    (N'NUR-005', N'HV-NORTH', N'Nina', N'Park', N'nina.park@healthvault.test', N'Nurse', N'Pediatrics', N'TX-RN-22066', N'+1-512-555-0215', 1),
    (N'STF-001', N'HV-MAIN', N'Ava', N'Chen', N'ava.chen@healthvault.test', N'Staff', N'Administration', NULL, N'+1-512-555-0221', 1),
    (N'STF-002', N'HV-LAB', N'Taylor', N'Brooks', N'taylor.brooks@healthvault.test', N'Staff', N'Lab Support', NULL, N'+1-512-555-0222', 1),
    (N'STF-003', N'HV-RX', N'Quinn', N'Reed', N'quinn.reed@healthvault.test', N'Staff', N'Pharmacy Admin', NULL, N'+1-512-555-0223', 1),
    (N'STF-004', N'HV-MAIN', N'Grace', N'Thornton', N'grace.thornton@healthvault.test', N'Staff', N'Registration', NULL, N'+1-512-555-0224', 1),
    (N'STF-005', N'HV-NORTH', N'Leo', N'Morrison', N'leo.morrison@healthvault.test', N'Staff', N'Clinic Support', NULL, N'+1-512-555-0225', 1),
    (N'STF-006', N'HV-LAB', N'Victor', N'Almeida', N'victor.almeida@healthvault.test', N'Staff', N'Lab Operations', NULL, N'+1-512-555-0226', 1);

-- All demo staff already have People rows seeded above; this join only
-- resolves PersonId/HealthcareProviderId, no names/emails go onto dbo.Staff.
INSERT INTO [dbo].[Staff]
(
    [PersonId],
    [StaffCode],
    [HealthcareProviderId],
    [Specialty],
    [LicenseNumber],
    [Phone],
    [IsActive]
)
SELECT
    person.[Id],
    source.[StaffCode],
    provider.[Id],
    source.[Specialty],
    source.[LicenseNumber],
    source.[Phone],
    source.[IsActive]
FROM @Staff AS source
INNER JOIN [dbo].[Provider] AS provider
    ON provider.[ProviderCode] = source.[ProviderCode]
INNER JOIN [dbo].[People] AS person
    ON person.[Email] = source.[Email]
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[Staff] AS existing
    WHERE existing.[StaffCode] = source.[StaffCode]
       OR existing.[PersonId] = person.[Id]
);
GO

IF OBJECT_ID(N'dbo.Staff', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Staff', N'IsActive') IS NOT NULL
BEGIN
    UPDATE [dbo].[Staff]
    SET [IsActive] = 1
    WHERE [IsActive] <> 1;
END;
GO

PRINT N'HealthVault sample data setup complete.';

SELECT
    person.[Id],
    person.[FirstName],
    person.[LastName],
    person.[Email],
    person.[Gender],
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
    person.[Gender],
    person.[MustChangePassword]
ORDER BY person.[Id];

SELECT
    patient.[Id],
    patient.[PersonId],
    person.[FirstName],
    person.[LastName],
    person.[Email],
    person.[Gender],
    patient.[AbhaId],
    patient.[AadhaarNumber],
    patient.[DateOfBirth],
    patient.[MobileNumber]
FROM [dbo].[Patients] AS patient
INNER JOIN [dbo].[People] AS person
    ON person.[Id] = patient.[PersonId]
ORDER BY patient.[Id];

SELECT
    provider.[Id],
    provider.[ProviderCode],
    provider.[Name],
    provider.[ProviderType],
    provider.[City],
    provider.[IsActive]
FROM [dbo].[Provider] AS provider
ORDER BY provider.[Id];

SELECT
    staff.[Id],
    staff.[StaffCode],
    staff.[PersonId],
    person.[Email] AS [PersonEmail],
    person.[FirstName],
    person.[LastName],
    person.[Gender],
    staff.[HealthcareProviderId],
    provider.[Name] AS [ProviderName],
    STRING_AGG(claim.[Role], N', ')
        WITHIN GROUP (ORDER BY claim.[Role]) AS [Roles],
    staff.[Specialty],
    staff.[IsActive]
FROM [dbo].[Staff] AS staff
INNER JOIN [dbo].[Provider] AS provider
    ON provider.[Id] = staff.[HealthcareProviderId]
INNER JOIN [dbo].[People] AS person
    ON person.[Id] = staff.[PersonId]
LEFT JOIN [dbo].[UserClaims] AS claim
    ON claim.[PersonId] = person.[Id]
GROUP BY
    staff.[Id],
    staff.[StaffCode],
    staff.[PersonId],
    person.[Email],
    person.[FirstName],
    person.[LastName],
    person.[Gender],
    staff.[HealthcareProviderId],
    provider.[Name],
    staff.[Specialty],
    staff.[IsActive]
ORDER BY person.[LastName], person.[FirstName];
GO

/* ============================================================================
   LOINC reference codes (common vitals + labs)
   Full LOINC catalog is licensed from loinc.org; this is a starter subset.
   ============================================================================ */

IF OBJECT_ID(N'dbo.LoincCodes', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.LoincCodes is missing. Run SetupHealthVault.sql first.', 16, 1);
    RETURN;
END;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

MERGE [dbo].[LoincCodes] AS target
USING
(
    VALUES
    -- Vitals
    (N'8302-2',  N'Body height',                    N'Len',   N'Pt', N'^Patient',           N'Qn', NULL, N'CLIN', N'Body height',              N'Body height',                                      N'ACTIVE', 2, N'cm'),
    (N'29463-7', N'Body weight',                    N'Mass',  N'Pt', N'^Patient',           N'Qn', NULL, N'CLIN', N'Weight',                   N'Body weight',                                      N'ACTIVE', 2, N'kg'),
    (N'39156-5', N'Body mass index (BMI)',          N'Ratio', N'Pt', N'^Patient',           N'Qn', N'Calculated', N'CLIN', N'BMI', N'Body mass index (BMI) [Ratio]', N'ACTIVE', 2, N'kg/m2'),
    (N'8480-6',  N'Systolic blood pressure',        N'Pres',  N'Pt', N'Arterial system',    N'Qn', NULL, N'CLIN', N'Systolic BP',              N'Systolic blood pressure',                          N'ACTIVE', 2, N'mm[Hg]'),
    (N'8462-4',  N'Diastolic blood pressure',       N'Pres',  N'Pt', N'Arterial system',    N'Qn', NULL, N'CLIN', N'Diastolic BP',             N'Diastolic blood pressure',                         N'ACTIVE', 2, N'mm[Hg]'),
    (N'8867-4',  N'Heart rate',                     N'NRat',  N'Pt', N'Heart',              N'Qn', NULL, N'CLIN', N'Heart rate',               N'Heart rate',                                       N'ACTIVE', 2, N'/min'),
    (N'9279-1',  N'Respiratory rate',               N'NRat',  N'Pt', N'Respiratory system', N'Qn', NULL, N'CLIN', N'Resp rate',                N'Respiratory rate',                                 N'ACTIVE', 2, N'/min'),
    (N'8310-5',  N'Body temperature',               N'Temp',  N'Pt', N'^Patient',           N'Qn', NULL, N'CLIN', N'Body temp',                N'Body temperature',                                 N'ACTIVE', 2, N'Cel'),
    (N'59408-5', N'Oxygen saturation in Arterial blood by Pulse oximetry', N'MFr', N'Pt', N'BldA', N'Qn', N'Pulse oximetry', N'CLIN', N'SpO2', N'Oxygen saturation in Arterial blood by Pulse oximetry', N'ACTIVE', 2, N'%'),
    (N'72514-3', N'Pain severity',                  N'Score', N'Pt', N'^Patient',           N'Qn', NULL, N'SURVEY.GNHLTH', N'Pain score 0-10', N'Pain severity [Score] Visual analog score', N'ACTIVE', 2, N'{score}'),

    -- Hematology
    (N'718-7',   N'Hemoglobin',                     N'MCnc',  N'Pt', N'Bld',                N'Qn', NULL, N'HEM/BC', N'Hgb Bld-mCnc',             N'Hemoglobin [Mass/volume] in Blood',                N'ACTIVE', 1, N'g/dL'),
    (N'4544-3',  N'Hematocrit',                     N'VFr',   N'Pt', N'Bld',                N'Qn', N'Automated count', N'HEM/BC', N'Hct VFr Bld Auto', N'Hematocrit [Volume Fraction] of Blood by Automated count', N'ACTIVE', 1, N'%'),
    (N'789-8',   N'Erythrocytes',                   N'NCnc',  N'Pt', N'Bld',                N'Qn', N'Automated count', N'HEM/BC', N'RBC # Bld Auto', N'Erythrocytes [#/volume] in Blood by Automated count', N'ACTIVE', 1, N'10*6/uL'),
    (N'6690-2',  N'Leukocytes',                     N'NCnc',  N'Pt', N'Bld',                N'Qn', N'Automated count', N'HEM/BC', N'WBC # Bld Auto', N'Leukocytes [#/volume] in Blood by Automated count', N'ACTIVE', 1, N'10*3/uL'),
    (N'777-3',   N'Platelets',                      N'NCnc',  N'Pt', N'Bld',                N'Qn', N'Automated count', N'HEM/BC', N'Platelet # Bld Auto', N'Platelets [#/volume] in Blood by Automated count', N'ACTIVE', 1, N'10*3/uL'),
    (N'787-2',   N'Erythrocyte mean corpuscular volume', N'EntVol', N'Pt', N'RBC',        N'Qn', N'Automated count', N'HEM/BC', N'MCV', N'MCV [Entitic volume] by Automated count', N'ACTIVE', 1, N'fL'),
    (N'785-6',   N'Erythrocyte mean corpuscular hemoglobin', N'EntMass', N'Pt', N'RBC',  N'Qn', N'Automated count', N'HEM/BC', N'MCH', N'MCH [Entitic mass] by Automated count', N'ACTIVE', 1, N'pg'),
    (N'786-4',   N'Erythrocyte mean corpuscular hemoglobin concentration', N'MCnc', N'Pt', N'RBC', N'Qn', N'Automated count', N'HEM/BC', N'MCHC', N'MCHC [Mass/volume] by Automated count', N'ACTIVE', 1, N'g/dL'),

    -- Chemistry
    (N'2345-7',  N'Glucose',                        N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Glucose SerPl-mCnc',       N'Glucose [Mass/volume] in Serum or Plasma',         N'ACTIVE', 1, N'mg/dL'),
    (N'2160-0',  N'Creatinine',                     N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Creatinine SerPl-mCnc',    N'Creatinine [Mass/volume] in Serum or Plasma',      N'ACTIVE', 1, N'mg/dL'),
    (N'3094-0',  N'Urea nitrogen',                  N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'BUN SerPl-mCnc',           N'Urea nitrogen [Mass/volume] in Serum or Plasma',   N'ACTIVE', 1, N'mg/dL'),
    (N'1751-7',  N'Albumin',                        N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Albumin SerPl-mCnc',       N'Albumin [Mass/volume] in Serum or Plasma',         N'ACTIVE', 1, N'g/dL'),
    (N'2885-2',  N'Protein',                        N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Protein SerPl-mCnc',       N'Protein [Mass/volume] in Serum or Plasma',         N'ACTIVE', 1, N'g/dL'),
    (N'1975-2',  N'Bilirubin',                      N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Bilirub SerPl-mCnc',       N'Bilirubin.total [Mass/volume] in Serum or Plasma', N'ACTIVE', 1, N'mg/dL'),
    (N'1742-6',  N'Alanine aminotransferase',       N'CCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'ALT SerPl-cCnc',           N'Alanine aminotransferase [Enzymatic activity/volume] in Serum or Plasma', N'ACTIVE', 1, N'U/L'),
    (N'1920-8',  N'Aspartate aminotransferase',     N'CCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'AST SerPl-cCnc',           N'Aspartate aminotransferase [Enzymatic activity/volume] in Serum or Plasma', N'ACTIVE', 1, N'U/L'),
    (N'6768-6',  N'Alkaline phosphatase',           N'CCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'ALP SerPl-cCnc',           N'Alkaline phosphatase [Enzymatic activity/volume] in Serum or Plasma', N'ACTIVE', 1, N'U/L'),
    (N'2324-2',  N'Gamma glutamyl transferase',     N'CCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'GGT SerPl-cCnc',           N'Gamma glutamyl transferase [Enzymatic activity/volume] in Serum or Plasma', N'ACTIVE', 1, N'U/L'),

    -- Electrolytes
    (N'2951-2',  N'Sodium',                         N'SCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Sodium SerPl-sCnc',        N'Sodium [Moles/volume] in Serum or Plasma',         N'ACTIVE', 1, N'mmol/L'),
    (N'2823-3',  N'Potassium',                      N'SCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Potassium SerPl-sCnc',     N'Potassium [Moles/volume] in Serum or Plasma',      N'ACTIVE', 1, N'mmol/L'),
    (N'2075-0',  N'Chloride',                       N'SCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Chloride SerPl-sCnc',      N'Chloride [Moles/volume] in Serum or Plasma',       N'ACTIVE', 1, N'mmol/L'),
    (N'2028-9',  N'Carbon dioxide',                 N'SCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'CO2 SerPl-sCnc',           N'Carbon dioxide, total [Moles/volume] in Serum or Plasma', N'ACTIVE', 1, N'mmol/L'),
    (N'17861-6', N'Calcium',                        N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Calcium SerPl-mCnc',       N'Calcium [Mass/volume] in Serum or Plasma',         N'ACTIVE', 1, N'mg/dL'),
    (N'19123-9', N'Magnesium',                      N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Magnesium SerPl-mCnc',     N'Magnesium [Mass/volume] in Serum or Plasma',       N'ACTIVE', 1, N'mg/dL'),

    -- Lipids / diabetes
    (N'2093-3',  N'Cholesterol',                    N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Cholest SerPl-mCnc',       N'Cholesterol [Mass/volume] in Serum or Plasma',     N'ACTIVE', 1, N'mg/dL'),
    (N'2085-9',  N'Cholesterol in HDL',             N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'HDLc SerPl-mCnc',          N'Cholesterol in HDL [Mass/volume] in Serum or Plasma', N'ACTIVE', 1, N'mg/dL'),
    (N'2089-1',  N'Cholesterol in LDL',             N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'LDLc SerPl-mCnc',          N'Cholesterol in LDL [Mass/volume] in Serum or Plasma', N'ACTIVE', 1, N'mg/dL'),
    (N'2571-8',  N'Triglyceride',                   N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'Triglyc SerPl-mCnc',       N'Triglyceride [Mass/volume] in Serum or Plasma',    N'ACTIVE', 1, N'mg/dL'),
    (N'4548-4',  N'Hemoglobin A1c',                 N'MFr',   N'Pt', N'Bld',                N'Qn', NULL, N'HEM/BC', N'HbA1c MFr Bld',          N'Hemoglobin A1c/Hemoglobin.total in Blood',         N'ACTIVE', 1, N'%'),

    -- Urinalysis / other
    (N'5804-0',  N'Protein',                        N'MCnc',  N'Pt', N'Urine',              N'Qn', NULL, N'UA', N'Protein Ur-mCnc',             N'Protein [Mass/volume] in Urine',                   N'ACTIVE', 1, N'mg/dL'),
    (N'5792-7',  N'Glucose',                        N'MCnc',  N'Pt', N'Urine',              N'Qn', N'Test strip', N'UA', N'Glucose Ur Strip', N'Glucose [Mass/volume] in Urine by Test strip', N'ACTIVE', 1, N'mg/dL'),
    (N'20454-5', N'Protein',                        N'MRat',  N'24H', N'Urine',             N'Qn', NULL, N'CHEM', N'Protein 24h Ur-mRate',     N'Protein [Mass/time] in 24 hour Urine',             N'ACTIVE', 1, N'g/(24.h)'),
    (N'11580-8', N'Thyrotropin',                    N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'TSH SerPl-mCnc',           N'Thyrotropin [Mass/volume] in Serum or Plasma',     N'ACTIVE', 1, N'm[IU]/L'),
    (N'3016-3',  N'Thyroxine (T4)',                 N'MCnc',  N'Pt', N'Ser/Plas',           N'Qn', NULL, N'CHEM', N'T4 SerPl-mCnc',            N'Thyroxine (T4) [Mass/volume] in Serum or Plasma',  N'ACTIVE', 1, N'ug/dL')
)
AS source
(
    [LoincNum], [Component], [Property], [TimeAspct], [System], [ScaleTyp], [MethodTyp],
    [Class], [ShortName], [LongCommonName], [Status], [ClassType], [ExampleUnits]
)
ON target.[LoincNum] = source.[LoincNum]
WHEN MATCHED THEN
    UPDATE SET
        target.[Component] = source.[Component],
        target.[Property] = source.[Property],
        target.[TimeAspct] = source.[TimeAspct],
        target.[System] = source.[System],
        target.[ScaleTyp] = source.[ScaleTyp],
        target.[MethodTyp] = source.[MethodTyp],
        target.[Class] = source.[Class],
        target.[ShortName] = source.[ShortName],
        target.[LongCommonName] = source.[LongCommonName],
        target.[Status] = source.[Status],
        target.[ClassType] = source.[ClassType],
        target.[ExampleUnits] = source.[ExampleUnits]
WHEN NOT MATCHED THEN
    INSERT
    (
        [LoincNum], [Component], [Property], [TimeAspct], [System], [ScaleTyp], [MethodTyp],
        [Class], [ShortName], [LongCommonName], [Status], [ClassType], [ExampleUnits]
    )
    VALUES
    (
        source.[LoincNum], source.[Component], source.[Property], source.[TimeAspct], source.[System],
        source.[ScaleTyp], source.[MethodTyp], source.[Class], source.[ShortName],
        source.[LongCommonName], source.[Status], source.[ClassType], source.[ExampleUnits]
    );
GO

SELECT
    [Id],
    [LoincNum],
    [ShortName],
    [LongCommonName],
    [Class],
    [ExampleUnits],
    [Status]
FROM [dbo].[LoincCodes]
ORDER BY [Class], [LoincNum];
GO

/* ============================================================================
   Patient observation mock data — Glucose (LOINC 2345-7)
   ============================================================================ */

IF OBJECT_ID(N'dbo.PatientData', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PatientData is missing. Run SetupHealthVault.sql first.', 16, 1);
    RETURN;
END;
GO

DECLARE @GlucoseLoincId int =
(
    SELECT [Id]
    FROM [dbo].[LoincCodes]
    WHERE [LoincNum] = N'2345-7'
);

IF @GlucoseLoincId IS NULL
BEGIN
    RAISERROR(N'Glucose LOINC 2345-7 was not found in dbo.LoincCodes.', 16, 1);
    RETURN;
END;

DECLARE @JordanPatientId int =
(
    SELECT patient.[Id]
    FROM [dbo].[Patients] AS patient
    INNER JOIN [dbo].[People] AS person
        ON person.[Id] = patient.[PersonId]
    WHERE person.[Email] = N'healthtest797@gmail.com'
);

DECLARE @SamPatientId int =
(
    SELECT patient.[Id]
    FROM [dbo].[Patients] AS patient
    INNER JOIN [dbo].[People] AS person
        ON person.[Id] = patient.[PersonId]
    WHERE person.[Email] = N'sam.patel@healthvault.test'
);

DECLARE @GlucoseRows TABLE
(
    [PatientId]  int,
    [Value]      nvarchar(100),
    [Units]      nvarchar(50),
    [ObservedAt] datetime2,
    [Notes]      nvarchar(500)
);

IF @JordanPatientId IS NOT NULL
BEGIN
    INSERT INTO @GlucoseRows ([PatientId], [Value], [Units], [ObservedAt], [Notes])
    VALUES
        (@JordanPatientId, N'98',  N'mg/dL', DATEADD(DAY, -14, SYSUTCDATETIME()), N'Fasting glucose'),
        (@JordanPatientId, N'112', N'mg/dL', DATEADD(DAY, -7, SYSUTCDATETIME()),  N'Random glucose'),
        (@JordanPatientId, N'105', N'mg/dL', DATEADD(DAY, -1, SYSUTCDATETIME()),  N'Fasting glucose');
END;

IF @SamPatientId IS NOT NULL
BEGIN
    INSERT INTO @GlucoseRows ([PatientId], [Value], [Units], [ObservedAt], [Notes])
    VALUES
        (@SamPatientId, N'126', N'mg/dL', DATEADD(DAY, -10, SYSUTCDATETIME()), N'Fasting glucose'),
        (@SamPatientId, N'141', N'mg/dL', DATEADD(DAY, -3, SYSUTCDATETIME()),  N'Postprandial glucose');
END;

INSERT INTO [dbo].[PatientData]
(
    [PatientId],
    [LoincCodeId],
    [Value],
    [Units],
    [ObservedAt],
    [Notes]
)
SELECT
    source.[PatientId],
    @GlucoseLoincId,
    source.[Value],
    source.[Units],
    source.[ObservedAt],
    source.[Notes]
FROM @GlucoseRows AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM [dbo].[PatientData] AS existing
    WHERE existing.[PatientId] = source.[PatientId]
      AND existing.[LoincCodeId] = @GlucoseLoincId
      AND existing.[ObservedAt] = source.[ObservedAt]
      AND existing.[Value] = source.[Value]
);
GO

SELECT
    data.[Id],
    patient.[AbhaId],
    person.[FirstName],
    person.[LastName],
    loinc.[LoincNum],
    loinc.[ShortName],
    data.[Value],
    data.[Units],
    data.[ObservedAt],
    data.[Notes]
FROM [dbo].[PatientData] AS data
INNER JOIN [dbo].[Patients] AS patient
    ON patient.[Id] = data.[PatientId]
INNER JOIN [dbo].[People] AS person
    ON person.[Id] = patient.[PersonId]
INNER JOIN [dbo].[LoincCodes] AS loinc
    ON loinc.[Id] = data.[LoincCodeId]
ORDER BY person.[LastName], data.[ObservedAt] DESC;
GO
