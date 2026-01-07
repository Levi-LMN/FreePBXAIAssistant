using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreePBXAIAssistant.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AISettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SystemPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", nullable: false, defaultValue: 0.7f),
                    MaxTokens = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 800),
                    ConfidenceThreshold = table.Column<float>(type: "REAL", nullable: false, defaultValue: 0.7f),
                    EnableTransferToAgent = table.Column<bool>(type: "INTEGER", nullable: false),
                    TransferKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    EscalationRules = table.Column<string>(type: "TEXT", nullable: false),
                    MaxConversationTurns = table.Column<int>(type: "INTEGER", nullable: false),
                    EnableSentimentAnalysis = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CallId = table.Column<string>(type: "TEXT", nullable: false),
                    CallerNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    Transcription = table.Column<string>(type: "TEXT", nullable: false),
                    AIResponses = table.Column<string>(type: "TEXT", nullable: false),
                    Intent = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Outcome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TransferredToAgent = table.Column<bool>(type: "INTEGER", nullable: false),
                    TransferReason = table.Column<string>(type: "TEXT", nullable: true),
                    AgentExtension = table.Column<string>(type: "TEXT", nullable: true),
                    ConversationLog = table.Column<string>(type: "TEXT", nullable: false),
                    SentimentScore = table.Column<float>(type: "REAL", nullable: false),
                    AudioFilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_CallId",
                table: "CallRecords",
                column: "CallId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_Intent",
                table: "CallRecords",
                column: "Intent");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_StartTime",
                table: "CallRecords",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_Category",
                table: "KnowledgeItems",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_IsActive",
                table: "KnowledgeItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AISettings");

            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "KnowledgeItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
