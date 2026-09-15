
CREATE DATABASE BlossomDB;
GO

USE BlossomDB;
GO

-- ============================================================
--                        TABLES
-- ============================================================

-- 1. USERS
CREATE TABLE Users (
    UserID       INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastLoginAt  DATETIME2 NULL
);
GO

-- 2. ACTIVITY_LOGS
CREATE TABLE ActivityLogs (
    LogID      INT IDENTITY(1,1) PRIMARY KEY,
    UserID     INT NOT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    Action     NVARCHAR(50) NOT NULL,
    Detail     NVARCHAR(500) NULL,
    LoggedAt   DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 3. CYCLE_LOGS
CREATE TABLE CycleLogs (
    CycleLogID      INT IDENTITY(1,1) PRIMARY KEY,
    UserID          INT NOT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    LastPeriodDate  DATE NOT NULL,
    CycleLengthDays INT NOT NULL CHECK (CycleLengthDays BETWEEN 15 AND 60),
    PeriodLengthDays INT NOT NULL CHECK (PeriodLengthDays BETWEEN 1 AND 15),
    NextPeriodDate  DATE NOT NULL,
    OvulationDate   DATE NOT NULL,
    FertileStart    DATE NOT NULL,
    FertileEnd      DATE NOT NULL,
    CurrentPhase    NVARCHAR(20) NOT NULL,
    LoggedAt        DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 4. GOODIE_BASKET_ORDERS
CREATE TABLE GoodieBasketOrders (
    OrderID         INT IDENTITY(1,1) PRIMARY KEY,
    UserID          INT NOT NULL FOREIGN KEY REFERENCES Users(UserID) ON DELETE CASCADE,
    FlowType        NVARCHAR(20) NOT NULL,
    PadSelected     NVARCHAR(100) NOT NULL,
    ChocolatesList  NVARCHAR(300) NOT NULL,
    PainkillerName  NVARCHAR(100) NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    PhoneNumber     NVARCHAR(20) NOT NULL,
    HomeAddress     NVARCHAR(300) NOT NULL,
    City            NVARCHAR(100) NOT NULL,
    DeliveryMethod  NVARCHAR(50) NOT NULL,
    PaymentMethod   NVARCHAR(50) NOT NULL,
    DeliveryFeeRs   INT NOT NULL,
    OrderStatus     NVARCHAR(30) NOT NULL DEFAULT 'Placed',
    PlacedAt        DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- 5. SYMPTOMS
CREATE TABLE Symptoms (
    SymptomId    INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    SymptomName  NVARCHAR(50) NOT NULL,
    Severity     INT NOT NULL CHECK (Severity BETWEEN 1 AND 5),
    DateLogged   DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- 6. WATER_INTAKE
CREATE TABLE WaterIntake (
    WaterId          INT IDENTITY(1,1) PRIMARY KEY,
    UserId           INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    GlassesConsumed  INT NOT NULL CHECK (GlassesConsumed >= 0),
    Goal             INT NOT NULL CHECK (Goal > 0),
    DateLogged       DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- 7. REMINDERS
CREATE TABLE Reminders (
    ReminderId    INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    ReminderType  NVARCHAR(50) NOT NULL,
    ReminderDate  DATE NOT NULL,
    Status        NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
--                        INDEXES
-- ============================================================

CREATE INDEX IX_CycleLogs_UserID ON CycleLogs(UserID);
CREATE INDEX IX_Symptoms_UserId_Date ON Symptoms(UserId, DateLogged DESC);
CREATE INDEX IX_WaterIntake_UserId_Date ON WaterIntake(UserId, DateLogged DESC);
CREATE INDEX IX_Reminders_UserId_Status ON Reminders(UserId, Status);
GO

-- ============================================================
--                  AUDIT TABLES (for tracking changes)
-- ============================================================

-- Symptoms Audit
CREATE TABLE Symptoms_Audit (
    AuditId      INT IDENTITY(1,1) PRIMARY KEY,
    SymptomId    INT NOT NULL,
    UserId       INT NOT NULL,
    AuditAction  NVARCHAR(10) NOT NULL,
    OldSeverity  INT NULL,
    NewSeverity  INT NULL,
    ChangedAt    DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Reminders Audit
CREATE TABLE Reminders_Audit (
    AuditId       INT IDENTITY(1,1) PRIMARY KEY,
    ReminderId    INT NOT NULL,
    UserId        INT NOT NULL,
    AuditAction   NVARCHAR(10) NOT NULL,
    OldStatus     NVARCHAR(20) NULL,
    NewStatus     NVARCHAR(20) NULL,
    ChangedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- ============================================================
--                     STORED PROCEDURES
-- ============================================================

-- sp_RegisterUser
CREATE OR ALTER PROCEDURE sp_RegisterUser
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
    BEGIN
        SELECT -1 AS Result;
        RETURN;
    END
    INSERT INTO Users (Username, PasswordHash) VALUES (@Username, @PasswordHash);
    SELECT SCOPE_IDENTITY() AS Result;
END
GO

-- sp_LoginUser
CREATE OR ALTER PROCEDURE sp_LoginUser
    @Username     NVARCHAR(100),
    @PasswordHash NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserID INT;
    SELECT @UserID = UserID FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash;
    IF @UserID IS NOT NULL
    BEGIN
        UPDATE Users SET LastLoginAt = GETDATE() WHERE UserID = @UserID;
        SELECT @UserID AS Result;
    END
    ELSE
        SELECT -1 AS Result;
END
GO

-- sp_LogActivity
CREATE OR ALTER PROCEDURE sp_LogActivity
    @UserId INT,
    @Action NVARCHAR(50),
    @Detail NVARCHAR(500) = ''
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO ActivityLogs (UserId, Action, Detail) VALUES (@UserId, @Action, @Detail);
END
GO

-- sp_SaveCycleLog
CREATE OR ALTER PROCEDURE sp_SaveCycleLog
    @UserID           INT,
    @LastPeriodDate   DATE,
    @CycleLengthDays  INT,
    @PeriodLengthDays INT,
    @CurrentPhase     NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @NextPeriod  DATE = DATEADD(DAY, @CycleLengthDays, @LastPeriodDate);
    DECLARE @Ovulation   DATE = DATEADD(DAY, @CycleLengthDays - 14, @LastPeriodDate);
    DECLARE @FertileStart DATE = DATEADD(DAY, -5, @Ovulation);
    DECLARE @FertileEnd   DATE = DATEADD(DAY, 1, @Ovulation);
    
    INSERT INTO CycleLogs (UserID, LastPeriodDate, CycleLengthDays, PeriodLengthDays,
        NextPeriodDate, OvulationDate, FertileStart, FertileEnd, CurrentPhase)
    VALUES (@UserID, @LastPeriodDate, @CycleLengthDays, @PeriodLengthDays,
        @NextPeriod, @Ovulation, @FertileStart, @FertileEnd, @CurrentPhase);
END
GO

-- sp_PlaceOrder
CREATE OR ALTER PROCEDURE sp_PlaceOrder
    @UserID         INT,
    @FlowType       NVARCHAR(20),
    @PadSelected    NVARCHAR(100),
    @ChocolatesList NVARCHAR(300),
    @PainkillerName NVARCHAR(100),
    @FullName       NVARCHAR(150),
    @PhoneNumber    NVARCHAR(20),
    @HomeAddress    NVARCHAR(300),
    @City           NVARCHAR(100),
    @DeliveryMethod NVARCHAR(50),
    @PaymentMethod  NVARCHAR(50),
    @DeliveryFeeRs  INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO GoodieBasketOrders (
        UserID, FlowType, PadSelected, ChocolatesList, PainkillerName,
        FullName, PhoneNumber, HomeAddress, City, DeliveryMethod, PaymentMethod, DeliveryFeeRs
    ) VALUES (
        @UserID, @FlowType, @PadSelected, @ChocolatesList, @PainkillerName,
        @FullName, @PhoneNumber, @HomeAddress, @City, @DeliveryMethod, @PaymentMethod, @DeliveryFeeRs
    );
END
GO

-- sp_SaveSymptom
CREATE OR ALTER PROCEDURE sp_SaveSymptom
    @UserId      INT,
    @SymptomName NVARCHAR(50),
    @Severity    INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Symptoms (UserId, SymptomName, Severity) VALUES (@UserId, @SymptomName, @Severity);
END
GO

-- sp_GetSymptomHistory
CREATE OR ALTER PROCEDURE sp_GetSymptomHistory
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 10 SymptomName, Severity, DateLogged
    FROM Symptoms WHERE UserId = @UserId
    ORDER BY DateLogged DESC, SymptomId DESC;
END
GO

-- sp_SaveWaterIntake
CREATE OR ALTER PROCEDURE sp_SaveWaterIntake
    @UserId          INT,
    @GlassesConsumed INT,
    @Goal            INT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM WaterIntake WHERE UserId = @UserId AND DateLogged = CAST(GETDATE() AS DATE))
    BEGIN
        UPDATE WaterIntake SET GlassesConsumed = @GlassesConsumed, Goal = @Goal
        WHERE UserId = @UserId AND DateLogged = CAST(GETDATE() AS DATE);
    END
    ELSE
    BEGIN
        INSERT INTO WaterIntake (UserId, GlassesConsumed, Goal) VALUES (@UserId, @GlassesConsumed, @Goal);
    END
END
GO

-- sp_GetWaterHistory
CREATE OR ALTER PROCEDURE sp_GetWaterHistory
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 7 GlassesConsumed, Goal, DateLogged
    FROM WaterIntake WHERE UserId = @UserId
    ORDER BY DateLogged DESC;
END
GO

-- sp_SaveReminder
CREATE OR ALTER PROCEDURE sp_SaveReminder
    @UserId       INT,
    @ReminderType NVARCHAR(50),
    @ReminderDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM Reminders WHERE UserId = @UserId AND ReminderType = @ReminderType AND ReminderDate = @ReminderDate AND Status = 'Pending')
    BEGIN
        INSERT INTO Reminders (UserId, ReminderType, ReminderDate) VALUES (@UserId, @ReminderType, @ReminderDate);
    END
END
GO

-- sp_GetUpcomingReminders
CREATE OR ALTER PROCEDURE sp_GetUpcomingReminders
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ReminderId, ReminderType, ReminderDate, Status
    FROM Reminders
    WHERE UserId = @UserId AND Status = 'Pending' AND ReminderDate >= CAST(GETDATE() AS DATE)
    ORDER BY ReminderDate ASC;
END
GO

-- sp_MarkReminderDone
CREATE OR ALTER PROCEDURE sp_MarkReminderDone
    @UserId       INT,
    @ReminderType NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE TOP (1) Reminders
    SET Status = 'Done'
    WHERE UserId = @UserId AND ReminderType = @ReminderType AND Status = 'Pending';
END
GO

-- ============================================================
--                        TRIGGERS
-- ============================================================

-- Symptoms Audit Trigger
CREATE OR ALTER TRIGGER trg_Symptoms_Audit ON Symptoms AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO Symptoms_Audit (SymptomId, UserId, AuditAction, OldSeverity, NewSeverity)
        SELECT i.SymptomId, i.UserId, 'UPDATE', d.Severity, i.Severity
        FROM inserted i JOIN deleted d ON i.SymptomId = d.SymptomId;
    END
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO Symptoms_Audit (SymptomId, UserId, AuditAction, OldSeverity)
        SELECT SymptomId, UserId, 'DELETE', Severity FROM deleted;
    END
END
GO

-- Reminders Audit Trigger
CREATE OR ALTER TRIGGER trg_Reminders_Audit ON Reminders AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(Status)
    BEGIN
        INSERT INTO Reminders_Audit (ReminderId, UserId, AuditAction, OldStatus, NewStatus)
        SELECT i.ReminderId, i.UserId, 'UPDATE', d.Status, i.Status
        FROM inserted i JOIN deleted d ON i.ReminderId = d.ReminderId
        WHERE d.Status <> i.Status;
    END
END
GO

-- Auto-expire past reminders
CREATE OR ALTER TRIGGER trg_AutoExpireReminders ON Reminders AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Reminders SET Status = 'Expired'
    WHERE ReminderId IN (SELECT ReminderId FROM inserted WHERE ReminderDate < CAST(GETDATE() AS DATE));
END
GO

-- ============================================================
--                      SAMPLE DATA
-- ============================================================

-- Sample users (password hash for "password123")
INSERT INTO Users (Username, PasswordHash) VALUES
    ('demo_user', 'EF92B778BAFE771E89245B89ECBF08A9045C0F6AFED3A477A0A09F5D15A6F5A1');
GO

PRINT 'Database created successfully!';
PRINT 'Tables: Users, ActivityLogs, CycleLogs, GoodieBasketOrders, Symptoms, WaterIntake, Reminders';
PRINT 'Audit tables: Symptoms_Audit, Reminders_Audit';
PRINT 'All stored procedures and triggers are ready.';
PRINT 'Demo user: username="demo_user", password="password123"';
GO
