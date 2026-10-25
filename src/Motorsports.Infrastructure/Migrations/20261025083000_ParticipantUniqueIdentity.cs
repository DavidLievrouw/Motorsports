using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantUniqueIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
