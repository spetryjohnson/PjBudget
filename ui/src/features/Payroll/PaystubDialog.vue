<template>
	<v-dialog v-model="open" max-width="640">
		<v-card v-if="check" variant="elevated">
			<v-card-title class="d-flex align-center">
				Check {{ check.number }} · {{ longDate(check.payDate) }}
				<v-spacer />
				<v-btn icon="mdi-close" variant="text" size="small" @click="open = false" />
			</v-card-title>
			<v-card-text>
				<v-table class="paystub">
					<thead>
						<tr>
							<th></th>
							<th class="num">This check</th>
							<th class="num">Year to date</th>
						</tr>
					</thead>
					<tbody>
						<template v-for="group in groups" :key="group.title">
							<tr class="group-row">
								<td colspan="3">{{ group.title }}</td>
							</tr>
							<tr v-for="line in group.lines" :key="line.label" :class="line.style">
								<td>{{ line.label }}</td>
								<td class="num">{{ money(line.value(check.current)) }}</td>
								<td class="num">{{ money(line.value(check.yearToDate)) }}</td>
							</tr>
						</template>
						<tr class="total-row">
							<td>Net pay</td>
							<td class="num">{{ money(check.current.netPay) }}</td>
							<td class="num">{{ money(check.yearToDate.netPay) }}</td>
						</tr>
					</tbody>
				</v-table>

				<div class="subhead">Taxable wages</div>
				<v-table class="paystub">
					<tbody>
						<tr v-for="wage in wageLines" :key="wage.label">
							<td>{{ wage.label }}</td>
							<td class="num">{{ money(wage.value(check.current)) }}</td>
							<td class="num">{{ money(wage.value(check.yearToDate)) }}</td>
						</tr>
					</tbody>
				</v-table>
			</v-card-text>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
	import { computed } from 'vue'
	import { longDate, money } from '../../lib/format'
	import type { Paycheck, PaycheckLines, PayrollSimulation } from './types-payroll'

	interface Line {
		label: string
		value: (lines: PaycheckLines) => number
		style?: string
	}

	const props = defineProps<{ check: Paycheck | null; simulation: PayrollSimulation }>()
	const open = defineModel<boolean>({ required: true })

	// A line that is zero all year (e.g. no stipend) is noise on a paystub, so it's left out.
	const annual = computed(() => props.simulation.summary.annualTotals)
	const used = (line: Line) => line.value(annual.value) !== 0

	const groups = computed<{ title: string; lines: Line[] }[]>(() => [
		{
			title: 'Earnings',
			lines: ([
				{ label: 'Base pay', value: l => l.basePay },
				{ label: 'Taxable stipend', value: l => l.taxableStipend },
				{ label: 'Gross pay', value: l => l.grossPay, style: 'subtotal-row' },
			] as Line[]).filter(line => line.label === 'Gross pay' || used(line)),
		},
		{
			title: 'Taxes',
			lines: ([
				{ label: 'Federal income tax', value: l => l.federalIncomeTax },
				{ label: 'Social Security', value: l => l.socialSecurityTax },
				{ label: 'Medicare', value: l => l.medicareTax },
				{ label: 'Additional Medicare', value: l => l.additionalMedicareTax },
				{ label: 'State income tax', value: l => l.stateIncomeTax },
				{ label: 'City income tax', value: l => l.cityIncomeTax },
				{ label: 'School district tax', value: l => l.schoolDistrictTax },
			] as Line[]).filter(used),
		},
		{
			title: 'Deductions',
			lines: ([
				{ label: 'Traditional 401(k)', value: l => l.traditional401k },
				{ label: 'HSA', value: l => l.hsaEmployee },
				{ label: 'Health FSA', value: l => l.healthFsa },
				...props.simulation.deductions.map((d, i) => ({ label: d.label, value: (l: PaycheckLines) => l.deductions[i] ?? 0 })),
			] as Line[]).filter(used),
		},
		{
			title: 'Other',
			lines: ([
				{ label: 'Non-taxable stipend', value: l => l.nonTaxableStipend },
				{ label: 'Net pay adjustment', value: l => l.netPayAdjustment },
				{ label: 'Employer 401(k) (not paid to you)', value: l => l.employerRetirement, style: 'memo-row' },
				{ label: 'Employer HSA (not paid to you)', value: l => l.employerHsa, style: 'memo-row' },
			] as Line[]).filter(used),
		},
	].filter(group => group.lines.length > 0))

	const wageLines: Line[] = [
		{ label: 'Federal', value: l => l.taxableWages.federal },
		{ label: 'State', value: l => l.taxableWages.state },
		{ label: 'Social Security (up to the wage base)', value: l => l.socialSecurityTaxedWages },
		{ label: 'Medicare', value: l => l.taxableWages.medicare },
		{ label: 'City', value: l => l.taxableWages.city },
		{ label: 'School district', value: l => l.taxableWages.school },
	]
</script>

<style scoped>
	.paystub {
		font-size: 0.8125rem;
	}

	.paystub :deep(td),
	.paystub :deep(th) {
		height: 28px !important;
	}

	.group-row td {
		font-weight: 600;
		color: #495057;
		background: #f8f9fa;
	}

	.subtotal-row td {
		font-weight: 600;
	}

	.memo-row td {
		color: #6c757d;
		font-style: italic;
	}

	.total-row td {
		font-weight: 700;
		border-top: 2px solid #dee2e6;
	}

	.subhead {
		font-weight: 600;
		margin: 1rem 0 0.25rem;
	}
</style>
