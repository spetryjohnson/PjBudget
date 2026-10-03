<template>
	<v-card>
		<v-card-title class="section-title">Retirement, HSA &amp; FSA</v-card-title>
		<v-card-text>
			<div class="subhead">Traditional 401(k)</div>
			<v-row dense>
				<v-col cols="6" sm="4">
					<NumberField v-model="source.traditional401kPercent" label="Contribution" suffix="% of pay" :error-messages="errors.traditional401kPercent" />
				</v-col>
				<v-col cols="6" sm="4">
					<NumberField
						v-model="source.traditional401kPerCheckOverride"
						label="Exact amount per check"
						prefix="$"
						nullable
						hint="Optional; overrides the %"
						:error-messages="errors.traditional401kPerCheckOverride"
					/>
				</v-col>
				<v-col cols="12" sm="4" class="helper">
					<template v-if="summary">
						{{ wholeMoney(summary.annualTotals.traditional401k) }} of {{ wholeMoney(summary.traditional401kLimit) }} limit<br />
						Max out with {{ summary.traditional401kMaxOutPercent }}% of pay
					</template>
				</v-col>
				<v-col cols="4">
					<NumberField v-model="source.employerNonElectivePercent" label="Employer contribution" suffix="%" hint="Flat, regardless of yours" :error-messages="errors.employerNonElectivePercent" />
				</v-col>
				<v-col cols="4">
					<NumberField v-model="source.employerMatchPercent" label="Employer match" suffix="%" hint="Of what you contribute…" :error-messages="errors.employerMatchPercent" />
				</v-col>
				<v-col cols="4">
					<NumberField v-model="source.employerMatchCapPercent" label="Match up to" suffix="% of pay" hint="…up to this much of pay" :error-messages="errors.employerMatchCapPercent" />
				</v-col>
			</v-row>

			<div class="subhead">Health savings account</div>
			<v-row dense>
				<v-col cols="6" sm="4">
					<NumberField v-model="source.hsaEmployeePerCheck" label="Your contribution" prefix="$" suffix="/check" :error-messages="errors.hsaEmployeePerCheck" />
				</v-col>
				<v-col cols="6" sm="4">
					<NumberField v-model="source.hsaEmployerPerCheck" label="Employer contribution" prefix="$" suffix="/check" :error-messages="errors.hsaEmployerPerCheck" />
				</v-col>
				<v-col cols="12" sm="4" class="helper">
					<template v-if="summary">
						<template v-if="summary.hsaLimit > 0">
							{{ wholeMoney(summary.annualTotals.hsaEmployee + summary.annualTotals.employerHsa) }} of {{ wholeMoney(summary.hsaLimit) }} limit
						</template>
						<template v-else>No HSA coverage set — <router-link to="/household">household settings</router-link></template>
					</template>
				</v-col>
			</v-row>

			<div class="subhead">Health FSA</div>
			<v-row dense>
				<v-col cols="6" sm="4">
					<NumberField v-model="source.healthFsaAnnualElection" label="Annual election" prefix="$" :error-messages="errors.healthFsaAnnualElection" />
				</v-col>
				<v-col cols="6" sm="4">
					<NumberField
						v-model="source.healthFsaPerCheckOverride"
						label="Exact amount per check"
						prefix="$"
						nullable
						hint="Optional; overrides election ÷ checks"
						:error-messages="errors.healthFsaPerCheckOverride"
					/>
				</v-col>
			</v-row>

			<div class="subhead">Stipend</div>
			<v-row dense align="center">
				<v-col cols="6" sm="4">
					<NumberField v-model="source.stipendPerCheck" label="Stipend" prefix="$" suffix="/check" :error-messages="errors.stipendPerCheck" />
				</v-col>
				<v-col cols="6" sm="8">
					<v-checkbox v-model="source.stipendIsTaxable" label="Taxable (part of gross pay)" />
				</v-col>
			</v-row>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import NumberField from '../../../components/NumberField.vue'
	import { wholeMoney } from '../../../lib/format'
	import type { PayrollSource, PayrollSummary } from '../types-payroll'

	defineProps<{ errors: Record<string, string[]>; summary: PayrollSummary | null }>()
	const source = defineModel<PayrollSource>({ required: true })
</script>

<style scoped>
	.subhead {
		font-weight: 600;
		font-size: 0.8125rem;
		margin: 0.75rem 0 0.25rem;
	}

	.subhead:first-child {
		margin-top: 0;
	}

	.helper {
		font-size: 0.8125rem;
		color: #495057;
	}
</style>
