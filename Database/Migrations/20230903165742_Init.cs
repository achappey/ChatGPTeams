using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace achappey.ChatGPTeams.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Model",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxTokenSize = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Model", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assistants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assistants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assistants_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assistants_Model_ModelId",
                        column: x => x.ModelId,
                        principalTable: "Model",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssistantId = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    ChatType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CutOff = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversations_Assistants_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Assistants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    AssistantId = table.Column<int>(type: "int", nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prompts_Assistants_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Assistants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prompts_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prompts_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeamsId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChatType = table.Column<int>(type: "int", nullable: false),
                    FunctionCall = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssistantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Assistants_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Assistants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Functions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssistantId = table.Column<int>(type: "int", nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PromptId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Functions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Functions_Assistants_AssistantId",
                        column: x => x.AssistantId,
                        principalTable: "Assistants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Functions_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Functions_Prompts_PromptId",
                        column: x => x.PromptId,
                        principalTable: "Prompts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assistants_DepartmentId",
                table: "Assistants",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assistants_ModelId",
                table: "Assistants",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_AssistantId",
                table: "Conversations",
                column: "AssistantId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_AssistantId",
                table: "Prompts",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_DepartmentId",
                table: "Prompts",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_OwnerId",
                table: "Prompts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AssistantId",
                table: "Resources",
                column: "AssistantId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ConversationId",
                table: "Resources",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Functions");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Assistants");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Model");
        }
    }
}
