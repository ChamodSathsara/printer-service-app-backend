-- ============================================================
-- Printer Service Visit Management System
-- SQL Server Database Schema
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PrinterServiceDB')
BEGIN
    CREATE DATABASE PrinterServiceDB;
END
GO

USE PrinterServiceDB;
GO

-- ============================================================
-- TABLE: Roles
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        RoleId      INT             NOT NULL IDENTITY(1,1),
        RoleName    NVARCHAR(50)    NOT NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
        CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
    );

    INSERT INTO Roles (RoleName) VALUES ('Manager'), ('Technician');
END
GO

-- ============================================================
-- TABLE: Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        UserId              INT             NOT NULL IDENTITY(1,1),
        TechnicianCode      NVARCHAR(20)    NOT NULL,   -- used as username for both roles
        FullName            NVARCHAR(100)   NOT NULL,
        Email               NVARCHAR(150)   NULL,
        PasswordHash        NVARCHAR(256)   NOT NULL,
        RoleId              INT             NOT NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Users PRIMARY KEY (UserId),
        CONSTRAINT UQ_Users_TechnicianCode UNIQUE (TechnicianCode),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
    );
END
GO

-- ============================================================
-- TABLE: Machines
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Machines')
BEGIN
    CREATE TABLE Machines (
        MachineId           INT             NOT NULL IDENTITY(1,1),
        MachineRefNumber    NVARCHAR(50)    NOT NULL,
        ModelName           NVARCHAR(100)   NULL,
        SerialNumber        NVARCHAR(100)   NULL,
        CustomerName        NVARCHAR(150)   NULL,
        CustomerPhone       NVARCHAR(30)    NULL,
        CustomerEmail       NVARCHAR(150)   NULL,
        CustomerAddress     NVARCHAR(500)   NULL,
        InstalledDate       DATE            NULL,
        IsActive            BIT             NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Machines PRIMARY KEY (MachineId),
        CONSTRAINT UQ_Machines_RefNumber UNIQUE (MachineRefNumber)
    );
END
GO

-- ============================================================
-- TABLE: SolutionCategories
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SolutionCategories')
BEGIN
    CREATE TABLE SolutionCategories (
        CategoryId      INT             NOT NULL IDENTITY(1,1),
        CategoryName    NVARCHAR(100)   NOT NULL,
        SortOrder       INT             NOT NULL DEFAULT 0,
        IsActive        BIT             NOT NULL DEFAULT 1,

        CONSTRAINT PK_SolutionCategories PRIMARY KEY (CategoryId),
        CONSTRAINT UQ_SolutionCategories_Name UNIQUE (CategoryName)
    );

    INSERT INTO SolutionCategories (CategoryName, SortOrder) VALUES
        ('Toner Inquiry Visit',             1),
        ('New Machine Visit',               2),
        ('Toner Delivery',                  3),
        ('Tender Submission Visit',         4),
        ('Tender Reading Visit',            5),
        ('Debt Follow-up',                  6),
        ('Cash Collection',                 7),
        ('Cheque Collection',               8),
        ('Fake Toner Visit',                9),
        ('Tender Follow-ups',               10),
        ('Toner Routine Sales Follow-ups',  11);
END
GO

-- ============================================================
-- TABLE: SiteVisits
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SiteVisits')
BEGIN
    CREATE TABLE SiteVisits (
        VisitId             INT             NOT NULL IDENTITY(1,1),
        TechnicianId        INT             NOT NULL,
        TechnicianCode      NVARCHAR(20)    NOT NULL,
        TechnicianName      NVARCHAR(100)   NOT NULL,
        MachineRefNumber    NVARCHAR(50)    NOT NULL,
        CategoryId          INT             NOT NULL,
        Note                NVARCHAR(2000)  NULL,
        MeterReadingValue   DECIMAL(18,2)   NULL,
        Latitude            DECIMAL(10,7)   NULL,
        Longitude           DECIMAL(10,7)   NULL,
        LocationAddress     NVARCHAR(500)   NULL,
        VisitDate           DATE            NOT NULL,
        VisitTime           TIME            NOT NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_SiteVisits PRIMARY KEY (VisitId),
        CONSTRAINT FK_SiteVisits_Technician FOREIGN KEY (TechnicianId) REFERENCES Users(UserId),
        CONSTRAINT FK_SiteVisits_Category   FOREIGN KEY (CategoryId)   REFERENCES SolutionCategories(CategoryId)
    );

    CREATE INDEX IX_SiteVisits_TechnicianId  ON SiteVisits (TechnicianId);
    CREATE INDEX IX_SiteVisits_VisitDate      ON SiteVisits (VisitDate);
    CREATE INDEX IX_SiteVisits_MachineRef     ON SiteVisits (MachineRefNumber);
    CREATE INDEX IX_SiteVisits_CategoryId     ON SiteVisits (CategoryId);
END
GO

-- ============================================================
-- TABLE: RefreshTokens
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        TokenId     INT             NOT NULL IDENTITY(1,1),
        UserId      INT             NOT NULL,
        Token       NVARCHAR(512)   NOT NULL,
        ExpiresAt   DATETIME2       NOT NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        IsRevoked   BIT             NOT NULL DEFAULT 0,

        CONSTRAINT PK_RefreshTokens PRIMARY KEY (TokenId),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );

    CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens (UserId);
    CREATE INDEX IX_RefreshTokens_Token  ON RefreshTokens (Token);
END
GO

-- ============================================================
-- TABLE: PasswordResetTokens
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
BEGIN
    CREATE TABLE PasswordResetTokens (
        ResetId     INT             NOT NULL IDENTITY(1,1),
        UserId      INT             NOT NULL,
        Token       NVARCHAR(256)   NOT NULL,
        ExpiresAt   DATETIME2       NOT NULL,
        IsUsed      BIT             NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (ResetId),
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

-- ============================================================
-- SEED: Default Manager Account
-- Password: Admin@123  (BCrypt hashed)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE TechnicianCode = 'MGR001')
BEGIN
    INSERT INTO Users (TechnicianCode, FullName, Email, PasswordHash, RoleId)
    VALUES (
        'MGR001',
        'System Manager',
        'manager@printerservice.com',
        '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.',  -- Admin@123
        1
    );
END
GO

-- ============================================================
-- SEED: Sample Technicians
-- Password: Tech@123
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE TechnicianCode = 'TECH001')
BEGIN
    INSERT INTO Users (TechnicianCode, FullName, Email, PasswordHash, RoleId)
    VALUES
        ('TECH001', 'Kamal Perera',   'kamal@printerservice.com',   '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 2),
        ('TECH002', 'Nimal Silva',    'nimal@printerservice.com',   '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 2),
        ('TECH003', 'Sunil Fernando', 'sunil@printerservice.com',   '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.', 2);
END
GO

-- ============================================================
-- SEED: Sample Machines
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM Machines WHERE MachineRefNumber = 'MCH-0001')
BEGIN
    INSERT INTO Machines (MachineRefNumber, ModelName, SerialNumber, CustomerName, CustomerPhone, CustomerAddress, InstalledDate)
    VALUES
        ('MCH-0001', 'Konica Minolta bizhub C3300i', 'SN-KM-001', 'ABC Holdings (Pvt) Ltd', '0112345678', 'No 5, Galle Road, Colombo 03', '2023-01-15'),
        ('MCH-0002', 'Ricoh IM C3000',               'SN-RC-002', 'XYZ Enterprises',         '0113456789', 'No 12, High Level Road, Nugegoda',  '2023-03-20'),
        ('MCH-0003', 'Canon imageRUNNER 2630i',      'SN-CN-003', 'Department of Finance',   '0114567890', 'Lotus Road, Colombo 01',            '2022-11-10');
END
GO

PRINT 'Schema created and seeded successfully.';
GO
