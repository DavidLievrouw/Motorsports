﻿CREATE TABLE [dbo].[Status] (
    [Name] NVARCHAR (20) NOT NULL,
    [Step] TINYINT NOT NULL, 
    CONSTRAINT [PK_Status] PRIMARY KEY CLUSTERED ([Name] ASC)
);

