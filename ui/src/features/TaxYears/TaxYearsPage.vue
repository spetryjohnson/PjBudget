<template>
	<div>
		<div class="page-header">
			<h1>Tax years</h1>
			<div class="page-actions">
				<v-btn-toggle :model-value="selectedYear" mandatory density="compact" color="primary" variant="outlined" divided @update:model-value="selectYear">
					<v-btn v-for="y in store.years" :key="y" :value="y">{{ y }}</v-btn>
				</v-btn-toggle>
				<v-btn variant="text" prepend-icon="mdi-content-copy" :disabled="!draft || dirty" @click="copyYear">Copy to {{ nextYear }}</v-btn>
				<v-btn variant="text" color="error" prepend-icon="mdi-delete-outline" :disabled="!draft || dirty" @click="deleteYear">Delete</v-btn>
				<v-chip v-if="dirty" size="small" color="warning" variant="tonal">Unsaved changes</v-chip>
				<v-btn color="primary" variant="flat" prepend-icon="mdi-content-save" :disabled="!dirty" :loading="saving" @click="save">Save</v-btn>
			</div>
		</div>
		<p class="hint">
			Tax rules and contribution limits for each year, from IRS and Ohio publications. To start a new year, copy the
			previous one and update what changed. Rates are percentages.
		</p>

		<v-alert v-if="message" :type="messageType" class="mb-3" closable @click:close="message = ''">
			{{ message }}
			<ul v-if="messages.length" class="message-list">
				<li v-for="m in messages" :key="m">{{ m }}</li>
			</ul>
		</v-alert>

		<template v-if="draft">
			<v-row>
				<v-col cols="12" lg="7">
					<v-card>
						<v-card-title class="section-title">Federal income tax</v-card-title>
						<v-card-text>
							<v-table class="compact-table mb-4">
								<thead>
									<tr>
										<th>Filing status</th>
										<th>Standard deduction</th>
										<th title="Pub 15-T Worksheet 1A line 1g">W-4 line 1g amount</th>
										<th>Add'l Medicare owed above</th>
									</tr>
								</thead>
								<tbody>
									<tr v-for="status in draft.filingStatuses" :key="status.filingStatus">
										<td>{{ statusTitle(status.filingStatus) }}</td>
										<td><NumberField v-model="status.standardDeduction" prefix="$" hide-details /></td>
										<td><NumberField v-model="status.standardWithholdingAdjustment" prefix="$" hide-details /></td>
										<td><NumberField v-model="status.additionalMedicareLiabilityThreshold" prefix="$" hide-details /></td>
									</tr>
								</tbody>
							</v-table>

							<v-tabs v-model="bracketTab" density="compact" color="primary">
								<v-tab v-for="status in filingStatuses" :key="status.value" :value="status.value">{{ status.title }}</v-tab>
							</v-tabs>
							<template v-for="status in filingStatuses" :key="status.value">
								<ScheduleTable
									v-if="bracketTab === status.value"
									v-model="schedule('FEDERAL_INCOME', status.value).rows"
									over-label="Taxable income over"
									rate-label="Rate"
									derive-base
									:errors="scheduleErrors('FEDERAL_INCOME', status.value)"
								/>
							</template>
						</v-card-text>
					</v-card>

					<v-card class="mt-3">
						<v-card-title class="section-title">Ohio</v-card-title>
						<v-card-text>
							<v-row dense>
								<v-col cols="6" sm="4">
									<NumberField v-model="draft.ohio.withholdingExemptionAmount" label="Withholding per IT 4 exemption" prefix="$" />
								</v-col>
								<v-col cols="6" sm="4">
									<NumberField v-model="draft.ohio.exemptionMagiLimit" label="No exemptions at MAGI of" prefix="$" />
								</v-col>
								<v-col cols="6" sm="4">
									<NumberField v-model="draft.ohio.jointFilingCreditCap" label="Joint filing credit cap" prefix="$" />
								</v-col>
								<v-col cols="6" sm="4">
									<NumberField v-model="draft.ohio.jointFilingCreditMagiLimit" label="No joint filing credit at MAGI of" prefix="$" />
								</v-col>
								<v-col cols="6" sm="4">
									<NumberField v-model="draft.ohio.jointFilingCreditMinSpouseIncome" label="Min income per spouse" prefix="$" />
								</v-col>
							</v-row>

							<v-tabs v-model="ohioTab" density="compact" color="primary" class="mt-3">
								<v-tab value="OHIO_WITHHOLDING">Withholding formula</v-tab>
								<v-tab value="OHIO_INCOME">Income tax</v-tab>
								<v-tab value="OHIO_EXEMPTION">Exemptions</v-tab>
								<v-tab value="OHIO_JOINT_FILING_CREDIT">Joint filing credit</v-tab>
							</v-tabs>
							<ScheduleTable
								v-if="ohioTab === 'OHIO_WITHHOLDING'"
								v-model="schedule('OHIO_WITHHOLDING').rows"
								over-label="Annual wages over"
								base-label="Base amount"
								rate-label="Rate"
								:errors="scheduleErrors('OHIO_WITHHOLDING')"
							/>
							<ScheduleTable
								v-if="ohioTab === 'OHIO_INCOME'"
								v-model="schedule('OHIO_INCOME').rows"
								over-label="Taxable income over"
								base-label="Base amount"
								rate-label="Rate"
								:errors="scheduleErrors('OHIO_INCOME')"
							/>
							<ScheduleTable
								v-if="ohioTab === 'OHIO_EXEMPTION'"
								v-model="schedule('OHIO_EXEMPTION').rows"
								over-label="Modified AGI over"
								base-label="Amount per exemption"
								:errors="scheduleErrors('OHIO_EXEMPTION')"
							/>
							<ScheduleTable
								v-if="ohioTab === 'OHIO_JOINT_FILING_CREDIT'"
								v-model="schedule('OHIO_JOINT_FILING_CREDIT').rows"
								over-label="Income less exemptions over"
								rate-label="Credit"
								:errors="scheduleErrors('OHIO_JOINT_FILING_CREDIT')"
							/>
						</v-card-text>
					</v-card>
				</v-col>

				<v-col cols="12" lg="5">
					<v-card>
						<v-card-title class="section-title">Social Security &amp; Medicare</v-card-title>
						<v-card-text>
							<v-row dense>
								<v-col cols="6"><NumberField v-model="draft.fica.socialSecurityRate" label="Social Security rate" suffix="%" :scale="100" :error-messages="errors['fica.socialSecurityRate']" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.fica.socialSecurityWageBase" label="Wage base" prefix="$" :error-messages="errors['fica.socialSecurityWageBase']" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.fica.medicareRate" label="Medicare rate" suffix="%" :scale="100" :error-messages="errors['fica.medicareRate']" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.fica.additionalMedicareRate" label="Additional Medicare rate" suffix="%" :scale="100" :error-messages="errors['fica.additionalMedicareRate']" /></v-col>
								<v-col cols="12"><NumberField v-model="draft.fica.additionalMedicareWithholdingThreshold" label="Employers withhold Additional Medicare above" prefix="$" :error-messages="errors['fica.additionalMedicareWithholdingThreshold']" /></v-col>
							</v-row>
						</v-card-text>
					</v-card>

					<v-card class="mt-3">
						<v-card-title class="section-title">Contribution limits</v-card-title>
						<v-card-text>
							<v-row dense>
								<v-col cols="6"><NumberField v-model="draft.limits.electiveDeferral" label="401(k) deferral" prefix="$" :error-messages="errors['limits.electiveDeferral']" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.limits.healthFsa" label="Health FSA" prefix="$" :error-messages="errors['limits.healthFsa']" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.limits.catchUpAge50" label="401(k) catch-up, 50+" prefix="$" /></v-col>
								<v-col cols="6"><NumberField v-model="draft.limits.catchUpAge60To63" label="401(k) catch-up, 60–63" prefix="$" /></v-col>
								<v-col cols="4"><NumberField v-model="draft.limits.hsaSelfOnly" label="HSA self-only" prefix="$" /></v-col>
								<v-col cols="4"><NumberField v-model="draft.limits.hsaFamily" label="HSA family" prefix="$" /></v-col>
								<v-col cols="4"><NumberField v-model="draft.limits.hsaCatchUpAge55" label="HSA catch-up, 55+" prefix="$" /></v-col>
							</v-row>
						</v-card-text>
					</v-card>

					<v-card class="mt-3">
						<v-card-title class="section-title">Derived withholding tables</v-card-title>
						<v-card-text>
							<p class="hint">
								Built from the saved brackets. These should match the "Annual Percentage Method" tables in IRS
								Publication 15-T for {{ selectedYear }}; if they don't, a bracket or deduction above is off.
							</p>
							<v-expansion-panels variant="accordion">
								<v-expansion-panel v-for="tables in withholdingTables" :key="tables.filingStatus" :title="statusTitle(tables.filingStatus)">
									<v-expansion-panel-text>
										<v-row dense>
											<v-col v-for="kind in (['standard', 'step2Checkbox'] as const)" :key="kind" cols="12" sm="6">
												<div class="table-title">{{ kind === 'standard' ? 'Standard' : 'Step 2 checkbox' }}</div>
												<v-table class="compact-table">
													<thead><tr><th class="num">At least</th><th class="num">Tentative</th><th class="num">Rate</th></tr></thead>
													<tbody>
														<tr v-for="row in tables[kind]" :key="row.atLeast">
															<td class="num">{{ wholeMoney(row.atLeast) }}</td>
															<td class="num">{{ money(row.tentativeAmount) }}</td>
															<td class="num">{{ rate(row.rate) }}</td>
														</tr>
													</tbody>
												</v-table>
											</v-col>
										</v-row>
									</v-expansion-panel-text>
								</v-expansion-panel>
							</v-expansion-panels>
						</v-card-text>
					</v-card>
				</v-col>
			</v-row>
		</template>
	</div>
</template>

<script setup lang="ts">
	import { computed, onMounted, ref } from 'vue'
	import { onBeforeRouteLeave } from 'vue-router'
	import NumberField from '../../components/NumberField.vue'
	import { allMessages, toApiError } from '../../lib/apiErrors'
	import { money, rate, wholeMoney } from '../../lib/format'
	import { filingStatuses, type FilingStatus } from '../../lib/types-domain'
	import { useReferenceDataStore } from '../../stores/referenceData'
	import { taxYearsApi, type TaxScheduleKind, type TaxYear, type WithholdingTables } from './api-tax-years'
	import ScheduleTable from './ScheduleTable.vue'

	const store = useReferenceDataStore()
	const selectedYear = ref<number | null>(null)
	const draft = ref<TaxYear | null>(null)
	const savedSnapshot = ref('')
	const withholdingTables = ref<WithholdingTables[]>([])
	const errors = ref<Record<string, string[]>>({})
	const message = ref('')
	const messages = ref<string[]>([])
	const messageType = ref<'success' | 'error'>('success')
	const saving = ref(false)
	const bracketTab = ref<FilingStatus>('MFJ')
	const ohioTab = ref<TaxScheduleKind>('OHIO_WITHHOLDING')

	const dirty = computed(() => draft.value != null && JSON.stringify(draft.value) !== savedSnapshot.value)
	const nextYear = computed(() => Math.max(...store.years, selectedYear.value ?? 0) + 1)

	onMounted(async () => {
		await store.refreshTaxYears()
		const currentYear = new Date().getFullYear()
		await showYear(store.years.includes(currentYear) ? currentYear : lastYear())
	})

	/** Switching years from the toggle. If the user keeps their edits, the toggle stays on the year being edited. */
	async function selectYear(year: number | null) {
		if (year == null || year === selectedYear.value) return
		if (dirty.value && !window.confirm(`Discard unsaved changes to ${selectedYear.value}?`)) return
		await showYear(year)
	}

	async function showYear(year: number | null) {
		selectedYear.value = year
		if (year == null) {
			draft.value = null
			return
		}

		const taxYear = await taxYearsApi.get(year)
		draft.value = taxYear
		savedSnapshot.value = JSON.stringify(taxYear)
		errors.value = {}
		withholdingTables.value = await taxYearsApi.withholdingTables(year)
	}

	function lastYear(): number | null {
		return store.years.length ? store.years[store.years.length - 1]! : null
	}

	function schedule(kind: TaxScheduleKind, filingStatus: FilingStatus | null = null) {
		return draft.value!.schedules.find(s => s.kind === kind && s.filingStatus === filingStatus)!
	}

	function scheduleErrors(kind: TaxScheduleKind, filingStatus: FilingStatus | null = null) {
		const index = draft.value!.schedules.indexOf(schedule(kind, filingStatus))
		return Object.entries(errors.value)
			.filter(([key]) => key.startsWith(`schedules[${index}]`))
			.flatMap(([, value]) => value)
	}

	function statusTitle(status: FilingStatus) {
		return filingStatuses.find(s => s.value === status)?.title ?? status
	}

	function show(type: 'success' | 'error', text: string, details: string[] = []) {
		messageType.value = type
		message.value = text
		messages.value = details
	}

	async function save() {
		if (!draft.value) return
		saving.value = true
		try {
			draft.value = await taxYearsApi.update(draft.value)
			savedSnapshot.value = JSON.stringify(draft.value)
			errors.value = {}
			withholdingTables.value = await taxYearsApi.withholdingTables(draft.value.year)
			show('success', `Saved ${draft.value.year}.`)
		} catch (e) {
			const error = await toApiError(e)
			errors.value = error.fieldErrors
			show('error', 'Fix these and save again:', allMessages(error))
		} finally {
			saving.value = false
		}
	}

	async function copyYear() {
		if (!draft.value || !window.confirm(`Create ${nextYear.value} as a copy of ${draft.value.year}?`)) return
		try {
			const copy = await taxYearsApi.copy(draft.value.year, nextYear.value)
			await store.refreshTaxYears()
			await showYear(copy.year)
			show('success', `Created ${copy.year}. Update the values that changed and save.`)
		} catch (e) {
			show('error', (await toApiError(e)).message)
		}
	}

	async function deleteYear() {
		if (!draft.value || !window.confirm(`Delete the ${draft.value.year} tax rules? Paychecks for that year can't be simulated without them.`)) return
		try {
			await taxYearsApi.remove(draft.value.year)
			await store.refreshTaxYears()
			await showYear(lastYear())
		} catch (e) {
			show('error', (await toApiError(e)).message)
		}
	}

	onBeforeRouteLeave(() => !dirty.value || window.confirm('You have unsaved changes. Leave without saving?'))
</script>

<style scoped>
	.compact-table {
		font-size: 0.8125rem;
	}

	.compact-table :deep(td),
	.compact-table :deep(th) {
		padding: 2px 6px !important;
	}

	.table-title {
		font-weight: 600;
		font-size: 0.8125rem;
		margin-bottom: 0.25rem;
	}

	.message-list {
		margin: 0.25rem 0 0 1.25rem;
	}
</style>
