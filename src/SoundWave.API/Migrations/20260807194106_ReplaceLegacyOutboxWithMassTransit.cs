using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoundWave.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceLegacyOutboxWithMassTransit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Sent_IsDeadLetter_RetryCount",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Error",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Exchange",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IsDeadLetter",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Sent",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.RenameColumn(
                name: "RoutingKey",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "ExpirationTime");

            migrationBuilder.RenameColumn(
                name: "Payload",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "MessageType");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "SentTime");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "MessageId");

            migrationBuilder.AddColumn<long>(
                name: "SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "Body",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationAddress",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnqueueTime",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaultAddress",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headers",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InboxConsumerId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InboxMessageId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InitiatorId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OutboxId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Properties",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseAddress",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceAddress",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "SharedKernel",
                table: "OutboxMessages",
                column: "SequenceNumber");

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "SharedKernel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiveCount = table.Column<int>(type: "int", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Consumed = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Delivered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                schema: "SharedKernel",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Delivered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EnqueueTime",
                schema: "SharedKernel",
                table: "OutboxMessages",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ExpirationTime",
                schema: "SharedKernel",
                table: "OutboxMessages",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true,
                filter: "[InboxMessageId] IS NOT NULL AND [InboxConsumerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_OutboxId_SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true,
                filter: "[OutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "SharedKernel",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "SharedKernel",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_InboxState_InboxMessageId_InboxConsumerId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                columns: new[] { "InboxMessageId", "InboxConsumerId" },
                principalSchema: "SharedKernel",
                principalTable: "InboxState",
                principalColumns: new[] { "MessageId", "ConsumerId" });

            migrationBuilder.AddForeignKey(
                name: "FK_OutboxMessages_OutboxState_OutboxId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                column: "OutboxId",
                principalSchema: "SharedKernel",
                principalTable: "OutboxState",
                principalColumn: "OutboxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_InboxState_InboxMessageId_InboxConsumerId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_OutboxMessages_OutboxState_OutboxId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "SharedKernel");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "SharedKernel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_EnqueueTime",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ExpirationTime",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_OutboxId_SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Body",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DestinationAddress",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "EnqueueTime",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "FaultAddress",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Headers",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "InboxConsumerId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "InboxMessageId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "InitiatorId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "OutboxId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Properties",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RequestId",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "ResponseAddress",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "SourceAddress",
                schema: "SharedKernel",
                table: "OutboxMessages");

            migrationBuilder.RenameColumn(
                name: "SentTime",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "MessageType",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "Payload");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ExpirationTime",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                schema: "SharedKernel",
                table: "OutboxMessages",
                newName: "RoutingKey");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeadLetter",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Sent",
                schema: "SharedKernel",
                table: "OutboxMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "SharedKernel",
                table: "OutboxMessages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Sent_IsDeadLetter_RetryCount",
                schema: "SharedKernel",
                table: "OutboxMessages",
                columns: new[] { "Sent", "IsDeadLetter", "RetryCount" });
        }
    }
}
