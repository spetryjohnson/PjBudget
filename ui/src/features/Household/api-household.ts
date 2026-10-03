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

export const householdApi = {
	get: () => apiClient.get('api/household').json<Household>(),
	update: (household: Household) => apiClient.put('api/household', { json: household }).json<Household>(),
}
