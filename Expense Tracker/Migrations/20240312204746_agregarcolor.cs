using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expense_Tracker.Migrations
{
    public partial class agregarcolor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Categories",
                type: "nvarchar(5)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Categories");
        }
    }
}
