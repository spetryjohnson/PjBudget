/**
 * Authentication related type definitions
 */
import type { User } from '../Identity/types-identity'

export interface LoginRequest {
	email: string
	password: string
	rememberMe: boolean
}

export interface LoginResponse {
	ok: boolean
	user: User
	mustChangePassword?: boolean
}

export interface ChangePasswordRequest {
	currentPassword: string
	newPassword: string
}

export interface ChangePasswordResponse {
	ok: boolean
	errors?: string[]
}
