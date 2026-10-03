import { defineStore } from 'pinia'
import { localesApi, type Locale } from '../features/Locales/api-locales'
import { peopleApi, type Person } from '../features/People/api-people'
import { payrollApi } from '../features/Payroll/api-payroll'
import type { PayrollReference } from '../features/Payroll/types-payroll'
import { taxYearsApi, type TaxYearSummary } from '../features/TaxYears/api-tax-years'

/**
 * Small lists that many pages use for dropdowns and labels. Pages that change them call the matching refresh
 * function, so the other pages never show stale choices.
 */
export const useReferenceDataStore = defineStore('referenceData', {
	state: () => ({
		people: [] as Person[],
		locales: [] as Locale[],
		taxYears: [] as TaxYearSummary[],
		payroll: null as PayrollReference | null,
	}),

	getters: {
		personName: (state) => (id: number) => state.people.find(p => p.id === id)?.displayName ?? '',
		localeName: (state) => (id: number | null) => state.locales.find(l => l.id === id)?.name ?? '',
		years: (state) => state.taxYears.map(t => t.year),
	},

	actions: {
		async loadAll() {
			await Promise.all([this.refreshPeople(), this.refreshLocales(), this.refreshTaxYears(), this.loadPayrollReference()])
		},

		async refreshPeople() {
			this.people = await peopleApi.list()
		},

		async refreshLocales() {
			this.locales = await localesApi.list()
		},

		async refreshTaxYears() {
			this.taxYears = await taxYearsApi.list()
		},

		async loadPayrollReference() {
			this.payroll ??= await payrollApi.reference()
		},

		/** The year pages default to: the current year when it has tax rules, else the latest year that does. */
		defaultYear(): number {
			const current = this.payroll?.currentYear ?? new Date().getFullYear()
			const years = this.years
			return years.includes(current) || years.length === 0 ? current : Math.max(...years)
		},
	},
})
