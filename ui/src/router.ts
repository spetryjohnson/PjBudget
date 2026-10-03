import { createRouter, createWebHistory } from 'vue-router'
import Home from './components/Home.vue'

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes: [
		{
			path: '/',
			name: 'home',
			component: Home,
		},
		{
			path: '/payroll',
			name: 'payroll',
			component: () => import('./features/Payroll/PayrollSourcesPage.vue'),
		},
		{
			path: '/payroll/:id',
			name: 'payroll-source',
			component: () => import('./features/Payroll/PayrollSourceEditorPage.vue'),
		},
		{
			path: '/household',
			name: 'household',
			component: () => import('./features/Household/HouseholdPage.vue'),
		},
		{
			path: '/settings/locales',
			name: 'locales',
			component: () => import('./features/Locales/LocalesPage.vue'),
		},
		{
			path: '/settings/tax-years',
			name: 'tax-years',
			component: () => import('./features/TaxYears/TaxYearsPage.vue'),
		},
	],
})

export default router
