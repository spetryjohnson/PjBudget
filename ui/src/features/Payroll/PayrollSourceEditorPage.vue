<template>
	<div v-if="source && store.payroll">
		<div class="page-header">
			<div>
				<router-link to="/payroll" class="back-link"><v-icon icon="mdi-arrow-left" size="small" /> Payroll</router-link>
				<h1>{{ isNew ? 'New payroll source' : source.name || 'Payroll source' }}</h1>
			</div>
			<div class="page-actions">
				<v-chip v-if="dirty" size="small" color="warning" variant="tonal">Unsaved changes</v-chip>
				<v-btn v-if="!isNew" variant="text" color="error" prepend-icon="mdi-delete-outline" @click="remove">Delete</v-btn>
				<v-btn color="primary" variant="flat" prepend-icon="mdi-content-save" :loading="saving" :disabled="!dirty && !isNew" @click="save">
					Save
				</v-btn>
			</div>
		</div>

		<v-alert v-if="saveError" type="error" class="mb-3" closable @click:close="saveError = ''">{{ saveError }}</v-alert>

		<v-row>
			<v-col cols="12" lg="5" class="form-column">
				<GeneralSection v-model="source" :errors="fieldErrors" :people="store.people" :locales="store.locales" />
				<PaySection v-model="source" :errors="fieldErrors" />
				<BenefitsSection v-model="source" :errors="fieldErrors" :summary="simulation?.summary ?? null" />
				<DeductionsSection v-model="source" :errors="fieldErrors" :default-treatment="store.payroll.defaultDeductionTreatment" />
				<TaxTreatmentSection v-model="source" :reference="store.payroll" />
				<WithholdingSection v-model="source" :errors="fieldErrors" />
				<CalibrationSection v-model="source" :errors="fieldErrors" :summary="simulation?.summary ?? null" />
			</v-col>
			<v-col cols="12" lg="7">
				<div class="preview-column">
					<PayrollPreviewPanel
						v-model:year="year"
						:simulation="simulation"
						:loading="previewing"
						:problems="previewProblems"
						:years="yearChoices"
					/>
				</div>
			</v-col>
		</v-row>
	</div>
	<v-alert v-else-if="loadError" type="error">{{ loadError }}</v-alert>
	<v-progress-linear v-else indeterminate color="primary" />
</template>

<script setup lang="ts">
	import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
	import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router'
	import { allMessages, isAbort, toApiError } from '../../lib/apiErrors'
	import { clone } from '../../lib/clone'
	import { useReferenceDataStore } from '../../stores/referenceData'
	import { payrollApi } from './api-payroll'
	import PayrollPreviewPanel from './PayrollPreviewPanel.vue'
	import BenefitsSection from './sections/BenefitsSection.vue'
	import CalibrationSection from './sections/CalibrationSection.vue'
	import DeductionsSection from './sections/DeductionsSection.vue'
	import GeneralSection from './sections/GeneralSection.vue'
	import PaySection from './sections/PaySection.vue'
	import TaxTreatmentSection from './sections/TaxTreatmentSection.vue'
	import WithholdingSection from './sections/WithholdingSection.vue'
	import type { PayrollSimulation, PayrollSource } from './types-payroll'

	const route = useRoute()
	const router = useRouter()
	const store = useReferenceDataStore()

	const source = ref<PayrollSource | null>(null)
	const savedSnapshot = ref('')
	const year = ref(new Date().getFullYear())
	const simulation = ref<PayrollSimulation | null>(null)
	const previewing = ref(false)
	const previewProblems = ref<string[]>([])
	const fieldErrors = ref<Record<string, string[]>>({})
	const saving = ref(false)
	const saveError = ref('')
	const loadError = ref('')

	const isNew = computed(() => route.params.id === 'new')
	const dirty = computed(() => source.value != null && JSON.stringify(source.value) !== savedSnapshot.value)
	const yearChoices = computed(() => (store.years.length ? store.years : [year.value]))

	onMounted(async () => {
		try {
			await store.loadAll()
			year.value = store.defaultYear()
			setSaved(isNew.value ? newSource() : await payrollApi.get(Number(route.params.id)))
		} catch (e) {
			loadError.value = (await toApiError(e)).message
		}
	})

	function newSource(): PayrollSource {
		const template = clone(store.payroll!.newSourceTemplate)
		// With a single choice there's nothing to decide, so pick it.
		const [onlyPerson, ...otherPeople] = store.people
		const [onlyLocale, ...otherLocales] = store.locales
		if (onlyPerson && otherPeople.length === 0) template.personId = onlyPerson.id
		if (onlyLocale && otherLocales.length === 0) template.workLocaleId = onlyLocale.id
		return template
	}

	function setSaved(value: PayrollSource) {
		source.value = value
		savedSnapshot.value = JSON.stringify(value)
	}

	// ---- Live preview: re-simulate shortly after edits stop, cancelling any preview still in flight. ----

	let previewTimer: number | undefined
	let previewAbort: AbortController | null = null

	watch([source, year], () => {
		window.clearTimeout(previewTimer)
		previewTimer = window.setTimeout(runPreview, 350)
	}, { deep: true })

	onBeforeUnmount(() => {
		window.clearTimeout(previewTimer)
		previewAbort?.abort()
	})

	async function runPreview() {
		if (!source.value) return

		previewAbort?.abort()
		const abort = new AbortController()
		previewAbort = abort
		previewing.value = true

		try {
			simulation.value = await payrollApi.preview(year.value, source.value, abort.signal)
			previewProblems.value = []
			fieldErrors.value = {}
		} catch (e) {
			if (isAbort(e)) return
			const error = await toApiError(e)
			previewProblems.value = allMessages(error)
			fieldErrors.value = error.fieldErrors
		} finally {
			if (previewAbort === abort) previewing.value = false
		}
	}

	// ---- Save / delete ----

	async function save() {
		if (!source.value) return
		saving.value = true
		saveError.value = ''
		try {
			const saved = isNew.value ? await payrollApi.create(source.value) : await payrollApi.update(source.value)
			setSaved(saved)
			if (isNew.value) await router.replace(`/payroll/${saved.id}`)
		} catch (e) {
			const error = await toApiError(e)
			fieldErrors.value = error.fieldErrors
			saveError.value = error.status === 400 ? 'Fix the highlighted fields and save again.' : error.message
		} finally {
			saving.value = false
		}
	}

	async function remove() {
		if (!source.value || !window.confirm(`Delete ${source.value.name}? This can't be undone.`)) return
		try {
			await payrollApi.remove(source.value.id)
			savedSnapshot.value = JSON.stringify(source.value)
			await router.push('/payroll')
		} catch (e) {
			saveError.value = (await toApiError(e)).message
		}
	}

	onBeforeRouteLeave(() => !dirty.value || window.confirm('You have unsaved changes. Leave without saving?'))
</script>

<style scoped>
	.back-link {
		font-size: 0.8125rem;
		text-decoration: none;
		color: #1976d2;
	}

	.form-column > * + * {
		margin-top: 12px;
	}

	/* Keep the paychecks visible while scrolling through the form. */
	.preview-column {
		position: sticky;
		top: 12px;
	}
</style>
