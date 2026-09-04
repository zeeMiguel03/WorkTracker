-- Reference data shared by all users

INSERT INTO "TransactionType" ("Name", "Color") VALUES
('Income', '#22C55E'),
('Expense', '#EF4444');

INSERT INTO "AccountType" ("Name") VALUES
('Card'),
('Bank Account'),
('Cash'),
('Digital Wallet'),
('Platform Balance'),
('Other');

-- Kanban statuses are user-specific.
-- Replace :UserId with the id of the user being initialized.

INSERT INTO "TaskStatus" ("UserId", "Name", "Color", "SortOrder") VALUES
(:UserId, 'To Do', '#64748B', 1),
(:UserId, 'Doing', '#3B82F6', 2),
(:UserId, 'Testing', '#F59E0B', 3),
(:UserId, 'Done', '#22C55E', 4);
