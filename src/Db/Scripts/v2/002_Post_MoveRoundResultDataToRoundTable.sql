IF COL_LENGTH('dbo.Round', 'WinningTeam') IS NULL BEGIN
  ALTER TABLE [dbo].[Round] ADD [WinningTeam] INT NULL;
  ALTER TABLE [dbo].[Round] ADD [Status] NVARCHAR (20) NULL;
  ALTER TABLE [dbo].[Round] ADD [Rating] DECIMAL(2, 1) NULL;
  ALTER TABLE [dbo].[Round] ADD [Rain] DECIMAL(1) NULL;
  ALTER TABLE [dbo].[Round] ADD CONSTRAINT DF_Round_Status DEFAULT N'Scheduled' FOR [Status];
  ALTER TABLE [dbo].[Round] ADD [Status] NVARCHAR (20) NOT NULL;
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_Team_Round') BEGIN
  ALTER TABLE [dbo].[Round] ADD CONSTRAINT FK_Team_Round FOREIGN KEY ([WinningTeam])
  REFERENCES [dbo].[Team] (Id)
  ON UPDATE NO ACTION 
  ON DELETE NO ACTION;
END
GO

IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'RoundResult') BEGIN
  UPDATE
      [dbo].[Round]
  SET
      [dbo].[Round].[WinningTeam] = [dbo].[RoundResult].[WinningTeam],
      [dbo].[Round].[Status] = ISNULL([dbo].[RoundResult].[Status], 'Scheduled'),
      [dbo].[Round].[Rating] = [dbo].[RoundResult].[Rating],
      [dbo].[Round].[Rain] = [dbo].[RoundResult].[Rain]
  FROM
      [dbo].[Round]
      INNER JOIN [dbo].[RoundResult] ON [dbo].[Round].[Id] = [dbo].[RoundResult].[Round];
END
GO

IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_Team_RoundResult') BEGIN
  ALTER TABLE [dbo].[RoundResult] DROP CONSTRAINT FK_Team_RoundResult;
END
GO

IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_Status_RoundResult') BEGIN
  ALTER TABLE [dbo].[RoundResult] DROP CONSTRAINT FK_Status_RoundResult;
END
GO

IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME='FK_Round_RoundResult') BEGIN
  ALTER TABLE [dbo].[RoundResult] DROP CONSTRAINT FK_Round_RoundResult;
END
GO

IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'RoundResult') BEGIN
  DROP TABLE [dbo].[RoundResult];
END
GO