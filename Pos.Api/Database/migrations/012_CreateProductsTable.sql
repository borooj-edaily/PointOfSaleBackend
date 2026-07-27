============================================================
-- Migration: 002_CreateProductsTable
-- Description: إنشاء جدول Products - مرتبط بـ Categories عبر CategoryId
-- Depends on: 001_CreateCategoriesTable
-- ============================================================

CREATE TABLE IF NOT EXISTS Products (
    Id                  INT             NOT NULL AUTO_INCREMENT,
    Name                VARCHAR(150)    NOT NULL,
    CategoryId          INT             NOT NULL,

    -- Dual-Unit Selling Logic (BR-02.1)
    SellBy              TINYINT UNSIGNED NOT NULL,   -- 1=Piece, 2=Package, 3=Both
    PiecesPerPackage    INT             NULL,
    PricePerPiece       DECIMAL(18, 2)  NULL,
    PricePerPackage     DECIMAL(18, 2)  NULL,

    -- المخزون دائماً موحّد بوحدة الحبة (Base Unit)
    StockInPieces       INT             NOT NULL DEFAULT 0,

    IsActive            BOOLEAN         NOT NULL DEFAULT TRUE,

    -- Audit Fields (من AuditableEntity)
    CreatedAt           DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreatedByUserId     INT             NULL,
    UpdatedAt           DATETIME(6)     NULL,
    UpdatedByUserId     INT             NULL,

    CONSTRAINT pk_products PRIMARY KEY (Id),

    CONSTRAINT fk_products_category
        FOREIGN KEY (CategoryId) REFERENCES Categories (Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    -- اسم المنتج فريد فقط جوا نفس Category (مو Globally)
    CONSTRAINT uq_products_category_name UNIQUE (CategoryId, Name),

    -- Checks بسيطة (موجبة فقط) - القواعد الشرطية المعقدة تتطبق بـ FluentValidation
    CONSTRAINT chk_products_stock_non_negative
        CHECK (StockInPieces >= 0),

    CONSTRAINT chk_products_price_per_piece_non_negative
        CHECK (PricePerPiece IS NULL OR PricePerPiece >= 0),

    CONSTRAINT chk_products_price_per_package_non_negative
        CHECK (PricePerPackage IS NULL OR PricePerPackage >= 0),

    CONSTRAINT chk_products_pieces_per_package_positive
        CHECK (PiecesPerPackage IS NULL OR PiecesPerPackage > 0),

    CONSTRAINT chk_products_sellby_valid
        CHECK (SellBy IN (1, 2, 3))

) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- Index مساعد لتسريع البحث عن الأصناف الفعّالة ضمن كاتيجوري معيّن (شاشة الكاشير)
CREATE INDEX ix_products_category_isactive ON Products (CategoryId, IsActive);

-- Index مساعد لتنبيهات نقص المخزون (Low Stock Alerts)
CREATE INDEX ix_products_stock_in_pieces ON Products (StockInPieces);