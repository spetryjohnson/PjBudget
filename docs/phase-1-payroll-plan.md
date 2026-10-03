# Phase 1: Payroll Modeling (Implementation Plan)

> **Status:** implemented on branch `phase-1-payroll` (milestones M1–M8). **Scope:** payroll modeling only. The budget matrix, the scenario UI and transaction analysis come in later phases.
> This document contains no personal data and is safe to commit.

## 1. Goal

Simulate every paycheck in a calendar year for each payroll source, so that the **simulated net pay matches the real paycheck to the cent**. These per-check results will feed the budget matrix and scenario comparisons ("what if I lower my 401k?"). The engine therefore keeps full line-item and year-to-date detail, not just annual totals.

## 2. Decisions (from Q&A)

| Topic | Decision |
|---|---|
| People | Each payroll source belongs to a household member (`Person`). There are at most 4 sources per scenario. |
| Pay frequency | Semimonthly (default paydays: the 10th and 25th) and biweekly (computed from an anchor date). Biweekly support must work from day one because of the upcoming job change. |
| Pay basis | Salary, or hourly (rate × hours per check). |
| 401k | Traditional only. The employer contribution is a flat non-elective %, with an optional single-tier match. The limit works the way payroll does: take the % from each check until the annual limit is reached, then stop. |
| Catch-up | Included. Eligibility comes from `Person.BirthDate` using the age reached by Dec 31: 50+ adds $8,000 to the 401k limit, 60–63 adds $11,250 instead (2026 amounts), and 55+ adds $1,000 to the HSA limit. |
| Federal W-4 | All 2020+ fields: filing status (Single / MFJ / HoH), the Step 2 checkbox, Step 3 credits, and Steps 4(a), 4(b) and 4(c). |
| Ohio IT 4 | Exemptions, plus additional withholding per check. Each job can also turn off school district withholding, because some employers leave that tax to be paid separately. The tax is still calculated and projected. |
| Group-term life | The taxable value of employer-paid life insurance (the paystub's "GTL" line) is entered per check. It isn't paid, but it raises taxable wages. |
| Amount bases | Insurance and FSA are annual elections, deducted per check. HSA and stipend are entered per check. The stipend has a "taxable" toggle. |
| Deductions | A list of typed deductions (Medical, Dental, Vision, Life, Disability, Legal, Other). Each has pre-tax flags per wage type. There is no dependent-care FSA. |
| Calibration | Per-check overrides for deductions other than taxes, a net-pay adjustment, and an "actual paycheck" comparison. Taxes are never overridden, because they must respond to what-if changes. |
| Locales | The household has a **home** locale, which drives school district tax. Each source has a **work** locale, which drives city tax. |
| Household projection | A simple projection of federal, Ohio, school district and city tax owed, compared with what is withheld. |
| Scenarios | A scenario is a full copy of the plan, and one copy is marked current. Phase 1 adds the schema with a single "Current" scenario, but no UI. |
| Data | Only public tax data is seeded. Personal data is entered through the UI, and the SQLite database stays out of git. |
| Enums | Enums are stored as string constants everywhere, with flags stored as `A\|B` lists. The API serializes them the same way. |
| Concurrency | A `Version` GUID on each aggregate root blocks stale updates (HTTP 409). There is no live sync between users. |
| Out of scope | Frontend tests, user management, states other than Ohio (but keep the extension point), pre-2020 W-4s, imputed income other than group-term life, bonuses and supplemental wages, effective-dated mid-year changes (a raise or a tax-rule change means editing the plan or the tax year), and SSE plumbing. |

## 3. Payroll rules (engine specification)

Money is rounded to cents per line per check, with midpoints rounded away from zero (the same as Excel's ROUND). The exception is Social Security and Medicare, which are rounded on year-to-date totals the way ADP calculates them (§3.6).

### 3.1 Pay schedule
- **Semimonthly:** two paydays per month (`PayDay1` and `PayDay2`, defaulting to the 10th and 25th). A day past the end of the month moves to the last day of the month. That gives 24 checks a year, and annualization uses 24 periods.
- **Biweekly:** every 14 days from `BiweeklyAnchorDate`, which can be any known payday. A calendar year has 26 or 27 checks. Annualization always uses 26 periods (IRS Pub 15-T Table 3), and salary per check is the annual salary ÷ 26.
- **Shifted paydays:** a payday that falls on a weekend or a Federal Reserve holiday moves to the previous business day.
- **Tax year:** a check belongs to the tax year of its adjusted pay date.

### 3.2 Gross pay
- **Salary:** `round(annual ÷ periods)`.
- **Hourly:** `round(rate × hoursPerCheck)`.
- **Stipend:** a taxable stipend is added to gross pay. A non-taxable stipend is added after taxes.
- **Base pay:** 401k deferrals and employer contributions are calculated on base pay, which excludes the stipend.

### 3.3 Deductions
Every deduction line has `PreTaxFor` flags covering six wage types: Fed, State, SS, Medicare, City and School. The defaults below can be edited, and the UI highlights any flag that differs from its default.

| Line | Amount per check | Year-to-date cap | Default pre-tax for |
|---|---|---|---|
| Traditional 401k | override, or `round(% × base pay)` | 402(g) limit + catch-up for the person's age | Fed, State, School |
| HSA (employee) | amount per check | coverage-tier limit (+ catch-up at 55+) minus employer HSA | all six |
| Health FSA | override, or `round(election ÷ periods)` | the smaller of the election and the FSA limit | all six |
| Medical / Dental / Vision | override, or `round(annual ÷ periods)` | none | all six (Section 125) |
| Life / Disability / Legal / Other | override, or `round(annual ÷ periods)` | none | none |

- **401k tax treatment:** 401k deferrals are taxable for SS and Medicare (federal law) and for city tax (ORC 718.01 adds them back). They are excluded from school earned income because they are not part of Ohio AGI.
- **Overrides:** an override still respects the year-to-date caps.
- **Limits:** amounts above a limit are capped during simulation for the selected year and produce a warning. This replaces the earlier idea of validating limits when saving, because limits depend on the year and a payroll source is not tied to a year.
- **Employer memo lines:** these are shown but not included in net pay.
  - Employer 401k = `non-elective % × base pay + match% × min(this check's deferral, matchCap% × base pay)`.
  - Employer HSA is an amount per check.
- **Group-term life insurance:** the taxable value per check is a memo line, not pay. It has `TaxedFor` flags, which list the wage types it's added to. By default these are SS, Medicare and City. Employers must include it in SS and Medicare wages, and Ohio cities tax Medicare wages. Withholding income tax on it is optional, and employers usually don't.

### 3.4 Taxable wages
For each wage type: `gross − Σ deductions that are flagged pre-tax for that type` (with a minimum of 0), `+ group-term life` if it's taxed for that type.

### 3.5 Federal income tax withholding: IRS Pub 15-T (2026), Worksheet 1A
1. `1c = federal wages × periods`
2. `1e = 1c + Step 4(a)`
3. `1g = 0` if the Step 2 box is checked; otherwise $12,900 for MFJ and $8,600 for any other filing status.
4. `1i = max(0, 1e − (Step 4(b) + 1g))`
5. Look up the table row for 1i: use the STANDARD schedule if the box is not checked, or the CHECKBOX schedule if it is. Then `2g = 2c + (1i − 2b) × 2d`.
6. `2h = round(2g ÷ periods)`; `3b = round(Step 3 ÷ periods)`; `3c = max(0, 2h − 3b)`.
7. **Withholding** = `3c + Step 4(c)`.

The withholding tables are **derived** from the tax year's statutory brackets, so only the brackets need to be maintained:
- **STANDARD schedule:** `offset = standard deduction − line 1g amount`. Each row starts at `offset + bracket start`, and its base is the cumulative tax at the bracket start.
- **CHECKBOX schedule:** `offset = standard deduction ÷ 2`. Each row starts at `offset + bracket start ÷ 2`, and its base is the cumulative tax at the bracket start ÷ 2.
- **IRS presentation rounding:** thresholds are rounded to whole dollars, and bases to cents. Bases are computed from the unrounded thresholds. For example, the Single checkbox schedule shows 108,937.50 as $108,938.
- This derivation reproduces all six published 2026 schedules exactly (MFJ, Single and HoH, each in standard and checkbox form). Tests pin every row.

### 3.6 FICA
Each tax is figured on year-to-date wages and rounded once. A check withholds whatever brings the year's total up to that amount, so identical checks can differ by a cent. This is how ADP calculates them. It also makes the year's Social Security end exactly on the annual maximum.
- **Social Security:** taxed wages this check = `min(SS wages, max(0, wage base − YTD taxed wages))`. Tax = `round(6.2% × (YTD taxed wages + this check's)) − YTD SS tax`. The output also reports the final check with SS withheld and the number of checks without it.
- **Medicare:** `round(1.45% × YTD Medicare wages including this check) − YTD Medicare tax`.
- **Additional Medicare (withholding rule):** `round(0.9% × max(0, YTD Medicare wages including this check − $200,000)) − YTD Additional Medicare tax`. The threshold applies per employer, regardless of filing status. The $250k MFJ threshold is a liability rule and is used only in the household projection.

### 3.7 Ohio withholding: optional computer formula (effective 2026-08-01)
`TW = state wages × periods − $650 × IT 4 exemptions`, with a minimum of 0.

| TW | Annual withholding |
|---|---|
| ≤ $26,050 | TW × 1.6% |
| $26,050 < TW ≤ $100,000 | $416.80 + 2.99% × (TW − 26,050) |
| > $100,000 | $2,627.91 + 3.4% × (TW − 100,000) |

Withholding per check = `round(annual ÷ periods)` + IT 4 additional withholding. The brackets are tax-year data, not code. ADP paystubs under this formula confirm the rounding.

### 3.8 City and school district (Ohio)
- **City** (from the work locale): `round(city rate × city wages)`.
- **School district** (from the home locale), following Ohio's 2026 employer guidelines:
  - **Earned-income base:** `round(rate × school wages)`. No exemptions apply.
  - **Traditional base:** this uses the same wages and exemptions as state withholding. The formula is `round(rate × max(0, state wages × periods − $650 × exemptions) ÷ periods)`.
  - **Not withheld:** when a source turns school district withholding off, the tax is still calculated. It's reported as a "not withheld" memo line and left out of total taxes and net pay.

### 3.9 Net pay
`net = gross − all taxes − all deductions + non-taxable stipend + net adjustment`

### 3.10 Simulation output
- **Per check:** check number, pay date, gross pay, every deduction and tax line, taxable wages by type, employer memo lines, net pay, and year-to-date totals for everything.
- **Summary:**
  - annual totals for every line
  - the final check with SS withheld, and the number of checks without SS
  - the check where the 401k limit is reached, if any
  - the check where Additional Medicare tax starts, if any
  - net pay statistics: the regular check (the most common amount, or the median when all checks differ), minimum, maximum and average
  - net pay per month, with 3-check months flagged (this is for the budget phase)
  - a 401k "max-out %" helper (the % that reaches the limit on the final check)
  - warnings, such as caps applied or the 401k reaching its limit early
  - **actual vs. simulated:** the difference between the actual net pay and the simulated check on that date, with an "apply as net adjustment" action

### 3.11 Household tax projection (per scenario and year)
- **Wages on the return:** each source's wages for a tax include its group-term life insurance even when payroll didn't add it to that tax's withholding wages. It's income on the return either way. This applies to federal wages, Medicare wages and school earned income.
- **Federal:**
  - AGI = Σ federal wages + other income − adjustments.
  - Deduction = the larger of the standard deduction and the itemized deductions entered.
  - Tax comes from the progressive brackets for the filing status, minus credits entered (not allowed to go below 0).
  - Additional Medicare tax is 0.9% of Σ Medicare wages above the liability threshold ($250k for MFJ, $200k otherwise).
  - Each person's excess SS withholding across multiple employers is credited back.
  - The result is compared with Σ federal income tax and Additional Medicare tax withheld, giving a projected refund or balance due.
- **Ohio (2026):**
  - Ohio AGI = federal AGI ± Ohio adjustments.
  - Exemptions are counted at $2,350, $2,100 or $1,850 each, for MAGI of up to $40k, up to $80k, or above $80k. None are allowed when MAGI ≥ $500k.
  - Tax = $0 up to $26,050, then $332 + 2.75% of the amount over $26,050.
  - The joint filing credit applies to MFJ returns where each spouse has at least $500 of qualifying income and MAGI is under $500k. It is 20%, 15%, 10% or 5% of tax, for income less exemptions of up to $25k, $50k, $75k, or above. It is capped at $650.
  - Other Ohio credits entered are subtracted.
  - The result is compared with Σ Ohio withholding.
- **School district:**
  - Earned-income base: rate × Σ school wages.
  - Traditional base: rate × Ohio taxable income.
  - Either is compared with Σ school district withholding. When some source doesn't withhold it, a note says to pay the balance with the return or with SD 100ES estimated payments.
- **City:** liability equals withholding when the work city is the same as the home city. Otherwise the projection shows a "not projected" note, because the resident-city credit is out of scope.
- **Cross-source warnings:** the family HSA limit is exceeded, or one person's 401k across all of their sources exceeds the 402(g) limit.

## 4. Data model

**Conventions**
- **Keys:** integer identity keys.
- **Money:** stored as `decimal` (EF stores it as TEXT in SQLite), so there is no SQL-side arithmetic on money.
- **Percent vs. rate:** `*Percent` properties hold 0–100 values entered by the user. `*Rate` properties hold fractions (for example `0.062`).
- **Timestamps:** `CreatedAt` and `UpdatedAt` are stored in UTC and set in `SaveChanges` using `ISystemClock`.
- **Concurrency:** aggregate roots carry a `Version` GUID (`[ConcurrencyCheck]`), regenerated on every update.
- **Entity layout:** entities live in their feature folders, with `IEntityTypeConfiguration<T>` classes applied from the assembly.

**Enums** (stored as string constants)

| Enum | Constants |
|---|---|
| `PayFrequency` | `SEMIMONTHLY`, `BIWEEKLY` |
| `PayBasis` | `SALARY`, `HOURLY` |
| `FilingStatus` | `SINGLE`, `MFJ`, `HOH` |
| `DeductionType` | `MEDICAL`, `DENTAL`, `VISION`, `LIFE`, `DISABILITY`, `LEGAL`, `OTHER` |
| `HsaCoverage` | `NONE`, `SELF_ONLY`, `FAMILY` |
| `SchoolDistrictTaxBase` | `EARNED_INCOME`, `TRADITIONAL` |
| `TaxScheduleKind` | `FEDERAL_INCOME`, `OHIO_WITHHOLDING`, `OHIO_INCOME`, `OHIO_EXEMPTION`, `OHIO_JOINT_FILING_CREDIT` |
| `TaxableWageTypes` (`[Flags]`) | `NONE`, `FED`, `STATE`, `SS`, `MEDICARE`, `CITY`, `SCHOOL` (stored as, e.g., `FED\|STATE\|SCHOOL`) |

### Global reference data
| Entity | Fields |
|---|---|
| `TaxYear` | `Year` (unique); FICA: `SocialSecurityRate`, `SocialSecurityWageBase`, `MedicareRate`, `AdditionalMedicareRate`, `AdditionalMedicareWithholdingThreshold`; limits: `ElectiveDeferralLimit`, `CatchUpLimit50`, `CatchUpLimit60To63`, `HsaLimitSelfOnly`, `HsaLimitFamily`, `HsaCatchUpLimit55`, `HealthFsaLimit`; Ohio: `OhioWithholdingExemptionAmount`, `OhioExemptionMagiLimit`, `OhioJointFilingCreditCap`, `OhioJointFilingCreditMagiLimit`, `OhioJointFilingCreditMinSpouseIncome` |
| `FederalFilingStatusParameters` | child of `TaxYear`: `FilingStatus`, `StandardDeduction`, `W4Line1gAmount`, `AdditionalMedicareLiabilityThreshold` |
| `TaxScheduleRow` | child of `TaxYear`: `Kind`, `FilingStatus?` (federal only), `Over` (exclusive lower bound; the first row is 0), `BaseAmount`, `Rate`. One table holds every bracket-like schedule. For exemption tiers, `BaseAmount` is the amount per exemption. For the joint filing credit, `Rate` is the credit rate. Federal bases are derived, not stored. |
| `Locale` | `Name`, `City`, `StateCode`, `ZipCode`, `MunicipalTaxRate`, `SchoolDistrictName?`, `SchoolDistrictNumber?` (Ohio 4-digit), `SchoolDistrictTaxRate?`, `SchoolDistrictTaxBase?` |
| `Person` | `DisplayName`, `BirthDate?` (drives catch-up eligibility), `SortOrder` |

### Scenario-scoped plan data
| Entity | Fields |
|---|---|
| `Scenario` | `Name`, `Description?`, `IsCurrent` (a unique partial index enforces a single current scenario) |
| `HouseholdProfile` | one per scenario: `HomeLocaleId?`, `HsaCoverage`, `TaxFilingStatus` (default MFJ); projection inputs: `FederalOtherIncome`, `FederalAdjustments`, `FederalItemizedDeductions?`, `FederalCredits`, `OhioAdjustments`, `OhioExemptionCount`, `OhioOtherCredits` |
| `PayrollSource` | `ScenarioId`, `PersonId`, `Name`, `EmployerName?`, `SortOrder`, `WorkLocaleId`; **pay:** `PayBasis`, `AnnualSalary?`, `HourlyRate?`, `HoursPerCheck?`, `PayFrequency`, `SemimonthlyPayDay1?`, `SemimonthlyPayDay2?`, `BiweeklyAnchorDate?`; **401k:** `Traditional401kPercent`, `Traditional401kPerCheckOverride?`, `Traditional401kPreTaxFor`, `EmployerNonElectivePercent`, `EmployerMatchPercent`, `EmployerMatchCapPercent`; **HSA:** `HsaEmployeePerCheck`, `HsaEmployerPerCheck`, `HsaPreTaxFor`; **FSA:** `HealthFsaAnnualElection`, `HealthFsaPerCheckOverride?`, `HealthFsaPreTaxFor`; **stipend:** `StipendPerCheck`, `StipendIsTaxable`; **group-term life:** `GroupTermLifePerCheck`, `GroupTermLifeTaxedFor`; **W-4:** `W4FilingStatus`, `W4MultipleJobs`, `W4Credits`, `W4OtherIncome`, `W4Deductions`, `W4ExtraWithholding`; **IT 4:** `StateWithholdingExemptions`, `StateAdditionalWithholding`, `WithholdsSchoolDistrictTax`; **calibration:** `NetPayAdjustmentPerCheck`, `ActualNetPay?`, `ActualNetPayDate?` |
| `PayrollDeduction` | child of `PayrollSource`: `Type`, `Label`, `AnnualAmount`, `PerCheckOverride?`, `PreTaxFor`, `SortOrder` |

### Deletion rules
- A `Locale` or `Person` that is still referenced cannot be deleted (HTTP 409 with an explanation).
- `PayrollDeduction` rows are replaced as part of their parent source.

## 5. Backend architecture

### 5.1 Layout
```
app/
  Framework/                    StringConstant helpers (+ flags support, per-type caching)
  Shared/
    Database/                   AppDbContext, enum convention, timestamps/version handling
    Errors/                     DomainValidationException, NotFoundException, ConflictException, exception handler
    Json/                       StringConstant JSON converter factory
  Features/
    Scenarios/                  Scenario entity, ICurrentScenarioAccessor
    TaxYears/                   entities, TaxYearService, 2026 seed, endpoints
    Locales/  People/           entity, service, endpoints
    Household/                  HouseholdProfile, HouseholdService, projection service, endpoints
    Payroll/                    entities, PayrollSourceService, PayrollScheduleService, input mapper, endpoints
      Engine/                   pure calculation code (no EF, no DI requirements)
        States/                 IStateTaxModule, StateTaxModuleRegistry, Ohio/OhioTaxModule
tests/PjBudget.Tests/           NUnit + Moq (added to PjBudget.sln)
```

### 5.2 Enum string constants
- **Framework helpers:**
  - `ToStringConstant()` writes a `[Flags]` value as the constants of its single-bit members, in value order, joined with `|`. Zero is written as the zero member's constant (`NONE`).
  - `ToEnum<T>()` splits flag strings on `|` and combines the parts. It currently rejects combinations because `Enum.IsDefined` fails on them.
  - Both helpers gain a per-type cache, so conversions no longer use reflection on every call.
- **EF:** in `OnModelCreating`, every enum or nullable-enum property gets a `StringConstantEnumConverter<TEnum>` (built from `ToStringConstant` and `ToEnum<T>`). New enums therefore need no per-property configuration.
- **JSON:** the API uses the same constants, and flags are serialized as arrays of constants (for example `["FED","STATE"]`). This is registered through the FastEndpoints serializer options. TypeScript types use string-literal unions.

### 5.3 Engine (pure)
- **Small focused calculators:**
  - `PayScheduleGenerator` (with `BankHolidayCalendar`)
  - `ProgressiveTaxSchedule` (generic "over / base / rate" math)
  - `FederalWithholdingTables` (derivation and IRS rounding)
  - `FederalIncomeTaxWithholding` (Worksheet 1A)
  - `FicaCalculator` (year-to-date aware)
  - `IStateTaxModule` (state withholding, local withholding and liability projection), with `OhioTaxModule` as the only implementation. Any other state code produces an "unsupported state" validation error.
- **Simulators:** `PaycheckSimulator` runs the check-by-check pipeline and tracks year-to-date totals. `HouseholdTaxProjector` consumes the per-source schedules.
- **Inputs and outputs:** immutable records. Services map EF entities to engine inputs, so the same engine serves saved sources, the live preview and future scenarios.

### 5.4 Services and cross-cutting rules
- **Scope:** services take an explicit `scenarioId`. Endpoints resolve it from `ICurrentScenarioAccessor`, which in phase 1 always returns the current scenario.
- **Validation:** request shape is checked by FastEndpoints validators. Business rules live in services: at most 4 sources, required fields for the chosen pay basis and frequency, percentages between 0 and 100, and referenced records existing with a supported state.
- **Errors:** an `IExceptionHandler` maps domain exceptions to 400, 404 or 409, using the same response shape as FastEndpoints validation errors.
- **Updates:** the client sends the `Version` it loaded. A mismatch, or EF's `DbUpdateConcurrencyException`, returns 409.
- **Seeding:** `SeedData` creates the "Current" scenario and its `HouseholdProfile` if they are missing. It seeds the 2026 tax year only when no tax years exist, so later edits made by the user are never overwritten. All seeded values are public (see §10).

### 5.5 API
All endpoints require the existing `RequireUser` policy.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/tax-years` | List tax years |
| GET / PUT / DELETE | `/api/tax-years/{year}` | Read, replace (versioned) or delete a year's parameters |
| POST | `/api/tax-years/{year}/copy` | Copy one year to another, to start next year's data entry |
| GET / POST | `/api/locales` | List or create locales |
| PUT / DELETE | `/api/locales/{id}` | Update or delete a locale (deletion is blocked while it's in use) |
| GET / POST | `/api/people` | List or create people |
| PUT / DELETE | `/api/people/{id}` | Update or delete a person (deletion is blocked while they're in use) |
| GET / PUT | `/api/household` | The current scenario's household profile |
| GET | `/api/household/projection?year=` | Tax projection plus cross-source warnings |
| GET / POST | `/api/payroll-sources` | List sources (with headline numbers for the current year) or create one |
| GET / PUT / DELETE | `/api/payroll-sources/{id}` | Read, update (versioned) or delete a source |
| GET | `/api/payroll-sources/{id}/schedule?year=` | Full simulation for a saved source |
| POST | `/api/payroll/preview` | Simulate the editor's unsaved state (live preview) |

## 6. Frontend
- **Shell:** keep the existing shell and add navigation entries for Payroll, Household and Settings (Tax years, Locales). Home becomes a small dashboard showing each source's regular net check and the projection headline.
- **`/payroll`:** a list of payroll sources with an "add" button, which is disabled once there are 4.
- **`/payroll/:id` (and `/payroll/new`):** the main editor.
  - **Left side, form sections:** general (person, name, work locale); pay (basis, amount, frequency, schedule); retirement; HSA/FSA; deductions; stipend; federal W-4; Ohio IT 4; calibration.
  - **Tax treatment grid:** rows for 401k, HSA, FSA, each deduction and (when entered) group-term life; columns for Fed, State, SS, Medicare, City and School, under a "Taxed by" heading. A check means that tax applies to the amount, so a pre-tax deduction is unchecked for the taxes it comes out before. Deductions are still stored as `PreTaxFor` flags, and the grid shows the complement. Cells that differ from the default are highlighted, and a "reset" action restores them.
  - **Right side, live preview:** shown alongside the form as you scroll. It includes a year selector; headline figures (regular gross and net per check, annual net, the final check with SS withheld, the 401k max-out %); warnings; and the paycheck register.
  - **Live preview behavior:** the preview updates after a 300 ms debounce by calling `POST /api/payroll/preview`. It shows a stale indicator while loading, and shows validation errors inline.
  - **Saving:** save is versioned, and the page guards against leaving with unsaved changes.
- **Paycheck register:** a dense `v-data-table` with one row per check and columns for date, gross, each tax, 401k, HSA, FSA, insurance, other, stipend and net. Numbers are right-aligned with tabular digits, and the table ends with a totals row. Clicking a row opens a paystub view with current and year-to-date columns.
- **`/household`:** people, household settings (home locale, HSA coverage, projection inputs), the projection (liability, withheld and refund or balance due for each jurisdiction) and cross-source warnings.
- **`/settings/tax-years`:** edit FICA values, limits, per-filing-status parameters and schedules, with derived federal bases shown read-only. Includes a "copy to next year" action.
- **`/settings/locales`:** locale CRUD.
- **Code structure:**
  - Per feature: `features/<Feature>/api-*.ts` and `types-*.ts`, following the existing pattern.
  - Shared inputs: `MoneyField` and `PercentField`.
  - Formatting helpers: `lib/format.ts`.
  - Reference data (locales, people, tax years) is cached in a small Pinia store; other state stays local to each page.
- **Housekeeping:** rename `ui/package.json` from `homework-tracker-ui` to `pjbudget-ui`.

## 7. Testing
- **Project:** `tests/PjBudget.Tests` (NUnit, Moq, `Microsoft.NET.Test.Sdk`, `NUnit3TestAdapter`).
- **Engine tests (no mocks):**
  - Pay schedule: specific 2026 dates, including weekend and holiday shifts, day-31 clamping, and biweekly years with 26 vs. 27 checks.
  - Federal: all six 2026 Pub 15-T schedules pinned row by row, plus Worksheet 1A cases (each filing status, checkbox, Step 3 floor, Steps 4(a), 4(b) and 4(c)).
  - FICA: the check where SS crosses the wage base, and the check where Additional Medicare crosses $200k.
  - Ohio: each withholding band and its boundaries with exemptions, city tax, and school tax under both bases.
- **Simulator scenarios (synthetic data):**
  - a salaried semimonthly source with every deduction type
  - an hourly biweekly source
  - a 401k front-loaded until it hits the cap
  - HSA and FSA caps
  - overrides, taxable vs. non-taxable stipend, net adjustment, and the actual-vs-simulated difference
- **Projection tests:**
  - standard vs. itemized deduction, credits floor, and Additional Medicare above $250k
  - the excess SS credit
  - Ohio exemption tiers, the $332 base, joint filing credit tiers and cap, and the MAGI limits
- **Framework and services (in-memory SQLite, real `DbContext`):**
  - enum round trips, including checking the stored text with raw SQL (`BIWEEKLY`, `FED|STATE`)
  - JSON converter
  - payroll source CRUD, the 4-source rule, replacing child rows, and version conflicts
  - delete-blocking rules, tax year copy, and seeding that runs once only
- **Moq:** used where it actually isolates something, such as `ISystemClock` (the default year) and isolating the simulator from a state module in tests focused on federal logic.
- **Local calibration (not committed):** compare the engine with your real paystubs and the spreadsheet on your machine.

## 8. Milestones
The order puts end-to-end modeling of the upcoming biweekly job first.

| # | Milestone | Done when |
|---|---|---|
| M1 | **Foundations:** test project; enum constants for EF and JSON, including flags; timestamps and `Version`; error handling; `Scenario` and `HouseholdProfile` seeding | Tests pass; the migration applies on a fresh database |
| M2 | **Tax parameters:** `TaxYear` model, 2026 seed and service; read, update and copy endpoints; federal table derivation | The pinned Pub 15-T tests pass |
| M3 | **Engine:** schedule, federal, FICA, Ohio module, simulator and summary | Engine tests pass |
| M4 | **Payroll API:** `Locale`, `Person`, `PayrollSource` and deductions; CRUD, schedule and preview endpoints | Service tests pass; endpoints are exercised through Swagger |
| M5 | **Payroll UI:** navigation, minimal Locales and People pages, source list, and the editor with live preview, register and paystub view | **You can model the new job end to end** |
| M6 | **Household:** profile, projection engine, endpoint and page, cross-source warnings | Projection tests pass; the page renders |
| M7 | **Tax-year editor UI** | 2027 can be created by copying and editing 2026 |
| M8 | **Polish:** README (setup, run, test, data entry) and a final review | `dotnet test` and `npm run build` are clean |

Work happens on a feature branch, with one commit per milestone (subject to your OK).

## 9. Known limitations and open items
- **Roth catch-up rule (SECURE 2.0):** catch-up contributions must be Roth when the prior year's Social Security wages from the same employer exceeded $150,000 (2026 threshold). This isn't modeled because only traditional contributions are supported. It also doesn't apply in the first year with a new employer.
- **Catch-up plan features:** the plan is assumed to allow age-50 catch-up and the age 60–63 super catch-up.
- **Mid-year rule changes are not effective-dated.** A year's simulation applies that year's current parameters to every check. For example, Ohio's August 1, 2026 formula is applied to all 2026 checks, even though checks before then used the formula effective October 1, 2025. This is deliberate: the app forecasts upcoming pay, so when a rule changes mid-year, the tax year is edited. Earlier checks in that year are then recalculated with the new rule and may no longer match their paystubs. Verify against paystubs issued after the latest change.
- **The resident-city credit** (when the work city differs from the home city) is not modeled.
- **27-check years:** deductions are taken from every check at the annual amount ÷ 26, and the FSA is capped at the election.
- **Payroll provider rounding** follows ADP, including year-to-date FICA. Other providers may differ by a few cents, and the net adjustment absorbs any remaining difference. The target is within $1 of each real check.
- **Federal credits** in the projection are treated as nonrefundable, which is a simplification.

## 10. References
- IRS Publication 15-T (2026): Worksheet 1A and the Annual Percentage Method tables (https://www.irs.gov/pub/irs-pdf/p15t.pdf)
- Ohio Optional Computer Formula, effective 2026-08-01 (tax.ohio.gov employer withholding, "2026 Withholding Tables")
- 2026 Ohio Employer and School District Withholding Tax Filing Guidelines; Ohio IT 4 (Rev. 01/24)
- Ohio Revised Code 5747.02 (rates), 5747.025 (exemptions), 5747.05 (joint filing credit), 718.01 (municipal qualifying wages)
- 2026 IRS limits: 402(g) $24,500; catch-up $8,000 (50+) and $11,250 (ages 60–63); HSA $4,400 / $8,750 (+$1,000 at 55+); health FSA $3,400; SS wage base $184,500

### 2026 seed values
- **Federal brackets** (each entry is "over $X: rate"):
  - Single: 0: 10%, 12,400: 12%, 50,400: 22%, 105,700: 24%, 201,775: 32%, 256,225: 35%, 640,600: 37%
  - MFJ: 0: 10%, 24,800: 12%, 100,800: 22%, 211,400: 24%, 403,550: 32%, 512,450: 35%, 768,700: 37%
  - HoH: 0: 10%, 17,700: 12%, 67,450: 22%, 105,700: 24%, 201,750: 32%, 256,200: 35%, 640,600: 37%
- **Standard deduction:** Single 16,100; MFJ 32,200; HoH 24,150.
- **W-4 line 1g:** MFJ 12,900; others 8,600.
- **Additional Medicare liability threshold:** MFJ 250,000; others 200,000.
- **FICA:** SS 6.2% up to a wage base of 184,500; Medicare 1.45%; Additional Medicare 0.9%, withheld above 200,000.
- **Ohio withholding:** exemption $650; brackets: over 0: 0 + 1.6%; over 26,050: 416.80 + 2.99%; over 100,000: 2,627.91 + 3.4%.
- **Ohio income tax:** over 26,050: 332 + 2.75%.
- **Ohio exemptions:** over 0: 2,350; over 40,000: 2,100; over 80,000: 1,850. Exemptions require MAGI below 500,000.
- **Ohio joint filing credit:** over 0: 20%; over 25,000: 15%; over 50,000: 10%; over 75,000: 5%. Cap $650; MAGI must be below 500,000; each spouse needs at least $500 of income.
