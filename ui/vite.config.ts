import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import mkcert from 'vite-plugin-mkcert'

export default defineConfig({
	plugins: [
		vue(),
		vuetify({ autoImport: true }),
		mkcert()
	],
	server: {
		https: true as any,
		port: 5175,
		proxy: {
			'/api': {
				target: 'https://localhost:7265',
				changeOrigin: false,
				secure: false
			}
		}
	},
	build: {
		outDir: 'dist',
		emptyOutDir: true
	}
})
