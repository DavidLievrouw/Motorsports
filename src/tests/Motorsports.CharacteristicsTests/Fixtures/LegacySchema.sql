DROP TABLE IF EXISTS `Country`;
CREATE TABLE `Country` (
  `ISO` char(2) NOT NULL,
  `Name` varchar(80) DEFAULT NULL,
  `NiceName` varchar(80) DEFAULT NULL,
  `ISO3` char(3) DEFAULT NULL,
  `NumCode` smallint(6) DEFAULT NULL,
  `PhoneCode` smallint(6) DEFAULT NULL,
  PRIMARY KEY (`ISO`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Participant`;
CREATE TABLE `Participant` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Title` varchar(10) NOT NULL,
  `FirstName` varchar(100) NOT NULL,
  `LastName` varchar(100) NOT NULL,
  `Country` char(2) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Participant_Country_FK` (`Country`),
  CONSTRAINT `Participant_Country_FK` FOREIGN KEY (`Country`) REFERENCES `Country` (`ISO`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Round`;
CREATE TABLE `Round` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Date` date NOT NULL,
  `Number` smallint(6) NOT NULL,
  `Name` varchar(100) DEFAULT NULL,
  `Season` int(11) NOT NULL,
  `Venue` varchar(100) NOT NULL,
  `Status` varchar(20) NOT NULL DEFAULT 'Scheduled',
  `Rating` decimal(3,1) DEFAULT NULL,
  `Rain` decimal(1,0) DEFAULT NULL,
  `WinningTeam` int(11) DEFAULT NULL,
  `Note` text DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Round_Season_FK` (`Season`),
  KEY `Round_Status_FK` (`Status`),
  KEY `Round_Team_FK` (`WinningTeam`),
  KEY `Round_Venue_FK` (`Venue`),
  CONSTRAINT `Round_Season_FK` FOREIGN KEY (`Season`) REFERENCES `Season` (`Id`),
  CONSTRAINT `Round_Status_FK` FOREIGN KEY (`Status`) REFERENCES `Status` (`Name`),
  CONSTRAINT `Round_Team_FK` FOREIGN KEY (`WinningTeam`) REFERENCES `Team` (`Id`),
  CONSTRAINT `Round_Venue_FK` FOREIGN KEY (`Venue`) REFERENCES `Venue` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `RoundWinner`;
CREATE TABLE `RoundWinner` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Round` int(11) NOT NULL,
  `Participant` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `RoundWinner_Round_FK` (`Round`),
  KEY `RoundWinner_Participant_FK` (`Participant`),
  CONSTRAINT `RoundWinner_Participant_FK` FOREIGN KEY (`Participant`) REFERENCES `Participant` (`Id`),
  CONSTRAINT `RoundWinner_Round_FK` FOREIGN KEY (`Round`) REFERENCES `Round` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Season`;
CREATE TABLE `Season` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Sport` varchar(50) NOT NULL,
  `Label` varchar(100) DEFAULT NULL,
  `WinningTeam` int(11) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Season_Sport_FK` (`Sport`),
  KEY `Season_Team_FK` (`WinningTeam`),
  CONSTRAINT `Season_Sport_FK` FOREIGN KEY (`Sport`) REFERENCES `Sport` (`Name`),
  CONSTRAINT `Season_Team_FK` FOREIGN KEY (`WinningTeam`) REFERENCES `Team` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `SeasonEntry`;
CREATE TABLE `SeasonEntry` (
  `Season` int(11) NOT NULL,
  `Team` int(11) NOT NULL,
  `Name` varchar(50) NOT NULL,
  PRIMARY KEY (`Season`,`Team`),
  KEY `SeasonEntry_Team_FK` (`Team`),
  CONSTRAINT `SeasonEntry_Season_FK` FOREIGN KEY (`Season`) REFERENCES `Season` (`Id`),
  CONSTRAINT `SeasonEntry_Team_FK` FOREIGN KEY (`Team`) REFERENCES `Team` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `SeasonWinner`;
CREATE TABLE `SeasonWinner` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Season` int(11) NOT NULL,
  `Participant` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `SeasonWinner_Participant_FK` (`Participant`),
  KEY `SeasonWinner_Season_FK` (`Season`),
  CONSTRAINT `SeasonWinner_Participant_FK` FOREIGN KEY (`Participant`) REFERENCES `Participant` (`Id`),
  CONSTRAINT `SeasonWinner_Season_FK` FOREIGN KEY (`Season`) REFERENCES `Season` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Sport`;
CREATE TABLE `Sport` (
  `Name` varchar(50) NOT NULL,
  `FullName` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Status`;
CREATE TABLE `Status` (
  `Name` varchar(20) NOT NULL,
  `Step` tinyint(4) NOT NULL,
  PRIMARY KEY (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Team`;
CREATE TABLE `Team` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(50) NOT NULL,
  `Sport` varchar(50) NOT NULL,
  `Country` char(2) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `Team_Sport_FK` (`Sport`),
  KEY `Team_Country_FK` (`Country`),
  CONSTRAINT `Team_Country_FK` FOREIGN KEY (`Country`) REFERENCES `Country` (`ISO`),
  CONSTRAINT `Team_Sport_FK` FOREIGN KEY (`Sport`) REFERENCES `Sport` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `User`;
CREATE TABLE `User` (
  `Id` varchar(36) NOT NULL,
  `Username` varchar(255) NOT NULL,
  `PasswordHash` varbinary(50) NOT NULL,
  `Salt` varbinary(50) NOT NULL,
  `Iterations` int(11) NOT NULL,
  `Prf` varchar(10) NOT NULL,
  `ForceChangePassword` tinyint(1) NOT NULL DEFAULT 0,
  `Title` varchar(255) DEFAULT NULL,
  `GivenName` varchar(255) DEFAULT NULL,
  `FamilyName` varchar(255) DEFAULT NULL,
  `EmailAddress` varchar(255) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  CONSTRAINT `User_CHECK` CHECK (`Prf` = 'HMACSHA1' or `Prf` = 'HMACSHA256' or `Prf` = 'HMACSHA512')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
DROP TABLE IF EXISTS `Venue`;
CREATE TABLE `Venue` (
  `Name` varchar(100) NOT NULL,
  `Country` char(2) NOT NULL,
  PRIMARY KEY (`Name`),
  KEY `Venue_Country_FK` (`Country`),
  CONSTRAINT `Venue_Country_FK` FOREIGN KEY (`Country`) REFERENCES `Country` (`ISO`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
