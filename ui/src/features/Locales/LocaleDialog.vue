<template>
	<v-dialog v-model="open" max-width="640" persistent>
		<v-card v-if="draft" variant="elevated">
			<v-card-title>{{ draft.id ? 'Edit locale' : 'Add locale' }}</v-card-title>
			<v-card-text>
				<v-row dense>
					<v-col cols="12">
						<v-text-field v-model="draft.name" label="Name" hint="How it appears in lists, e.g. 'Columbus, OH'" :error-messages="errors.name" autofocus />
					</v-col>
					<v-col cols="6">
						<v-text-field v-model="draft.city" label="City" :error-messages="errors.city" />
					</v-col>
					<v-col cols="3">
						<v-text-field v-model="draft.stateCode" label="State" maxlength="2" :error-messages="errors.stateCode" @update:model-value="draft.stateCode = $event.toUpperCase()" />
					</v-col>
					<v-col cols="3">
						<v-text-field v-model="draft.zipCode" label="ZIP" :error-messages="errors.zipCode" />
					</v-col>
					<v-col cols="6">
						<NumberField v-model="draft.municipalTaxRate" label="City income tax rate" suffix="%" :scale="100" :error-messages="errors.municipalTaxRate" />
					</v-col>
				</v-row>

				<div class="subhead">School district</div>
				<v-row dense>
					<v-col cols="8">
						<v-text-field v-model="draft.schoolDistrictName" label="District name" :error-messages="errors.schoolDistrictName" />
					</v-col>
					<v-col cols="4">
						<v-text-field v-model="draft.schoolDistrictNumber" label="District number" hint="4 digits, from Ohio's Finder" :error-messages="errors.schoolDistrictNumber" />
					</v-col>
					<v-col cols="4">
						<NumberField v-model="draft.schoolDistrictTaxRate" label="Income tax rate" suffix="%" :scale="100" nullable :error-messages="errors.schoolDistrictTaxRate" />
					</v-col>
					<v-col cols="8">
						<v-select v-model="draft.schoolDistrictTaxBase" :items="schoolDistrictTaxBases" label="Tax base" clearable :error-messages="errors.schoolDistrictTaxBase" />
					</v-col>
				</v-row>

				<v-alert v-if="generalError" type="error" class="mt-3">{{ generalError }}</v-alert>
			</v-card-text>
			<v-card-actions>
				<v-spacer />
				<v-btn variant="text" @click="open = false">Cancel</v-btn>
				<v-btn color="primary" variant="flat" :loading="saving" @click="save">Save</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
	import { ref, watch } from 'vue'
	import NumberField from '../../components/NumberField.vue'
	import { toApiError } from '../../lib/apiErrors'
	import { clone } from '../../lib/clone'
	import { schoolDistrictTaxBases } from '../../lib/types-domain'
	import { localesApi, type Locale } from './api-locales'

	const props = defineProps<{ locale: Locale | null }>()
	const emit = defineEmits<{ saved: [locale: Locale] }>()
	const open = defineModel<boolean>({ required: true })

	const draft = ref<Locale | null>(null)
	const errors = ref<Record<string, string[]>>({})
	const generalError = ref('')
	const saving = ref(false)

	watch(open, isOpen => {
		if (isOpen && props.locale) {
			draft.value = clone(props.locale)
			errors.value = {}
			generalError.value = ''
		}
	})

	async function save() {
		if (!draft.value) return
		saving.value = true
		try {
			const saved = draft.value.id ? await localesApi.update(draft.value) : await localesApi.create(draft.value)
			emit('saved', saved)
			open.value = false
		} catch (e) {
			const error = await toApiError(e)
			errors.value = error.fieldErrors
			generalError.value = Object.keys(error.fieldErrors).length ? '' : error.message
		} finally {
			saving.value = false
		}
	}
</script>

<style scoped>
	.subhead {
		margin: 1rem 0 0.5rem;
		font-weight: 600;
		font-size: 0.875rem;
	}
</style>
