<template>
	<div>
		<v-table class="schedule">
			<thead>
				<tr>
					<th>{{ overLabel }}</th>
					<th v-if="baseLabel">{{ baseLabel }}</th>
					<th v-if="rateLabel">{{ rateLabel }}</th>
					<th v-if="deriveBase" class="num" title="Tax owed on income up to the start of this bracket">Tax below</th>
					<th style="width: 40px"></th>
				</tr>
			</thead>
			<tbody>
				<tr v-for="(row, i) in rows" :key="i">
					<td><NumberField v-model="row.over" prefix="$" hide-details /></td>
					<td v-if="baseLabel"><NumberField v-model="row.baseAmount" prefix="$" hide-details /></td>
					<td v-if="rateLabel"><NumberField v-model="row.rate" suffix="%" :scale="100" hide-details /></td>
					<td v-if="deriveBase" class="num">{{ money(derivedBases[i]) }}</td>
					<td>
						<v-btn icon="mdi-close" size="x-small" variant="text" title="Remove row" :disabled="rows.length === 1" @click="rows.splice(i, 1)" />
					</td>
				</tr>
			</tbody>
		</v-table>
		<div class="d-flex align-center">
			<v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="addRow">Add row</v-btn>
			<span v-for="message in errors ?? []" :key="message" class="error-text">{{ message }}</span>
		</div>
	</div>
</template>

<script setup lang="ts">
	import { computed } from 'vue'
	import NumberField from '../../components/NumberField.vue'
	import { money } from '../../lib/format'

	interface Row {
		over: number
		baseAmount: number
		rate: number
	}

	defineProps<{
		overLabel: string
		baseLabel?: string
		rateLabel?: string
		/** Show the cumulative tax below each bracket, for statutory brackets that only store marginal rates. */
		deriveBase?: boolean
		errors?: string[]
	}>()
	const rows = defineModel<Row[]>({ required: true })

	const derivedBases = computed(() => {
		const bases: number[] = []
		rows.value.forEach((row, i) => {
			const previous = rows.value[i - 1]
			bases.push(i === 0 || !previous ? 0 : (bases[i - 1] ?? 0) + (row.over - previous.over) * previous.rate)
		})
		return bases
	})

	function addRow() {
		const last = rows.value[rows.value.length - 1]
		rows.value.push({ over: last ? last.over : 0, baseAmount: 0, rate: last ? last.rate : 0 })
	}
</script>

<style scoped>
	.schedule :deep(td) {
		padding: 2px 4px !important;
	}

	.error-text {
		color: #dc3545;
		font-size: 0.8125rem;
		margin-left: 0.5rem;
	}
</style>
