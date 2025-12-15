using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrapheneTrace.Migrations
{
    /// <inheritdoc />
    public partial class SystemSettingsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    AutoLogout = table.Column<string>(type: "TEXT", nullable: false),
                    DataRetention = table.Column<string>(type: "TEXT", nullable: false),
                    BackupFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    EmailNotifications = table.Column<string>(type: "TEXT", nullable: false),
                    SmsAlerts = table.Column<string>(type: "TEXT", nullable: false),
                    WeeklySummary = table.Column<string>(type: "TEXT", nullable: false),
                    MaintenanceMode = table.Column<string>(type: "TEXT", nullable: false),
                    MaxLoginAttempts = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
