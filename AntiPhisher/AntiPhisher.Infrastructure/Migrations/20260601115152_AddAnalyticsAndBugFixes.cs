using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AntiPhisher.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAndBugFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserAnswer",
                table: "UserAttempts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClickedLink",
                table: "UserAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCredentialLeaked",
                table: "UserAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReported",
                table: "UserAttempts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsedSlots",
                table: "Subscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MaxSlots",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CompanyId",
                table: "Subscriptions",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Companies_CompanyId",
                table: "Subscriptions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.SetNull);

            // ────────────────────────────────────────────────────────────────────
            // DATA MIGRATION: SubscriptionPlan.Name (INT "0"/"1"/"2" → tên thật)
            // ────────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                UPDATE SubscriptionPlans SET Name = N'Gói Cơ Bản',        MaxSlots = 10  WHERE Name = '0'
                UPDATE SubscriptionPlans SET Name = N'Gói Chuyên Nghiệp', MaxSlots = 30  WHERE Name = '1'
                UPDATE SubscriptionPlans SET Name = N'Gói Doanh Nghiệp Pro', MaxSlots = 100 WHERE Name = '2'
            ");

            // ────────────────────────────────────────────────────────────────────
            // BUG 1 FIX: Dọn Subscription.PaymentStatus sai do bug order.Id vs order.SubscriptionId
            // Chỉ chạy nếu bảng Orders và Payments tồn tại (safe guard)
            // ────────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'Orders', N'U') IS NOT NULL
                   AND OBJECT_ID(N'Payments', N'U') IS NOT NULL
                BEGIN
                    UPDATE s
                    SET s.PaymentStatus = 0
                    FROM Subscriptions s
                    WHERE s.PaymentStatus != 0
                      AND EXISTS (
                          SELECT 1
                          FROM Orders o
                          INNER JOIN Payments p ON p.OrderId = o.Id
                          WHERE o.SubscriptionId = s.Id
                            AND p.StatusPayment = 4
                      )
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Companies_CompanyId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CompanyId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IsClickedLink",
                table: "UserAttempts");

            migrationBuilder.DropColumn(
                name: "IsCredentialLeaked",
                table: "UserAttempts");

            migrationBuilder.DropColumn(
                name: "IsReported",
                table: "UserAttempts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "UsedSlots",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MaxSlots",
                table: "SubscriptionPlans");

            migrationBuilder.AlterColumn<string>(
                name: "UserAnswer",
                table: "UserAttempts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
