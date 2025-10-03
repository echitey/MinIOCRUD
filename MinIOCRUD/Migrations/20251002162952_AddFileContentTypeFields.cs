using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinIOCRUD.Migrations
{
    /// <inheritdoc />
    public partial class AddFileContentTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FriendlyContentType",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeContentType",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FriendlyContentType",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "SafeContentType",
                table: "Files");
        }
    }
}
