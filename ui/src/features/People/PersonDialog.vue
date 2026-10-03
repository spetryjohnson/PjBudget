<template>
	<v-dialog v-model="open" max-width="440" persistent>
		<v-card v-if="draft" variant="elevated">
			<v-card-title>{{ draft.id ? 'Edit person' : 'Add person' }}</v-card-title>
			<v-card-text>
				<v-text-field v-model="draft.displayName" label="Name" class="mb-3" :error-messages="errors.displayName" autofocus />
				<DateField
					v-model="draft.birthDate"
					label="Birth date"
					hint="Optional. Used to apply 401(k) catch-up (50+) and HSA catch-up (55+) contributions."
					persistent-hint
					:error-messages="errors.birthDate"
				/>
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
	import DateField from '../../components/DateField.vue'
	import { toApiError } from '../../lib/apiErrors'
	import { clone } from '../../lib/clone'
	import { peopleApi, type Person } from './api-people'

	const props = defineProps<{ person: Person | null }>()
	const emit = defineEmits<{ saved: [person: Person] }>()
	const open = defineModel<boolean>({ required: true })

	const draft = ref<Person | null>(null)
	const errors = ref<Record<string, string[]>>({})
	const generalError = ref('')
	const saving = ref(false)

	watch(open, isOpen => {
		if (isOpen && props.person) {
			draft.value = clone(props.person)
			errors.value = {}
			generalError.value = ''
		}
	})

	async function save() {
		if (!draft.value) return
		saving.value = true
		try {
			const saved = draft.value.id ? await peopleApi.update(draft.value) : await peopleApi.create(draft.value)
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
