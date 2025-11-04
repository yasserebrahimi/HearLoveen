using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HearLoveen.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users table
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            // Children table
            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Children_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Children_ParentUserId",
                table: "Children",
                column: "ParentUserId");

            // AudioSubmissions table
            migrationBuilder.CreateTable(
                name: "AudioSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioSubmissions_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AudioSubmissions_ChildId",
                table: "AudioSubmissions",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioSubmissions_UploadedAt",
                table: "AudioSubmissions",
                column: "UploadedAt");

            // FeatureVectors table
            migrationBuilder.CreateTable(
                name: "FeatureVectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VectorData = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureVectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureVectors_AudioSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "AudioSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureVectors_SubmissionId",
                table: "FeatureVectors",
                column: "SubmissionId");

            // FeedbackReports table
            migrationBuilder.CreateTable(
                name: "FeedbackReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Transcript = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Feedback = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackReports_AudioSubmissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "AudioSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReports_SubmissionId",
                table: "FeedbackReports",
                column: "SubmissionId",
                unique: true);

            // Consents table
            migrationBuilder.CreateTable(
                name: "Consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consents_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Consents_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consents_ParentUserId",
                table: "Consents",
                column: "ParentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Consents_ChildId",
                table: "Consents",
                column: "ChildId");

            // PhonemeRatings table
            migrationBuilder.CreateTable(
                name: "PhonemeRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Phoneme = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EloRating = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1500.0),
                    SampleCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhonemeRatings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhonemeRatings_Phoneme",
                table: "PhonemeRatings",
                column: "Phoneme",
                unique: true);

            // PhonemePrerequisites table
            migrationBuilder.CreateTable(
                name: "PhonemePrerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhonemeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhonemePrerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhonemePrerequisites_PhonemeRatings_PhonemeId",
                        column: x => x.PhonemeId,
                        principalTable: "PhonemeRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhonemePrerequisites_PhonemeRatings_PrerequisiteId",
                        column: x => x.PrerequisiteId,
                        principalTable: "PhonemeRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhonemePrerequisites_PhonemeId_PrerequisiteId",
                table: "PhonemePrerequisites",
                columns: new[] { "PhonemeId", "PrerequisiteId" },
                unique: true);

            // ChildCurricula table
            migrationBuilder.CreateTable(
                name: "ChildCurricula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPhonemeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MasteredPhonemes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildCurricula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildCurricula_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildCurricula_PhonemeRatings_CurrentPhonemeId",
                        column: x => x.CurrentPhonemeId,
                        principalTable: "PhonemeRatings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChildCurricula_ChildId",
                table: "ChildCurricula",
                column: "ChildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildCurricula_CurrentPhonemeId",
                table: "ChildCurricula",
                column: "CurrentPhonemeId");

            // TherapistAssignments table
            migrationBuilder.CreateTable(
                name: "TherapistAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TherapistUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapistAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapistAssignments_Users_TherapistUserId",
                        column: x => x.TherapistUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TherapistAssignments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TherapistAssignments_TherapistUserId_ChildId",
                table: "TherapistAssignments",
                columns: new[] { "TherapistUserId", "ChildId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TherapistAssignments_ChildId",
                table: "TherapistAssignments",
                column: "ChildId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TherapistAssignments");
            migrationBuilder.DropTable(name: "ChildCurricula");
            migrationBuilder.DropTable(name: "PhonemePrerequisites");
            migrationBuilder.DropTable(name: "PhonemeRatings");
            migrationBuilder.DropTable(name: "Consents");
            migrationBuilder.DropTable(name: "FeedbackReports");
            migrationBuilder.DropTable(name: "FeatureVectors");
            migrationBuilder.DropTable(name: "AudioSubmissions");
            migrationBuilder.DropTable(name: "Children");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
