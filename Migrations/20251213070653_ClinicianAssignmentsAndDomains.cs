using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrapheneTrace.Migrations
{
    /// <inheritdoc />
    public partial class ClinicianAssignmentsAndDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleType",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "ClinicianPatientAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClinicianUserId = table.Column<string>(type: "TEXT", nullable: false),
                    PatientUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicianPatientAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDomains",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDomains", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicianPatientAssignments_ClinicianUserId_PatientUserId",
                table: "ClinicianPatientAssignments",
                columns: new[] { "ClinicianUserId", "PatientUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicianPatientAssignments");

            migrationBuilder.DropTable(
                name: "UserDomains");

            migrationBuilder.AddColumn<string>(
                name: "RoleType",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }
    }
}
