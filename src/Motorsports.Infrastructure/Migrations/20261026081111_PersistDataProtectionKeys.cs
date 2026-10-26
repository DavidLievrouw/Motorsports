using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS `DataProtectionKeys` (
                    `Id` int NOT NULL AUTO_INCREMENT,
                    `FriendlyName` varchar(255) NULL,
                    `Xml` longtext NULL,
                    CONSTRAINT `PK_DataProtectionKeys` PRIMARY KEY (`Id`)
                ) CHARACTER SET=utf8mb4;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS `DataProtectionKeys`;");
        }
    }
}
