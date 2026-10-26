using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Team_Sport_Name` ON `Team` (`Sport`, `Name`);"
            );
            migrationBuilder.Sql("DROP INDEX IF EXISTS `IX_Team_Sport` ON `Team`;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS `Team_Sport_FK` ON `Team`;");
            migrationBuilder.Sql(
                """
                ALTER TABLE `Round`
                ADD COLUMN IF NOT EXISTS `ChampionshipNumber` smallint
                    GENERATED ALWAYS AS (CASE WHEN `Number` > 0 THEN `Number` ELSE NULL END) PERSISTENT;
                """
            );
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS `IX_Round_Season_ChampionshipNumber` "
                    + "ON `Round` (`Season`, `ChampionshipNumber`);"
            );
            migrationBuilder.Sql("DROP INDEX IF EXISTS `IX_Round_Season` ON `Round`;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS `Round_Season_FK` ON `Round`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS `Team_Sport_FK` ON `Team` (`Sport`);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS `Round_Season_FK` ON `Round` (`Season`);");
            migrationBuilder.Sql("DROP INDEX IF EXISTS `IX_Team_Sport_Name` ON `Team`;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS `IX_Round_Season_ChampionshipNumber` ON `Round`;");
            migrationBuilder.Sql("ALTER TABLE `Round` DROP COLUMN IF EXISTS `ChampionshipNumber`;");
        }
    }
}
