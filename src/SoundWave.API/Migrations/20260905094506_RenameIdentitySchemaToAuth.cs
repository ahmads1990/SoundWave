using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameIdentitySchemaToAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Auth");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "Identity",
                newName: "Users",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "UserProfiles",
                schema: "Identity",
                newName: "UserProfiles",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "Identity",
                newName: "RefreshTokens",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "Countries",
                schema: "Identity",
                newName: "Countries",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "AdminProfiles",
                schema: "Identity",
                newName: "AdminProfiles",
                newSchema: "Auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "Auth",
                newName: "Users",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "UserProfiles",
                schema: "Auth",
                newName: "UserProfiles",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "Auth",
                newName: "RefreshTokens",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "Countries",
                schema: "Auth",
                newName: "Countries",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "AdminProfiles",
                schema: "Auth",
                newName: "AdminProfiles",
                newSchema: "Identity");
        }
    }
}
