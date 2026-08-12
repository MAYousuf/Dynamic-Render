using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnicalInspection.PoC.Migrations
{
    /// <inheritdoc />
    public partial class Added_TechnicalInspection_Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppEvidenceInspectionMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InspectionTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEvidenceInspectionMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppEvidenceTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEvidenceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppInspectionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInspectionRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppInspectionTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInspectionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppExhibits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppExhibits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppExhibits_AppInspectionRequests_InspectionRequestId",
                        column: x => x.InspectionRequestId,
                        principalTable: "AppInspectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExhibitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppEvidences_AppExhibits_ExhibitId",
                        column: x => x.ExhibitId,
                        principalTable: "AppExhibits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InspectionTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DataDiscriminator = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InspectionDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppInspections_AppEvidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "AppEvidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppEvidenceInspectionMappings_EvidenceTypeCode_InspectionTypeCode",
                table: "AppEvidenceInspectionMappings",
                columns: new[] { "EvidenceTypeCode", "InspectionTypeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppEvidences_ExhibitId",
                table: "AppEvidences",
                column: "ExhibitId");

            migrationBuilder.CreateIndex(
                name: "IX_AppEvidenceTypes_Code",
                table: "AppEvidenceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppExhibits_InspectionRequestId",
                table: "AppExhibits",
                column: "InspectionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInspectionRequests_RequestNumber",
                table: "AppInspectionRequests",
                column: "RequestNumber");

            migrationBuilder.CreateIndex(
                name: "IX_AppInspections_EvidenceId",
                table: "AppInspections",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppInspections_EvidenceTypeCode_InspectionTypeCode",
                table: "AppInspections",
                columns: new[] { "EvidenceTypeCode", "InspectionTypeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_AppInspectionTypes_Code",
                table: "AppInspectionTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppEvidenceInspectionMappings");

            migrationBuilder.DropTable(
                name: "AppEvidenceTypes");

            migrationBuilder.DropTable(
                name: "AppInspections");

            migrationBuilder.DropTable(
                name: "AppInspectionTypes");

            migrationBuilder.DropTable(
                name: "AppEvidences");

            migrationBuilder.DropTable(
                name: "AppExhibits");

            migrationBuilder.DropTable(
                name: "AppInspectionRequests");
        }
    }
}
