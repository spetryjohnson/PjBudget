<template>
	<v-card>
		<v-card-title class="section-title d-flex align-center">
			Insurance &amp; other deductions
			<v-spacer />
			<v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="add">Add deduction</v-btn>
		</v-card-title>
		<v-card-text>
			<div v-for="(deduction, i) in source.deductions" :key="i" class="deduction">
				<v-row dense align="center">
					<v-col cols="5">
						<v-select :model-value="deduction.type" :items="deductionTypes" label="Type" @update:model-value="changeType(deduction, $event)" />
					</v-col>
					<v-col cols="6">
						<v-text-field v-model="deduction.label" label="Label" :error-messages="errors[`deductions[${i}].label`]" />
					</v-col>
					<v-col cols="1" class="text-right">
						<v-btn icon="mdi-close" size="small" variant="text" title="Remove" @click="source.deductions.splice(i, 1)" />
					</v-col>
					<v-col cols="5">
						<NumberField v-model="deduction.annualAmount" label="Annual amount" prefix="$" :error-messages="errors[`deductions[${i}].annualAmount`]" />
					</v-col>
					<v-col cols="6">
						<NumberField
							v-model="deduction.perCheckOverride"
							label="Exact per check (optional)"
							prefix="$"
							nullable
							:error-messages="errors[`deductions[${i}].perCheckOverride`]"
						/>
					</v-col>
				</v-row>
			</div>
			<p class="hint mt-2 mb-0">
				Premiums are entered per year and divided evenly across the year's checks. Use "exact per check" when your
				paystub shows a slightly different amount.
			</p>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import NumberField from '../../../components/NumberField.vue'
	import { deductionTypes, type DeductionType, type WageType } from '../../../lib/types-domain'
	import type { PayrollDeduction, PayrollSource } from '../types-payroll'

	const props = defineProps<{
		errors: Record<string, string[]>
		defaultTreatment: Record<DeductionType, WageType[]>
	}>()
	const source = defineModel<PayrollSource>({ required: true })

	const titleOf = (type: DeductionType) => deductionTypes.find(t => t.value === type)?.title ?? type

	function add() {
		source.value.deductions.push({
			type: 'MEDICAL',
			label: titleOf('MEDICAL'),
			annualAmount: 0,
			perCheckOverride: null,
			preTaxFor: [...props.defaultTreatment.MEDICAL],
		})
	}

	/**
	 * Changing the type also updates the label and tax treatment, but only where they still hold the old type's
	 * defaults. Anything the user customized is left alone.
	 */
	function changeType(deduction: PayrollDeduction, type: DeductionType) {
		if (deduction.label === '' || deduction.label === titleOf(deduction.type)) deduction.label = titleOf(type)
		if (sameSet(deduction.preTaxFor, props.defaultTreatment[deduction.type])) deduction.preTaxFor = [...props.defaultTreatment[type]]
		deduction.type = type
	}

	function sameSet(a: WageType[], b: WageType[]) {
		return a.length === b.length && a.every(x => b.includes(x))
	}
</script>

<style scoped>
	.deduction {
		padding: 6px 0;
		border-bottom: 1px solid #eef0f2;
	}

	.deduction:first-child {
		padding-top: 0;
	}
</style>
