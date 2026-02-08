import { api } from './client'

export type TopItem = {
  menuItemId: number
  name: string
  quantity: number
}

export type PaymentMethod = {
  method: string
  total: number
}

export type BusiestHour = {
  hour: number
  orders: number
}

export type DailyReport = {
  dateUtc: string
  ordersToday: number
  totalSales: number
  ingredientCost: number
  profit: number
  topItems: TopItem[]
  revenueByMethod: PaymentMethod[]
  busiestHour?: BusiestHour | null
}

export type DailyBreakdown = {
  dateUtc: string
  orders: number
  totalSales: number
  ingredientCost: number
  profit: number
}

export type MonthlyReport = {
  startUtc: string
  endUtc: string
  ordersTotal: number
  totalSales: number
  ingredientCost: number
  profit: number
  topItems: TopItem[]
  revenueByMethod: PaymentMethod[]
  dailyBreakdown: DailyBreakdown[]
}

export async function fetchDailyReport(dateUtc?: string) {
  const response = await api.get<DailyReport>('/api/reports/daily', {
    params: dateUtc ? { dateUtc } : undefined
  })
  return response.data
}

export async function exportDailyReport(dateUtc?: string) {
  const response = await api.get('/api/reports/daily/export', {
    params: dateUtc ? { dateUtc } : undefined,
    responseType: 'blob'
  })
  return response.data as Blob
}

export async function closeDay(dateUtc?: string) {
  const response = await api.post('/api/reports/close-day', {
    dateUtc: dateUtc ? new Date(dateUtc).toISOString() : null
  })
  return response.data
}

export async function fetchMonthlyReport(startUtc?: string, endUtc?: string) {
  const response = await api.get<MonthlyReport>('/api/reports/monthly', {
    params: startUtc && endUtc ? { startUtc, endUtc } : undefined
  })
  return response.data
}

export async function exportMonthlyReport(startUtc?: string, endUtc?: string) {
  const response = await api.get('/api/reports/monthly/export', {
    params: startUtc && endUtc ? { startUtc, endUtc } : undefined,
    responseType: 'blob'
  })
  return response.data as Blob
}
