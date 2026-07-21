-- Migration: 003_CreateInvoiceReturnsTable
-- Owner: Person C (Invoices module)

CREATE TABLE IF NOT EXISTS InvoiceReturns (
    Id                      INT AUTO_INCREMENT PRIMARY KEY,
    InvoiceId               INT NOT NULL,
    InvoiceItemId           INT NOT NULL,
    Type                    VARCHAR(10) NOT NULL,   -- 'return' | 'exchange'
    ReturnedQuantity        INT NOT NULL,
    ReplacementProductId    INT NULL,               -- exchange only
    ReplacementQuantity     INT NULL,                -- exchange only
    ProcessedBy             INT NOT NULL,           -- user_id, FK added later
    Reason                  VARCHAR(255) NULL,
    CreatedAt               DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_InvoiceReturns_Invoices FOREIGN KEY (InvoiceId)
        REFERENCES Invoices(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InvoiceReturns_InvoiceItems FOREIGN KEY (InvoiceItemId)
        REFERENCES InvoiceItems(Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
