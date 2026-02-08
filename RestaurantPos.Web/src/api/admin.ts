import { api } from './client'
import type { AppUser } from '../types/admin'

export async function fetchUsers() {
  const response = await api.get<AppUser[]>('/api/users')
  return response.data
}

export async function updateUserRole(userId: string, role: string) {
  await api.put(`/api/users/${userId}/role`, role, {
    headers: { 'Content-Type': 'application/json' }
  })
}

export async function resetUserPassword(userId: string, newPassword: string) {
  await api.post('/api/users/reset-password', {
    userId,
    newPassword
  })
}
