CREATE TABLE [dbo].[Country] (
    [ISO]       CHAR (2)     NOT NULL,
    [Name]      VARCHAR (80) NULL,
    [NiceName]  VARCHAR (80) NULL,
    [ISO3]      CHAR (3)     NULL,
    [NumCode]   SMALLINT     NULL,
    [PhoneCode] SMALLINT     NULL,
    CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED ([ISO] ASC)
);

