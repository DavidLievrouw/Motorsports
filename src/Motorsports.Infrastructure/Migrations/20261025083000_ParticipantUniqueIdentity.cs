using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    [Migration("20261025083000_ParticipantUniqueIdentity")]
    public partial class ParticipantUniqueIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE `Participant`
                SET `Title` = TRIM(`Title`), `FirstName` = TRIM(`FirstName`), `LastName` = TRIM(`LastName`);
                """
            );
            migrationBuilder.Sql(
                """
                DELETE p
                FROM `Participant` p
                INNER JOIN `Participant` older
                    ON older.`Title` = p.`Title`
                    AND older.`FirstName` = p.`FirstName`
                    AND older.`LastName` = p.`LastName`
                    AND older.`Country` = p.`Country`
                    AND older.`Id` < p.`Id`
                WHERE NOT EXISTS (SELECT 1 FROM `RoundWinner` rw WHERE rw.`Participant` = p.`Id`)
                  AND NOT EXISTS (SELECT 1 FROM `SeasonWinner` sw WHERE sw.`Participant` = p.`Id`);
                """
            );
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS `IX_Participant_Title_FirstName_LastName_Country`
                ON `Participant` (`Title`, `FirstName`, `LastName`, `Country`);
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS `IX_Participant_Title_FirstName_LastName_Country` ON `Participant`;"
            );
        }
    }
}
