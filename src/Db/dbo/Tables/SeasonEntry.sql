CREATE TABLE [dbo].[SeasonEntry] (
  [Season] INT NOT NULL, 
  [Team] INT NOT NULL, 
  [Name] NVARCHAR(50) NOT NULL, 
  PRIMARY KEY ([Season], [Team]), 
  CONSTRAINT [FK_Season_SeasonEntry] FOREIGN KEY ([Season]) REFERENCES [dbo].[Season]([Id]) ON DELETE CASCADE,
  CONSTRAINT [FK_Team_SeasonEntry] FOREIGN KEY ([Team]) REFERENCES [dbo].[Team]([Id])
)
