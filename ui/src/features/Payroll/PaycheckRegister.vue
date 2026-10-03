<template>
	<v-table fixed-header :height="height" class="register">
		<thead>
			<tr>
				<th class="num">#</th>
				<th>Date</th>
				<th class="num">Gross</th>
				<th class="num">Federal</th>
				<th class="num" title="Social Security + Medicare">FICA</th>
				<th class="num">State</th>
				<th class="num" title="City + school district">Local</th>
				<th class="num">401(k)</th>
				<th class="num" title="HSA, FSA, insurance and other deductions">Other</th>
				<th class="num">Net</th>
			</tr>
		</thead>
		<tbody>
			<tr
				v-for="check in simulation.checks"
				:key="check.number"
				class="clickable-row"
				:class="{ irregular: check.current.netPay !== simulation.summary.regularNetPay }"
				:title="`Check ${check.number}: click for the full paystub`"
				@click="emit('select', check)"
			>
				<td class="num">{{ check.number }}</td>
				<td>{{ shortDate(check.payDate) }}</td>
				<td class="num">{{ amount(check.current.grossPay) }}</td>
				<td class="num">{{ amount(check.current.federalIncomeTax) }}</td>
				<td class="num">{{ amount(fica(check.current)) }}</td>
				<td class="num">{{ amount(check.current.stateIncomeTax) }}</td>
				<td class="num">{{ amount(local(check.current)) }}</td>
				<td class="num">{{ amount(check.current.traditional401k) }}</td>
				<td class="num">{{ amount(other(check.current)) }}</td>
				<td class="num net">{{ amount(check.current.netPay) }}</td>
			</tr>
		</tbody>
		<tfoot>
			<tr class="totals">
				<td colspan="2">Year</td>
				<td class="num">{{ amount(totals.grossPay) }}</td>
				<td class="num">{{ amount(totals.federalIncomeTax) }}</td>
				<td class="num">{{ amount(fica(totals)) }}</td>
				<td class="num">{{ amount(totals.stateIncomeTax) }}</td>
				<td class="num">{{ amount(local(totals)) }}</td>
				<td class="num">{{ amount(totals.traditional401k) }}</td>
				<td class="num">{{ amount(other(totals)) }}</td>
				<td class="num net">{{ amount(totals.netPay) }}</td>
			</tr>
		</tfoot>
	</v-table>
</template>

<script setup lang="ts">
	import { computed } from 'vue'
	import { amount, shortDate } from '../../lib/format'
	import type { Paycheck, PaycheckLines, PayrollSimulation } from './types-payroll'

	const props = withDefaults(defineProps<{ simulation: PayrollSimulation; height?: string | number }>(), { height: 480 })
	const emit = defineEmits<{ select: [check: Paycheck] }>()

	const totals = computed(() => props.simulation.summary.annualTotals)

	const fica = (l: PaycheckLines) => l.socialSecurityTax + l.medicareTax + l.additionalMedicareTax
	const local = (l: PaycheckLines) => l.cityIncomeTax + l.schoolDistrictTax
	const other = (l: PaycheckLines) => l.hsaEmployee + l.healthFsa + l.deductions.reduce((sum, d) => sum + d, 0)
</script>

<style scoped>
	.register {
		font-size: 0.8125rem;
	}

	.register :deep(td),
	.register :deep(th) {
		padding: 0 8px !important;
		height: 30px !important;
		white-space: nowrap;
	}

	.irregular {
		background-color: #eef6ff;
	}

	.net {
		font-weight: 600;
	}

	.totals td {
		font-weight: 600;
		border-top: 2px solid #dee2e6;
		background: #f8f9fa;
	}
</style>
