import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'

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
		VTextField: {
			density: 'compact',
			hideDetails: 'auto',
		},
	},
})
