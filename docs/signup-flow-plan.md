# Signup Flow – Review, Plan & DB Schema

## 1. Current State of the Signup (Registration) Backend

### Components Involved

| Layer | File | Responsibility |
|-------|------|---------------|
| HTTP Endpoint | `AuthFunctions.cs` → `Register()` | Deserializes `User` JSON, calls `AuthManager.RegisterAsync`, returns `200 OK` or `409 Conflict`. |
| Business Logic | `AuthManager.cs` → `RegisterAsync()` | Checks for duplicate email, inserts user, generates JWT on success. |
| Data Access | `UserRepository.cs` → `EmailExistsAsync()` / `CreateUserAsync()` | Parameterized SQL queries against the `Users` table. |
| Model | `User.cs` | DTO with `Id`, `Username`, `Email`, `MasterPasswordKey`, `AuthSalt`, `EncryptionSalt`, timestamps, `PasswordCount`. |
| Response | `ServerResponse<AuthData>` | Standard envelope (`Success`, `Message`, `Data`). |

### What Already Works

- Email-uniqueness check (`EmailExistsAsync`).
- User insertion with salts, hashed key, and server-side timestamps (`CreateUserAsync`).
- JWT generation on success (`JwtHelper.GenerateJwtToken`).
- Immediate `AuthData` response containing `UserId`, `Username`, `Token`.

---

## 2. Missing / Recommended Improvements

### 2.1 Input Validation

`RegisterAsync` currently trusts the incoming `User` object.  
Add validation **before** the database round-trip:

| Field | Validation Rule |
|-------|----------------|
| `Email` | Must not be null/empty, must match a basic email regex pattern. |
| `Username` | Must not be null/empty, 2-50 characters, alphanumeric + underscores. |
| `MasterPasswordKey` | Must not be null/empty (the client sends a derived key, not the raw password). |
| `AuthSalt` | Must not be null/empty (generated client-side). |
| `EncryptionSalt` | Must not be null/empty (generated client-side). |

Return `ServerResponse<AuthData> { Success = false, Message = "..." }` with a descriptive message for each violation.

### 2.2 Null-Safety on Deserialization

In `AuthFunctions.Register`, the deserialized `User` could be `null` if the body is empty or malformed.  
Add a null-check immediately after deserialization:

```
if (signup == null)
    return new BadRequestObjectResult(new ServerResponse<object> { Success = false, Message = "Invalid request body." });
```

### 2.3 Salt Retrieval Endpoint (Pre-Login)

The client needs the user's `AuthSalt` before it can derive the key to submit during login.  
A `GetSalt` endpoint is missing:

- **Route:** `POST /api/GetSalt` accepting `{ "email": "..." }`.
- **Logic:** Fetch `AuthSalt` and `EncryptionSalt` from the DB by email.
- **Response:** `ServerResponse<SaltData>`.
- **Repository method needed:** `GetSaltsByEmailAsync(string email)` on `IUserRepository`.

### 2.4 Email Normalization

Emails should be lowered/trimmed before any DB comparison to avoid duplicates caused by casing (e.g., `User@example.com` vs `user@example.com`).  
Apply `email.Trim().ToLowerInvariant()` in both `RegisterAsync` and `VerifyLoginAsync`.

### 2.5 Logging

Neither `AuthManager` nor `AuthFunctions` logs registration attempts, failures, or exceptions.  
Inject `ILogger<AuthManager>` and log:
- Registration attempts (info level).
- Duplicate-email rejections (warning level).
- Database errors (error level).

### 2.6 Duplicate `using` Directives

Both `AuthManager.cs` and `AuthFunctions.cs` have a duplicate `using SecurioModels.DataTransferObjects;` line. These should be cleaned up.

---

## 3. Database Schema Instructions

The `CreateUserAsync` SQL tells us the exact shape the `Users` table must have.  
Run the following script against your SQL Server database to ensure correctness.

### 3.1 Create / Verify the `Users` Table

```sql
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users
    (
        Id                  INT             IDENTITY(1,1) PRIMARY KEY,
        Username            NVARCHAR(100)   NOT NULL,
        Email               NVARCHAR(256)   NOT NULL,
        MasterPasswordKey   NVARCHAR(512)   NOT NULL,
        AuthSalt            NVARCHAR(256)   NOT NULL,
        EncryptionSalt      NVARCHAR(256)   NOT NULL,
        LastLogin           DATETIME2       NOT NULL DEFAULT GETDATE(),
        LastPasswordUpdate  DATETIME2       NOT NULL DEFAULT GETDATE(),
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETDATE()
    );
END
GO
```

### 3.2 Add Unique Constraint on Email

Prevent duplicate registrations at the database level (defense in depth):

```sql
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Users_Email' AND object_id = OBJECT_ID('Users')
)
BEGIN
    CREATE UNIQUE INDEX UQ_Users_Email ON Users (Email);
END
GO
```

### 3.3 Verification Query

After running the above, verify the schema:

```sql
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
```

Expected output:

| Column | Type | Max Length | Nullable | Default |
|--------|------|-----------|----------|---------|
| Id | int | — | NO | — |
| Username | nvarchar | 100 | NO | — |
| Email | nvarchar | 256 | NO | — |
| MasterPasswordKey | nvarchar | 512 | NO | — |
| AuthSalt | nvarchar | 256 | NO | — |
| EncryptionSalt | nvarchar | 256 | NO | — |
| LastLogin | datetime2 | — | NO | GETDATE() |
| LastPasswordUpdate | datetime2 | — | NO | GETDATE() |
| CreatedAt | datetime2 | — | NO | GETDATE() |

### 3.4 Notes

- All string columns use `NVARCHAR` to support Unicode.
- `MasterPasswordKey` is 512 chars wide to accommodate Base64-encoded derived keys.
- Salt columns are 256 chars wide for Base64-encoded random bytes.
- `IDENTITY(1,1)` auto-generates the `Id` on insert; do **not** supply it.
- The `OUTPUT INSERTED.Id` clause in `CreateUserAsync` returns the new ID immediately.

---

## 4. Implementation Priority

1. **Input validation** in `AuthManager.RegisterAsync` – prevents garbage data in the DB.
2. **Null-check** after deserialization in `AuthFunctions.Register` – prevents `NullReferenceException`.
3. **Email normalization** – prevents duplicate accounts.
4. **DB unique index on Email** – defense in depth.
5. **Salt retrieval endpoint** – required for the client login flow to work.
6. **Logging** – observability for production.
7. **Clean up duplicate usings** – code hygiene.

---

## 5. Unit Tests

Unit tests for `AuthManager.RegisterAsync` are provided in the `SecurioBackendFunction.Tests` project.  
They mock `IUserRepository` to verify the business logic in isolation:

- **Successful registration** – returns `Success = true`, `AuthData` with token and user info.
- **Duplicate email** – returns `Success = false`, `"Email already registered."`.
- **Database failure** – returns `Success = false`, `"Database error."` when `CreateUserAsync` returns `0`.
- **Null/empty field validation** – returns `Success = false` for missing required fields (once validation is added).
