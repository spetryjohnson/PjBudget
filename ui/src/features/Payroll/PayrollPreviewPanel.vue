<template>
	<v-card class="preview">
		<v-card-title class="section-title d-flex align-center">
			Paychecks
			<v-progress-circular v-if="loading" indeterminate size="16" width="2" color="primary" class="ml-2" />
			<v-spacer />
			<v-select v-model="year" :items="years" hide-details style="max-width: 110px" />
		</v-card-title>
		<v-card-text>
			<v-alert v-if="problems.length" type="warning" class="mb-3">
				{{ simulation ? 'The preview is out of date until these are fixed:' : 'Complete these to see the paychecks:' }}
				<ul class="problem-list">
					<li v-for="problem in problems" :key="problem">{{ problem }}</li>
				</ul>
			</v-alert>

			<div v-if="simulation" :class="{ stale: problems.length > 0 }">
				<div class="stats">
					<div class="stat">
						<div class="stat-label">Regular check</div>
						<div class="stat-value primary">{{ money(summary!.regularNetPay) }}</div>
					</div>
					<div class="stat">
						<div class="stat-label">Net for the year</div>
						<div class="stat-value">{{ wholeMoney(summary!.annualTotals.netPay) }}</div>
					</div>
					<div class="stat">
						<div class="stat-label">Gross for the year</div>
						<div class="stat-value">{{ wholeMoney(summary!.annualTotals.grossPay) }}</div>
					</div>
					<div class="stat">
						<div class="stat-label">Taxes</div>
						<div class="stat-value">{{ wholeMoney(summary!.annualTotals.totalTaxes) }}</div>
						<div class="stat-note">{{ taxShare }} of gross</div>
					</div>
					<div class="stat">
						<div class="stat-label">Checks</div>
						<div class="stat-value">{{ summary!.checkCount }}</div>
						<div class="stat-note">{{ ssNote }}</div>
					</div>
				</div>

				<v-alert v-for="warning in simulation.warnings" :key="warning.code" type="info" class="mb-2" icon="mdi-information-outline">
					{{ warning.message }}
				</v-alert>

				<PaycheckRegister :simulation="simulation" class="mb-2" @select="showCheck" />
				<p class="hint mb-0">Shaded checks differ from the regular check. Select a check for its full paystub.</p>
			</div>
		</v-card-text>

		<PaystubDialog v-if="simulation" v-model="paystubOpen" :check="selectedCheck" :simulation="simulation" />
	</v-card>
</template>

<script setup lang="ts">
	import { computed, ref } from 'vue'
	import { money, wholeMoney } from '../../lib/format'
	import PaycheckRegister from './PaycheckRegister.vue'
	import PaystubDialog from './PaystubDialog.vue'
	import type { Paycheck, PayrollSimulation } from './types-payroll'

	const props = defineProps<{
		simulation: PayrollSimulation | null
		loading: boolean
		problems: string[]
		years: number[]
	}>()
	const year = defineModel<number>('year', { required: true })

	const paystubOpen = ref(false)
	const selectedCheck = ref<Paycheck | null>(null)

	const summary = computed(() => props.simulation?.summary)

	const taxShare = computed(() => {
		const totals = summary.value?.annualTotals
		if (!totals || totals.grossPay === 0) return '—'
		return `${((totals.totalTaxes / totals.grossPay) * 100).toFixed(1)}%`
	})

	const ssNote = computed(() => {
		const s = summary.value
		if (!s || s.checksWithoutSocialSecurity === 0) return 'SS withheld from all'
		return `No SS after check ${s.finalSocialSecurityCheck}`
	})

	function showCheck(check: Paycheck) {
		selectedCheck.value = check
		paystubOpen.value = true
	}
</script>

<style scoped>
	.stats {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
		gap: 0.5rem;
		margin-bottom: 0.75rem;
	}

	.stat {
		border: 1px solid #dee2e6;
		border-radius: 6px;
		padding: 0.5rem 0.75rem;
	}

	.stat-label {
		font-size: 0.75rem;
		color: #6c757d;
	}

	.stat-value {
		font-size: 1.125rem;
		font-weight: 600;
		font-variant-numeric: tabular-nums;
	}

	.stat-value.primary {
		color: #1976d2;
	}

	.stat-note {
		font-size: 0.75rem;
		color: #6c757d;
	}

	.stale {
		opacity: 0.55;
	}

	.problem-list {
		margin: 0.25rem 0 0 1.25rem;
	}
</style>
