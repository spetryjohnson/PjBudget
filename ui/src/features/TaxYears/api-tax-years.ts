import { apiClient } from '../../lib/apiClient'
import type { FilingStatus } from '../../lib/types-domain'

export interface TaxYearSummary {
	year: number
	updatedAt: string
}

export type TaxScheduleKind =
	| 'FEDERAL_INCOME'
	| 'OHIO_WITHHOLDING'
	| 'OHIO_INCOME'
	| 'OHIO_EXEMPTION'
	| 'OHIO_JOINT_FILING_CREDIT'

/** Rates are fractions (0.062 = 6.2%). */
export interface TaxYear {
	year: number
	version: string
	fica: {
		socialSecurityRate: number
		socialSecurityWageBase: number
		medicareRate: number
		additionalMedicareRate: number
		additionalMedicareWithholdingThreshold: number
	}
	limits: {
		electiveDeferral: number
		catchUpAge50: number
		catchUpAge60To63: number
		hsaSelfOnly: number
		hsaFamily: number
		hsaCatchUpAge55: number
		healthFsa: number
	}
	ohio: {
		withholdingExemptionAmount: number
		exemptionMagiLimit: number
		jointFilingCreditCap: number
		jointFilingCreditMagiLimit: number
		jointFilingCreditMinSpouseIncome: number
	}
	filingStatuses: {
		filingStatus: FilingStatus
		standardDeduction: number
		standardWithholdingAdjustment: number
		additionalMedicareLiabilityThreshold: number
	}[]
	schedules: {
		kind: TaxScheduleKind
		filingStatus: FilingStatus | null
		rows: { over: number; baseAmount: number; rate: number }[]
	}[]
}

/** A Pub 15-T Annual Percentage Method table derived from the brackets, for checking against the published one. */
export interface WithholdingTables {
	filingStatus: FilingStatus
	standard: WithholdingTableRow[]
	step2Checkbox: WithholdingTableRow[]
}

export interface WithholdingTableRow {
	atLeast: number
	tentativeAmount: number
	rate: number
}

export const taxYearsApi = {
	list: () => apiClient.get('api/tax-years').json<TaxYearSummary[]>(),
	get: (year: number) => apiClient.get(`api/tax-years/${year}`).json<TaxYear>(),
	update: (taxYear: TaxYear) => apiClient.put(`api/tax-years/${taxYear.year}`, { json: taxYear }).json<TaxYear>(),
	copy: (year: number, targetYear: number) =>
		apiClient.post(`api/tax-years/${year}/copy`, { json: { targetYear } }).json<TaxYear>(),
	remove: (year: number) => apiClient.delete(`api/tax-years/${year}`),
	withholdingTables: (year: number) => apiClient.get(`api/tax-years/${year}/withholding-tables`).json<WithholdingTables[]>(),
}
