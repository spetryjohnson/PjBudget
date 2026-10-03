<template>
	<v-card>
		<v-card-title class="section-title">Tax treatment</v-card-title>
		<v-card-text>
			<p class="hint">
				A check means the deduction comes out <strong>before</strong> that tax. Highlighted boxes differ from the usual
				treatment; change them only if your paystub shows otherwise.
			</p>
			<v-table class="tax-grid">
				<thead>
					<tr>
						<th>Deduction</th>
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
							:class="{ 'non-default': row.value.includes(w.value) !== row.defaults.includes(w.value) }"
						>
							<v-checkbox-btn
								:model-value="row.value.includes(w.value)"
								density="compact"
								class="d-inline-flex"
								:aria-label="`${row.label}: pre-tax for ${w.title}`"
								@update:model-value="toggle(row, w.value, !!$event)"
							/>
						</td>
						<td class="num">
							<v-btn v-if="!sameSet(row.value, row.defaults)" size="x-small" variant="text" @click="row.set([...row.defaults])">
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

	interface Row {
		key: string
		label: string
		value: WageType[]
		defaults: WageType[]
		set: (value: WageType[]) => void
	}

	const props = defineProps<{ reference: PayrollReference }>()
	const source = defineModel<PayrollSource>({ required: true })

	const rows = computed<Row[]>(() => {
		const s = source.value
		const r = props.reference
		return [
			{ key: '401k', label: 'Traditional 401(k)', value: s.traditional401kPreTaxFor, defaults: r.defaultTraditional401kTreatment, set: v => (s.traditional401kPreTaxFor = v) },
			{ key: 'hsa', label: 'HSA', value: s.hsaPreTaxFor, defaults: r.defaultCafeteriaPlanTreatment, set: v => (s.hsaPreTaxFor = v) },
			{ key: 'fsa', label: 'Health FSA', value: s.healthFsaPreTaxFor, defaults: r.defaultCafeteriaPlanTreatment, set: v => (s.healthFsaPreTaxFor = v) },
			...s.deductions.map((d, i) => ({
				key: `deduction-${i}`,
				label: d.label || 'Deduction',
				value: d.preTaxFor,
				defaults: r.defaultDeductionTreatment[d.type],
				set: (v: WageType[]) => (d.preTaxFor = v),
			})),
		]
	})

	function toggle(row: Row, wageType: WageType, checked: boolean) {
		const next = new Set(row.value)
		if (checked) next.add(wageType)
		else next.delete(wageType)
		row.set(wageTypes.map(w => w.value).filter(w => next.has(w)))
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

	.non-default {
		background-color: #fff3cd;
	}
</style>
