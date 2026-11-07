using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedArtistAndListenerUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "Auth",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$AoniWBuNHQEcD61jvgci4OWzGxe4m54kcNj/hYXC2lzwH/hxJOoe2");

            migrationBuilder.InsertData(
                schema: "Auth",
                table: "Users",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Email", "IsEmailVerified", "LockoutUntilUtc", "PasswordHash", "Role", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000010"), null, new DateTime(2026, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc), "artist@soundwave.com", true, null, "$2a$11$AoniWBuNHQEcD61jvgci4OWzGxe4m54kcNj/hYXC2lzwH/hxJOoe2", 1, null, null },
                    { new Guid("00000000-0000-0000-0000-000000000020"), null, new DateTime(2026, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc), "listener@soundwave.com", true, null, "$2a$11$AoniWBuNHQEcD61jvgci4OWzGxe4m54kcNj/hYXC2lzwH/hxJOoe2", 0, null, null }
                });

            migrationBuilder.InsertData(
                schema: "Auth",
                table: "UserProfiles",
                columns: new[] { "Id", "CountryId", "CoverImageUrl", "CreatedBy", "CreatedDate", "DateOfBirth", "DisplayName", "FirstName", "Gender", "Language", "LastName", "PhoneNumber", "ProfilePicUrl", "UpdatedBy", "UpdatedDate", "UserId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000011"), null, null, null, new DateTime(2026, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, "Luna Echo", "Luna", 1, "en", "Echo", null, null, null, null, new Guid("00000000-0000-0000-0000-000000000010") },
                    { new Guid("00000000-0000-0000-0000-000000000021"), null, null, null, new DateTime(2026, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alex Rivera", "Alex", 0, "en", "Rivera", null, null, null, null, new Guid("00000000-0000-0000-0000-000000000020") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Auth",
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "Auth",
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                schema: "Auth",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                schema: "Auth",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020"));

            migrationBuilder.UpdateData(
                schema: "Auth",
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "PasswordHash",
                value: "$2a$11$IuYQ6gpIFG5UdYqEi3U88.cSBGwoiZgqQOJwA37t7U7OOgmF//J5W");
        }
    }
}
