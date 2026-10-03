<template>
	<div>
		<div class="page-header">
			<h1>Home</h1>
		</div>

		<v-card class="mb-4" max-width="900">
			<v-card-title class="section-title d-flex align-center">
				Take-home pay in {{ year }}
				<v-spacer />
				<v-btn size="small" variant="text" to="/payroll">Payroll</v-btn>
			</v-card-title>
			<v-table>
				<thead>
					<tr>
						<th>Source</th>
						<th>Person</th>
						<th class="num">Regular check</th>
						<th class="num">Checks</th>
						<th class="num">Net for the year</th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="source in sources" :key="source.id" class="clickable-row" @click="router.push(`/payroll/${source.id}`)">
						<td>{{ source.name }}</td>
						<td>{{ source.personName }}</td>
						<template v-if="source.headline">
							<td class="num"><strong>{{ money(source.headline.regularNetPay) }}</strong></td>
							<td class="num">{{ source.headline.checkCount }}</td>
							<td class="num">{{ wholeMoney(source.headline.annualNetPay) }}</td>
						</template>
						<td v-else colspan="3" class="hint">{{ source.simulationError }}</td>
					</tr>
					<tr v-if="loaded && sources.length === 0">
						<td colspan="5" class="empty-row">
							No payroll sources yet. <router-link to="/payroll">Set up payroll</router-link>.
						</td>
					</tr>
				</tbody>
			</v-table>
		</v-card>
	</div>
</template>

<script setup lang="ts">
	import { onMounted, ref } from 'vue'
	import { useRouter } from 'vue-router'
	import { payrollApi } from '../features/Payroll/api-payroll'
	import type { PayrollSourceSummary } from '../features/Payroll/types-payroll'
	import { money, wholeMoney } from '../lib/format'
	import { useReferenceDataStore } from '../stores/referenceData'

	const router = useRouter()
	const store = useReferenceDataStore()
	const sources = ref<PayrollSourceSummary[]>([])
	const year = ref(new Date().getFullYear())
	const loaded = ref(false)

	onMounted(async () => {
		await store.loadAll()
		year.value = store.defaultYear()
		sources.value = await payrollApi.list(year.value)
		loaded.value = true
	})
</script>
