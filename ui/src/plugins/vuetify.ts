import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'

// Dense defaults: this is a data-entry tool, so more fields on screen beats generous spacing.
const compactField = { density: 'compact', hideDetails: 'auto', variant: 'outlined' } as const

export default createVuetify({
	components,
	directives,
	theme: {
		defaultTheme: 'light',
		themes: {
			light: {
				colors: {
					primary: '#1976d2',
					error: '#dc3545',
					info: '#055160',
					success: '#198754',
				},
			},
		},
	},
	defaults: {
		VBtn: {
			density: 'default',
			style: 'text-transform: none;',
		},
		VTextField: compactField,
		VSelect: compactField,
		VCheckbox: { density: 'compact', hideDetails: 'auto' },
		VCard: { variant: 'outlined' },
		VTable: { density: 'compact' },
		VAlert: { density: 'compact', variant: 'tonal' },
	},
})
