CREATE TABLE IF NOT EXISTS StockMovements (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProductId INT NOT NULL,
    Type VARCHAR(20) NOT NULL, -- 'Restock' | 'Sale' | 'Return' | 'Exchange' | 'ManualDeduction'
    QuantityInPieces INT NOT NULL,
    BalanceBefore INT NOT NULL,
    BalanceAfter INT NOT NULL,
    Reason VARCHAR(250) NULL,
    ReferenceInvoiceId INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy INT NULL,
    CONSTRAINT FK_StockMovements_Products FOREIGN KEY (ProductId)
        REFERENCES Products(Id)
        ON DELETE RESTRICT
);

CREATE INDEX IX_StockMovements_ProductId_CreatedAt ON StockMovements(ProductId, CreatedAt);