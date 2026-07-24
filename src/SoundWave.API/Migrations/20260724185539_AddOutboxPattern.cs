using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SharedKernel");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "SharedKernel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RoutingKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsDeadLetter = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Sent_IsDeadLetter_RetryCount",
                schema: "SharedKernel",
                table: "OutboxMessages",
                columns: new[] { "Sent", "IsDeadLetter", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "SharedKernel");
        }
    }
}
