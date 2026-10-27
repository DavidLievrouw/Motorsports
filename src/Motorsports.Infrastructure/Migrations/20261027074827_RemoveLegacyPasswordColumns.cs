using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPasswordColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1 FROM `User`
                    WHERE `IsDeleted` = 0 AND (`PasswordHashV3` IS NULL OR TRIM(`PasswordHashV3`) = '')
                ) THEN
                    SIGNAL SQLSTATE '45000'
                    SET MESSAGE_TEXT = 'Active users need Identity password hashes before removing legacy credentials.';
                END IF;
                """
            );
            migrationBuilder.Sql(
                """
                ALTER TABLE `User`
                    DROP COLUMN IF EXISTS `PasswordHash`,
                    DROP COLUMN IF EXISTS `Salt`,
                    DROP COLUMN IF EXISTS `Iterations`,
                    DROP COLUMN IF EXISTS `Prf`;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed credentials cannot be recovered; rollback restores only the schema.
            migrationBuilder.Sql(
                """
                ALTER TABLE `User`
                    ADD COLUMN IF NOT EXISTS `PasswordHash` varbinary(50) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS `Salt` varbinary(50) NOT NULL DEFAULT '',
                    ADD COLUMN IF NOT EXISTS `Iterations` int NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS `Prf` varchar(10) NOT NULL DEFAULT '';
                """
            );
        }
    }
}
