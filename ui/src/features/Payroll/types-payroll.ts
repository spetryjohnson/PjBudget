import type { DeductionType, FilingStatus, PayBasis, PayFrequency, WageType } from '../../lib/types-domain'

/** `*Percent` fields are 0–100; amounts are dollars. `*PreTaxFor` lists the wage bases a deduction reduces. */
export interface PayrollSource {
	id: number
	version: string
	personId: number
	name: string
	employerName: string | null
	sortOrder: number
	workLocaleId: number

	payBasis: PayBasis
	annualSalary: number | null
	hourlyRate: number | null
	hoursPerCheck: number | null
	payFrequency: PayFrequency
	semimonthlyPayDay1: number | null
	semimonthlyPayDay2: number | null
	biweeklyAnchorDate: string | null

	traditional401kPercent: number
	traditional401kPerCheckOverride: number | null
	traditional401kPreTaxFor: WageType[]
	employerNonElectivePercent: number
	employerMatchPercent: number
	employerMatchCapPercent: number

	hsaEmployeePerCheck: number
	hsaEmployerPerCheck: number
	hsaPreTaxFor: WageType[]

	healthFsaAnnualElection: number
	healthFsaPerCheckOverride: number | null
	healthFsaPreTaxFor: WageType[]

	stipendPerCheck: number
	stipendIsTaxable: boolean

	w4FilingStatus: FilingStatus
	w4MultipleJobs: boolean
	w4Credits: number
	w4OtherIncome: number
	w4Deductions: number
	w4ExtraWithholding: number

	stateWithholdingExemptions: number
	stateAdditionalWithholding: number

	netPayAdjustmentPerCheck: number
	actualNetPay: number | null
	actualNetPayDate: string | null

	deductions: PayrollDeduction[]
}

export interface PayrollDeduction {
	type: DeductionType
	label: string
	annualAmount: number
	perCheckOverride: number | null
	preTaxFor: WageType[]
}

export interface PayrollSourceSummary {
	id: number
	name: string
	employerName: string | null
	personId: number
	personName: string
	payFrequency: PayFrequency
	sortOrder: number
	headline: {
		year: number
		checkCount: number
		annualGrossPay: number
		annualNetPay: number
		regularNetPay: number
		warningCount: number
	} | null
	simulationError: string | null
}

export interface PayrollReference {
	currentYear: number
	supportedStates: string[]
	newSourceTemplate: PayrollSource
	defaultDeductionTreatment: Record<DeductionType, WageType[]>
	defaultTraditional401kTreatment: WageType[]
	defaultCafeteriaPlanTreatment: WageType[]
	maxPayrollSources: number
}

export interface TaxableWages {
	federal: number
	state: number
	socialSecurity: number
	medicare: number
	city: number
	school: number
}

export interface PaycheckLines {
	basePay: number
	taxableStipend: number
	grossPay: number
	traditional401k: number
	hsaEmployee: number
	healthFsa: number
	deductions: number[]
	taxableWages: TaxableWages
	socialSecurityTaxedWages: number
	federalIncomeTax: number
	socialSecurityTax: number
	medicareTax: number
	additionalMedicareTax: number
	stateIncomeTax: number
	cityIncomeTax: number
	schoolDistrictTax: number
	nonTaxableStipend: number
	netPayAdjustment: number
	netPay: number
	employerRetirement: number
	employerHsa: number
	totalTaxes: number
	totalDeductions: number
}

export interface Paycheck {
	number: number
	payDate: string
	current: PaycheckLines
	yearToDate: PaycheckLines
}

export interface PayrollSummary {
	annualTotals: PaycheckLines
	checkCount: number
	finalSocialSecurityCheck: number
	checksWithoutSocialSecurity: number
	additionalMedicareStartsOnCheck: number | null
	traditional401kLimit: number
	traditional401kLimitReachedOnCheck: number | null
	traditional401kMaxOutPercent: number
	hsaLimit: number
	regularNetPay: number
	minimumNetPay: number
	maximumNetPay: number
	averageNetPay: number
	netPayByMonth: { month: number; checkCount: number; netPay: number }[]
	actualComparison: {
		payDate: string
		checkNumber: number
		actualNetPay: number
		simulatedNetPay: number
		difference: number
	} | null
}

export interface PayrollWarning {
	code: string
	message: string
}

export interface PayrollSimulation {
	year: number
	periodsPerYear: number
	deductions: { type: DeductionType; label: string }[]
	checks: Paycheck[]
	summary: PayrollSummary
	warnings: PayrollWarning[]
}
