CREATE TABLE [dbo].[Round] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Date]        DATE           NOT NULL,
    [Number]      SMALLINT       NOT NULL,
    [Name]        NVARCHAR (100) NULL,
    [Season]      INT            NOT NULL,
    [Venue]       NVARCHAR (50)  NOT NULL,
    [Status]      NVARCHAR (20)  NOT NULL CONSTRAINT DF_Round_Status DEFAULT N'Scheduled',
    [Rating]      DECIMAL (3, 1) NULL,
    [Rain]        DECIMAL (1)    NULL,
    [WinningTeam] INT            NULL,
    CONSTRAINT [PK_Round] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Season_Round] FOREIGN KEY ([Season]) REFERENCES [dbo].[Season] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Status_Round] FOREIGN KEY ([Status]) REFERENCES [dbo].[Status] ([Name]) ON UPDATE CASCADE,
    CONSTRAINT [FK_Venue_Round] FOREIGN KEY ([Venue]) REFERENCES [dbo].[Venue] ([Name]) ON UPDATE CASCADE,
    CONSTRAINT [FK_Team_Round] FOREIGN KEY ([WinningTeam]) REFERENCES [dbo].[Team] ([Id])
);

