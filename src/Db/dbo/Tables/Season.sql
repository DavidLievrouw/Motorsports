CREATE TABLE [dbo].[Season] (
    [Id]    INT            IDENTITY (1, 1) NOT NULL,
    [Sport] NVARCHAR (50)  NOT NULL,
    [Label] NVARCHAR (100) NULL,
    [WinningTeam] INT NULL,
    CONSTRAINT [PK_Season] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Sport_Season] FOREIGN KEY ([Sport]) REFERENCES [dbo].[Sport] ([Name]) ON UPDATE CASCADE,
    CONSTRAINT [FK_Team_Season] FOREIGN KEY ([WinningTeam]) REFERENCES [dbo].[Team] ([Id])
);
