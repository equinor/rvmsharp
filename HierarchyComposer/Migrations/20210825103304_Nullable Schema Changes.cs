using Microsoft.EntityFrameworkCore.Migrations;

namespace Mop.Hierarchy.Migrations
{
    public partial class NullableSchemaChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeToPDMSEntries");

            migrationBuilder.AlterColumn<double>(
                name: "min_z",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "min_y",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "min_x",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "max_z",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "max_y",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "max_x",
                table: "AABBs",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosticInfo",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefNoDb",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefNoSequence",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NodeToPDMSEntry",
                columns: table => new
                {
                    NodeId = table.Column<uint>(type: "INTEGER", nullable: false),
                    PDMSEntryId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeToPDMSEntry", x => new { x.NodeId, x.PDMSEntryId });
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntry_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntry_PDMSEntries_PDMSEntryId",
                        column: x => x.PDMSEntryId,
                        principalTable: "PDMSEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeToPDMSEntry_PDMSEntryId",
                table: "NodeToPDMSEntry",
                column: "PDMSEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeToPDMSEntry");

            migrationBuilder.DropColumn(
                name: "DiagnosticInfo",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "RefNoDb",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "RefNoSequence",
                table: "Nodes");

            migrationBuilder.AlterColumn<float>(
                name: "min_z",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "min_y",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "min_x",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "max_z",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "max_y",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<float>(
                name: "max_x",
                table: "AABBs",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.CreateTable(
                name: "NodeToPDMSEntries",
                columns: table => new
                {
                    NodeId = table.Column<uint>(type: "INTEGER", nullable: false),
                    PDMSEntryId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeToPDMSEntries", x => new { x.NodeId, x.PDMSEntryId });
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntries_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeToPDMSEntries_PDMSEntries_PDMSEntryId",
                        column: x => x.PDMSEntryId,
                        principalTable: "PDMSEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeToPDMSEntries_PDMSEntryId",
                table: "NodeToPDMSEntries",
                column: "PDMSEntryId");
        }
    }
}
