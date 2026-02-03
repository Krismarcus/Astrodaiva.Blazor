using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrodaiva.Api.Migrations
{
    /// <inheritdoc />
    public partial class AstroDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "appdb_snapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Label = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AppDbJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appdb_snapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "astro_events",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    MoonPhase = table.Column<int>(type: "int", nullable: false),
                    SunEclipse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MoonEclipse = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Barber = table.Column<int>(type: "int", nullable: false),
                    Beauty = table.Column<int>(type: "int", nullable: false),
                    BuyStuff = table.Column<int>(type: "int", nullable: false),
                    Contracts = table.Column<int>(type: "int", nullable: false),
                    ImportantTasks = table.Column<int>(type: "int", nullable: false),
                    Gardening = table.Column<int>(type: "int", nullable: false),
                    Love = table.Column<int>(type: "int", nullable: false),
                    Meetings = table.Column<int>(type: "int", nullable: false),
                    NewIdeas = table.Column<int>(type: "int", nullable: false),
                    Tech = table.Column<int>(type: "int", nullable: false),
                    Travel = table.Column<int>(type: "int", nullable: false),
                    EventText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoonDayNew = table.Column<int>(type: "int", nullable: false),
                    MoonDayMiddle = table.Column<int>(type: "int", nullable: false),
                    MoonDayPrevious = table.Column<int>(type: "int", nullable: false),
                    MoonDayIsTriple = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MoonDayTransitionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MoonDayMiddleTransitionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_astro_events", x => x.Date);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "moon_day_details",
                columns: table => new
                {
                    MoonDay = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MoonDayInfo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moon_day_details", x => x.MoonDay);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "planet_in_retrograde_details",
                columns: table => new
                {
                    PlanetInRetrograde = table.Column<int>(type: "int", nullable: false),
                    PlanetInRetrogradeInfo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planet_in_retrograde_details", x => x.PlanetInRetrograde);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "planet_in_zodiac_details",
                columns: table => new
                {
                    Planet = table.Column<int>(type: "int", nullable: false),
                    ZodiacSign = table.Column<int>(type: "int", nullable: false),
                    PlanetInZodiacInfo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planet_in_zodiac_details", x => new { x.Planet, x.ZodiacSign });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "astro_event_planets",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Planet = table.Column<int>(type: "int", nullable: false),
                    NewZodiacSign = table.Column<int>(type: "int", nullable: false),
                    PreviousZodiacSign = table.Column<int>(type: "int", nullable: false),
                    IsRetrograde = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsZodiacTransitioning = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TransitionTime = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_astro_event_planets", x => new { x.Date, x.Planet });
                    table.ForeignKey(
                        name: "FK_astro_event_planets_astro_events_Date",
                        column: x => x.Date,
                        principalTable: "astro_events",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "astro_planet_events",
                columns: table => new
                {
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Planet1 = table.Column<int>(type: "int", nullable: false),
                    Planet2 = table.Column<int>(type: "int", nullable: false),
                    AspectSymbol = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_astro_planet_events", x => new { x.Date, x.Planet1, x.Planet2, x.AspectSymbol });
                    table.ForeignKey(
                        name: "FK_astro_planet_events_astro_events_Date",
                        column: x => x.Date,
                        principalTable: "astro_events",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_astro_planet_events_Date",
                table: "astro_planet_events",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appdb_snapshots");

            migrationBuilder.DropTable(
                name: "astro_event_planets");

            migrationBuilder.DropTable(
                name: "astro_planet_events");

            migrationBuilder.DropTable(
                name: "moon_day_details");

            migrationBuilder.DropTable(
                name: "planet_in_retrograde_details");

            migrationBuilder.DropTable(
                name: "planet_in_zodiac_details");

            migrationBuilder.DropTable(
                name: "astro_events");
        }
    }
}
