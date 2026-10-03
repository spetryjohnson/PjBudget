using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PjBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxYears",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Fica_SocialSecurityRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fica_SocialSecurityWageBase = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fica_MedicareRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fica_AdditionalMedicareRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    Fica_AdditionalMedicareWithholdingThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_ElectiveDeferral = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_CatchUpAge50 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_CatchUpAge60To63 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_HsaSelfOnly = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_HsaFamily = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_HsaCatchUpAge55 = table.Column<decimal>(type: "TEXT", nullable: false),
                    Limits_HealthFsa = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ohio_WithholdingExemptionAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ohio_ExemptionMagiLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ohio_JointFilingCreditCap = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ohio_JointFilingCreditMagiLimit = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ohio_JointFilingCreditMinSpouseIncome = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FederalFilingStatusParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaxYearId = table.Column<int>(type: "INTEGER", nullable: false),
                    FilingStatus = table.Column<string>(type: "TEXT", nullable: false),
                    StandardDeduction = table.Column<decimal>(type: "TEXT", nullable: false),
                    StandardWithholdingAdjustment = table.Column<decimal>(type: "TEXT", nullable: false),
                    AdditionalMedicareLiabilityThreshold = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FederalFilingStatusParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FederalFilingStatusParameters_TaxYears_TaxYearId",
                        column: x => x.TaxYearId,
                        principalTable: "TaxYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxScheduleRow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaxYearId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    FilingStatus = table.Column<string>(type: "TEXT", nullable: true),
                    Over = table.Column<decimal>(type: "TEXT", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxScheduleRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxScheduleRow_TaxYears_TaxYearId",
                        column: x => x.TaxYearId,
                        principalTable: "TaxYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FederalFilingStatusParameters_TaxYearId_FilingStatus",
                table: "FederalFilingStatusParameters",
                columns: new[] { "TaxYearId", "FilingStatus" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxScheduleRow_TaxYearId",
                table: "TaxScheduleRow",
                column: "TaxYearId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxYears_Year",
                table: "TaxYears",
                column: "Year",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FederalFilingStatusParameters");

            migrationBuilder.DropTable(
                name: "TaxScheduleRow");

            migrationBuilder.DropTable(
                name: "TaxYears");
        }
    }
}
