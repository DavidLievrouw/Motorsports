using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Motorsports.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSportImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `Sport` ADD COLUMN IF NOT EXISTS `Image` longblob NULL;");
            foreach (var name in new[] { "f1", "fe", "indycar", "motogp", "nascar", "wec" })
            {
                var image = ReadImage(name);
                migrationBuilder.Sql(
                    $"UPDATE `Sport` SET `Image` = 0x{Convert.ToHexString(image)} WHERE `Name` = '{name}' AND `Image` IS NULL;"
                );
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `Sport` DROP COLUMN IF EXISTS `Image`;");
        }

        private static byte[] ReadImage(string name)
        {
            var resourceName = $"Motorsports.Infrastructure.Migrations.SportImages.{name}.png";
            using var stream =
                typeof(AddSportImages).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded sport image resource '{resourceName}' is missing.");
            using var image = new MemoryStream();
            stream.CopyTo(image);
            return image.ToArray();
        }
    }
}
