import { HTTPError } from 'ky'

/**
 * A failed API call, normalized. The backend returns { statusCode, message, errors } for validation (400),
 * not-found (404) and conflict (409) responses; errors are keyed by camelCase property path.
 */
export interface ApiError {
	status: number
	message: string
	fieldErrors: Record<string, string[]>
}

export async function toApiError(error: unknown): Promise<ApiError> {
	if (error instanceof HTTPError) {
		let body: { message?: string; errors?: Record<string, string[]> | null } = {}
		try {
			body = await error.response.clone().json()
		} catch {
			// Not a JSON error body (e.g. a proxy error page); fall back to the status text.
		}

		return {
			status: error.response.status,
			message: body.message ?? error.response.statusText ?? 'The request failed.',
			fieldErrors: body.errors ?? {},
		}
	}

	return { status: 0, message: error instanceof Error ? error.message : String(error), fieldErrors: {} }
}

/** True for a request cancelled with an AbortController, which callers usually ignore. */
export function isAbort(error: unknown): boolean {
	return error instanceof DOMException && error.name === 'AbortError'
}

/** All messages as one list, with field messages first, for showing in a summary. */
export function allMessages(error: ApiError): string[] {
	const fieldMessages = Object.values(error.fieldErrors).flat()
	return fieldMessages.length > 0 ? fieldMessages : [error.message]
}
