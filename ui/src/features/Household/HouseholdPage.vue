<template>
	<div>
		<div class="page-header">
			<h1>Household</h1>
		</div>

		<v-alert v-if="error" type="error" class="mb-3" closable @click:close="error = ''">{{ error }}</v-alert>

		<v-row>
			<v-col cols="12" lg="6">
				<v-card>
					<v-card-title class="section-title d-flex align-center">
						People
						<v-spacer />
						<v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="editPerson(newPerson())">Add person</v-btn>
					</v-card-title>
					<v-table>
						<thead>
							<tr>
								<th>Name</th>
								<th>Birth date</th>
								<th>Catch-up in {{ year }}</th>
								<th></th>
							</tr>
						</thead>
						<tbody>
							<tr v-for="person in store.people" :key="person.id">
								<td>{{ person.displayName }}</td>
								<td>{{ longDate(person.birthDate) }}</td>
								<td>
									<v-chip v-for="label in catchUps(person)" :key="label" size="small" class="mr-1" color="success" variant="tonal">{{ label }}</v-chip>
								</td>
								<td class="num">
									<v-btn icon="mdi-pencil" size="small" variant="text" title="Edit" @click="editPerson(person)" />
									<v-btn icon="mdi-delete-outline" size="small" variant="text" title="Delete" @click="removePerson(person)" />
								</td>
							</tr>
							<tr v-if="store.people.length === 0">
								<td colspan="4" class="empty-row">Add each person who earns a paycheck.</td>
							</tr>
						</tbody>
					</v-table>
				</v-card>
			</v-col>

			<v-col cols="12" lg="6">
				<v-card v-if="household">
					<v-card-title class="section-title">Household settings</v-card-title>
					<v-card-text>
						<v-row dense>
							<v-col cols="12">
								<v-select
									v-model="household.homeLocaleId"
									:items="store.locales"
									item-title="name"
									item-value="id"
									label="Home"
									hint="Where you live. Its school district income tax is withheld from every paycheck."
									persistent-hint
									clearable
									:error-messages="fieldErrors.homeLocaleId"
								/>
							</v-col>
							<v-col cols="12" sm="6">
								<v-select v-model="household.hsaCoverage" :items="hsaCoverages" label="HSA coverage" hint="Sets the HSA contribution limit" persistent-hint />
							</v-col>
							<v-col cols="12" sm="6">
								<v-select v-model="household.taxFilingStatus" :items="filingStatuses" label="Tax return filing status" />
							</v-col>
						</v-row>
					</v-card-text>
					<v-card-actions>
						<span v-if="saved" class="hint ml-2 mb-0">Saved</span>
						<v-spacer />
						<v-btn color="primary" variant="flat" :disabled="!householdDirty" :loading="saving" @click="saveHousehold">Save settings</v-btn>
					</v-card-actions>
				</v-card>
			</v-col>
		</v-row>

		<PersonDialog v-model="personDialogOpen" :person="editingPerson" @saved="store.refreshPeople()" />
	</div>
</template>

<script setup lang="ts">
	import { computed, onMounted, ref, watch } from 'vue'
	import { toApiError } from '../../lib/apiErrors'
	import { longDate, parseDate } from '../../lib/format'
	import { filingStatuses, hsaCoverages } from '../../lib/types-domain'
	import { useReferenceDataStore } from '../../stores/referenceData'
	import { newPerson, peopleApi, type Person } from '../People/api-people'
	import PersonDialog from '../People/PersonDialog.vue'
	import { householdApi, type Household } from './api-household'

	const store = useReferenceDataStore()
	const household = ref<Household | null>(null)
	const savedHousehold = ref('')
	const fieldErrors = ref<Record<string, string[]>>({})
	const error = ref('')
	const saving = ref(false)
	const saved = ref(false)
	const personDialogOpen = ref(false)
	const editingPerson = ref<Person | null>(null)
	const year = ref(new Date().getFullYear())

	const householdDirty = computed(() => household.value != null && JSON.stringify(household.value) !== savedHousehold.value)

	watch(household, () => (saved.value = false), { deep: true })

	onMounted(async () => {
		await store.loadAll()
		year.value = store.defaultYear()
		setHousehold(await householdApi.get())
	})

	function setHousehold(value: Household) {
		household.value = value
		savedHousehold.value = JSON.stringify(value)
	}

	/** Catch-up eligibility depends on the age reached by December 31. */
	function catchUps(person: Person): string[] {
		if (!person.birthDate) return []
		const age = year.value - parseDate(person.birthDate).getFullYear()
		const labels: string[] = []
		if (age >= 60 && age <= 63) labels.push('401(k) 60–63')
		else if (age >= 50) labels.push('401(k) 50+')
		if (age >= 55) labels.push('HSA 55+')
		return labels
	}

	function editPerson(person: Person) {
		editingPerson.value = person
		personDialogOpen.value = true
	}

	async function removePerson(person: Person) {
		if (!window.confirm(`Delete ${person.displayName}?`)) return
		try {
			await peopleApi.remove(person.id)
			await store.refreshPeople()
		} catch (e) {
			error.value = (await toApiError(e)).message
		}
	}

	async function saveHousehold() {
		if (!household.value) return
		saving.value = true
		try {
			setHousehold(await householdApi.update(household.value))
			fieldErrors.value = {}
			saved.value = true
		} catch (e) {
			const apiError = await toApiError(e)
			fieldErrors.value = apiError.fieldErrors
			if (!Object.keys(apiError.fieldErrors).length) error.value = apiError.message
		} finally {
			saving.value = false
		}
	}
</script>
