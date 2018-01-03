CREATE TABLE [dbo].[User] (
    [Id]                  UNIQUEIDENTIFIER NOT NULL,
    [Username]            NVARCHAR (255)   NOT NULL,
    [PasswordHash]        VARBINARY (50)   NOT NULL,
    [Salt]                VARBINARY (50)   NOT NULL,
    [Iterations]          INT              NOT NULL,
    [Prf]                 VARCHAR (10)     NOT NULL,
    [ForceChangePassword] BIT              CONSTRAINT [DF_User_ForceChangePassword] DEFAULT ((0)) NOT NULL,
    [Title]               NVARCHAR (255)   NULL,
    [GivenName]           NVARCHAR (255)   NULL,
    [FamilyName]          NVARCHAR (255)   NULL,
    [EmailAddress]        NVARCHAR (255)   NULL,
    [IsDeleted]           BIT              CONSTRAINT [DF_User_IsDeleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_User_Prf] CHECK ([Prf]='HMACSHA1' OR [Prf]='HMACSHA256' OR [Prf]='HMACSHA512')
);

