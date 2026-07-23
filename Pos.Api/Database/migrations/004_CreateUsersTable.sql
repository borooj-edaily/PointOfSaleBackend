CREATE TABLE Users (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    FullName        VARCHAR(150)    NOT NULL,
    Username        VARCHAR(50)     NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255)    NOT NULL,
    Role            ENUM('Admin', 'Cashier', 'InventoryOnly', 'Custom') NOT NULL,
    IsActive        BOOLEAN         NOT NULL DEFAULT TRUE,
    CreatedAt       DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;
