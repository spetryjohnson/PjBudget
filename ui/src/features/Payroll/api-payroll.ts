import { apiClient } from '../../lib/apiClient'
import type { PayrollReference, PayrollSimulation, PayrollSource, PayrollSourceSummary } from './types-payroll'

export const payrollApi = {
	reference: () => apiClient.get('api/payroll/reference').json<PayrollReference>(),

	list: (year: number) =>
		apiClient.get('api/payroll-sources', { searchParams: { year } }).json<PayrollSourceSummary[]>(),

	get: (id: number) => apiClient.get(`api/payroll-sources/${id}`).json<PayrollSource>(),
	create: (source: PayrollSource) => apiClient.post('api/payroll-sources', { json: source }).json<PayrollSource>(),
	update: (source: PayrollSource) => apiClient.put(`api/payroll-sources/${source.id}`, { json: source }).json<PayrollSource>(),
	remove: (id: number) => apiClient.delete(`api/payroll-sources/${id}`),

	schedule: (id: number, year: number) =>
		apiClient.get(`api/payroll-sources/${id}/schedule`, { searchParams: { year } }).json<PayrollSimulation>(),

	/** Simulates unsaved editor state. Pass a signal so a newer preview can cancel an older one. */
	preview: (year: number, source: PayrollSource, signal?: AbortSignal) =>
		apiClient.post('api/payroll/preview', { json: { year, source }, signal, retry: 0 }).json<PayrollSimulation>(),
}
