import { api } from './client'
import type { DiningTable } from '../types/orders'

export type TableCreateRequest = {
  name: string
  capacity: number
  isAvailable: boolean
}

export async function fetchTables() {
  const response = await api.get<DiningTable[]>('/api/tables')
  return response.data
}

export async function createTable(payload: TableCreateRequest) {
  const response = await api.post<DiningTable>('/api/tables', payload)
  return response.data
}

export async function updateTable(id: number, payload: TableCreateRequest) {
  await api.put(`/api/tables/${id}`, payload)
}
