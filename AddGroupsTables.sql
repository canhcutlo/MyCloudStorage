CREATE TABLE [Groups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [OwnerId] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    CONSTRAINT [PK_Groups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Groups_AspNetUsers_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [GroupMembers] (
    [Id] int NOT NULL IDENTITY,
    [GroupId] int NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [DisplayName] nvarchar(100) NULL,
    [UserId] nvarchar(450) NULL,
    [AddedAt] datetime2 NOT NULL,
    [IsFromSharingHistory] bit NOT NULL,
    CONSTRAINT [PK_GroupMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GroupMembers_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GroupMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
);

CREATE UNIQUE INDEX [IX_Groups_OwnerId_Name] ON [Groups] ([OwnerId], [Name]);
CREATE UNIQUE INDEX [IX_GroupMembers_GroupId_Email] ON [GroupMembers] ([GroupId], [Email]);
CREATE INDEX [IX_GroupMembers_Email] ON [GroupMembers] ([Email]);
