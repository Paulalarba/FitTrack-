using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddQrEntryVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCodeToken",
                table: "AspNetUsers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CheckInLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberId = table.Column<string>(type: "text", nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInLogs_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_QrCodeToken",
                table: "AspNetUsers",
                column: "QrCodeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckInLogs_CheckInTime",
                table: "CheckInLogs",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_CheckInLogs_MemberId",
                table: "CheckInLogs",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInLogs");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_QrCodeToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "QrCodeToken",
                table: "AspNetUsers");
        }
    }
}
