import { api } from './client'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types/api'

export async function login(payload: LoginRequest) {
  const response = await api.post<AuthResponse>('/api/auth/login', payload)
  return response.data
}

export async function register(payload: RegisterRequest) {
  const response = await api.post<AuthResponse>('/api/auth/register', payload)
  return response.data
}
