<template>
	<v-card>
		<v-card-title class="section-title">General</v-card-title>
		<v-card-text>
			<v-row dense>
				<v-col cols="12" sm="6">
					<v-text-field v-model="source.name" label="Name" :error-messages="errors.name" />
				</v-col>
				<v-col cols="12" sm="6">
					<v-text-field v-model="source.employerName" label="Employer" :error-messages="errors.employerName" />
				</v-col>
				<v-col cols="12" sm="6">
					<v-select
						v-model="source.personId"
						:items="people"
						item-title="displayName"
						item-value="id"
						label="Earned by"
						:error-messages="errors.personId"
					/>
				</v-col>
				<v-col cols="12" sm="6">
					<v-select
						v-model="source.workLocaleId"
						:items="locales"
						item-title="name"
						item-value="id"
						label="Work location"
						hint="Sets the city income tax withheld"
						persistent-hint
						:error-messages="errors.workLocaleId"
					/>
				</v-col>
			</v-row>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import type { Locale } from '../../Locales/api-locales'
	import type { Person } from '../../People/api-people'
	import type { PayrollSource } from '../types-payroll'

	defineProps<{ errors: Record<string, string[]>; people: Person[]; locales: Locale[] }>()
	const source = defineModel<PayrollSource>({ required: true })
</script>
