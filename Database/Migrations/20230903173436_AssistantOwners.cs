using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achappey.ChatGPTeams.Database.Migrations
{
    /// <inheritdoc />
    public partial class AssistantOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssistantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssistantId",
                table: "Users",
                column: "AssistantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Assistants_AssistantId",
                table: "Users",
                column: "AssistantId",
                principalTable: "Assistants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Assistants_AssistantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssistantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssistantId",
                table: "Users");
        }
    }
}
