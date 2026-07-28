CREATE TABLE Permissions (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    Name            VARCHAR(100)    NOT NULL UNIQUE,
    Description     VARCHAR(255)    NULL
) ENGINE=InnoDB;
