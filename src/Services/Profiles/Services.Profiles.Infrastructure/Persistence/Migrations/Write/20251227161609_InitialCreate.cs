using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Services.Profiles.Infrastructure.Persistence.Migrations.Write
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "write");

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "write",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "write",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specializations",
                schema: "write",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specializations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                schema: "write",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CareerStartYear = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "write",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpecializationServices",
                schema: "write",
                columns: table => new
                {
                    SpecializationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecializationServices", x => new { x.SpecializationId, x.ServiceId });
                    table.ForeignKey(
                        name: "FK_SpecializationServices_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "write",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecializationServices_Specializations_SpecializationId",
                        column: x => x.SpecializationId,
                        principalSchema: "write",
                        principalTable: "Specializations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "write",
                table: "Services",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "Analyses" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, "Consultation" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), true, "Diagnostics" }
                });

            migrationBuilder.InsertData(
                schema: "write",
                table: "Specializations",
                columns: new[] { "Id", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), true, "Therapist" },
                    { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), true, "Surgeon" },
                    { new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), true, "Cardiologist" }
                });

            migrationBuilder.InsertData(
                schema: "write",
                table: "SpecializationServices",
                columns: new[] { "ServiceId", "SpecializationId" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890") },
                    { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901") },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901") },
                    { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012") },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012") },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_Email",
                schema: "write",
                table: "Doctors",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecializationId",
                schema: "write",
                table: "Doctors",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn_OccurredOn",
                schema: "write",
                table: "OutboxMessages",
                columns: new[] { "ProcessedOn", "OccurredOn" },
                filter: "\"ProcessedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Name",
                schema: "write",
                table: "Services",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specializations_Name",
                schema: "write",
                table: "Specializations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpecializationServices_ServiceId",
                schema: "write",
                table: "SpecializationServices",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Doctors",
                schema: "write");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "write");

            migrationBuilder.DropTable(
                name: "SpecializationServices",
                schema: "write");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "write");

            migrationBuilder.DropTable(
                name: "Specializations",
                schema: "write");
        }
    }
}
