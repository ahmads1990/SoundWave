using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverImageUrlToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                schema: "Auth",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "Auth",
                table: "UserProfiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "CoverImageUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                schema: "Auth",
                table: "UserProfiles");
        }
    }
}
