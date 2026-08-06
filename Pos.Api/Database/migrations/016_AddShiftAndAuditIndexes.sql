USE POSDB;

-- Ensures one open shift maximum for each user.
ALTER TABLE Shifts
    ADD COLUMN OpenUserId INT
        GENERATED ALWAYS AS (
            CASE WHEN LogoutAt IS NULL THEN UserId ELSE NULL END
        ) STORED,
    ADD CONSTRAINT UQ_Shifts_OneOpenShift UNIQUE (OpenUserId);

CREATE INDEX IX_Shifts_UserId_LoginAt
    ON Shifts (UserId, LoginAt);

CREATE INDEX IX_Shifts_LoginAt
    ON Shifts (LoginAt);

CREATE INDEX IX_AuditLog_UserId_CreatedAt
    ON AuditLog (UserId, CreatedAt);

CREATE INDEX IX_AuditLog_Action
    ON AuditLog (Action);

CREATE INDEX IX_AuditLog_Entity
    ON AuditLog (Entity);