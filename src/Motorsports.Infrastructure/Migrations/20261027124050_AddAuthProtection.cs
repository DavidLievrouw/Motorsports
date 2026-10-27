using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `AuthThrottleState` (
                    `Key` varchar(80) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
                    `WindowStart` datetime(6) NOT NULL,
                    `Attempts` int NOT NULL,
                    `Failures` int NOT NULL,
                    `BlockedUntil` datetime(6) NOT NULL,
                    `ExpiresAt` datetime(6) NOT NULL,
                    `LastFailure` datetime(6) NOT NULL,
                    `LastAccount` varchar(80) CHARACTER SET ascii COLLATE ascii_bin NULL,
                    `AccountChanges` int NOT NULL,
                    `LastSuccess` datetime(6) NULL,
                    PRIMARY KEY (`Key`)
                ) CHARACTER SET utf8mb4;
                """
            );
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS `IX_AuthThrottleState_ExpiresAt` ON `AuthThrottleState` (`ExpiresAt`);"
            );
            migrationBuilder.Sql(
                """
                INSERT IGNORE INTO `AuthThrottleState`
                    (`Key`, `WindowStart`, `Attempts`, `Failures`, `BlockedUntil`, `ExpiresAt`, `LastFailure`, `AccountChanges`)
                VALUES ('global', '2000-01-01', 0, 0, '2000-01-01', '2000-01-01', '2000-01-01', 0);
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `AuthThrottleState`;");
        }
    }
}
