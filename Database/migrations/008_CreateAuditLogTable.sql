CREATE TABLE AuditLog (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    UserId      INT NOT NULL,
    Action      VARCHAR(100) NOT NULL,
    Entity      VARCHAR(100) NOT NULL,
    EntityId    INT NULL,
    Details     JSON NULL,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_AuditLog_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;