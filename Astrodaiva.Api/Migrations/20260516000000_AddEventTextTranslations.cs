using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrodaiva.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTextTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventDescription",
                table: "astro_events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EventDescriptionEn",
                table: "astro_events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EventTextEn",
                table: "astro_events",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventDescription",
                table: "astro_events");

            migrationBuilder.DropColumn(
                name: "EventDescriptionEn",
                table: "astro_events");

            migrationBuilder.DropColumn(
                name: "EventTextEn",
                table: "astro_events");
        }
    }
}
