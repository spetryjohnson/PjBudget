<template>
	<v-card>
		<v-card-title class="section-title">Tax treatment</v-card-title>
		<v-card-text>
			<p class="hint">
				A check means that tax applies to the amount. Pre-tax deductions come out of pay before the taxes left unchecked.
				Highlighted boxes differ from the usual treatment; change them only if your paystub shows otherwise.
			</p>
			<v-table class="tax-grid">
				<thead>
					<tr class="taxed-by-row">
						<th></th>
						<th :colspan="wageTypes.length" class="text-center taxed-by">Taxed by</th>
						<th></th>
					</tr>
					<tr>
						<th></th>
						<th v-for="w in wageTypes" :key="w.value" class="text-center" :title="w.title">{{ w.label }}</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="row in rows" :key="row.key">
						<td>{{ row.label }}</td>
						<td
							v-for="w in wageTypes"
							:key="w.value"
							class="text-center"
							:class="{ 'non-default': row.taxed.includes(w.value) !== row.defaults.includes(w.value) }"
						>
							<v-checkbox-btn
								:model-value="row.taxed.includes(w.value)"
								density="compact"
								class="d-inline-flex"
								:aria-label="`${row.label}: taxed by ${w.title}`"
								@update:model-value="toggle(row, w.value, !!$event)"
							/>
						</td>
						<td class="num">
							<v-btn v-if="!sameSet(row.taxed, row.defaults)" size="x-small" variant="text" @click="row.set([...row.defaults])">
								Reset
							</v-btn>
						</td>
					</tr>
				</tbody>
			</v-table>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import { computed } from 'vue'
	import { wageTypes, type WageType } from '../../../lib/types-domain'
	import type { PayrollReference, PayrollSource } from '../types-payroll'

	/** An amount on the paystub and the taxes that apply to it. */
	interface Row {
		key: string
		label: string
		taxed: WageType[]
		defaults: WageType[]
		set: (taxed: WageType[]) => void
	}

	const props = defineProps<{ reference: PayrollReference }>()
	const source = defineModel<PayrollSource>({ required: true })

	const allWageTypes = wageTypes.map(w => w.value)
	const allExcept = (types: WageType[]) => allWageTypes.filter(w => !types.includes(w))

	// Deductions are stored as the taxes they come out before, so the taxes that apply to them are the rest.
	function deductionRow(
		key: string,
		label: string,
		preTaxFor: WageType[],
		defaultPreTaxFor: WageType[],
		setPreTaxFor: (preTaxFor: WageType[]) => void,
	): Row {
		return {
			key,
			label,
			taxed: allExcept(preTaxFor),
			defaults: allExcept(defaultPreTaxFor),
			set: taxed => setPreTaxFor(allExcept(taxed)),
		}
	}

	const rows = computed<Row[]>(() => {
		const s = source.value
		const r = props.reference
		const result = [
			deductionRow('401k', 'Traditional 401(k)', s.traditional401kPreTaxFor, r.defaultTraditional401kTreatment, v => (s.traditional401kPreTaxFor = v)),
			deductionRow('hsa', 'HSA', s.hsaPreTaxFor, r.defaultCafeteriaPlanTreatment, v => (s.hsaPreTaxFor = v)),
			deductionRow('fsa', 'Health FSA', s.healthFsaPreTaxFor, r.defaultCafeteriaPlanTreatment, v => (s.healthFsaPreTaxFor = v)),
			...s.deductions.map((d, i) =>
				deductionRow(`deduction-${i}`, d.label || 'Deduction', d.preTaxFor, r.defaultDeductionTreatment[d.type], v => (d.preTaxFor = v)),
			),
		]
		if (s.groupTermLifePerCheck > 0) {
			result.push({
				key: 'gtl',
				label: 'Group-term life',
				taxed: s.groupTermLifeTaxedFor,
				defaults: r.defaultGroupTermLifeTreatment,
				set: v => (s.groupTermLifeTaxedFor = v),
			})
		}
		return result
	})

	function toggle(row: Row, wageType: WageType, checked: boolean) {
		const next = new Set(row.taxed)
		if (checked) next.add(wageType)
		else next.delete(wageType)
		row.set(allWageTypes.filter(w => next.has(w)))
	}

	function sameSet(a: WageType[], b: WageType[]) {
		return a.length === b.length && a.every(x => b.includes(x))
	}
</script>

<style scoped>
	.tax-grid td,
	.tax-grid th {
		padding: 0 6px !important;
	}

	.taxed-by-row th {
		height: 28px !important;
		border-bottom: none !important;
	}

	/* The rule under the heading groups the tax columns beneath it. */
	.taxed-by-row th.taxed-by {
		font-weight: 600;
		border-bottom: thin solid rgba(var(--v-border-color), var(--v-border-opacity)) !important;
	}

	.non-default {
		background-color: #fff3cd;
	}
</style>
