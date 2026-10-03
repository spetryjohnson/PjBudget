<template>
	<v-card>
		<v-card-title class="section-title">Withholding elections</v-card-title>
		<v-card-text>
			<div class="subhead">Federal W-4</div>
			<v-row dense align="center">
				<v-col cols="12" sm="6">
					<v-select v-model="source.w4FilingStatus" :items="filingStatuses" label="Step 1(c): filing status" />
				</v-col>
				<v-col cols="12" sm="6">
					<v-checkbox v-model="source.w4MultipleJobs" label="Step 2(c): two jobs box is checked" />
				</v-col>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.w4Credits" label="Step 3: credits" prefix="$" suffix="/yr" :error-messages="errors.w4Credits" />
				</v-col>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.w4OtherIncome" label="4(a): other income" prefix="$" suffix="/yr" :error-messages="errors.w4OtherIncome" />
				</v-col>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.w4Deductions" label="4(b): deductions" prefix="$" suffix="/yr" :error-messages="errors.w4Deductions" />
				</v-col>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.w4ExtraWithholding" label="4(c): extra" prefix="$" suffix="/check" :error-messages="errors.w4ExtraWithholding" />
				</v-col>
			</v-row>

			<div class="subhead">Ohio IT 4</div>
			<v-row dense>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.stateWithholdingExemptions" label="Exemptions" step="1" hint="Line 4" :error-messages="errors.stateWithholdingExemptions" />
				</v-col>
				<v-col cols="6" sm="3">
					<NumberField v-model="source.stateAdditionalWithholding" label="Additional" prefix="$" suffix="/check" hint="Line 5" :error-messages="errors.stateAdditionalWithholding" />
				</v-col>
				<v-col cols="12" sm="6">
					<v-checkbox
						v-model="source.withholdsSchoolDistrictTax"
						label="Withhold school district tax"
						hint="Uncheck if your paystub has no school district line. It's still owed, and the household tax projection includes it."
						persistent-hint
					/>
				</v-col>
			</v-row>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import NumberField from '../../../components/NumberField.vue'
	import { filingStatuses } from '../../../lib/types-domain'
	import type { PayrollSource } from '../types-payroll'

	defineProps<{ errors: Record<string, string[]> }>()
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
</style>
