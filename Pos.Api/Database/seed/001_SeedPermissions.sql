-- No 'void_invoice' permission: per BR-14, invoices can never be voided or
-- deleted once finalized. Only returns/exchanges are possible.
INSERT INTO Permissions (Name, Description) VALUES
('create_invoice',      'Create and finalize a sales invoice'),
('process_return',       'Register a return or exchange on an invoice'),
('print_invoice',        'Print or reprint an invoice receipt'),
('edit_price',           'Edit product prices'),
('manage_inventory',     'Restock and manually deduct stock'),
('manage_products',      'Create/edit/deactivate products and categories'),
('manage_users',         'Create/edit/deactivate employees and permissions'),
('view_all_invoices',    'View invoices created by any employee'),
('view_reports',         'Access sales/inventory/attendance reports'),
('view_audit_log',       'Access the audit log');
