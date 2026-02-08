import { api } from './client'

export type DashboardSummary = {
  openOrders: number
  kitchenQueue: number
  occupiedTables: number
  totalTables: number
  lowStockCount: number
}

export async function fetchDashboardSummary() {
  const response = await api.get<DashboardSummary>('/api/dashboard/summary')
  return response.data
}
