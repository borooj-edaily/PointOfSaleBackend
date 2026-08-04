CREATE TABLE IF NOT EXISTS UserSessions (
    Id          CHAR(36) PRIMARY KEY,
    UserId      INT NOT NULL,
    CreatedAt   DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    ExpiresAt   DATETIME(6) NOT NULL,
    CONSTRAINT UQ_UserSessions_User UNIQUE (UserId),
    CONSTRAINT FK_UserSessions_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_UserSessions_ExpiresAt (ExpiresAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
