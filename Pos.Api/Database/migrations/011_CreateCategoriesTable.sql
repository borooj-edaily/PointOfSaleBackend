
============================================================
-- Migration: 001_CreateCategoriesTable
-- Description: إنشاء جدول Categories - أبسط جدول، صفر Foreign Keys
-- ============================================================

CREATE TABLE IF NOT EXISTS Categories (
    Id                INT             NOT NULL AUTO_INCREMENT,
    Name              VARCHAR(100)    NOT NULL,
    IsActive          BOOLEAN         NOT NULL DEFAULT TRUE,

    -- Audit Fields (من BaseEntity / AuditableEntity)
    CreatedAt         DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CreatedByUserId   INT             NULL,
    UpdatedAt         DATETIME(6)     NULL,
    UpdatedByUserId   INT             NULL,

    CONSTRAINT pk_categories PRIMARY KEY (Id),
    CONSTRAINT uq_categories_name UNIQUE (Name)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- Index مساعد لتسريع فلترة التصنيفات الفعّالة (يُستخدم كتير بشاشة البيع)
CREATE INDEX ix_categories_isactive ON Categories (IsActive);