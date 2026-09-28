<template>
	<div class="change-password-container">
		<div class="change-password-box">
			<h2 class="box-title">Change password</h2>
			<div class="warning-box">
				You're signed in with the default password. For security, you must change it before continuing.
			</div>
			<form @submit.prevent="handleChangePassword" class="change-password-form">
				<div class="form-group">
					<label for="currentPassword">Current password</label>
					<input
						id="currentPassword"
						v-model="currentPassword"
						type="password"
						class="form-input"
						autocomplete="current-password"
						required
					/>
				</div>
				<div class="form-group">
					<label for="newPassword">New password</label>
					<input
						id="newPassword"
						v-model="newPassword"
						type="password"
						class="form-input"
						autocomplete="new-password"
						required
					/>
				</div>
				<div class="form-group">
					<label for="confirmPassword">Confirm new password</label>
					<input
						id="confirmPassword"
						v-model="confirmPassword"
						type="password"
						class="form-input"
						autocomplete="new-password"
						required
					/>
				</div>
				<div class="password-requirements">
					<strong>Requirements</strong>
					<ul>
						<li>At least 8 characters</li>
						<li>At least one uppercase letter (A-Z)</li>
						<li>At least one lowercase letter (a-z)</li>
						<li>At least one digit (0-9)</li>
					</ul>
				</div>
				<button type="submit" class="change-password-btn" :disabled="isSubmitting">
					{{ isSubmitting ? 'Changing password...' : 'Change password' }}
				</button>
			</form>
			<div v-if="errorMessage" class="error-message">
				{{ errorMessage }}
			</div>
			<div v-if="successMessage" class="success-message">
				{{ successMessage }}
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { ref } from 'vue'
	import type { ChangePasswordRequest, ChangePasswordResponse } from './types-auth'
	import { HTTPError } from 'ky'
	import { apiClient } from '../../lib/apiClient'

	const currentPassword = ref('')
	const newPassword = ref('')
	const confirmPassword = ref('')
	const errorMessage = ref('')
	const successMessage = ref('')
	const isSubmitting = ref(false)

	const emit = defineEmits<{
		passwordChanged: []
	}>()

	async function handleChangePassword() {
		errorMessage.value = ''
		successMessage.value = ''

		if (newPassword.value !== confirmPassword.value) {
			errorMessage.value = 'New passwords do not match.'
			return
		}
		if (newPassword.value.length < 8) {
			errorMessage.value = 'Password must be at least 8 characters long.'
			return
		}
		if (!/[A-Z]/.test(newPassword.value)) {
			errorMessage.value = 'Password must contain at least one uppercase letter.'
			return
		}
		if (!/[a-z]/.test(newPassword.value)) {
			errorMessage.value = 'Password must contain at least one lowercase letter.'
			return
		}
		if (!/[0-9]/.test(newPassword.value)) {
			errorMessage.value = 'Password must contain at least one digit.'
			return
		}

		const payload: ChangePasswordRequest = {
			currentPassword: currentPassword.value,
			newPassword: newPassword.value
		}

		isSubmitting.value = true

		try {
			const response = await apiClient.post('api/auth/change-password', { json: payload })
				.json<ChangePasswordResponse>()

			if (response.ok) {
				successMessage.value = 'Password changed successfully. Redirecting…'
				currentPassword.value = ''
				newPassword.value = ''
				confirmPassword.value = ''

				setTimeout(() => emit('passwordChanged'), 1200)
			} else {
				errorMessage.value = response.errors?.join(' ') || 'Failed to change password.'
			}
		}
		catch (error) {
			if (error instanceof HTTPError) {
				try {
					const errorData = await error.response.json() as ChangePasswordResponse
					errorMessage.value = errorData.errors?.join(' ') ||
						'Failed to change password. Please check your current password.'
				} catch {
					errorMessage.value = 'Failed to change password. Please check your current password.'
				}
			} else {
				errorMessage.value = 'An unexpected error occurred.'
			}
		}
		finally {
			isSubmitting.value = false
		}
	}
</script>

<style scoped>
	.change-password-container {
		display: flex;
		justify-content: center;
		align-items: center;
		padding: 0 1rem;
	}

	.change-password-box {
		width: 100%;
		max-width: 600px;
		padding: 2rem;
		border: 1px solid #ced4da;
		border-radius: 0.5rem;
		background-color: #ffffff;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		box-sizing: border-box;
	}

	.box-title {
		margin-top: 0;
		margin-bottom: 1rem;
		font-size: 1.5rem;
		font-weight: 600;
		color: #212529;
		text-align: center;
	}

	.warning-box {
		padding: 1rem;
		margin-bottom: 1.5rem;
		background-color: #fff3cd;
		border: 1px solid #ffecb5;
		border-radius: 0.375rem;
		color: #664d03;
		font-size: 0.95rem;
		line-height: 1.5;
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-group label {
		display: block;
		margin-bottom: 0.5rem;
		font-weight: 500;
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

	.password-requirements {
		padding: 1rem;
		margin-bottom: 1.5rem;
		background-color: #f8f9fa;
		border: 1px solid #dee2e6;
		border-radius: 0.375rem;
		font-size: 0.875rem;
	}

	.password-requirements strong {
		display: block;
		margin-bottom: 0.5rem;
	}

	.password-requirements ul {
		margin: 0;
		padding-left: 1.5rem;
		color: #6c757d;
	}

	.change-password-btn {
		background-color: #0d6efd;
		color: white;
		border: none;
		padding: 0.75rem 1.5rem;
		border-radius: 0.375rem;
		font-size: 1rem;
		cursor: pointer;
		width: 100%;
	}

	.change-password-btn:hover:not(:disabled) {
		background-color: #0b5ed7;
	}

	.change-password-btn:disabled {
		background-color: #6c757d;
		cursor: not-allowed;
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

	.success-message {
		color: #0f5132;
		font-size: 0.875rem;
		margin-top: 1rem;
		padding: 0.5rem;
		background-color: #d1e7dd;
		border: 1px solid #badbcc;
		border-radius: 0.375rem;
	}
</style>
