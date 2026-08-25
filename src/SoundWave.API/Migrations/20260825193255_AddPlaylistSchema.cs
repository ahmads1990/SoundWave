using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylistSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Playlist");

            migrationBuilder.CreateTable(
                name: "LikedAlbums",
                schema: "Playlist",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlbumId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LikedAlbums", x => new { x.UserId, x.AlbumId });
                });

            migrationBuilder.CreateTable(
                name: "LikedTracks",
                schema: "Playlist",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LikedTracks", x => new { x.UserId, x.TrackId });
                });

            migrationBuilder.CreateTable(
                name: "Playlists",
                schema: "Playlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Visibility = table.Column<byte>(type: "tinyint", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    TrackCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalDurationSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FollowerCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LikedPlaylists",
                schema: "Playlist",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LikedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LikedPlaylists", x => new { x.UserId, x.PlaylistId });
                    table.ForeignKey(
                        name: "FK_LikedPlaylists_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "Playlist",
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistCollaborators",
                schema: "Playlist",
                columns: table => new
                {
                    PlaylistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistCollaborators", x => new { x.PlaylistId, x.UserId });
                    table.ForeignKey(
                        name: "FK_PlaylistCollaborators_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "Playlist",
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaylistTracks",
                schema: "Playlist",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaylistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaylistTracks_Playlists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalSchema: "Playlist",
                        principalTable: "Playlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LikedAlbums_AlbumId",
                schema: "Playlist",
                table: "LikedAlbums",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_LikedAlbums_UserId",
                schema: "Playlist",
                table: "LikedAlbums",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LikedPlaylists_PlaylistId",
                schema: "Playlist",
                table: "LikedPlaylists",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_LikedPlaylists_UserId",
                schema: "Playlist",
                table: "LikedPlaylists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LikedTracks_TrackId",
                schema: "Playlist",
                table: "LikedTracks",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_LikedTracks_UserId",
                schema: "Playlist",
                table: "LikedTracks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistCollaborators_PlaylistId",
                schema: "Playlist",
                table: "PlaylistCollaborators",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistCollaborators_UserId",
                schema: "Playlist",
                table: "PlaylistCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_IsDeleted",
                schema: "Playlist",
                table: "Playlists",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OwnerId",
                schema: "Playlist",
                table: "Playlists",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_OwnerId_IsSystem",
                schema: "Playlist",
                table: "Playlists",
                columns: new[] { "OwnerId", "IsSystem" });

            migrationBuilder.CreateIndex(
                name: "IX_Playlists_Visibility",
                schema: "Playlist",
                table: "Playlists",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_IsDeleted",
                schema: "Playlist",
                table: "PlaylistTracks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_PlaylistId_Position",
                schema: "Playlist",
                table: "PlaylistTracks",
                columns: new[] { "PlaylistId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaylistTracks_PlaylistId_TrackId",
                schema: "Playlist",
                table: "PlaylistTracks",
                columns: new[] { "PlaylistId", "TrackId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LikedAlbums",
                schema: "Playlist");

            migrationBuilder.DropTable(
                name: "LikedPlaylists",
                schema: "Playlist");

            migrationBuilder.DropTable(
                name: "LikedTracks",
                schema: "Playlist");

            migrationBuilder.DropTable(
                name: "PlaylistCollaborators",
                schema: "Playlist");

            migrationBuilder.DropTable(
                name: "PlaylistTracks",
                schema: "Playlist");

            migrationBuilder.DropTable(
                name: "Playlists",
                schema: "Playlist");
        }
    }
}
