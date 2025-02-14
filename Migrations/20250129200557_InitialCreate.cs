using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Khronos4.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Congregation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdminRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SecurityQuestion1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecurityAnswer1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecurityQuestion2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecurityAnswer2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ElderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ElderEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ElderPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    AccountCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });*/
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
