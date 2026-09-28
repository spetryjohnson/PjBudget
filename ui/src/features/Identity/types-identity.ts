/**
 * Identity-related type definitions
 */

export interface User {
	id: string
	email: string
	displayName: string
	roles: string[]
	authMethod: 'local' | null
}

export interface WhoAmIResponse {
	user: User | null
}
