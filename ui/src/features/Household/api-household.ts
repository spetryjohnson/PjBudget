import { apiClient } from '../../lib/apiClient'
import type { FilingStatus, HsaCoverage } from '../../lib/types-domain'

export interface Household {
	version: string
	homeLocaleId: number | null
	hsaCoverage: HsaCoverage
	taxFilingStatus: FilingStatus
	federalOtherIncome: number
	federalAdjustments: number
	federalItemizedDeductions: number | null
	federalCredits: number
	ohioAdjustments: number
	ohioExemptionCount: number
	ohioOtherCredits: number
}

export interface TaxProjectionSection {
	title: string
	lines: { label: string; amount: number }[]
	liability: number
	withheld: number
	note: string | null
	/** Positive is a projected refund; negative is a balance due. */
	refundOrBalanceDue: number
}

export interface HouseholdTaxProjection {
	year: number
	sections: TaxProjectionSection[]
	warnings: { code: string; message: string }[]
	totalLiability: number
	totalWithheld: number
	totalRefundOrBalanceDue: number
}

export const householdApi = {
	get: () => apiClient.get('api/household').json<Household>(),
	update: (household: Household) => apiClient.put('api/household', { json: household }).json<Household>(),
	projection: (year: number) =>
		apiClient.get('api/household/projection', { searchParams: { year } }).json<HouseholdTaxProjection>(),
}
