using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTrackFileStatusWithFileStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Placeholder rows created by the old CreateTrack (no upload requested) — no TrackFile now means no audio
            migrationBuilder.Sql("DELETE FROM [TrackFiles] WHERE [RawFilePath] IS NULL;");

            // Convert TrackFileStatus (tinyint) to FileStatus (string) preserving meaning:
            // Pending(0) → PendingUpload, Processing(1) → Processing, Ready(2) → Available, Failed(3) → Failed
            migrationBuilder.AddColumn<string>(
                name: "StatusNew",
                table: "TrackFiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [TrackFiles]
                SET [StatusNew] = CASE [Status]
                    WHEN 0 THEN 'PendingUpload'
                    WHEN 1 THEN 'Processing'
                    WHEN 2 THEN 'Available'
                    ELSE 'Failed'
                END;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrackFiles");

            migrationBuilder.RenameColumn(
                name: "StatusNew",
                table: "TrackFiles",
                newName: "Status");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TrackFiles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FileId",
                table: "TrackFiles",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileId",
                table: "TrackFiles");

            migrationBuilder.AddColumn<byte>(
                name: "StatusOld",
                table: "TrackFiles",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            // PendingValidation has no TrackFileStatus equivalent — map it back to Pending(0)
            migrationBuilder.Sql(@"
                UPDATE [TrackFiles]
                SET [StatusOld] = CASE [Status]
                    WHEN 'Processing' THEN 1
                    WHEN 'Available' THEN 2
                    WHEN 'Failed' THEN 3
                    ELSE 0
                END;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrackFiles");

            migrationBuilder.RenameColumn(
                name: "StatusOld",
                table: "TrackFiles",
                newName: "Status");
        }
    }
}
