using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SwarmApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdsAndDiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Households",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Households_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalSchema: "app",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Children_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "app",
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entries_Children_ChildId",
                        column: x => x.ChildId,
                        principalSchema: "app",
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entries_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "app",
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_HouseholdId",
                schema: "app",
                table: "Children",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ChildId_OccurredAt",
                schema: "app",
                table: "Entries",
                columns: new[] { "ChildId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_HouseholdId_OccurredAt",
                schema: "app",
                table: "Entries",
                columns: new[] { "HouseholdId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Households_OwnerUserId",
                schema: "app",
                table: "Households",
                column: "OwnerUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Children",
                schema: "app");

            migrationBuilder.DropTable(
                name: "Households",
                schema: "app");
        }
    }
}
