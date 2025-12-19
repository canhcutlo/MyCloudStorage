-- Create Favorites table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favorites')
BEGIN
    CREATE TABLE [Favorites] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [UserId] NVARCHAR(450) NOT NULL,
        [StorageItemId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Favorites] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Favorites_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favorites_StorageItems_StorageItemId] FOREIGN KEY ([StorageItemId]) 
            REFERENCES [StorageItems] ([Id]) ON DELETE NO ACTION
    );

    -- Create unique index to prevent duplicate favorites
    CREATE UNIQUE INDEX [IX_Favorites_UserId_StorageItemId] 
        ON [Favorites] ([UserId], [StorageItemId]);
    
    -- Create index on StorageItemId for faster queries
    CREATE INDEX [IX_Favorites_StorageItemId] 
        ON [Favorites] ([StorageItemId]);

    PRINT 'Favorites table created successfully'
END
ELSE
BEGIN
    PRINT 'Favorites table already exists'
END
