import { defineStore } from 'pinia'
import type { User, WhoAmIResponse } from '../features/Identity/types-identity'
import { apiClient } from '../lib/apiClient'

export const useAuthStore = defineStore('auth', {
	state: () => ({
		user: null as User | null,
	}),

	getters: {
		isAuthenticated: (state) => state.user !== null,
		isSysAdmin: (state) => state.user?.roles?.includes('SystemAdmin') ?? false,
	},

	actions: {
		setUser(user: User | null) {
			this.user = user
		},

		async loadUser() {
			try {
				const data = await apiClient.get('api/identity/whoami').json<WhoAmIResponse>()
				this.user = data.user
			} catch {
				this.user = null
			}
		},

		logout() {
			this.user = null
		},
	},
})
