INSERT INTO Country (ISO, Name, NiceName)
SELECT CONCAT(CHAR(65 + FLOOR((seq - 1) / 26)), CHAR(65 + MOD(seq - 1, 26))),
       CONCAT('Synthetic country ', seq), CONCAT('Synthetic country ', seq)
FROM seq_1_to_253;
INSERT INTO Sport (Name, FullName) VALUES
('f1', 'Synthetic F1'), ('fe', 'Synthetic FE'), ('indycar', 'Synthetic IndyCar'),
('motogp', 'Synthetic MotoGP'), ('nascar', 'Synthetic NASCAR'), ('wec', 'Synthetic WEC');
INSERT INTO Status (Name, Step) VALUES
('Scheduled', 0), ('ReadyToWatch', 1), ('Postponed', 10),
('Cancelled', 11), ('Finished', 20), ('Watched', 30);
INSERT INTO Venue (Name, Country)
SELECT CONCAT('Circuit ', LPAD(seq, 4, '0')), 'AA' FROM seq_1_to_144;
INSERT INTO Participant (Title, FirstName, LastName, Country)
SELECT '', CONCAT('Driver ', seq), CONCAT('Racer ', LPAD(seq, 5, '0')), 'AA'
FROM seq_1_to_214;
INSERT INTO Team (Id, Name, Sport, Country)
SELECT seq, CONCAT('Team ', LPAD(seq, 4, '0')),
       ELT(1 + MOD(seq - 1, 6), 'f1', 'fe', 'indycar', 'motogp', 'nascar', 'wec'), 'AA'
FROM seq_1_to_78;
INSERT INTO Season (Id, Sport, Label)
SELECT seq, ELT(1 + MOD(seq - 1, 6), 'f1', 'fe', 'indycar', 'motogp', 'nascar', 'wec'),
       CONCAT(2012 + MOD(FLOOR((seq - 1) / 6), 15), ' cohort ', FLOOR((seq - 1) / 90))
FROM seq_1_to_90;
INSERT INTO SeasonEntry (Season, Team, Name)
SELECT s.Id, 1 + MOD(s.Id - 1, 6) + 6 * (n.seq - 1) + 78 * FLOOR((s.Id - 1) / 90),
       CONCAT('Entry ', s.Id, '-', n.seq)
FROM Season s CROSS JOIN seq_1_to_5 n;
INSERT INTO Round (Id, Date, Number, Name, Season, Venue, Status, Rating, Rain, WinningTeam)
SELECT seq, DATE_ADD(MAKEDATE(2012 + MOD(FLOOR((FLOOR((seq - 1) / 20)) / 6), 15), 1),
                     INTERVAL (30 + 16 * MOD(seq - 1, 20)) DAY),
       1 + MOD(seq - 1, 20), CONCAT('Event ', seq), 1 + FLOOR((seq - 1) / 20),
       CONCAT('Circuit ', LPAD(1 + MOD(seq - 1, 144), 4, '0')),
       IF(MOD(FLOOR(FLOOR((seq - 1) / 20) / 6), 15) = 14 AND MOD(seq - 1, 20) >= 15,
          IF(MOD(seq, 2) = 0, 'Scheduled', 'ReadyToWatch'), 'Finished'),
       8, 0, 1 + MOD(FLOOR((seq - 1) / 20), 6) + 6 * MOD(seq - 1, 5)
             + 78 * FLOOR(FLOOR((seq - 1) / 20) / 90)
FROM seq_1_to_1800;
INSERT INTO Round (Date, Number, Name, Season, Venue, Status)
SELECT '2026-10-01', 0, CONCAT('Exhibition ', seq), 1 + MOD(seq - 1, 90),
       CONCAT('Circuit ', LPAD(1 + MOD(seq - 1, 144), 4, '0')), 'Finished'
FROM seq_1_to_24;
INSERT INTO RoundWinner (Round, Participant)
SELECT Id, 1 + MOD(Id - 1, __PARTICIPANTS__) FROM Round WHERE Status = 'Finished';
INSERT INTO RoundWinner (Round, Participant)
SELECT Id, 1 + MOD(Id, __PARTICIPANTS__) FROM Round WHERE Status = 'Finished' AND MOD(Id, 10) = 0;
INSERT INTO SeasonWinner (Season, Participant)
SELECT Id, 1 + MOD(Id - 1, __PARTICIPANTS__) FROM Season;
