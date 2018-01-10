CREATE TABLE [dbo].[SeasonEntry] (
  [SeasonId] INT NOT NULL, 
  [TeamId] INT NOT NULL, 
  [Name] NVARCHAR(50) NOT NULL, 
  PRIMARY KEY ([SeasonId], [TeamId]), 
  CONSTRAINT [FK_Season_SeasonEntry] FOREIGN KEY ([SeasonId]) REFERENCES [dbo].[Season]([Id]) ON DELETE CASCADE,
  CONSTRAINT [FK_Team_SeasonEntry] FOREIGN KEY ([TeamId]) REFERENCES [dbo].[Team]([Id])
)
