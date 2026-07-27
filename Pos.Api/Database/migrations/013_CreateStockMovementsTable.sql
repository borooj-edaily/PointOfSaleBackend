============================================================
-- Migration: 003_CreateStockMovementsTable
-- Description: إنشاء جدول StockMovements - سجل تاريخي ثابت (Immutable)
--              لكل حركة على مخزون أي منتج. لا يوجد UPDATE أو DELETE
--              على هذا الجدول على مستوى التطبيق أبداً.
-- Depends on: 002_CreateProductsTable
-- ============================================================



CREATE TABLE IF NOT EXISTS StockMovements (
    Id                  INT             NOT NULL AUTO_INCREMENT,
    ProductId           INT             NOT NULL,

    -- 1=Restock, 2=Sale, 3=Return, 4=ManualDeduction, 5=ManualAddition
    Type                TINYINT UNSIGNED NOT NULL,

    QuantityInPieces    INT             NOT NULL,
    BalanceBefore       INT             NOT NULL,
    BalanceAfter        INT             NOT NULL,

    -- إلزامية شرطياً عند Type = ManualDeduction/ManualAddition
    -- (التحقق الشرطي يتم عبر FluentValidation، ليس هنا)
    Reason              VARCHAR(250)    NULL,

    -- بدون FK حقيقي مؤقتاً - Invoices لسا مش موجود (بارت C)
    ReferenceInvoiceId  INT             NULL,

    -- Audit Fields (من BaseEntity فقط - بدون UpdatedAt/UpdatedByUserId
    -- لأن السجل ثابت ولا يُعدَّل بعد إنشائه)
    CreatedAt           DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    -- بدون FK حقيقي مؤقتاً - Users لسا مش موجود (بارت A)
    CreatedByUserId     INT             NULL,

    CONSTRAINT pk_stockmovements PRIMARY KEY (Id),

    CONSTRAINT fk_stockmovements_product
        FOREIGN KEY (ProductId) REFERENCES Products (Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    -- Checks بسيطة (موجبة فقط) - القواعد الشرطية المعقدة (Reason الإلزامي
    -- حسب Type، تطابق BalanceAfter مع BalanceBefore±Quantity) تتطبق بـ
    -- FluentValidation والـ Handler، وليس هنا
    CONSTRAINT chk_stockmovements_quantity_positive
        CHECK (QuantityInPieces > 0),

    CONSTRAINT chk_stockmovements_balance_before_non_negative
        CHECK (BalanceBefore >= 0),

    CONSTRAINT chk_stockmovements_balance_after_non_negative
        CHECK (BalanceAfter >= 0),

    CONSTRAINT chk_stockmovements_type_valid
        CHECK (Type IN (1, 2, 3, 4, 5))

) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- Index أساسي لجلب هيستوري حركات صنف معيّن بالترتيب الزمني (Stock History API)
CREATE INDEX ix_stockmovements_product_createdat ON StockMovements (ProductId, CreatedAt);

-- Index مساعد للتقارير المفلترة حسب نوع الحركة (مثلاً كل عمليات الـ Restock بفترة معيّنة)
CREATE INDEX ix_stockmovements_type ON StockMovements (Type);

-- Index مساعد لربط حركة بفاتورة معيّنة (لما بارت C يبني Invoices)
CREATE INDEX ix_stockmovements_reference_invoice ON StockMovements (ReferenceInvoiceId);