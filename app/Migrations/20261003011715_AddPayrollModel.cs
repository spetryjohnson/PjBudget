using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PjBudget.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    MunicipalTaxRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    SchoolDistrictName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SchoolDistrictNumber = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    SchoolDistrictTaxRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    SchoolDistrictTaxBase = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScenarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    HomeLocaleId = table.Column<int>(type: "INTEGER", nullable: true),
                    HsaCoverage = table.Column<string>(type: "TEXT", nullable: false),
                    TaxFilingStatus = table.Column<string>(type: "TEXT", nullable: false),
                    FederalOtherIncome = table.Column<decimal>(type: "TEXT", nullable: false),
                    FederalAdjustments = table.Column<decimal>(type: "TEXT", nullable: false),
                    FederalItemizedDeductions = table.Column<decimal>(type: "TEXT", nullable: true),
                    FederalCredits = table.Column<decimal>(type: "TEXT", nullable: false),
                    OhioAdjustments = table.Column<decimal>(type: "TEXT", nullable: false),
                    OhioExemptionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    OhioOtherCredits = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdProfiles_Locales_HomeLocaleId",
                        column: x => x.HomeLocaleId,
                        principalTable: "Locales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdProfiles_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ScenarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkLocaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PayBasis = table.Column<string>(type: "TEXT", nullable: false),
                    AnnualSalary = table.Column<decimal>(type: "TEXT", nullable: true),
                    HourlyRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    HoursPerCheck = table.Column<decimal>(type: "TEXT", nullable: true),
                    PayFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    SemimonthlyPayDay1 = table.Column<int>(type: "INTEGER", nullable: true),
                    SemimonthlyPayDay2 = table.Column<int>(type: "INTEGER", nullable: true),
                    BiweeklyAnchorDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Traditional401kPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    Traditional401kPerCheckOverride = table.Column<decimal>(type: "TEXT", nullable: true),
                    Traditional401kPreTaxFor = table.Column<string>(type: "TEXT", nullable: false),
                    EmployerNonElectivePercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    EmployerMatchPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    EmployerMatchCapPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    HsaEmployeePerCheck = table.Column<decimal>(type: "TEXT", nullable: false),
                    HsaEmployerPerCheck = table.Column<decimal>(type: "TEXT", nullable: false),
                    HsaPreTaxFor = table.Column<string>(type: "TEXT", nullable: false),
                    HealthFsaAnnualElection = table.Column<decimal>(type: "TEXT", nullable: false),
                    HealthFsaPerCheckOverride = table.Column<decimal>(type: "TEXT", nullable: true),
                    HealthFsaPreTaxFor = table.Column<string>(type: "TEXT", nullable: false),
                    StipendPerCheck = table.Column<decimal>(type: "TEXT", nullable: false),
                    StipendIsTaxable = table.Column<bool>(type: "INTEGER", nullable: false),
                    W4FilingStatus = table.Column<string>(type: "TEXT", nullable: false),
                    W4MultipleJobs = table.Column<bool>(type: "INTEGER", nullable: false),
                    W4Credits = table.Column<decimal>(type: "TEXT", nullable: false),
                    W4OtherIncome = table.Column<decimal>(type: "TEXT", nullable: false),
                    W4Deductions = table.Column<decimal>(type: "TEXT", nullable: false),
                    W4ExtraWithholding = table.Column<decimal>(type: "TEXT", nullable: false),
                    StateWithholdingExemptions = table.Column<int>(type: "INTEGER", nullable: false),
                    StateAdditionalWithholding = table.Column<decimal>(type: "TEXT", nullable: false),
                    NetPayAdjustmentPerCheck = table.Column<decimal>(type: "TEXT", nullable: false),
                    ActualNetPay = table.Column<decimal>(type: "TEXT", nullable: true),
                    ActualNetPayDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollSources_Locales_WorkLocaleId",
                        column: x => x.WorkLocaleId,
                        principalTable: "Locales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSources_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSources_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDeduction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PayrollSourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AnnualAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PerCheckOverride = table.Column<decimal>(type: "TEXT", nullable: true),
                    PreTaxFor = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDeduction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDeduction_PayrollSources_PayrollSourceId",
                        column: x => x.PayrollSourceId,
                        principalTable: "PayrollSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdProfiles_HomeLocaleId",
                table: "HouseholdProfiles",
                column: "HomeLocaleId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdProfiles_ScenarioId",
                table: "HouseholdProfiles",
                column: "ScenarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeduction_PayrollSourceId",
                table: "PayrollDeduction",
                column: "PayrollSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSources_PersonId",
                table: "PayrollSources",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSources_ScenarioId",
                table: "PayrollSources",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSources_WorkLocaleId",
                table: "PayrollSources",
                column: "WorkLocaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseholdProfiles");

            migrationBuilder.DropTable(
                name: "PayrollDeduction");

            migrationBuilder.DropTable(
                name: "PayrollSources");

            migrationBuilder.DropTable(
                name: "Locales");

            migrationBuilder.DropTable(
                name: "People");
        }
    }
}
