using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMaster.Migrations
{
    /// <inheritdoc />
    public partial class Update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsListedForSale",
                table: "Tickets",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Tickets",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_OwnerUserId",
                table: "Tickets",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_OwnerUserId",
                table: "Tickets",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_OwnerUserId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_OwnerUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsListedForSale",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tickets");
        }
    }
}
