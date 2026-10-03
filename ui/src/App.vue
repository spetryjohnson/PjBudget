<template>
	<!-- Logged-in shell: header + sidebar + main content -->
	<div v-if="authStore.user && !mustChangePassword" class="app-container">
		<header class="panel-header">
			<h1 class="panel-title">PjBudget</h1>
			<div class="header-right">
				<span class="profile-text">{{ authStore.user.email }}</span>
				<button class="logout-btn" @click="logout">Sign out</button>
			</div>
		</header>

		<div class="main-layout">
			<aside class="sidebar">
				<nav class="sidebar-nav">
					<ul class="nav-list">
						<li class="nav-item">
							<router-link to="/" class="nav-link">
								<v-icon icon="mdi-view-dashboard-outline" class="nav-icon" />
								<span class="nav-text">Home</span>
							</router-link>
						</li>
						<li class="nav-item">
							<router-link to="/payroll" class="nav-link">
								<v-icon icon="mdi-cash-multiple" class="nav-icon" />
								<span class="nav-text">Payroll</span>
							</router-link>
						</li>
						<li class="nav-item">
							<router-link to="/household" class="nav-link">
								<v-icon icon="mdi-home-account" class="nav-icon" />
								<span class="nav-text">Household</span>
							</router-link>
						</li>
					</ul>
					<div class="nav-heading">Settings</div>
					<ul class="nav-list">
						<li class="nav-item">
							<router-link to="/settings/locales" class="nav-link">
								<v-icon icon="mdi-map-marker-outline" class="nav-icon" />
								<span class="nav-text">Locales</span>
							</router-link>
						</li>
						<li class="nav-item">
							<router-link to="/settings/tax-years" class="nav-link">
								<v-icon icon="mdi-file-percent-outline" class="nav-icon" />
								<span class="nav-text">Tax years</span>
							</router-link>
						</li>
					</ul>
				</nav>
			</aside>

			<main class="main-content">
				<div class="content-area">
					<router-view />
				</div>
			</main>
		</div>
	</div>

	<!-- Forced password change -->
	<div v-else-if="mustChangePassword" class="auth-shell">
		<ChangePasswordForm @passwordChanged="handlePasswordChanged" />
	</div>

	<!-- Unauthenticated -->
	<div v-else class="auth-shell">
		<LoginForm
			@login="handleLoginSuccess"
			@requirePasswordChange="handleRequirePasswordChange"
		/>
	</div>
</template>

<script setup lang="ts">
	import { ref, onMounted } from 'vue'
	import LoginForm from './features/Authentication/LoginForm.vue'
	import ChangePasswordForm from './features/Authentication/ChangePasswordForm.vue'
	import { useAuthStore } from './stores/auth'
	import { useRouter } from 'vue-router'

	const authStore = useAuthStore()
	const router = useRouter()
	const mustChangePassword = ref(false)

	function handleLoginSuccess() {
		// Logged in successfully; the template swaps to the main shell automatically.
	}

	function handleRequirePasswordChange() {
		mustChangePassword.value = true
	}

	function handlePasswordChanged() {
		mustChangePassword.value = false
		router.push('/')
	}

	async function logout() {
		await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' })
		authStore.logout()
		router.push('/')
	}

	onMounted(async () => {
		await authStore.loadUser()
	})
</script>

<style scoped>
	.app-container {
		min-height: 100vh;
		display: flex;
		flex-direction: column;
	}

	.panel-header {
		background-color: #f8f9fa;
		border-bottom: 1px solid #dee2e6;
		padding: 1rem 1.5rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.panel-title {
		margin: 0;
		font-size: 1.25rem;
		font-weight: 600;
		color: #212529;
	}

	.header-right {
		display: flex;
		align-items: center;
		gap: 1rem;
	}

	.profile-text {
		font-size: 0.9375rem;
		color: #495057;
	}

	.logout-btn {
		background-color: transparent;
		color: #0d6efd;
		border: 1px solid #0d6efd;
		padding: 0.375rem 0.875rem;
		border-radius: 0.375rem;
		font-size: 0.875rem;
		cursor: pointer;
	}

	.logout-btn:hover {
		background-color: #0d6efd;
		color: white;
	}

	.main-layout {
		flex: 1;
		display: grid;
		grid-template-columns: 220px 1fr;
	}

	.sidebar {
		background-color: #f8f9fa;
		border-right: 1px solid #dee2e6;
	}

	.sidebar-nav {
		padding: 1rem 0;
	}

	.nav-list {
		list-style: none;
		margin: 0;
		padding: 0;
	}

	.nav-item {
		margin-bottom: 0.25rem;
	}

	.nav-heading {
		margin: 1rem 1.5rem 0.25rem;
		font-size: 0.75rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		color: #6c757d;
	}

	.nav-link {
		display: flex;
		align-items: center;
		padding: 0.75rem 1rem;
		color: #495057;
		text-decoration: none;
		border-radius: 0.375rem;
		margin: 0 0.5rem;
	}

	.nav-link:hover {
		background-color: #e9ecef;
		color: #212529;
	}

	.nav-icon {
		margin-right: 0.75rem;
		font-size: 1.125rem;
	}

	.nav-text {
		font-size: 0.9375rem;
		font-weight: 500;
	}

	.router-link-active.nav-link {
		background-color: #e9ecef;
		color: #212529;
	}

	.main-content {
		display: flex;
		flex-direction: column;
		background-color: #ffffff;
	}

	.content-area {
		flex: 1;
		padding: 1.25rem 1.5rem;
		min-width: 0;
	}

	.auth-shell {
		min-height: 100vh;
		display: flex;
		align-items: center;
		justify-content: center;
		background-color: #f8f9fa;
		padding: 2rem 1rem;
	}

	@media (max-width: 768px) {
		.main-layout {
			grid-template-columns: 1fr;
		}
		.sidebar {
			display: none;
		}
	}
</style>
