using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Services.Profiles.Infrastructure.Persistence.Migrations.Write
{
    /// <inheritdoc />
    public partial class AddDoctorSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "write",
                table: "Doctors",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "write",
                table: "Doctors");
        }
    }
}
