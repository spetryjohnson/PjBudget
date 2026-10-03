const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })
const wholeCurrency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

const plainAmount = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** A dollar amount without the "$", for dense tables whose headers already say what the columns are. */
export function amount(value: number | null | undefined): string {
	return value == null ? '' : plainAmount.format(value)
}

export function money(value: number | null | undefined): string {
	return value == null ? '' : currency.format(value)
}

export function wholeMoney(value: number | null | undefined): string {
	return value == null ? '' : wholeCurrency.format(value)
}

/** Formats a fraction (0.0275) as a percentage ("2.75%"). */
export function rate(value: number | null | undefined): string {
	if (value == null) return ''
	return `${Number((value * 100).toFixed(4))}%`
}

/**
 * Parses an API date ("2026-01-09") as a local calendar date. new Date("2026-01-09") would treat it as UTC midnight
 * and show the previous day in US time zones.
 */
export function parseDate(iso: string): Date {
	const [year = 1970, month = 1, day = 1] = iso.split('-').map(Number)
	return new Date(year, month - 1, day)
}

export function shortDate(iso: string | null | undefined): string {
	if (!iso) return ''
	return parseDate(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

export function longDate(iso: string | null | undefined): string {
	if (!iso) return ''
	return parseDate(iso).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' })
}

export function monthName(month: number): string {
	return new Date(2000, month - 1, 1).toLocaleDateString('en-US', { month: 'long' })
}
