<template>
	<v-card>
		<v-card-title class="section-title d-flex align-center flex-wrap ga-2">
			Projected taxes
			<v-progress-circular v-if="loading" indeterminate size="16" width="2" color="primary" />
			<v-spacer />
			<v-chip v-if="projection" :color="outcomeColor(projection.totalRefundOrBalanceDue)" variant="tonal">
				{{ outcomeLabel(projection.totalRefundOrBalanceDue) }}
			</v-chip>
			<v-select v-model="year" :items="years" hide-details style="max-width: 110px" />
		</v-card-title>
		<v-card-text>
			<p class="hint">
				What the household's {{ year }} returns would owe, compared with what every paycheck withholds. It uses the
				simulated wages plus the entries under <em>Tax return inputs</em>; it isn't a full return.
			</p>

			<v-alert v-if="error" type="warning" class="mb-3">{{ error }}</v-alert>
			<v-alert v-for="warning in projection?.warnings ?? []" :key="warning.code" type="info" class="mb-2">
				{{ warning.message }}
			</v-alert>

			<v-row v-if="projection" dense>
				<v-col v-for="section in projection.sections" :key="section.title" cols="12" md="6" xl="3">
					<div class="section">
						<div class="section-name">{{ section.title }}</div>
						<v-table class="lines">
							<tbody>
								<tr v-for="line in section.lines" :key="line.label">
									<td>{{ line.label }}</td>
									<td class="num">{{ wholeMoney(line.amount) }}</td>
								</tr>
								<tr class="summary">
									<td>Owed</td>
									<td class="num">{{ money(section.liability) }}</td>
								</tr>
								<tr>
									<td>Withheld</td>
									<td class="num">{{ money(section.withheld) }}</td>
								</tr>
								<tr class="outcome" :class="outcomeColor(section.refundOrBalanceDue)">
									<td>{{ section.refundOrBalanceDue >= 0 ? 'Refund' : 'Balance due' }}</td>
									<td class="num">{{ money(Math.abs(section.refundOrBalanceDue)) }}</td>
								</tr>
							</tbody>
						</v-table>
						<p v-if="section.note" class="hint mt-1 mb-0">{{ section.note }}</p>
					</div>
				</v-col>
			</v-row>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import { ref, watch } from 'vue'
	import { toApiError } from '../../lib/apiErrors'
	import { money, wholeMoney } from '../../lib/format'
	import { householdApi, type HouseholdTaxProjection } from './api-household'

	defineProps<{ years: number[] }>()
	const year = defineModel<number>('year', { required: true })

	const projection = ref<HouseholdTaxProjection | null>(null)
	const loading = ref(false)
	const error = ref('')

	watch(year, load, { immediate: true })

	async function load() {
		loading.value = true
		try {
			projection.value = await householdApi.projection(year.value)
			error.value = ''
		} catch (e) {
			projection.value = null
			error.value = (await toApiError(e)).message
		} finally {
			loading.value = false
		}
	}

	function outcomeLabel(amount: number) {
		return amount >= 0 ? `Refund ${money(amount)}` : `Balance due ${money(-amount)}`
	}

	function outcomeColor(amount: number) {
		return amount >= 0 ? 'success' : 'error'
	}

	defineExpose({ reload: load })
</script>

<style scoped>
	.section {
		border: 1px solid #dee2e6;
		border-radius: 6px;
		padding: 0.5rem 0.75rem;
		height: 100%;
	}

	.section-name {
		font-weight: 600;
		margin-bottom: 0.25rem;
	}

	.lines {
		font-size: 0.8125rem;
	}

	.lines :deep(td) {
		height: 26px !important;
		padding: 0 4px !important;
	}

	.summary td {
		border-top: 2px solid #dee2e6;
		font-weight: 600;
	}

	.outcome td {
		font-weight: 700;
	}

	.outcome.success td {
		color: #198754;
	}

	.outcome.error td {
		color: #dc3545;
	}
</style>
