/****** Object:  Database [Motorsports]    Script Date: 06/01/2022 16:55:05 ******/
CREATE DATABASE [Motorsports]  (EDITION = 'Basic', SERVICE_OBJECTIVE = 'Basic', MAXSIZE = 2 GB) WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;
GO
ALTER DATABASE [Motorsports] SET COMPATIBILITY_LEVEL = 150
GO
ALTER DATABASE [Motorsports] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [Motorsports] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [Motorsports] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [Motorsports] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [Motorsports] SET ARITHABORT OFF 
GO
ALTER DATABASE [Motorsports] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [Motorsports] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [Motorsports] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [Motorsports] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [Motorsports] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [Motorsports] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [Motorsports] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [Motorsports] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [Motorsports] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [Motorsports] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [Motorsports] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [Motorsports] SET  MULTI_USER 
GO
ALTER DATABASE [Motorsports] SET ENCRYPTION ON
GO
ALTER DATABASE [Motorsports] SET QUERY_STORE = ON
GO
ALTER DATABASE [Motorsports] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 7), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 10, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
/****** Object:  User [MotorsportsApp]    Script Date: 06/01/2022 16:55:05 ******/
CREATE USER [MotorsportsApp] FOR LOGIN [MotorsportsApp] WITH DEFAULT_SCHEMA=[dbo]
GO
sys.sp_addrolemember @rolename = N'db_datareader', @membername = N'MotorsportsApp'
GO
sys.sp_addrolemember @rolename = N'db_datawriter', @membername = N'MotorsportsApp'
GO
/****** Object:  Table [dbo].[Country]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Country](
	[ISO] [char](2) NOT NULL,
	[Name] [varchar](80) NULL,
	[NiceName] [varchar](80) NULL,
	[ISO3] [char](3) NULL,
	[NumCode] [smallint] NULL,
	[PhoneCode] [smallint] NULL,
 CONSTRAINT [PK_Country] PRIMARY KEY CLUSTERED 
(
	[ISO] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Participant]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Participant](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](10) NOT NULL,
	[FirstName] [nvarchar](100) NOT NULL,
	[LastName] [nvarchar](100) NOT NULL,
	[Country] [char](2) NOT NULL,
 CONSTRAINT [PK_Participant] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Round]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Round](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [date] NOT NULL,
	[Number] [smallint] NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Season] [int] NOT NULL,
	[Venue] [nvarchar](100) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[Rating] [decimal](3, 1) NULL,
	[Rain] [decimal](1, 0) NULL,
	[WinningTeam] [int] NULL,
	[Note] [nvarchar](max) NULL,
 CONSTRAINT [PK_Round] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RoundWinner]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoundWinner](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Round] [int] NOT NULL,
	[Participant] [int] NOT NULL,
 CONSTRAINT [PK_RoundWinner] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Season]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Season](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Sport] [nvarchar](50) NOT NULL,
	[Label] [nvarchar](100) NULL,
	[WinningTeam] [int] NULL,
 CONSTRAINT [PK_Season] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SeasonEntry]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SeasonEntry](
	[Season] [int] NOT NULL,
	[Team] [int] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Season] ASC,
	[Team] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SeasonWinner]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SeasonWinner](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Season] [int] NOT NULL,
	[Participant] [int] NOT NULL,
 CONSTRAINT [PK_SeasonWinner] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Sport]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Sport](
	[Name] [nvarchar](50) NOT NULL,
	[FullName] [nvarchar](100) NULL,
 CONSTRAINT [PK_Sport] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Status]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Status](
	[Name] [nvarchar](20) NOT NULL,
	[Step] [tinyint] NOT NULL,
 CONSTRAINT [PK_Status] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Team]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Team](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Sport] [nvarchar](50) NOT NULL,
	[Country] [char](2) NOT NULL,
 CONSTRAINT [PK_Team] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[Id] [uniqueidentifier] NOT NULL,
	[Username] [nvarchar](255) NOT NULL,
	[PasswordHash] [varbinary](50) NOT NULL,
	[Salt] [varbinary](50) NOT NULL,
	[Iterations] [int] NOT NULL,
	[Prf] [varchar](10) NOT NULL,
	[ForceChangePassword] [bit] NOT NULL,
	[Title] [nvarchar](255) NULL,
	[GivenName] [nvarchar](255) NULL,
	[FamilyName] [nvarchar](255) NULL,
	[EmailAddress] [nvarchar](255) NULL,
	[IsDeleted] [bit] NOT NULL,
 CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Venue]    Script Date: 06/01/2022 16:55:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Venue](
	[Name] [nvarchar](100) NOT NULL,
	[Country] [char](2) NOT NULL,
 CONSTRAINT [PK_Venue] PRIMARY KEY CLUSTERED 
(
	[Name] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Round] ADD  CONSTRAINT [DF_Round_Status]  DEFAULT (N'Scheduled') FOR [Status]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_ForceChangePassword]  DEFAULT ((0)) FOR [ForceChangePassword]
GO
ALTER TABLE [dbo].[User] ADD  CONSTRAINT [DF_User_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Participant]  WITH CHECK ADD  CONSTRAINT [FK_Country_Participant] FOREIGN KEY([Country])
REFERENCES [dbo].[Country] ([ISO])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Participant] CHECK CONSTRAINT [FK_Country_Participant]
GO
ALTER TABLE [dbo].[Round]  WITH CHECK ADD  CONSTRAINT [FK_Season_Round] FOREIGN KEY([Season])
REFERENCES [dbo].[Season] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Round] CHECK CONSTRAINT [FK_Season_Round]
GO
ALTER TABLE [dbo].[Round]  WITH CHECK ADD  CONSTRAINT [FK_Status_Round] FOREIGN KEY([Status])
REFERENCES [dbo].[Status] ([Name])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Round] CHECK CONSTRAINT [FK_Status_Round]
GO
ALTER TABLE [dbo].[Round]  WITH CHECK ADD  CONSTRAINT [FK_Team_Round] FOREIGN KEY([WinningTeam])
REFERENCES [dbo].[Team] ([Id])
GO
ALTER TABLE [dbo].[Round] CHECK CONSTRAINT [FK_Team_Round]
GO
ALTER TABLE [dbo].[Round]  WITH CHECK ADD  CONSTRAINT [FK_Venue_Round] FOREIGN KEY([Venue])
REFERENCES [dbo].[Venue] ([Name])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Round] CHECK CONSTRAINT [FK_Venue_Round]
GO
ALTER TABLE [dbo].[RoundWinner]  WITH CHECK ADD  CONSTRAINT [FK_Participant_RoundWinner] FOREIGN KEY([Participant])
REFERENCES [dbo].[Participant] ([Id])
GO
ALTER TABLE [dbo].[RoundWinner] CHECK CONSTRAINT [FK_Participant_RoundWinner]
GO
ALTER TABLE [dbo].[RoundWinner]  WITH CHECK ADD  CONSTRAINT [FK_Round_RoundWinner] FOREIGN KEY([Round])
REFERENCES [dbo].[Round] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RoundWinner] CHECK CONSTRAINT [FK_Round_RoundWinner]
GO
ALTER TABLE [dbo].[Season]  WITH CHECK ADD  CONSTRAINT [FK_Sport_Season] FOREIGN KEY([Sport])
REFERENCES [dbo].[Sport] ([Name])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Season] CHECK CONSTRAINT [FK_Sport_Season]
GO
ALTER TABLE [dbo].[Season]  WITH CHECK ADD  CONSTRAINT [FK_Team_Season] FOREIGN KEY([WinningTeam])
REFERENCES [dbo].[Team] ([Id])
GO
ALTER TABLE [dbo].[Season] CHECK CONSTRAINT [FK_Team_Season]
GO
ALTER TABLE [dbo].[SeasonEntry]  WITH CHECK ADD  CONSTRAINT [FK_Season_SeasonEntry] FOREIGN KEY([Season])
REFERENCES [dbo].[Season] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SeasonEntry] CHECK CONSTRAINT [FK_Season_SeasonEntry]
GO
ALTER TABLE [dbo].[SeasonEntry]  WITH CHECK ADD  CONSTRAINT [FK_Team_SeasonEntry] FOREIGN KEY([Team])
REFERENCES [dbo].[Team] ([Id])
GO
ALTER TABLE [dbo].[SeasonEntry] CHECK CONSTRAINT [FK_Team_SeasonEntry]
GO
ALTER TABLE [dbo].[SeasonWinner]  WITH CHECK ADD  CONSTRAINT [FK_Participant_SeasonWinner] FOREIGN KEY([Participant])
REFERENCES [dbo].[Participant] ([Id])
GO
ALTER TABLE [dbo].[SeasonWinner] CHECK CONSTRAINT [FK_Participant_SeasonWinner]
GO
ALTER TABLE [dbo].[SeasonWinner]  WITH CHECK ADD  CONSTRAINT [FK_Season_SeasonWinner] FOREIGN KEY([Season])
REFERENCES [dbo].[Season] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[SeasonWinner] CHECK CONSTRAINT [FK_Season_SeasonWinner]
GO
ALTER TABLE [dbo].[Team]  WITH CHECK ADD  CONSTRAINT [FK_Country_Team] FOREIGN KEY([Country])
REFERENCES [dbo].[Country] ([ISO])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Team] CHECK CONSTRAINT [FK_Country_Team]
GO
ALTER TABLE [dbo].[Team]  WITH CHECK ADD  CONSTRAINT [FK_Sport_Team] FOREIGN KEY([Sport])
REFERENCES [dbo].[Sport] ([Name])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Team] CHECK CONSTRAINT [FK_Sport_Team]
GO
ALTER TABLE [dbo].[Venue]  WITH CHECK ADD  CONSTRAINT [FK_Country_Venue] FOREIGN KEY([Country])
REFERENCES [dbo].[Country] ([ISO])
ON UPDATE CASCADE
GO
ALTER TABLE [dbo].[Venue] CHECK CONSTRAINT [FK_Country_Venue]
GO
ALTER TABLE [dbo].[User]  WITH CHECK ADD  CONSTRAINT [CK_User_Prf] CHECK  (([Prf]='HMACSHA1' OR [Prf]='HMACSHA256' OR [Prf]='HMACSHA512'))
GO
ALTER TABLE [dbo].[User] CHECK CONSTRAINT [CK_User_Prf]
GO
ALTER DATABASE [Motorsports] SET  READ_WRITE 
GO
