<template>
	<div>
		<div class="page-header">
			<h1>Tax years</h1>
		</div>
		<p class="hint">Tax rules and contribution limits for each year. Start a new year by copying the previous one.</p>
		<v-card max-width="500">
			<v-table>
				<thead>
					<tr>
						<th>Year</th>
						<th>Last updated</th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="taxYear in store.taxYears" :key="taxYear.year">
						<td>{{ taxYear.year }}</td>
						<td>{{ new Date(taxYear.updatedAt).toLocaleString() }}</td>
					</tr>
				</tbody>
			</v-table>
		</v-card>
	</div>
</template>

<script setup lang="ts">
	import { onMounted } from 'vue'
	import { useReferenceDataStore } from '../../stores/referenceData'

	const store = useReferenceDataStore()

	onMounted(() => store.refreshTaxYears())
</script>
