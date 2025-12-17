-- Create PasswordResetTokens table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PasswordResetTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [Token] NVARCHAR(450) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [IsUsed] BIT NOT NULL DEFAULT 0,
        [UsedAt] DATETIME2 NULL,
        CONSTRAINT [FK_PasswordResetTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_PasswordResetTokens_Token] ON [PasswordResetTokens] ([Token]);
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
    
    PRINT 'PasswordResetTokens table created successfully';
END
ELSE
BEGIN
    PRINT 'PasswordResetTokens table already exists';
END
GO
