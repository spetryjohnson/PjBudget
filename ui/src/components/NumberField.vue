<template>
	<v-text-field
		v-model="text"
		type="number"
		:step="step"
		:prefix="prefix"
		:suffix="suffix"
		inputmode="decimal"
		@blur="normalize"
	/>
</template>

<script setup lang="ts">
	/**
	 * A numeric text field bound to a number. VTextField works in strings, so this keeps the raw text locally
	 * (typing "2." mustn't be reformatted mid-keystroke) and emits numbers.
	 *
	 * - `scale` displays a stored value multiplied by a factor. Rates stored as fractions use scale 100, so
	 *   0.0275 shows as 2.75 with a "%" suffix.
	 * - `nullable` makes an empty field mean null (e.g. "no override") instead of 0.
	 */
	import { ref, watch } from 'vue'

	const props = withDefaults(defineProps<{
		prefix?: string
		suffix?: string
		step?: string | number
		scale?: number
		nullable?: boolean
	}>(), {
		step: 'any',
		scale: 1,
		nullable: false,
	})

	const model = defineModel<number | null | undefined>()

	const text = ref(toText(model.value))

	function toText(value: number | null | undefined): string {
		return value == null ? '' : String(Number((value * props.scale).toFixed(6)))
	}

	function fromText(value: string): number | null {
		if (value.trim() === '') return props.nullable ? null : 0
		const parsed = Number(value)
		return Number.isFinite(parsed) ? Number((parsed / props.scale).toFixed(8)) : null
	}

	watch(text, value => {
		const parsed = fromText(value)
		if (parsed !== null || props.nullable) model.value = parsed
	})

	// Only overwrite what the user typed when the value changed from outside (e.g. a reset or reload).
	watch(model, value => {
		if (fromText(text.value) !== (value ?? (props.nullable ? null : 0))) text.value = toText(value)
	})

	function normalize() {
		if (text.value.trim() === '' && !props.nullable) text.value = '0'
	}
</script>
