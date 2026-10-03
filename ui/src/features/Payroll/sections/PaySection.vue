<template>
	<v-card>
		<v-card-title class="section-title">Pay</v-card-title>
		<v-card-text>
			<v-row dense align="center">
				<v-col cols="12" sm="6">
					<v-btn-toggle v-model="source.payBasis" mandatory density="compact" color="primary" variant="outlined" divided>
						<v-btn value="SALARY">Salary</v-btn>
						<v-btn value="HOURLY">Hourly</v-btn>
					</v-btn-toggle>
				</v-col>
				<v-col cols="12" sm="6">
					<v-btn-toggle v-model="source.payFrequency" mandatory density="compact" color="primary" variant="outlined" divided>
						<v-btn value="SEMIMONTHLY">Twice a month</v-btn>
						<v-btn value="BIWEEKLY">Every 2 weeks</v-btn>
					</v-btn-toggle>
				</v-col>

				<template v-if="source.payBasis === 'SALARY'">
					<v-col cols="12" sm="6">
						<NumberField v-model="source.annualSalary" label="Annual salary" prefix="$" nullable :error-messages="errors.annualSalary" />
					</v-col>
				</template>
				<template v-else>
					<v-col cols="6" sm="3">
						<NumberField v-model="source.hourlyRate" label="Hourly rate" prefix="$" nullable :error-messages="errors.hourlyRate" />
					</v-col>
					<v-col cols="6" sm="3">
						<NumberField v-model="source.hoursPerCheck" label="Hours per check" nullable :error-messages="errors.hoursPerCheck" />
					</v-col>
				</template>

				<template v-if="source.payFrequency === 'SEMIMONTHLY'">
					<v-col cols="6" sm="3">
						<NumberField v-model="source.semimonthlyPayDay1" label="First payday" step="1" nullable :error-messages="errors.semimonthlyPayDay1" />
					</v-col>
					<v-col cols="6" sm="3">
						<NumberField
							v-model="source.semimonthlyPayDay2"
							label="Second payday"
							step="1"
							nullable
							hint="31 = last day of the month"
							:error-messages="errors.semimonthlyPayDay2"
						/>
					</v-col>
				</template>
				<v-col v-else cols="12" sm="6">
					<DateField
						v-model="source.biweeklyAnchorDate"
						label="Any payday"
						hint="e.g. the first check; the rest are every 14 days from it"
						persistent-hint
						:error-messages="errors.biweeklyAnchorDate"
					/>
				</v-col>
			</v-row>
			<p class="hint mt-2 mb-0">
				Paydays that fall on a weekend or bank holiday are paid the business day before.
			</p>
		</v-card-text>
	</v-card>
</template>

<script setup lang="ts">
	import DateField from '../../../components/DateField.vue'
	import NumberField from '../../../components/NumberField.vue'
	import type { PayrollSource } from '../types-payroll'

	defineProps<{ errors: Record<string, string[]> }>()
	const source = defineModel<PayrollSource>({ required: true })
</script>
