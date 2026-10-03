using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PjBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddLifeInsuranceAndSchoolWithholding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GroupTermLifePerCheck",
                table: "PayrollSources",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // Existing sources get the default treatment (DefaultTaxTreatment.GroupTermLife) and keep withholding
            // school district tax, which is what the engine did before these settings existed.
            migrationBuilder.AddColumn<string>(
                name: "GroupTermLifeTaxedFor",
                table: "PayrollSources",
                type: "TEXT",
                nullable: false,
                defaultValue: "SS|MEDICARE|CITY");

            migrationBuilder.AddColumn<bool>(
                name: "WithholdsSchoolDistrictTax",
                table: "PayrollSources",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupTermLifePerCheck",
                table: "PayrollSources");

            migrationBuilder.DropColumn(
                name: "GroupTermLifeTaxedFor",
                table: "PayrollSources");

            migrationBuilder.DropColumn(
                name: "WithholdsSchoolDistrictTax",
                table: "PayrollSources");
        }
    }
}
