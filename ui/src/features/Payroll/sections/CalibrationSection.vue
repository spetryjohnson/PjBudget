<template>
	<v-card>
		<v-card-title class="section-title">Match a real paycheck</v-card-title>
		<v-card-text>
			<p class="hint">
				Enter a recent paycheck to compare it with the simulation. If a difference remains that the settings above can't
				explain, add it as an adjustment so budgeted net pay matches exactly.
			</p>
			<v-row dense>
				<v-col cols="6" sm="4">
					<DateField v-model="source.actualNetPayDate" label="Paycheck date" :error-messages="errors.actualNetPayDate" />
				</v-col>
				<v-col cols="6" sm="4">
					<NumberField v-model="source.actualNetPay" label="Actual net pay" prefix="$" nullable :error-messages="errors.actualNetPay" />
				</v-col>
				<v-col cols="12" sm="4">
					<NumberField
						v-model="source.netPayAdjustmentPerCheck"
						label="Net pay adjustment"
						prefix="$"
						suffix="/check"
						:error-messages="errors.netPayAdjustmentPerCheck"
					/>
				</v-col>
			</v-row>

			<v-alert
				v-if="comparison"
				:type="comparison.difference === 0 ? 'success' : 'info'"
				class="mt-3"
			>
				<template v-if="comparison.difference === 0">
					Check {{ comparison.checkNumber }} ({{ shortDate(comparison.payDate) }}) matches your paycheck exactly.
				</template>
				<template v-else>
					Check {{ comparison.checkNumber }} ({{ shortDate(comparison.payDate) }}) simulates
					{{ money(comparison.simulatedNetPay) }}; your paycheck was {{ money(comparison.actualNetPay) }}
					({{ comparison.difference > 0 ? '+' : '' }}{{ money(comparison.difference) }}).
					<v-btn size="small" variant="text" color="primary" @click="applyDifference">Add to adjustment</v-btn>
				</template>
			</v-alert>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import { computed } from 'vue'
	import DateField from '../../../components/DateField.vue'
	import NumberField from '../../../components/NumberField.vue'
	import { money, shortDate } from '../../../lib/format'
	import type { PayrollSource, PayrollSummary } from '../types-payroll'

	const props = defineProps<{ errors: Record<string, string[]>; summary: PayrollSummary | null }>()
	const source = defineModel<PayrollSource>({ required: true })

	const comparison = computed(() => props.summary?.actualComparison ?? null)

	// The simulated check already includes the current adjustment, so the remaining difference adds on top of it.
	function applyDifference() {
		if (!comparison.value) return
		source.value.netPayAdjustmentPerCheck = Number((source.value.netPayAdjustmentPerCheck + comparison.value.difference).toFixed(2))
	}
</script>
