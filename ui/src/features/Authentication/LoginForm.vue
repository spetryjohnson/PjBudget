<template>
	<div class="login-container">
		<div class="login-box">
			<h2 class="box-title">Sign in</h2>
			<form @submit.prevent="handleLogin" class="login-form">
				<div class="form-group">
					<label for="email">Email</label>
					<input
						id="email"
						v-model="email"
						type="email"
						class="form-input"
						placeholder="you@example.com"
						autocomplete="username"
						required
					/>
				</div>
				<div class="form-group">
					<label for="password">Password</label>
					<input
						id="password"
						v-model="password"
						type="password"
						class="form-input"
						placeholder="Password"
						autocomplete="current-password"
						required
					/>
				</div>
				<button type="submit" class="login-btn">Sign in</button>
			</form>
			<div v-if="loginError" class="error-message">
				Login failed. Please check your email and password.
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref } from 'vue'
	import type { LoginRequest, LoginResponse } from './types-auth'
	import { HTTPError } from 'ky'
	import { apiClient } from '../../lib/apiClient'
	import { useAuthStore } from '../../stores/auth'

	const email = ref('')
	const password = ref('')
	const loginError = ref(false)

	const authStore = useAuthStore()

	const emit = defineEmits<{
		login: []
		requirePasswordChange: []
	}>()

	async function handleLogin() {
		loginError.value = false

		const payload: LoginRequest = {
			email: email.value,
			password: password.value,
			rememberMe: false
		}

		try {
			const resp = await apiClient.post('api/auth/login', { json: payload })
				.json<LoginResponse>()

			authStore.setUser(resp.user)
			// Refresh from WhoAmI so we pick up any fields only that endpoint returns.
			authStore.loadUser()

			if (resp.mustChangePassword) {
				emit('requirePasswordChange')
			} else {
				emit('login')
			}

			email.value = ''
			password.value = ''
		}
		catch (error) {
			if (error instanceof HTTPError) {
				console.error('Login failed with status', error.response.status)
			}
			loginError.value = true
		}
	}
</script>

<style scoped>
	.login-container {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 60vh;
		padding: 0 1rem;
	}

	.login-box {
		width: 100%;
		max-width: 400px;
		padding: 2rem;
		border: 1px solid #ced4da;
		border-radius: 0.5rem;
		background-color: #ffffff;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		box-sizing: border-box;
	}

	.box-title {
		margin-top: 0;
		margin-bottom: 1.5rem;
		font-size: 1.5rem;
		font-weight: 600;
		color: #212529;
		text-align: center;
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-group label {
		display: block;
		margin-bottom: 0.5rem;
		font-weight: 500;
		color: #212529;
	}

	.form-input {
		width: 100%;
		padding: 0.75rem;
		border: 1px solid #ced4da;
		border-radius: 0.375rem;
		font-size: 1rem;
		box-sizing: border-box;
	}

	.form-input:focus {
		outline: none;
		border-color: #86b7fe;
		box-shadow: 0 0 0 0.25rem rgba(13, 110, 253, 0.25);
	}

	.login-btn {
		background-color: #0d6efd;
		color: white;
		border: none;
		padding: 0.75rem 1.5rem;
		border-radius: 0.375rem;
		font-size: 1rem;
		cursor: pointer;
		width: 100%;
		margin-top: 0.5rem;
	}

	.login-btn:hover {
		background-color: #0b5ed7;
	}

	.error-message {
		color: #dc3545;
		font-size: 0.875rem;
		margin-top: 1rem;
		padding: 0.5rem;
		background-color: #f8d7da;
		border: 1px solid #f5c2c7;
		border-radius: 0.375rem;
	}
</style>
