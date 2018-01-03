CREATE TABLE [dbo].[Venue] (
    [Name]    NVARCHAR (50) NOT NULL,
    [Country] CHAR (2)      NOT NULL,
    CONSTRAINT [PK_Venue] PRIMARY KEY CLUSTERED ([Name] ASC),
    CONSTRAINT [FK_Country_Venue] FOREIGN KEY ([Country]) REFERENCES [dbo].[Country] ([ISO]) ON UPDATE CASCADE
);

