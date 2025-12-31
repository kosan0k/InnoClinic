using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Profiles.Infrastructure.Persistence.Migrations.Write
{
    /// <inheritdoc />
    public partial class AddDoctorDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "write",
                table: "Doctors",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "write",
                table: "Doctors");
        }
    }
}
