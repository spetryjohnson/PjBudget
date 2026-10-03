# PJ Budget App

A self-hosted personal budgeting application for a household. It replaces a budgeting spreadsheet with an app that
can model paychecks precisely, keep a current plan alongside what-if scenarios, and eventually compare the budget
with real transactions.

## Status

**Phase 1: payroll modeling** is implemented. See [docs/phase-1-payroll-plan.md](docs/phase-1-payroll-plan.md) for
the design and the payroll rules the engine follows.

- **Paycheck simulation:** every check in a calendar year, for 1–4 payroll sources (salaried or hourly, paid twice a
  month or every two weeks).
- **Federal withholding:** IRS Pub 15-T Worksheet 1A, using all fields of the 2020+ W-4.
- **FICA:** Social Security up to the wage base, Medicare, and Additional Medicare above $200k, rounded on
  year-to-date totals the way ADP does.
- **Ohio:** state withholding with the optional computer formula, city tax, and school district tax. Each job can
  turn off school district withholding, and the tax is still projected.
- **Year-to-date limits:** the 401(k) limit (including catch-up contributions by age), HSA and FSA limits, and the
  Social Security wage base.
- **Deductions:** insurance and other deductions with per-wage-base pre-tax settings, a stipend, taxable group-term
  life insurance, and employer contributions.
- **Calibration:** compare against a real paycheck and absorb any remaining difference, so simulated net pay matches
  actual pay exactly.
- **Household tax projection:** federal, Ohio, school district and city tax owed, compared with withholding.
- **Tax years:** tax rules are stored per year and maintained in the UI. 2026 is seeded.

Coming later: the budget matrix, scenarios (the data model is already scenario-aware), and transaction analysis.

## Running it

Prerequisites: the .NET 10 SDK and Node 22+.

```bash
cd ui
npm install
npm run dev:all
```

`npm run dev:all` starts the API (`dotnet run --project ../app`, on https://localhost:7265) and the Vite dev server
together. Open https://localhost:5175. The first sign-in uses the seeded admin account in
`app/Features/Authentication/DefaultCredentials.cs`, and the app asks you to change the password.

The SQLite database is created at `PjBudget.db` in the repository root. It is git-ignored, because it holds your
personal data. Migrations are applied at startup.

## First-time setup

1. **Household:** add each person who earns a paycheck. A birth date turns on 401(k) catch-up (50+) and HSA catch-up
   (55+) contributions.
2. **Settings → Locales:** add the city you work in and the place you live, with their income tax rates and school
   district.
3. **Household settings:** choose the home locale and your HSA coverage.
4. **Payroll → Add payroll source:** enter each job. The paychecks update live as you type.
5. **Calibrate:** under *Match a real paycheck*, enter a recent paycheck's date and net pay. Fix any setting that
   explains a difference (deduction amounts, the tax treatment grid, W-4 fields). Then use *Add to adjustment* to
   absorb whatever difference remains.

## Each new tax year

Go to **Settings → Tax years** and choose *Copy to <next year>*. Then update the federal brackets and standard
deductions, the contribution limits, the Social Security wage base, and Ohio's withholding formula and tax tables.
The *Derived withholding tables* panel shows the IRS percentage-method tables the engine builds from your brackets.
Compare it with the "Annual Percentage Method" tables in that year's IRS Publication 15-T.

## Tests

```bash
dotnet test tests/PjBudget.Tests
```

The tests use NUnit and Moq. Engine tests use made-up numbers worked out by hand, plus every row of the published
2026 IRS withholding tables. Service tests run against an in-memory SQLite database. No personal data is stored in
the repository.

## Layout

```
app/                          ASP.NET Core API (FastEndpoints, EF Core + SQLite)
  Features/
    Payroll/Engine/           pure calculation code: schedules, federal, FICA, simulator, projection
    Payroll/Engine/States/    per-state tax modules (Ohio); add a state by implementing IStateTaxModule
    Payroll/ Household/ Locales/ People/ TaxYears/ Scenarios/
                              entities, services, and thin endpoints per feature
  Shared/                     database conventions, error handling, JSON, domain enums
  Framework/                  StringConstant enum helpers
ui/                           Vue 3 + Vuetify front end, organized by feature under src/features
tests/PjBudget.Tests/         NUnit + Moq
docs/                         design notes
```

Conventions:

- **Enums** are stored and sent over the API as their `[StringConstant]` values (e.g. `BIWEEKLY`, and
  `FED|STATE` for flags), so renaming an enum member never touches stored data.
- **Concurrency:** each saved aggregate carries a `Version`. An update made from a stale copy is rejected with HTTP
  409 instead of overwriting a newer change.
- **Endpoints** only handle routing and authorization. Business rules live in services, and the calculation engine
  never touches the database.
