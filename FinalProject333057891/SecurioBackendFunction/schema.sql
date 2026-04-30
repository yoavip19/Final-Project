-- Securio Database Schema
-- Run this script once against your Azure SQL database before deploying the backend.
-- Azure Portal: Query editor, or connect with SSMS/Azure Data Studio.

-- ============================================================
-- Users
-- ============================================================
CREATE TABLE Users (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    Username            NVARCHAR(100)   NOT NULL,
    Email               NVARCHAR(256)   NOT NULL UNIQUE,
    MasterPasswordKey   NVARCHAR(512)   NOT NULL,
    AuthSalt            NVARCHAR(256)   NOT NULL,
    EncryptionSalt      NVARCHAR(256)   NOT NULL,
    LastLogin           DATETIME        NULL,
    LastPasswordUpdate  DATETIME        NULL,
    CreatedAt           DATETIME        NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- VaultItems  (foreign key to Users, CASCADE on delete)
-- ============================================================
CREATE TABLE VaultItems (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT             NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    AccountName     NVARCHAR(256)   NOT NULL,
    AccountUsername NVARCHAR(256)   NULL,
    IV              NVARCHAR(512)   NOT NULL,
    Tag             NVARCHAR(512)   NOT NULL,
    CipherText      NVARCHAR(MAX)   NOT NULL,
    Notes           NVARCHAR(MAX)   NULL,
    Sha1Hash        NVARCHAR(40)    NOT NULL,
    IsLeaked        BIT             NOT NULL DEFAULT 0,
    LastUpdate      DATETIME        NOT NULL DEFAULT GETDATE()
);

-- ============================================================
-- MasterPasswordHistory  (keeps the last N old master-password keys)
-- ============================================================
CREATE TABLE MasterPasswordHistory (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT             NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    PasswordKey NVARCHAR(512)   NOT NULL,
    AuthSalt    NVARCHAR(256)   NOT NULL,
    CreatedAt   DATETIME        NOT NULL DEFAULT GETDATE()
);
