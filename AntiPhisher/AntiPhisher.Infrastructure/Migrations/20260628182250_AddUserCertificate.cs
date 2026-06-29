using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiPhisher.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ContentHash",
                table: "Scenarios",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationStatus",
                table: "Scenarios",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Scenarios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "Scenarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceScenarioIds",
                table: "Scenarios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AIExpandedKnowledges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContextDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SampleEmailContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DifficultyId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIExpandedKnowledges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIExpandedKnowledges_DifficultyLevels_DifficultyId",
                        column: x => x.DifficultyId,
                        principalTable: "DifficultyLevels",
                        principalColumn: "DifficultyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIExpandedKnowledges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserCertificates",
                columns: table => new
                {
                    CertificateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CertificateCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CorrectRateSnapshot = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalAttemptsSnapshot = table.Column<int>(type: "int", nullable: false),
                    FullNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompanyNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCertificates", x => x.CertificateId);
                    table.ForeignKey(
                        name: "FK_UserCertificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFlawSummaries",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TopTacticsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAttempts = table.Column<int>(type: "int", nullable: false),
                    TotalFlaws = table.Column<int>(type: "int", nullable: false),
                    CurrentRiskScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    LastAiAdviceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastAnalyzedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFlawSummaries", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserFlawSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scenarios_ReviewedByUserId",
                table: "Scenarios",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIExpandedKnowledges_CreatedByUserId",
                table: "AIExpandedKnowledges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIExpandedKnowledges_DifficultyId",
                table: "AIExpandedKnowledges",
                column: "DifficultyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCertificates_CertificateCode",
                table: "UserCertificates",
                column: "CertificateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCertificates_UserId",
                table: "UserCertificates",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scenarios_Users_ReviewedByUserId",
                table: "Scenarios",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scenarios_Users_ReviewedByUserId",
                table: "Scenarios");

            migrationBuilder.DropTable(
                name: "AIExpandedKnowledges");

            migrationBuilder.DropTable(
                name: "UserCertificates");

            migrationBuilder.DropTable(
                name: "UserFlawSummaries");

            migrationBuilder.DropIndex(
                name: "IX_Scenarios_ReviewedByUserId",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "GenerationStatus",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "SourceScenarioIds",
                table: "Scenarios");
        }
    }
}
