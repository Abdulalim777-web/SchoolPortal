using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicatePerformedByUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the foreign key that references the duplicate column
            migrationBuilder.DropForeignKey(
                name: "FK_BulkOperationAudits_AspNetUsers_PerformedByUserId1",
                table: "BulkOperationAudits");

            // Drop the index on the duplicate column
            migrationBuilder.DropIndex(
                name: "IX_BulkOperationAudits_PerformedByUserId1",
                table: "BulkOperationAudits");

            // Drop the duplicate column
            migrationBuilder.DropColumn(
                name: "PerformedByUserId1",
                table: "BulkOperationAudits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PerformedByUserId1",
                table: "BulkOperationAudits",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkOperationAudits_PerformedByUserId1",
                table: "BulkOperationAudits",
                column: "PerformedByUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BulkOperationAudits_AspNetUsers_PerformedByUserId1",
                table: "BulkOperationAudits",
                column: "PerformedByUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
