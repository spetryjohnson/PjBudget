import { apiClient } from '../../lib/apiClient'
import type { SchoolDistrictTaxBase } from '../../lib/types-domain'

/** Rates are fractions: 0.02 means 2%. */
export interface Locale {
	id: number
	version: string
	name: string
	city: string
	stateCode: string
	zipCode: string | null
	municipalTaxRate: number
	schoolDistrictName: string | null
	schoolDistrictNumber: string | null
	schoolDistrictTaxRate: number | null
	schoolDistrictTaxBase: SchoolDistrictTaxBase | null
}

export function newLocale(): Locale {
	return {
		id: 0,
		version: '00000000-0000-0000-0000-000000000000',
		name: '',
		city: '',
		stateCode: 'OH',
		zipCode: null,
		municipalTaxRate: 0,
		schoolDistrictName: null,
		schoolDistrictNumber: null,
		schoolDistrictTaxRate: null,
		schoolDistrictTaxBase: null,
	}
}

export const localesApi = {
	list: () => apiClient.get('api/locales').json<Locale[]>(),
	create: (locale: Locale) => apiClient.post('api/locales', { json: locale }).json<Locale>(),
	update: (locale: Locale) => apiClient.put(`api/locales/${locale.id}`, { json: locale }).json<Locale>(),
	remove: (id: number) => apiClient.delete(`api/locales/${id}`),
}
