CREATE TABLE [dbo].[Participant] (
    [Id]        INT            IDENTITY (1, 1) NOT NULL,
    [Title]     NVARCHAR (10)  NOT NULL,
    [FirstName] NVARCHAR (100) NOT NULL,
    [LastName]  NVARCHAR (100) NOT NULL,
    [Country]   CHAR (2)       NOT NULL,
    CONSTRAINT [PK_Participant] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Country_Participant] FOREIGN KEY ([Country]) REFERENCES [dbo].[Country] ([ISO]) ON UPDATE CASCADE
);

