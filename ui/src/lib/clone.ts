/**
 * Deep-copies plain API data. structuredClone can't be used here: it rejects Vue's reactive proxies, which is what
 * anything read from a ref or a Pinia store actually is.
 */
export function clone<T>(value: T): T {
	return JSON.parse(JSON.stringify(value)) as T
}
