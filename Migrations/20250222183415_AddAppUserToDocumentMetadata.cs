using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMS.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserToDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "DocumentMetadata",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentMetadata_UserId",
                table: "DocumentMetadata",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentMetadata_AspNetUsers_UserId",
                table: "DocumentMetadata",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentMetadata_AspNetUsers_UserId",
                table: "DocumentMetadata");

            migrationBuilder.DropIndex(
                name: "IX_DocumentMetadata_UserId",
                table: "DocumentMetadata");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "DocumentMetadata",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
