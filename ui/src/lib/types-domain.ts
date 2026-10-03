/**
 * Domain enums, as the string constants the API sends and stores.
 */

export type PayFrequency = 'SEMIMONTHLY' | 'BIWEEKLY'
export type PayBasis = 'SALARY' | 'HOURLY'
export type FilingStatus = 'SINGLE' | 'MFJ' | 'HOH'
export type DeductionType = 'MEDICAL' | 'DENTAL' | 'VISION' | 'LIFE' | 'DISABILITY' | 'LEGAL' | 'OTHER'
export type HsaCoverage = 'NONE' | 'SELF_ONLY' | 'FAMILY'
export type SchoolDistrictTaxBase = 'EARNED_INCOME' | 'TRADITIONAL'

/** A wage base a pre-tax deduction can reduce. Sets of these travel as arrays, e.g. ['FED', 'STATE']. */
export type WageType = 'FED' | 'STATE' | 'SS' | 'MEDICARE' | 'CITY' | 'SCHOOL'

export const wageTypes: { value: WageType; label: string; title: string }[] = [
	{ value: 'FED', label: 'Fed', title: 'Federal income tax' },
	{ value: 'STATE', label: 'State', title: 'State income tax' },
	{ value: 'SS', label: 'SS', title: 'Social Security' },
	{ value: 'MEDICARE', label: 'Medicare', title: 'Medicare' },
	{ value: 'CITY', label: 'City', title: 'City income tax' },
	{ value: 'SCHOOL', label: 'School', title: 'School district income tax' },
]

export const filingStatuses: { value: FilingStatus; title: string }[] = [
	{ value: 'MFJ', title: 'Married filing jointly' },
	{ value: 'SINGLE', title: 'Single or married filing separately' },
	{ value: 'HOH', title: 'Head of household' },
]

export const deductionTypes: { value: DeductionType; title: string }[] = [
	{ value: 'MEDICAL', title: 'Medical' },
	{ value: 'DENTAL', title: 'Dental' },
	{ value: 'VISION', title: 'Vision' },
	{ value: 'LIFE', title: 'Life' },
	{ value: 'DISABILITY', title: 'Disability' },
	{ value: 'LEGAL', title: 'Legal' },
	{ value: 'OTHER', title: 'Other' },
]

export const hsaCoverages: { value: HsaCoverage; title: string }[] = [
	{ value: 'NONE', title: 'None (no HSA-eligible plan)' },
	{ value: 'SELF_ONLY', title: 'Self-only HDHP' },
	{ value: 'FAMILY', title: 'Family HDHP' },
]

export const schoolDistrictTaxBases: { value: SchoolDistrictTaxBase; title: string }[] = [
	{ value: 'EARNED_INCOME', title: 'Earned income' },
	{ value: 'TRADITIONAL', title: 'Traditional (Ohio taxable income)' },
]
