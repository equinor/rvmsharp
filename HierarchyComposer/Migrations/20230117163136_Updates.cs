using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mop.Hierarchy.Migrations
{
    /// <inheritdoc />
    public partial class Updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_Nodes_ParentId",
                table: "Nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_Nodes_TopNodeId",
                table: "Nodes");

            migrationBuilder.AlterColumn<uint>(
                name: "TopNodeId",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u,
                oldClrType: typeof(uint),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "EndId",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndId",
                table: "Nodes");

            migrationBuilder.AlterColumn<uint>(
                name: "TopNodeId",
                table: "Nodes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(uint),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_Nodes_ParentId",
                table: "Nodes",
                column: "ParentId",
                principalTable: "Nodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_Nodes_TopNodeId",
                table: "Nodes",
                column: "TopNodeId",
                principalTable: "Nodes",
                principalColumn: "Id");
        }
    }
}
