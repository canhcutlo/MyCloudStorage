-- Add Google Drive-like features to SharedItems table
-- Run this script against your CloudStorage database

USE CloudStorage;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Add new columns to SharedItems table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'AllowDownload')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [AllowDownload] BIT NOT NULL DEFAULT 1;
    
    PRINT 'Added AllowDownload column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'Notify')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [Notify] BIT NOT NULL DEFAULT 1;
    
    PRINT 'Added Notify column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'NotificationSent')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [NotificationSent] BIT NOT NULL DEFAULT 0;
    
    PRINT 'Added NotificationSent column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'LastAccessedAt')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [LastAccessedAt] DATETIME2 NULL;
    
    PRINT 'Added LastAccessedAt column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'AccessCount')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [AccessCount] INT NOT NULL DEFAULT 0;
    
    PRINT 'Added AccessCount column';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SharedItems]') AND name = 'Message')
BEGIN
    ALTER TABLE [dbo].[SharedItems]
    ADD [Message] NVARCHAR(MAX) NULL;
    
    PRINT 'Added Message column';
END
GO

-- Create index on AccessToken for faster lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SharedItems_AccessToken')
BEGIN
    CREATE INDEX IX_SharedItems_AccessToken ON [dbo].[SharedItems] ([AccessToken])
    WHERE [AccessToken] IS NOT NULL;
    
    PRINT 'Created index on AccessToken';
END
GO

-- Create index on LastAccessedAt for analytics queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SharedItems_LastAccessedAt')
BEGIN
    CREATE INDEX IX_SharedItems_LastAccessedAt ON [dbo].[SharedItems] ([LastAccessedAt])
    WHERE [LastAccessedAt] IS NOT NULL;
    
    PRINT 'Created index on LastAccessedAt';
END
GO

PRINT 'Migration completed successfully!';
GO
