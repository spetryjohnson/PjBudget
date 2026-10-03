<template>
	<v-text-field
		:model-value="model ?? ''"
		type="date"
		@update:model-value="model = $event ? $event : null"
	/>
</template>

<script setup lang="ts">
	/** A date input bound to an ISO date string ("2026-01-09"), where an empty field means null. */
	const model = defineModel<string | null | undefined>()
</script>
