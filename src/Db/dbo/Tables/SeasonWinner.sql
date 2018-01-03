CREATE TABLE [dbo].[SeasonWinner] (
    [Id]          INT IDENTITY (1, 1) NOT NULL,
    [Season]      INT NOT NULL,
    [Participant] INT NOT NULL,
    CONSTRAINT [PK_SeasonWinner] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Participant_SeasonWinner] FOREIGN KEY ([Participant]) REFERENCES [dbo].[Participant] ([Id]),
    CONSTRAINT [FK_Season_SeasonWinner] FOREIGN KEY ([Season]) REFERENCES [dbo].[Season] ([Id]) ON DELETE CASCADE
);

