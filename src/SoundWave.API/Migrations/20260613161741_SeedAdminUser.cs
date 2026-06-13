using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "Identity",
                table: "Users",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Email", "IsEmailVerified", "LockoutUntilUtc", "PasswordHash", "Role", "UpdatedBy", "UpdatedDate" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Utc), "admin@soundwave.com", true, null, "$2a$11$IuYQ6gpIFG5UdYqEi3U88.cSBGwoiZgqQOJwA37t7U7OOgmF//J5W", 2, null, null });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "AdminProfiles",
                columns: new[] { "Id", "CanApproveArtists", "CanLockUsers", "CanViewAuditLogs", "CreatedBy", "CreatedDate", "Department", "UpdatedBy", "UpdatedDate", "UserId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000003"), true, true, true, null, new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Utc), "IT", null, null, new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "UserProfiles",
                columns: new[] { "Id", "CountryId", "CreatedBy", "CreatedDate", "DateOfBirth", "DisplayName", "FirstName", "Gender", "Language", "LastName", "PhoneNumber", "ProfilePicUrl", "UpdatedBy", "UpdatedDate", "UserId" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), null, null, new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Utc), null, "Administrator", "System", 2, "en", "Admin", null, null, null, null, new Guid("00000000-0000-0000-0000-000000000001") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "AdminProfiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "Identity",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
