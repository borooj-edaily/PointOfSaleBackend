CREATE TABLE Shifts (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    UserId      INT NOT NULL,
    LoginAt     DATETIME NOT NULL,
    LogoutAt    DATETIME NULL,
    CONSTRAINT FK_Shifts_Users
        FOREIGN KEY (UserId) REFERENCES Users(Id)
) ENGINE=InnoDB;