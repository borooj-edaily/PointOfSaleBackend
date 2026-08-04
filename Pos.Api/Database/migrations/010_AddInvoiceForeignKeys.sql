ALTER TABLE InvoiceItems
    ADD CONSTRAINT FK_InvoiceItems_Products
    FOREIGN KEY (ProductId) REFERENCES Products(Id);

ALTER TABLE InvoiceReturns
    ADD CONSTRAINT FK_InvoiceReturns_Users
    FOREIGN KEY (ProcessedBy) REFERENCES Users(Id);

ALTER TABLE InvoiceReturns
    ADD CONSTRAINT FK_InvoiceReturns_Products
    FOREIGN KEY (ReplacementProductId) REFERENCES Products(Id);