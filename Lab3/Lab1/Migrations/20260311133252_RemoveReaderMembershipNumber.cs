using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lab1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReaderMembershipNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Readers_MembershipNumber",
                table: "Readers");

            migrationBuilder.DropColumn(
                name: "MembershipNumber",
                table: "Readers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MembershipNumber",
                table: "Readers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Readers_MembershipNumber",
                table: "Readers",
                column: "MembershipNumber",
                unique: true);
        }
    }
}
