using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achappey.ChatGPTeams.Database.Migrations
{
    /// <inheritdoc />
    public partial class RelationUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_Model_ModelId",
                table: "Assistants");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_Assistants_AssistantId",
                table: "Functions");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_Conversations_ConversationId",
                table: "Functions");

            migrationBuilder.DropForeignKey(
                name: "FK_Functions_Prompts_PromptId",
                table: "Functions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prompts_Users_OwnerId",
                table: "Prompts");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Assistants_AssistantId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Resources_Conversations_ConversationId",
                table: "Resources");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Assistants_AssistantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssistantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Resources_AssistantId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_ConversationId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Functions_AssistantId",
                table: "Functions");

            migrationBuilder.DropIndex(
                name: "IX_Functions_ConversationId",
                table: "Functions");

            migrationBuilder.DropIndex(
                name: "IX_Functions_PromptId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "AssistantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssistantId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ChatType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AssistantId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "PromptId",
                table: "Functions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Conversations");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Prompts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Prompts",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ModelId",
                table: "Assistants",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Assistants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssistantFunction",
                columns: table => new
                {
                    AssistantsId = table.Column<int>(type: "int", nullable: false),
                    FunctionsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantFunction", x => new { x.AssistantsId, x.FunctionsId });
                    table.ForeignKey(
                        name: "FK_AssistantFunction_Assistants_AssistantsId",
                        column: x => x.AssistantsId,
                        principalTable: "Assistants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssistantFunction_Functions_FunctionsId",
                        column: x => x.FunctionsId,
                        principalTable: "Functions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssistantResource",
                columns: table => new
                {
                    AssistantsId = table.Column<int>(type: "int", nullable: false),
                    ResourcesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantResource", x => new { x.AssistantsId, x.ResourcesId });
                    table.ForeignKey(
                        name: "FK_AssistantResource_Assistants_AssistantsId",
                        column: x => x.AssistantsId,
                        principalTable: "Assistants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssistantResource_Resources_ResourcesId",
                        column: x => x.ResourcesId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationFunction",
                columns: table => new
                {
                    ConversationsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FunctionsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationFunction", x => new { x.ConversationsId, x.FunctionsId });
                    table.ForeignKey(
                        name: "FK_ConversationFunction_Conversations_ConversationsId",
                        column: x => x.ConversationsId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationFunction_Functions_FunctionsId",
                        column: x => x.FunctionsId,
                        principalTable: "Functions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationResource",
                columns: table => new
                {
                    ConversationsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResourcesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationResource", x => new { x.ConversationsId, x.ResourcesId });
                    table.ForeignKey(
                        name: "FK_ConversationResource_Conversations_ConversationsId",
                        column: x => x.ConversationsId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationResource_Resources_ResourcesId",
                        column: x => x.ResourcesId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FunctionPrompt",
                columns: table => new
                {
                    FunctionsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PromptsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionPrompt", x => new { x.FunctionsId, x.PromptsId });
                    table.ForeignKey(
                        name: "FK_FunctionPrompt_Functions_FunctionsId",
                        column: x => x.FunctionsId,
                        principalTable: "Functions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FunctionPrompt_Prompts_PromptsId",
                        column: x => x.PromptsId,
                        principalTable: "Prompts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assistants_OwnerId",
                table: "Assistants",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantFunction_FunctionsId",
                table: "AssistantFunction",
                column: "FunctionsId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantResource_ResourcesId",
                table: "AssistantResource",
                column: "ResourcesId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationFunction_FunctionsId",
                table: "ConversationFunction",
                column: "FunctionsId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationResource_ResourcesId",
                table: "ConversationResource",
                column: "ResourcesId");

            migrationBuilder.CreateIndex(
                name: "IX_FunctionPrompt_PromptsId",
                table: "FunctionPrompt",
                column: "PromptsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_Model_ModelId",
                table: "Assistants",
                column: "ModelId",
                principalTable: "Model",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_Users_OwnerId",
                table: "Assistants",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prompts_Users_OwnerId",
                table: "Prompts",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_Model_ModelId",
                table: "Assistants");

            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_Users_OwnerId",
                table: "Assistants");

            migrationBuilder.DropForeignKey(
                name: "FK_Prompts_Users_OwnerId",
                table: "Prompts");

            migrationBuilder.DropTable(
                name: "AssistantFunction");

            migrationBuilder.DropTable(
                name: "AssistantResource");

            migrationBuilder.DropTable(
                name: "ConversationFunction");

            migrationBuilder.DropTable(
                name: "ConversationResource");

            migrationBuilder.DropTable(
                name: "FunctionPrompt");

            migrationBuilder.DropIndex(
                name: "IX_Assistants_OwnerId",
                table: "Assistants");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Assistants");

            migrationBuilder.AddColumn<int>(
                name: "AssistantId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssistantId",
                table: "Resources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "Resources",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Prompts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "Prompts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "ChatType",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssistantId",
                table: "Functions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "Functions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptId",
                table: "Functions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "ModelId",
                table: "Assistants",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssistantId",
                table: "Users",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AssistantId",
                table: "Resources",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ConversationId",
                table: "Resources",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Functions_AssistantId",
                table: "Functions",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Functions_ConversationId",
                table: "Functions",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Functions_PromptId",
                table: "Functions",
                column: "PromptId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_Model_ModelId",
                table: "Assistants",
                column: "ModelId",
                principalTable: "Model",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_Assistants_AssistantId",
                table: "Functions",
                column: "AssistantId",
                principalTable: "Assistants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_Conversations_ConversationId",
                table: "Functions",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Functions_Prompts_PromptId",
                table: "Functions",
                column: "PromptId",
                principalTable: "Prompts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Prompts_Users_OwnerId",
                table: "Prompts",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Assistants_AssistantId",
                table: "Resources",
                column: "AssistantId",
                principalTable: "Assistants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Conversations_ConversationId",
                table: "Resources",
                column: "ConversationId",
                principalTable: "Conversations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Assistants_AssistantId",
                table: "Users",
                column: "AssistantId",
                principalTable: "Assistants",
                principalColumn: "Id");
        }
    }
}
