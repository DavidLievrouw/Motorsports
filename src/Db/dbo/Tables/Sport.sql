CREATE TABLE [dbo].[Sport] (
    [Name]     NVARCHAR (50)  NOT NULL,
    [FullName] NVARCHAR (100) NULL,
    CONSTRAINT [PK_Sport] PRIMARY KEY CLUSTERED ([Name] ASC)
);

