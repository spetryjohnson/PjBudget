<template>
	<div>
		<div class="page-header">
			<h1>Locales</h1>
			<v-btn color="primary" prepend-icon="mdi-plus" @click="edit(newLocale())">Add locale</v-btn>
		</div>
		<p class="hint">
			Places that tax income. A payroll source's <strong>work</strong> locale sets its city tax, and the household's
			<strong>home</strong> locale sets its school district tax. Only Ohio's taxes are supported right now.
		</p>

		<v-alert v-if="error" type="error" class="mb-3" closable @click:close="error = ''">{{ error }}</v-alert>

		<v-card>
			<v-table>
				<thead>
					<tr>
						<th>Name</th>
						<th>State</th>
						<th class="num">City rate</th>
						<th>School district</th>
						<th class="num">School rate</th>
						<th>School tax base</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					<tr v-for="locale in store.locales" :key="locale.id">
						<td>{{ locale.name }}</td>
						<td>{{ locale.stateCode }}</td>
						<td class="num">{{ rate(locale.municipalTaxRate) }}</td>
						<td>{{ locale.schoolDistrictName }}</td>
						<td class="num">{{ rate(locale.schoolDistrictTaxRate) }}</td>
						<td>{{ baseTitle(locale) }}</td>
						<td class="num">
							<v-btn icon="mdi-pencil" size="small" variant="text" title="Edit" @click="edit(locale)" />
							<v-btn icon="mdi-delete-outline" size="small" variant="text" title="Delete" @click="remove(locale)" />
						</td>
					</tr>
					<tr v-if="store.locales.length === 0">
						<td colspan="7" class="empty-row">No locales yet. Add the city you work in and the place you live.</td>
					</tr>
				</tbody>
			</v-table>
		</v-card>

		<LocaleDialog v-model="dialogOpen" :locale="editing" @saved="store.refreshLocales()" />
	</div>
</template>

<script setup lang="ts">
	import { onMounted, ref } from 'vue'
	import { toApiError } from '../../lib/apiErrors'
	import { rate } from '../../lib/format'
	import { schoolDistrictTaxBases } from '../../lib/types-domain'
	import { useReferenceDataStore } from '../../stores/referenceData'
	import { localesApi, newLocale, type Locale } from './api-locales'
	import LocaleDialog from './LocaleDialog.vue'

	const store = useReferenceDataStore()
	const dialogOpen = ref(false)
	const editing = ref<Locale | null>(null)
	const error = ref('')

	onMounted(() => store.refreshLocales())

	function edit(locale: Locale) {
		editing.value = locale
		dialogOpen.value = true
	}

	function baseTitle(locale: Locale) {
		return schoolDistrictTaxBases.find(b => b.value === locale.schoolDistrictTaxBase)?.title ?? ''
	}

	async function remove(locale: Locale) {
		if (!window.confirm(`Delete ${locale.name}?`)) return
		try {
			await localesApi.remove(locale.id)
			await store.refreshLocales()
		} catch (e) {
			error.value = (await toApiError(e)).message
		}
	}
</script>
