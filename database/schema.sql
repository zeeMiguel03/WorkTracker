-- Current WorkTracker schema for SQL Server, aligned with the EF Core model
-- snapshot and migrations in backend/WorkeTracker/Infrastructure/Migrations.
-- For application deployments, apply migrations with `dotnet ef database update`.

CREATE TABLE [users] (
    [id] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(150) NOT NULL,
    [email] nvarchar(255) NOT NULL,
    [password_hash] nvarchar(500) NULL,
    [profile_image_url] nvarchar(500) NULL,
    [created_at] datetime2 NOT NULL,
    [token_version] int NOT NULL CONSTRAINT [DF_users_token_version] DEFAULT (0),
    [ut_creation] int NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([id])
);

CREATE TABLE [external_logins] (
    [id] int IDENTITY(1,1) NOT NULL,
    [provider] nvarchar(30) NOT NULL,
    [provider_subject] nvarchar(255) NOT NULL,
    [user_id] int NOT NULL,
    CONSTRAINT [PK_external_logins] PRIMARY KEY ([id]),
    CONSTRAINT [FK_external_logins_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [sources] (
    [id] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(150) NOT NULL,
    [image_url] nvarchar(500) NULL,
    [link] nvarchar(max) NULL,
    [created_at] datetime2 NOT NULL,
    [is_active] bit NOT NULL,
    [user_id] int NOT NULL,
    [ut_creation] int NULL,
    CONSTRAINT [PK_sources] PRIMARY KEY ([id]),
    CONSTRAINT [FK_sources_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [task_status] (
    [id] int IDENTITY(1,1) NOT NULL,
    [name] nvarchar(150) NOT NULL,
    [color] nvarchar(7) NOT NULL,
    [sort_order] int NOT NULL,
    [created_at] datetime2 NOT NULL,
    [user_id] int NOT NULL,
    [ut_creation] int NULL,
    CONSTRAINT [PK_task_status] PRIMARY KEY ([id]),
    CONSTRAINT [FK_task_status_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [purchase_orders] (
    [id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [source_id] int NULL,
    [status] nvarchar(30) NOT NULL,
    [shipping_cost] decimal(18,2) NOT NULL,
    [other_costs] decimal(18,2) NULL,
    [tracking_number] nvarchar(60) NULL,
    [notes] nvarchar(1000) NULL,
    [ordered_at] datetime2 NULL,
    [delivered_at] datetime2 NULL,
    [created_at] datetime2 NOT NULL,
    [updated_at] datetime2 NOT NULL,
    CONSTRAINT [PK_purchase_orders] PRIMARY KEY ([id]),
    CONSTRAINT [FK_purchase_orders_sources_source_id] FOREIGN KEY ([source_id])
        REFERENCES [sources] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_purchase_orders_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [products] (
    [id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [purchase_order_id] int NULL,
    [name] nvarchar(150) NOT NULL,
    [description] nvarchar(2000) NULL,
    [category] nvarchar(100) NULL,
    [brand] nvarchar(100) NULL,
    [size] nvarchar(50) NULL,
    [color] nvarchar(50) NULL,
    [condition] nvarchar(30) NOT NULL,
    [status] nvarchar(20) NOT NULL,
    [purchase_price] decimal(18,2) NOT NULL,
    [allocated_shipping_cost] decimal(18,2) NOT NULL,
    [allocated_other_costs] decimal(18,2) NOT NULL,
    [listing_price] decimal(18,2) NULL,
    [minimum_price] decimal(18,2) NULL,
    [sale_price] decimal(18,2) NULL,
    [sale_other_costs] decimal(18,2) NULL,
    [sale_source_id] int NULL,
    [sold_at] datetime2 NULL,
    [notes] nvarchar(1000) NULL,
    [created_at] datetime2 NOT NULL,
    [updated_at] datetime2 NOT NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [PK_products] PRIMARY KEY ([id]),
    CONSTRAINT [FK_products_purchase_orders_purchase_order_id] FOREIGN KEY ([purchase_order_id])
        REFERENCES [purchase_orders] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_products_sources_sale_source_id] FOREIGN KEY ([sale_source_id])
        REFERENCES [sources] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_products_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [product_images] (
    [id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [image_url] nvarchar(500) NOT NULL,
    [display_order] int NOT NULL,
    [is_cover] bit NOT NULL,
    [created_at] datetime2 NOT NULL,
    CONSTRAINT [PK_product_images] PRIMARY KEY ([id]),
    CONSTRAINT [FK_product_images_products_product_id] FOREIGN KEY ([product_id])
        REFERENCES [products] ([id]) ON DELETE CASCADE
);

CREATE TABLE [product_sales] (
    [id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [product_id] int NULL,
    [product_name] nvarchar(150) NOT NULL,
    [sale_source_id] int NULL,
    [sale_source_name] nvarchar(150) NULL,
    [purchase_price] decimal(18,2) NOT NULL,
    [allocated_shipping_cost] decimal(18,2) NOT NULL,
    [allocated_other_costs] decimal(18,2) NOT NULL,
    [sale_price] decimal(18,2) NOT NULL,
    [sale_other_costs] decimal(18,2) NOT NULL,
    [sold_at] datetime2 NOT NULL,
    [sale_date] date NOT NULL,
    CONSTRAINT [PK_product_sales] PRIMARY KEY ([id]),
    CONSTRAINT [FK_product_sales_products_product_id] FOREIGN KEY ([product_id])
        REFERENCES [products] ([id]) ON DELETE SET NULL,
    CONSTRAINT [FK_product_sales_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE NO ACTION
);

CREATE TABLE [refresh_tokens] (
    [id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [token_hash] nvarchar(64) NOT NULL,
    [token_version] int NOT NULL,
    [created_at] datetime2 NOT NULL,
    [expires_at] datetime2 NOT NULL,
    [revoked_at] datetime2 NULL,
    [row_version] rowversion NOT NULL,
    CONSTRAINT [PK_refresh_tokens] PRIMARY KEY ([id]),
    CONSTRAINT [FK_refresh_tokens_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE TABLE [tasks] (
    [id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [source_id] int NULL,
    [task_status_id] int NOT NULL,
    [title] nvarchar(150) NOT NULL,
    [description] nvarchar(500) NULL,
    [priority] int NOT NULL,
    [sort_order] int NOT NULL,
    [due_date] datetime2 NULL,
    [completed_at] datetime2 NULL,
    [created_at] datetime2 NOT NULL,
    [ut_creation] int NULL,
    CONSTRAINT [PK_tasks] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tasks_sources_source_id] FOREIGN KEY ([source_id])
        REFERENCES [sources] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_task_status_task_status_id] FOREIGN KEY ([task_status_id])
        REFERENCES [task_status] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_tasks_users_user_id] FOREIGN KEY ([user_id])
        REFERENCES [users] ([id]) ON DELETE CASCADE
);

CREATE INDEX [IX_products_purchase_order_id] ON [products] ([purchase_order_id]);
CREATE INDEX [IX_products_sale_source_id] ON [products] ([sale_source_id]);
CREATE INDEX [IX_products_user_id_status] ON [products] ([user_id], [status]);
CREATE INDEX [IX_products_user_id_created_at_id] ON [products] ([user_id], [created_at], [id]);
CREATE INDEX [IX_products_user_id_status_created_at_id] ON [products] ([user_id], [status], [created_at], [id]);
CREATE UNIQUE INDEX [IX_product_images_product_id_display_order] ON [product_images] ([product_id], [display_order]);
CREATE UNIQUE INDEX [UX_product_images_cover] ON [product_images] ([product_id]) WHERE [is_cover] = 1;
CREATE UNIQUE INDEX [IX_product_sales_product_id] ON [product_sales] ([product_id]) WHERE [product_id] IS NOT NULL;
CREATE INDEX [IX_product_sales_user_id_sale_date_id] ON [product_sales] ([user_id], [sale_date], [id]);
CREATE INDEX [IX_purchase_orders_source_id] ON [purchase_orders] ([source_id]);
CREATE INDEX [IX_purchase_orders_user_id_created_at_id] ON [purchase_orders] ([user_id], [created_at], [id]);
CREATE INDEX [IX_purchase_orders_user_id_status_created_at_id] ON [purchase_orders] ([user_id], [status], [created_at], [id]);
CREATE UNIQUE INDEX [IX_refresh_tokens_token_hash] ON [refresh_tokens] ([token_hash]);
CREATE INDEX [IX_refresh_tokens_user_id_expires_at] ON [refresh_tokens] ([user_id], [expires_at]);
CREATE INDEX [IX_sources_user_id_is_active] ON [sources] ([user_id], [is_active]);
CREATE INDEX [IX_sources_user_id_name] ON [sources] ([user_id], [name]);
CREATE INDEX [IX_task_status_user_id_sort_order] ON [task_status] ([user_id], [sort_order]);
CREATE UNIQUE INDEX [IX_task_status_user_id_name] ON [task_status] ([user_id], [name]);
CREATE INDEX [IX_tasks_source_id] ON [tasks] ([source_id]);
CREATE INDEX [IX_tasks_task_status_id_sort_order] ON [tasks] ([task_status_id], [sort_order]);
CREATE INDEX [IX_tasks_user_id_completed_at] ON [tasks] ([user_id], [completed_at]);
CREATE INDEX [IX_tasks_user_id_due_date] ON [tasks] ([user_id], [due_date]);
CREATE INDEX [IX_tasks_user_id_task_status_id] ON [tasks] ([user_id], [task_status_id]);
CREATE UNIQUE INDEX [IX_users_email] ON [users] ([email]);
CREATE UNIQUE INDEX [IX_external_logins_provider_provider_subject] ON [external_logins] ([provider], [provider_subject]);
CREATE UNIQUE INDEX [IX_external_logins_user_id_provider] ON [external_logins] ([user_id], [provider]);
