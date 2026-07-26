CREATE TABLE UserPermissions (
    UserId          INT NOT NULL,
    PermissionId    INT NOT NULL,
    PRIMARY KEY (UserId, PermissionId),
    CONSTRAINT FK_UserPermissions_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserPermissions_Permissions
        FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
) ENGINE=InnoDB;
