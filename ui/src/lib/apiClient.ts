/* ---------------------------------------
apiClient.ts

Thin wrapper around the "ky" HTTP library. Use this for all backend API calls so behaviors
like cookie-credentials, JSON content-type, and retry policy stay consistent across the app.
--------------------------------------------*/

import ky from 'ky';

export const apiClient = ky.create({
	prefixUrl: '/',

	// Send session cookies with same-origin calls
	credentials: 'include',

	timeout: 15000,
	retry: {
		limit: 2,
		methods: ['get', 'put', 'head', 'delete', 'options', 'trace'],
		statusCodes: [408, 413, 429, 502, 503, 504],
	},

	hooks: {
		beforeRequest: [
			async (request) => {
				if (!request.headers.has('Content-Type') && request.body) {
					request.headers.set('Content-Type', 'application/json');
				}
			}
		]
	}
});
