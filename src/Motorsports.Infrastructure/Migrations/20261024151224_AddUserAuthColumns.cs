using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuthColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Additive and self-guarding: the legacy app keeps using the legacy columns.
            migrationBuilder.Sql(
                "ALTER TABLE `User` ADD COLUMN IF NOT EXISTS `PasswordHashV3` varchar(512) NULL, "
                    + "ADD COLUMN IF NOT EXISTS `SecurityStamp` varchar(64) NULL, "
                    + "ADD COLUMN IF NOT EXISTS `ConcurrencyStamp` varchar(64) NULL;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `User` DROP COLUMN IF EXISTS `PasswordHashV3`, "
                    + "DROP COLUMN IF EXISTS `SecurityStamp`, DROP COLUMN IF EXISTS `ConcurrencyStamp`;"
            );
        }
    }
}
