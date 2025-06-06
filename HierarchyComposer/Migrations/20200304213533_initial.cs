using Microsoft.EntityFrameworkCore.Migrations;

namespace Mop.Hierarchy.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AABBs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    min_x = table.Column<float>(nullable: true),
                    min_y = table.Column<float>(nullable: true),
                    min_z = table.Column<float>(nullable: true),
                    max_x = table.Column<float>(nullable: true),
                    max_y = table.Column<float>(nullable: true),
                    max_z = table.Column<float>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AABBs", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "PDMSEntries",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PDMSEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<uint>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    HasMesh = table.Column<bool>(nullable: false),
                    ParentId = table.Column<uint>(nullable: true),
                    TopNodeId = table.Column<uint>(nullable: true),
                    AABBId = table.Column<int>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nodes_AABBs_AABBId",
                        column: x => x.AABBId,
                        principalTable: "AABBs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Nodes_Nodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_Nodes_Nodes_TopNodeId",
                        column: x => x.TopNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "NodeToPDMSEntries",
                columns: table => new
                {
                    NodeId = table.Column<uint>(nullable: false),
                    PDMSEntryId = table.Column<long>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeToPDMSEntries", x => new { x.NodeId, x.PDMSEntryId });
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntries_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntries_PDMSEntries_PDMSEntryId",
                        column: x => x.PDMSEntryId,
                        principalTable: "PDMSEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "IX_Nodes_AABBId", table: "Nodes", column: "AABBId");

            migrationBuilder.CreateIndex(name: "IX_Nodes_ParentId", table: "Nodes", column: "ParentId");

            migrationBuilder.CreateIndex(name: "IX_Nodes_TopNodeId", table: "Nodes", column: "TopNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeToPDMSEntries_PDMSEntryId",
                table: "NodeToPDMSEntries",
                column: "PDMSEntryId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NodeToPDMSEntries");

            migrationBuilder.DropTable(name: "Nodes");

            migrationBuilder.DropTable(name: "PDMSEntries");

            migrationBuilder.DropTable(name: "AABBs");
        }
    }
}
