<template>
	<div class="home">
		<h1>Home</h1>
		<p v-if="authStore.user">
			Signed in as <strong>{{ authStore.user.email }}</strong>.
		</p>
		<p class="placeholder">
			This is a placeholder home page. Start building here.
		</p>
	</div>
</template>

<script setup lang="ts">
	import { useAuthStore } from '../stores/auth'

	const authStore = useAuthStore()
</script>

<style scoped>
	.home {
		max-width: 800px;
	}

	.home h1 {
		margin-top: 0;
		font-size: 1.75rem;
		font-weight: 600;
	}

	.placeholder {
		color: #6c757d;
		font-style: italic;
	}
</style>
