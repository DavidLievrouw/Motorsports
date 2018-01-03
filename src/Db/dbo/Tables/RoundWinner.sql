CREATE TABLE [dbo].[RoundWinner] (
    [Id]          INT IDENTITY (1, 1) NOT NULL,
    [Round]       INT NOT NULL,
    [Participant] INT NOT NULL,
    CONSTRAINT [PK_RoundWinner] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Participant_RoundWinner] FOREIGN KEY ([Participant]) REFERENCES [dbo].[Participant] ([Id]),
    CONSTRAINT [FK_Round_RoundWinner] FOREIGN KEY ([Round]) REFERENCES [dbo].[Round] ([Id]) ON DELETE CASCADE
);

