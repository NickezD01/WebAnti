using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiPhisher.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhaseColorIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Phases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Phases",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Phases");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Phases");
        }
    }
}
