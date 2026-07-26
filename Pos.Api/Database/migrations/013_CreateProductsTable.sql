CREATE TABLE IF NOT EXISTS Products (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(150) NOT NULL,
    CategoryId INT NOT NULL,
    SellBy VARCHAR(20) NOT NULL, -- 'Piece' | 'Package' | 'Both'
    PiecesPerPackage INT NULL,
    PricePerPiece DECIMAL(10,2) NULL,
    PricePerPackage DECIMAL(10,2) NULL,
    StockInPieces INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT NULL,
    UpdatedAt DATETIME NULL,
    UpdatedBy INT NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(Id)
        ON DELETE RESTRICT
);

CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);