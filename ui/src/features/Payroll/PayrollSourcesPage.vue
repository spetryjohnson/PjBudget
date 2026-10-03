<template>
	<div>
		<div class="page-header">
			<h1>Payroll</h1>
			<div class="page-actions">
				<v-select :model-value="year" :items="yearChoices" label="Year" style="width: 110px" @update:model-value="changeYear" />
				<v-btn color="primary" prepend-icon="mdi-plus" :disabled="!canAdd" :to="canAdd ? '/payroll/new' : undefined">
					Add payroll source
				</v-btn>
			</div>
		</div>
		<p class="hint">Each job's paychecks, simulated check by check. Select a source to edit it and see every check.</p>

		<v-alert v-if="setupSteps.length" type="info" class="mb-3">
			Before adding a payroll source:
			<ul class="setup-list">
				<li v-for="step in setupSteps" :key="step.text">
					<router-link :to="step.to">{{ step.text }}</router-link>
				</li>
			</ul>
		</v-alert>
		<v-alert v-if="error" type="error" class="mb-3">{{ error }}</v-alert>

		<v-card>
			<v-progress-linear v-if="loading" indeterminate color="primary" />
			<v-table>
				<thead>
					<tr>
						<th>Source</th>
						<th>Person</th>
						<th>Pay schedule</th>
						<th class="num">Checks</th>
						<th class="num">Annual gross</th>
						<th class="num">Annual net</th>
						<th class="num">Regular check</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="source in sources" :key="source.id" class="clickable-row" @click="router.push(`/payroll/${source.id}`)">
						<td>
							<strong>{{ source.name }}</strong>
							<span v-if="source.employerName" class="hint"> · {{ source.employerName }}</span>
						</td>
						<td>{{ source.personName }}</td>
						<td>{{ source.payFrequency === 'BIWEEKLY' ? 'Every 2 weeks' : 'Twice a month' }}</td>
						<template v-if="source.headline">
							<td class="num">{{ source.headline.checkCount }}</td>
							<td class="num">{{ wholeMoney(source.headline.annualGrossPay) }}</td>
							<td class="num">{{ wholeMoney(source.headline.annualNetPay) }}</td>
							<td class="num"><strong>{{ money(source.headline.regularNetPay) }}</strong></td>
							<td>
								<v-chip v-if="source.headline.warningCount" size="small" color="warning" variant="tonal">
									{{ source.headline.warningCount }} {{ source.headline.warningCount === 1 ? 'note' : 'notes' }}
								</v-chip>
							</td>
						</template>
						<td v-else colspan="5" class="hint">{{ source.simulationError }}</td>
					</tr>
					<tr v-if="!loading && sources.length === 0">
						<td colspan="8" class="empty-row">No payroll sources yet.</td>
					</tr>
				</tbody>
				<tfoot v-if="totals">
					<tr class="totals">
						<td colspan="4">Household total</td>
						<td class="num">{{ wholeMoney(totals.gross) }}</td>
						<td class="num">{{ wholeMoney(totals.net) }}</td>
						<td colspan="2"></td>
					</tr>
				</tfoot>
			</v-table>
		</v-card>
	</div>
</template>

<script setup lang="ts">
	import { computed, onMounted, ref } from 'vue'
	import { useRouter } from 'vue-router'
	import { toApiError } from '../../lib/apiErrors'
	import { money, wholeMoney } from '../../lib/format'
	import { useReferenceDataStore } from '../../stores/referenceData'
	import { payrollApi } from './api-payroll'
	import type { PayrollSourceSummary } from './types-payroll'

	const router = useRouter()
	const store = useReferenceDataStore()
	const sources = ref<PayrollSourceSummary[]>([])
	const year = ref(new Date().getFullYear())
	const loading = ref(true)
	const error = ref('')

	const yearChoices = computed(() => (store.years.length ? store.years : [year.value]))

	const canAdd = computed(() =>
		store.people.length > 0 && store.locales.length > 0 && sources.value.length < (store.payroll?.maxPayrollSources ?? 4))

	const setupSteps = computed(() => {
		const steps: { text: string; to: string }[] = []
		if (store.people.length === 0) steps.push({ text: 'Add the people who earn income', to: '/household' })
		if (store.locales.length === 0) steps.push({ text: 'Add the locales where you work and live', to: '/settings/locales' })
		return steps
	})

	const totals = computed(() => {
		const withNumbers = sources.value.filter(s => s.headline)
		if (withNumbers.length < 2) return null
		return {
			gross: withNumbers.reduce((sum, s) => sum + s.headline!.annualGrossPay, 0),
			net: withNumbers.reduce((sum, s) => sum + s.headline!.annualNetPay, 0),
		}
	})

	onMounted(async () => {
		await store.loadAll()
		year.value = store.defaultYear()
		await load()
	})

	function changeYear(value: number) {
		year.value = value
		load()
	}

	async function load() {
		loading.value = true
		try {
			sources.value = await payrollApi.list(year.value)
			error.value = ''
		} catch (e) {
			error.value = (await toApiError(e)).message
		} finally {
			loading.value = false
		}
	}
</script>

<style scoped>
	.setup-list {
		margin: 0.25rem 0 0 1.25rem;
	}

	.totals td {
		font-weight: 600;
		border-top: 2px solid #dee2e6;
	}
</style>
