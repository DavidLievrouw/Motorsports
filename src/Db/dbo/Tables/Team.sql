CREATE TABLE [dbo].[Team] (
    [Id]      INT           IDENTITY (1, 1) NOT NULL,
    [Name]    NVARCHAR (50) NOT NULL,
    [Sport]   NVARCHAR (50) NOT NULL,
    [Country] CHAR (2)      NOT NULL,
    CONSTRAINT [PK_Team] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Country_Team] FOREIGN KEY ([Country]) REFERENCES [dbo].[Country] ([ISO]) ON UPDATE CASCADE,
    CONSTRAINT [FK_Sport_Team] FOREIGN KEY ([Sport]) REFERENCES [dbo].[Sport] ([Name]) ON UPDATE CASCADE
);

